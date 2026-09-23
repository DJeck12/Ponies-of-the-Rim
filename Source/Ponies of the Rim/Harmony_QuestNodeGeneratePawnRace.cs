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
                PonyLog.Error("Patch_QuestNode_GeneratePawn_RaceAware: не удалось получить поля 'kindDef' или " +
                              "'faction' у QuestNode_GeneratePawn. Исправление расы квестовых пешек не применено.");
                return;
            }

            MethodInfo runInt = AccessTools.Method(typeof(QuestNode_GeneratePawn), "RunInt");
            if (runInt == null)
            {
                PonyLog.Error("Bootstrap: метод не найден — 'QuestNode_GeneratePawn.RunInt'.");
                return;
            }

            try
            {
                harmony.Patch(
                    runInt,
                    prefix: new HarmonyMethod(
                        typeof(Patch_QuestNode_GeneratePawn_RaceAware), nameof(Prefix)));
                PonyLog.Trace("Bootstrap: ✓ QuestNode_GeneratePawn.RunInt (race-aware)");
            }
            catch (Exception ex)
            {
                PonyLog.Error($"Bootstrap: ошибка патча 'QuestNode_GeneratePawn.RunInt':\n{ex}");
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
                    PonyLog.WarnOnce(
                        "QuestPawnRace.NoAlienKind|" + faction.def.defName,
                        "Patch_QuestNode_GeneratePawn_RaceAware: для фракции '" + faction.def.defName +
                        "' не найден PawnKindDef расы " + factionPrimaryAlienRace.defName +
                        ". Квестовая пешка останется человеком.");
                    return;
                }

                bool overridden = false;
                foreach (string varName in KnownKindSlateVars)
                {
                    if (slate.TryGet(varName, out PawnKindDef existing) && existing == kindDef)
                    {
                        slate.Set(varName, replacement);
                        overridden = true;

                        PonyLog.Trace("Quest pawn kind fixed: slate['" + varName + "'] " +
                                      kindDef.defName + " → " + replacement.defName +
                                      " (faction: " + faction.def.defName + ")");
                        break;
                    }
                }

                if (!overridden)
                {
                    PonyLog.WarnOnce(
                        "QuestPawnRace.NoSlateVar|" + faction.def.defName + "|" + kindDef.defName,
                        "Patch_QuestNode_GeneratePawn_RaceAware: не найдена slate-переменная для kindDef '" +
                        kindDef.defName + "' во фракции '" + faction.def.defName +
                        "'. Если повторяется — добавьте имя переменной в KnownKindSlateVars.");
                }
            }
            catch (Exception ex)
            {
                PonyLog.ErrorCaught("Квесты: не удалось подобрать расу для квестовой пешки.", ex);
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