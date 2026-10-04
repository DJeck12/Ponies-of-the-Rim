using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PoniesOfTheRim
{

    public static class Patch_QuestNode_GeneratePawn_RaceAware
    {
        private static readonly Dictionary<FactionDef, ThingDef> _raceCache =
            new Dictionary<FactionDef, ThingDef>();

        private static readonly string[] KnownKindSlateVars =
        {
            "lodgersPawnKind",
            "slavePawnKind",
            "prisonerPawnKind",
            "pawnKind",
        };

        public static void Prefix(QuestNode_GeneratePawn __instance)
        {
            Slate slate = QuestGen.slate;
            if (slate == null)
            {
                return;
            }
            try
            {
                PawnKindDef kindDef = __instance.kindDef.GetValue(slate);
                Faction faction = __instance.faction.GetValue(slate);
                if (kindDef == null || faction == null || kindDef.race != ThingDefOf.Human)
                {
                    return;
                }

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
                        "Квесты: для фракции '" + faction.def.defName + "' не найден PawnKindDef расы "
                        + factionPrimaryAlienRace.defName + " — квестовая пешка останется человеком.");
                    return;
                }

                for (int i = 0; i < KnownKindSlateVars.Length; i++)
                {
                    string varName = KnownKindSlateVars[i];
                    if (slate.TryGet(varName, out PawnKindDef existing) && existing == kindDef)
                    {
                        slate.Set(varName, replacement);
                        PonyLog.Trace("Квесты: вид квестовой пешки заменён: slate['" + varName + "'] "
                            + kindDef.defName + " → " + replacement.defName + " (фракция " + faction.def.defName + ").");
                        return;
                    }
                }

                PonyLog.WarnOnce(
                    "QuestPawnRace.NoSlateVar|" + faction.def.defName + "|" + kindDef.defName,
                    "Квесты: не найдена slate-переменная для kindDef '" + kindDef.defName + "' во фракции '"
                    + faction.def.defName + "'. Если повторяется — добавьте имя переменной в KnownKindSlateVars.");
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