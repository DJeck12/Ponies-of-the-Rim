using System.Collections.Generic;
using PoniesOfTheRim.Multiplayer;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class PegasusFlightUtility
    {
        public const string LeftWingPartDef = "Pony_LeftWing";
        public const string RightWingPartDef = "Pony_RightWing";

        public static readonly HashSet<string> WingHediffDefs = new HashSet<string>
        {
            "Pony_NaturalWing",
            "Pony_SimpleProstheticWing",
            "Pony_BionicWing",
            "Pony_ArchotechWing",
        };

        public static WingHediffExtension GetWingExtension(HediffDef def)
        {
            return def?.GetModExtension<WingHediffExtension>();
        }

        public static bool IsWingHediff(Hediff h)
        {
            if (h?.def == null) return false;
            if (WingHediffDefs.Contains(h.def.defName)) return true;
            if (GetWingExtension(h.def) != null) return true;
            return h is Hediff_AddedPart;
        }

        public static Hediff FindWingHediff(HediffSet diffSet, BodyPartRecord part)
        {
            if (diffSet?.hediffs == null || part == null) return null;

            List<Hediff> hediffs = diffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].Part == part && IsWingHediff(hediffs[i]))
                    return hediffs[i];
            }
            return null;
        }

        public static float GetWingEfficiency(HediffDef def)
        {
            WingHediffExtension ext = GetWingExtension(def);
            if (ext != null) return ext.efficiency;

            switch (def?.defName)
            {
                case "Pony_NaturalWing": return 0.5f;
                case "Pony_SimpleProstheticWing": return 0.35f;
                case "Pony_BionicWing": return 0.625f;
                case "Pony_ArchotechWing": return 0.75f;
                default: return 0.5f;
            }
        }

        public static string GetWingTypeLabel(HediffDef def)
        {
            WingHediffExtension ext = GetWingExtension(def);
            if (!string.IsNullOrEmpty(ext?.typeLabel)) return ext.typeLabel;

            switch (def?.defName)
            {
                case "Pony_NaturalWing": return "Natural";
                case "Pony_SimpleProstheticWing": return "Prosthetic";
                case "Pony_BionicWing": return "Bionic";
                case "Pony_ArchotechWing": return "Archotech";
                default: return def != null ? def.LabelCap.ToString() : "Natural";
            }
        }

        public static bool HasUsableWings(Pawn pawn)
        {
            return PonyFlightCache.HasUsableWingsFast(pawn);
        }

        public static bool IsPegasusConstantFlight(Pawn p)
        {
            return PonyFlightCache.IsPegasusConstantFlight(p);
        }

        public static bool CanFlyOverObstacles(Pawn pawn)
        {
            PoniesOfTheRimSettingsData settings = PoniesOfTheRimSettings.settings;
            if (settings == null || pawn == null) return true;

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return settings.pegasusFlyOverObstaclesAI;
            return settings.pegasusFlyOverObstaclesPlayer;
        }

        public static bool IsFlyingOverObstacles(Pawn p)
        {
            return IsPegasusConstantFlight(p) && CanFlyOverObstacles(p);
        }

        public static float FlightCellsForStamina(Pawn pawn, CompPegasusFlightTimer timer, float share)
        {
            if (pawn == null || timer == null) return 0f;

            float flightSpeedFactor = pawn.flight != null ? pawn.RaceProps.flightSpeedFactor : 1f;
            return FlightCellsForStamina(share, timer.MaxFlightDurationTicks, timer.WingDrainMultiplier,
                pawn.TicksPerMoveCardinal, flightSpeedFactor);
        }

        internal static float FlightCellsForStamina(float share, int maxFlightTicks, float drainMultiplier,
            float ticksPerMoveCardinal, float flightSpeedFactor)
        {
            if (share <= 0f || maxFlightTicks <= 0) return 0f;
            if (drainMultiplier <= 0f) return float.MaxValue;

            float flightTicks = share * maxFlightTicks / drainMultiplier;
            float speedFactor = flightSpeedFactor > 0f ? flightSpeedFactor : 1f;
            float ticksPerCell = Mathf.Max(1f, ticksPerMoveCardinal / speedFactor);
            return flightTicks / ticksPerCell;
        }

        public static float FlightRangeCells(Pawn pawn, CompPegasusFlightTimer timer)
        {
            if (timer == null) return 0f;
            return FlightCellsForStamina(pawn, timer, timer.CurrentStaminaPercent - 0.05f);
        }

        public static void LandPegasiOverObstacles()
        {
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null || MultiplayerCompat.InMultiplayer)
                return;

            int landed = 0;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];
                    if (pawn == null || !pawn.Spawned || pawn.Map != map) continue;

                    CompPegasusFlightToggle toggle = PonyFlightCache.GetToggle(pawn);
                    if (toggle != null && toggle.FlightEnabled && !CanFlyOverObstacles(pawn) && !pawn.Position.Walkable(map))
                    {
                        SafeLand(pawn);
                        landed++;
                    }
                }
            }

            if (landed > 0)
                PonyLog.Trace($"Полёт: перелёт через препятствия выключен — посажено пегасов, зависших над постройками: {landed}.");
        }

        public static bool CanFlyToCell(Pawn pawn, IntVec3 c, Map map)
        {
            if (!IsPegasusConstantFlight(pawn)) return false;
            if (c.Roofed(map)) return false;
            if (c.Fogged(map)) return false;
            if (!c.Walkable(map) && IsImpassableMountain(c, map)) return false;
            return true;
        }

        public static bool IsImpassableMountain(IntVec3 c, Map map)
        {
            Building edifice = c.GetEdifice(map);
            if (edifice?.def?.building != null && edifice.def.building.isNaturalRock) return true;

            TerrainDef terrain = c.GetTerrain(map);
            return terrain != null && terrain.passability == Traversability.Impassable;
        }

        public static void SafeLand(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Position.Walkable(pawn.Map))
                return;

            Map map = pawn.Map;
            IntVec3 best = IntVec3.Invalid;
            int bestScore = -1;
            for (int radius = 1; radius <= 15; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, radius == 1))
                {
                    if (!cell.InBounds(map) || !cell.Walkable(map)) continue;

                    int score = 0;
                    if (!cell.Fogged(map)) score += 2;
                    if (!cell.Roofed(map)) score++;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = cell;
                    }
                }
                if (best.IsValid) break;
            }

            if (best.IsValid)
            {
                pawn.Position = best;
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
            }
            else if (CellFinder.TryFindRandomCellNear(pawn.Position, map, 30,
                         c => c.Walkable(map) && !c.Fogged(map), out IntVec3 fallback))
            {
                pawn.Position = fallback;
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
            }
        }
    }
}