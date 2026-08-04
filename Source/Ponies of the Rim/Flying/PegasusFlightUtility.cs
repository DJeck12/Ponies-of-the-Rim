using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class PegasusFlightUtility
    {
        public const string LeftWingPartDef = "Pony_LeftWing";

        public const string RightWingPartDef = "Pony_RightWing";

        public static readonly HashSet<string> WingHediffDefs = new HashSet<string> { "Pony_NaturalWing", "Pony_SimpleProstheticWing", "Pony_BionicWing", "Pony_ArchotechWing" };

        public static WingHediffExtension GetWingExtension(HediffDef def)
        {
            return def?.GetModExtension<WingHediffExtension>();
        }

        public static bool IsWingHediff(Hediff h)
        {
            if (h?.def == null)
            {
                return false;
            }
            if (WingHediffDefs.Contains(h.def.defName))
            {
                return true;
            }
            if (GetWingExtension(h.def) != null)
            {
                return true;
            }
            return h is Hediff_AddedPart;
        }

        public static Hediff FindWingHediff(HediffSet diffSet, BodyPartRecord part)
        {
            if (diffSet?.hediffs == null || part == null)
            {
                return null;
            }
            List<Hediff> hediffs = diffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].Part == part && IsWingHediff(hediffs[i]))
                {
                    return hediffs[i];
                }
            }
            return null;
        }

        public static float GetWingEfficiency(HediffDef def)
        {
            return GetWingExtension(def)?.efficiency ?? (def?.defName switch
            {
                "Pony_NaturalWing" => 0.5f,
                "Pony_SimpleProstheticWing" => 0.35f,
                "Pony_BionicWing" => 0.625f,
                "Pony_ArchotechWing" => 0.75f,
                _ => 0.5f,
            });
        }

        public static string GetWingTypeLabel(HediffDef def)
        {
            WingHediffExtension wingExtension = GetWingExtension(def);
            if (!string.IsNullOrEmpty(wingExtension?.typeLabel))
            {
                return wingExtension.typeLabel;
            }
            return def?.defName switch
            {
                "Pony_NaturalWing" => "Natural",
                "Pony_SimpleProstheticWing" => "Prosthetic",
                "Pony_BionicWing" => "Bionic",
                "Pony_ArchotechWing" => "Archotech",
                _ => (def != null) ? def.LabelCap.ToString() : "Natural",
            };
        }
        public static bool HasUsableWings(Pawn pawn)
        {
            return PonyFlightCache.HasUsableWingsFast(pawn);
        }

        public static bool IsPegasusConstantFlight(Pawn p)
        {
            return PonyFlightCache.IsPegasusConstantFlight(p);
        }

        public static bool CanFlyToCell(Pawn pawn, IntVec3 c, Map map)
        {
            if (!IsPegasusConstantFlight(pawn))
            {
                return false;
            }
            if (c.Roofed(map))
            {
                return false;
            }
            if (c.Fogged(map))
            {
                return false;
            }
            if (!c.Walkable(map) && IsImpassableMountain(c, map))
            {
                return false;
            }
            return true;
        }

        public static bool IsImpassableMountain(IntVec3 c, Map map)
        {
            Building edifice = c.GetEdifice(map);
            if (edifice != null && edifice.def?.building != null && edifice.def.building.isNaturalRock)
            {
                return true;
            }
            TerrainDef terrain = c.GetTerrain(map);
            if (terrain != null && terrain.passability == Traversability.Impassable)
            {
                return true;
            }
            return false;
        }

        public static void SafeLand(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Position.Walkable(pawn.Map))
            {
                return;
            }
            Map map = pawn.Map;
            IntVec3 position = IntVec3.Invalid;
            int num = -1;
            for (int i = 1; i <= 15; i++)
            {
                foreach (IntVec3 item in GenRadial.RadialCellsAround(pawn.Position, i, i == 1))
                {
                    if (item.InBounds(map) && item.Walkable(map))
                    {
                        int num2 = 0;
                        if (!item.Fogged(map))
                        {
                            num2 += 2;
                        }
                        if (!item.Roofed(map))
                        {
                            num2++;
                        }
                        if (num2 > num)
                        {
                            num = num2;
                            position = item;
                        }
                    }
                }
                if (position.IsValid)
                {
                    break;
                }
            }
            IntVec3 result;
            if (position.IsValid)
            {
                pawn.Position = position;
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
            }
            else if (CellFinder.TryFindRandomCellNear(pawn.Position, map, 30, (IntVec3 c) => c.Walkable(map) && !c.Fogged(map), out result))
            {
                pawn.Position = result;
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
            }
        }
    }
}