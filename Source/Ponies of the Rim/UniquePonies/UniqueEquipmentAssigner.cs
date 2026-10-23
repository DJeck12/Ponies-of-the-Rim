using System;
using System.Collections.Generic;
using System.Linq;
using PoniesOfTheRim.Compatibility;
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
                if (__result == null)
                    return;

                Faction faction = request.Faction ?? __result.Faction;
                if (faction != null && !faction.IsPlayer
                    && UniquePawnConfig.ByKindDef.ContainsKey(__result.kindDef.defName)
                    && request.Context != PawnGenerationContext.PlayerStarter)
                {
                    EquipForFaction(__result, faction);
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Unique: equipment not assigned", ex);
            }
        }

        private static void EquipForFaction(Pawn pawn, Faction faction)
        {
            PawnKindDef combatKind = FindCombatKindForRace(faction.def, pawn.def);
            if (combatKind == null)
            {
                return;
            }

            PawnKindDef originalKind = pawn.kindDef;
            try
            {
                List<Apparel> oldApparel = new List<Apparel>();
                if (pawn.apparel != null)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel.ToList())
                    {
                        pawn.apparel.Remove(apparel);
                        oldApparel.Add(apparel);
                    }
                }
                pawn.kindDef = combatKind;
                PawnGenerationRequest request = new PawnGenerationRequest(combatKind, faction);
                PawnApparelGenerator.GenerateStartingApparelFor(pawn, request);
                PawnInventoryGenerator.GenerateInventoryFor(pawn, request);
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, request);

                foreach (Apparel old in oldApparel)
                {
                    bool conflicts = false;
                    if (pawn.apparel != null)
                    {
                        foreach (Apparel worn in pawn.apparel.WornApparel)
                        {
                            if (!ApparelUtility.CanWearTogether(old.def, worn.def, pawn.RaceProps.body))
                            {
                                conflicts = true;
                                break;
                            }
                        }
                    }

                    if (!conflicts && pawn.apparel != null && ApparelUtility.HasPartsToWear(pawn, old.def))
                    {
                        pawn.apparel.Wear(old, dropReplacedApparel: false);
                    }
                    else
                    {
                        pawn.inventory?.innerContainer?.TryAdd(old);
                    }
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught($"Unique: could not equip {pawn.LabelShort}", ex);
            }
            finally
            {
                PawnKindDef usedKind = pawn.kindDef;
                pawn.kindDef = originalKind;
                CombatExtendedCompatability.TryUpdateInventory(pawn);
                CombatExtendedLoadoutDiagnostics.Report(pawn, usedKind, "Unique equipment");
            }
        }

        private static PawnKindDef FindCombatKindForRace(FactionDef factionDef, ThingDef race)
        {
            if (factionDef.pawnGroupMakers == null)
            {
                return null;
            }

            List<PawnGenOption> combatOptions = new List<PawnGenOption>();
            List<PawnGenOption> otherOptions = new List<PawnGenOption>();
            foreach (PawnGroupMaker groupMaker in factionDef.pawnGroupMakers)
            {
                if (groupMaker?.options == null)
                {
                    continue;
                }

                List<PawnGenOption> raceOptions = groupMaker.options.Where(o => o?.kind?.race == race).ToList();
                if (groupMaker.kindDef == PawnGroupKindDefOf.Combat)
                {
                    combatOptions.AddRange(raceOptions);
                }
                else
                {
                    otherOptions.AddRange(raceOptions);
                }
            }

            if (combatOptions.Count > 0)
            {
                return combatOptions.RandomElementByWeight(o => o.selectionWeight).kind;
            }

            if (otherOptions.Count > 0)
            {
                return otherOptions.RandomElementByWeight(o => o.selectionWeight).kind;
            }

            return null;
        }
    }
}