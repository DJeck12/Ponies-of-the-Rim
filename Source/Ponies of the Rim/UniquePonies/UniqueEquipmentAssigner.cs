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
                if (__result != null)
                {
                    Faction faction = request.Faction ?? __result.Faction;
                    if (faction != null && !faction.IsPlayer && UniquePawnConfig.ByKindDef.ContainsKey(__result.kindDef.defName) && request.Context != PawnGenerationContext.PlayerStarter)
                    {
                        EquipForFaction(__result, faction);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[PoniesOfTheRim] UniqueEquipmentAssigner error: " + ex.Message);
            }
        }

        private static void EquipForFaction(Pawn pawn, Faction faction)
        {
            PawnKindDef pawnKindDef = FindCombatKindForRace(faction.def, pawn.def);
            if (pawnKindDef == null)
            {
                return;
            }

            PawnKindDef kindDef = pawn.kindDef;
            try
            {
                List<Apparel> list = new List<Apparel>();
                if (pawn.apparel != null)
                {
                    foreach (Apparel item in pawn.apparel.WornApparel.ToList())
                    {
                        pawn.apparel.Remove(item);
                        list.Add(item);
                    }
                }
                pawn.kindDef = pawnKindDef;
                PawnGenerationRequest request = new PawnGenerationRequest(pawnKindDef, faction);
                PawnApparelGenerator.GenerateStartingApparelFor(pawn, request);
                PawnInventoryGenerator.GenerateInventoryFor(pawn, request);
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, request);

                foreach (Apparel item2 in list)
                {
                    bool flag = false;
                    if (pawn.apparel != null)
                    {
                        foreach (Apparel item3 in pawn.apparel.WornApparel)
                        {
                            if (!ApparelUtility.CanWearTogether(item2.def, item3.def, pawn.RaceProps.body))
                            {
                                flag = true;
                                break;
                            }
                        }
                    }

                    if (!flag && pawn.apparel != null && ApparelUtility.HasPartsToWear(pawn, item2.def))
                    {
                        pawn.apparel.Wear(item2, dropReplacedApparel: false);
                    }
                    else
                    {
                        pawn.inventory?.innerContainer?.TryAdd(item2);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[PoniesOfTheRim] Failed to equip " + pawn.LabelShort + ": " + ex.Message);
            }
            finally
            {
                PawnKindDef combatKind = pawn.kindDef;
                pawn.kindDef = kindDef;
                CombatExtendedCompatability.TryUpdateInventory(pawn);
                CombatExtendedLoadoutDiagnostics.Report(pawn, combatKind, "итог");
            }
        }

        private static PawnKindDef FindCombatKindForRace(FactionDef factionDef, ThingDef race)
        {
            if (factionDef.pawnGroupMakers == null)
            {
                return null;
            }

            List<PawnGenOption> list = new List<PawnGenOption>();
            List<PawnGenOption> list2 = new List<PawnGenOption>();
            foreach (PawnGroupMaker pawnGroupMaker in factionDef.pawnGroupMakers)
            {
                if (pawnGroupMaker?.options != null)
                {
                    List<PawnGenOption> collection = pawnGroupMaker.options.Where((PawnGenOption o) => o?.kind?.race == race).ToList();
                    if (pawnGroupMaker.kindDef == PawnGroupKindDefOf.Combat)
                    {
                        list.AddRange(collection);
                    }
                    else
                    {
                        list2.AddRange(collection);
                    }
                }
            }

            if (list.Count > 0)
            {
                return list.RandomElementByWeight((PawnGenOption o) => o.selectionWeight).kind;
            }

            if (list2.Count > 0)
            {
                return list2.RandomElementByWeight((PawnGenOption o) => o.selectionWeight).kind;
            }

            return null;
        }
    }
}