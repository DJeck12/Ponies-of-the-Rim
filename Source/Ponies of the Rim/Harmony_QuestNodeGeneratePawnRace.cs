using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_QuestNode_GeneratePawn_RaceAware
    {
        private static FieldInfo _kindDefField;
        private static FieldInfo _factionField;

        private static readonly Dictionary<FactionDef, ThingDef> _raceCache =
            new Dictionary<FactionDef, ThingDef>();

        private static readonly string[] KnownKindSlateVars =
        {
            "lodgersPawnKind",
            "slavePawnKind",
            "prisonerPawnKind",
            "pawnKind",
        };

        public static void Register(Harmony harmony)
        {
            _kindDefField = AccessTools.Field(typeof(QuestNode_GeneratePawn), "kindDef");
            _factionField = AccessTools.Field(typeof(QuestNode_GeneratePawn), "faction");

            if (_kindDefField == null || _factionField == null)
            {
                Log.Error("[PoniesOfTheRim] Patch_QuestNode_GeneratePawn_RaceAware: " +
                          "Could not reflect 'kindDef' or 'faction' on QuestNode_GeneratePawn. " +
                          "Quest pawn race fix will not be applied.");
                return;
            }

            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(QuestNode_GeneratePawn), "RunInt"),
                    prefix: new HarmonyMethod(
                        typeof(Patch_QuestNode_GeneratePawn_RaceAware), nameof(Prefix)));
                Log.Message("[PoniesOfTheRim] Bootstrap: ✓ QuestNode_GeneratePawn.RunInt (race-aware)");
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Bootstrap: ошибка патча 'QuestNode_GeneratePawn.RunInt':\n{ex}");
            }
        }

        private static void Prefix(QuestNode_GeneratePawn __instance)
        {
            Slate slate = QuestGen.slate;
            if (slate == null) return;

            try
            {
                var kindDefRef = (SlateRef<PawnKindDef>)_kindDefField.GetValue(__instance);
                var factionRef = (SlateRef<Faction>)_factionField.GetValue(__instance);

                PawnKindDef kindDef = kindDefRef.GetValue(slate);
                Faction faction = factionRef.GetValue(slate);

                if (kindDef == null || faction == null) return;

                if (kindDef.race != ThingDefOf.Human) return;

                ThingDef factionPrimaryAlienRace = GetFactionPrimaryAlienRace(faction.def);
                if (factionPrimaryAlienRace == null || !PonyHelper.IsPonyRace(factionPrimaryAlienRace))
                {
                    return;
                }

                PawnKindDef replacement = FindBestAlienKind(faction, factionPrimaryAlienRace);
                if (replacement == null)
                {
                    Log.WarningOnce(
                        "[PoniesOfTheRim] Patch_QuestNode_GeneratePawn_RaceAware: No alien PawnKindDef found for faction '" +
                        faction.def.defName + "' (race: " + factionPrimaryAlienRace.defName +
                        "). Quest pawn will remain human.",
                        faction.def.defName.GetHashCode() ^ 0x51E571);
                    return;
                }

                bool overridden = false;
                foreach (string varName in KnownKindSlateVars)
                {
                    if (slate.TryGet(varName, out PawnKindDef existing) && existing == kindDef)
                    {
                        slate.Set(varName, replacement);
                        overridden = true;

                        if (Prefs.DevMode)
                        {
                            Log.Message("[PoniesOfTheRim] Quest pawn kind fixed: slate['" + varName + "'] " +
                                        kindDef.defName + " → " + replacement.defName +
                                        " (faction: " + faction.def.defName + ")");
                        }
                        break;
                    }
                }

                if (!overridden)
                {
                    Log.WarningOnce(
                        "[PoniesOfTheRim] Patch_QuestNode_GeneratePawn_RaceAware: Could not locate slate variable for kindDef '" +
                        kindDef.defName + "' in faction '" + faction.def.defName +
                        "'. Add the variable name to KnownKindSlateVars if this repeats.",
                        (faction.def.defName + "|" + kindDef.defName).GetHashCode());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"[PoniesOfTheRim] Patch_QuestNode_GeneratePawn_RaceAware.Prefix: {ex}");
            }
        }

        private static ThingDef GetFactionPrimaryAlienRace(FactionDef factionDef)
        {
            if (_raceCache.TryGetValue(factionDef, out ThingDef cached))
                return cached;

            ThingDef result = null;

            ThingDef basicRace = factionDef.basicMemberKind?.race;
            if (basicRace != null && basicRace != ThingDefOf.Human)
            {
                result = basicRace;
            }

            if (result == null && !factionDef.pawnGroupMakers.NullOrEmpty())
            {
                result = factionDef.pawnGroupMakers
                    .SelectMany(gm => gm.options ?? Enumerable.Empty<PawnGenOption>())
                    .Select(opt => opt?.kind?.race)
                    .Where(r => r != null && r != ThingDefOf.Human)
                    .GroupBy(r => r)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()
                    ?.Key;
            }

            _raceCache[factionDef] = result;
            return result;
        }

        private static PawnKindDef FindBestAlienKind(Faction faction, ThingDef alienRace)
        {
            if (faction.def.pawnGroupMakers.NullOrEmpty()) return null;

            var civilianGroups = new HashSet<PawnGroupKindDef>
            {
                PawnGroupKindDefOf.Peaceful,
                PawnGroupKindDefOf.Settlement,
                PawnGroupKindDefOf.Settlement_RangedOnly,
            };

            List<PawnKindDef> civilianKinds = faction.def.pawnGroupMakers
                .Where(gm => civilianGroups.Contains(gm.kindDef))
                .SelectMany(gm => gm.options ?? Enumerable.Empty<PawnGenOption>())
                .Select(opt => opt?.kind)
                .Where(k => k != null && k.race == alienRace)
                .Distinct()
                .ToList();

            if (civilianKinds.Count > 0)
                return civilianKinds.RandomElement();

            List<PawnKindDef> anyKinds = faction.def.pawnGroupMakers
                .SelectMany(gm => gm.options ?? Enumerable.Empty<PawnGenOption>())
                .Select(opt => opt?.kind)
                .Where(k => k != null && k.race == alienRace)
                .Distinct()
                .ToList();

            return anyKinds.Count > 0 ? anyKinds.RandomElement() : null;
        }
    }
}