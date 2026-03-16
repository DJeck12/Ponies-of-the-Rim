using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueEquipmentAssigner
    {
        public static void GeneratePawn_Postfix(Pawn __result, PawnGenerationRequest request)
        {
            try
            {
                if (__result == null) return;

                var faction = request.Faction ?? __result.Faction;
                if (faction == null || faction.IsPlayer) return;

                if (!UniquePawnConfig.ByKindDef.ContainsKey(__result.kindDef.defName))
                    return;

                if (request.Context == PawnGenerationContext.PlayerStarter)
                    return;

                EquipForFaction(__result, faction);
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] UniqueEquipmentAssigner error: {ex.Message}");
            }
        }

        private static void EquipForFaction(Pawn pawn, Faction faction)
        {
            var templateKind = FindCombatKindForRace(faction.def, pawn.def);
            if (templateKind == null) return;

            var originalKind = pawn.kindDef;

            try
            {
                var savedApparel = new List<Apparel>();
                if (pawn.apparel != null)
                {
                    foreach (var ap in pawn.apparel.WornApparel.ToList())
                    {
                        pawn.apparel.Remove(ap);
                        savedApparel.Add(ap);
                    }
                }

                pawn.kindDef = templateKind;

                var equipReq = new PawnGenerationRequest(
                    templateKind,
                    faction,
                    PawnGenerationContext.NonPlayer
                );

                PawnApparelGenerator.GenerateStartingApparelFor(pawn, equipReq);
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, equipReq);
                PawnInventoryGenerator.GenerateInventoryFor(pawn, equipReq);

                foreach (var saved in savedApparel)
                {
                    bool conflicts = false;

                    if (pawn.apparel != null)
                    {
                        foreach (var worn in pawn.apparel.WornApparel)
                        {
                            if (!ApparelUtility.CanWearTogether(saved.def, worn.def, pawn.RaceProps.body))
                            {
                                conflicts = true;
                                break;
                            }
                        }
                    }

                    if (!conflicts && pawn.apparel != null && ApparelUtility.HasPartsToWear(pawn, saved.def))
                    {
                        pawn.apparel.Wear(saved, dropReplacedApparel: false);
                    }
                    else
                    {
                        pawn.inventory?.innerContainer?.TryAdd(saved);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] Failed to equip {pawn.LabelShort}: {ex.Message}");
            }
            finally
            {
                pawn.kindDef = originalKind;
            }
        }

        private static PawnKindDef FindCombatKindForRace(FactionDef factionDef, ThingDef race)
        {
            if (factionDef.pawnGroupMakers == null)
                return null;

            var combatCandidates = new List<PawnGenOption>();
            var fallbackCandidates = new List<PawnGenOption>();

            foreach (var pgm in factionDef.pawnGroupMakers)
            {
                if (pgm?.options == null) continue;

                var matching = pgm.options
                    .Where(o => o?.kind?.race == race)
                    .ToList();

                if (pgm.kindDef == PawnGroupKindDefOf.Combat)
                    combatCandidates.AddRange(matching);
                else
                    fallbackCandidates.AddRange(matching);
            }

            if (combatCandidates.Count > 0)
                return combatCandidates
                    .RandomElementByWeight(o => o.selectionWeight)
                    .kind;

            if (fallbackCandidates.Count > 0)
                return fallbackCandidates
                    .RandomElementByWeight(o => o.selectionWeight)
                    .kind;

            return null;
        }
    }
}