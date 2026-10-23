using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class Patch_CE_PonyCoverPeek
    {
        private const float MuzzleBelowTopFactor = 0.14999998f;
        private const float PeekMargin = 0.01f;
        private const float MetersPerCellHeight = 1.75f;
        public const float MaxMuzzleHeightFactor = 1.3f;

        private static readonly HashSet<ThingDef> ReportedRaise = new HashSet<ThingDef>();
        private static readonly HashSet<ThingDef> ReportedRaiseInFlight = new HashSet<ThingDef>();
        private static readonly HashSet<ThingDef> ReportedTooHigh = new HashSet<ThingDef>();

        public static void Postfix(Thing thing, ref FloatRange heightRange, ref float shotHeight)
        {
            if (!(thing is Pawn pawn) || !PonyHelper.IsPonyRace(pawn.def))
            {
                return;
            }

            try
            {
                Apply(pawn, ref heightRange, ref shotHeight, false);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("CE: cover peek failed", ex);
            }
        }

        public static void ApplyGrounded(Pawn pawn, ref FloatRange heightRange, ref float shotHeight)
        {
            if (pawn == null || !PonyHelper.IsPonyRace(pawn.def))
            {
                return;
            }

            try
            {
                Apply(pawn, ref heightRange, ref shotHeight, true);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("CE: cover peek in flight failed", ex);
            }
        }

        private static void Apply(Pawn pawn, ref FloatRange heightRange, ref float shotHeight, bool inFlight)
        {
            if (!pawn.Spawned || pawn.Downed || pawn.GetPosture() != PawnPosture.Standing)
            {
                return;
            }

            float muzzleBelowTop = heightRange.max - shotHeight;
            if (muzzleBelowTop <= 0f)
            {
                return;
            }

            Thing cover = FindHighestBlockingCover(pawn, shotHeight, out float coverTop);
            if (cover == null)
            {
                return;
            }

            if (!inFlight && (pawn.Flying || Patch_CE_CollisionVerticalLift.IsGroundedForCalculation(pawn)))
            {
                return;
            }

            float bodyHeight = muzzleBelowTop / MuzzleBelowTopFactor;
            float desiredShot = coverTop + PeekMargin;
            float maxShot = heightRange.min + bodyHeight * MaxMuzzleHeightFactor;

            if (desiredShot > maxShot)
            {
                if (ShouldReport(ReportedTooHigh, cover.def))
                {
                    PonyLog.Trace($"CE: {cover.def.defName} ({Height(coverTop)}) is too high for {pawn.def.defName}, muzzle reaches {Height(maxShot)}");
                }
                return;
            }

            float oldShot = shotHeight;
            float oldTop = heightRange.max;
            float delta = desiredShot - shotHeight;

            heightRange.max += delta;
            shotHeight = desiredShot;

            if (ShouldReport(inFlight ? ReportedRaiseInFlight : ReportedRaise, cover.def))
            {
                PonyLog.Trace($"CE: {pawn.def.defName}{(inFlight ? " in flight" : "")} peeks over {cover.def.defName} ({Height(coverTop)}), " +
                              $"Muzzle {oldShot:0.000} -> {shotHeight:0.000}, top {oldTop:0.000} -> {heightRange.max:0.000}");
            }
        }

        private static Thing FindHighestBlockingCover(Pawn pawn, float shotHeight, out float coverTop)
        {
            coverTop = 0f;
            Thing best = null;

            Map map = pawn.Map;
            if (map == null)
            {
                return null;
            }

            IntVec3 position = pawn.Position;
            IntVec3[] offsets = GenAdj.AdjacentCells;
            for (int i = 0; i < offsets.Length; i++)
            {
                IntVec3 cell = position + offsets[i];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Thing cover = cell.GetCover(map);
                if (cover == null || cover.def.Fillage != FillCategory.Partial || cover.def.category == ThingCategory.Plant)
                {
                    continue;
                }

                if (cover is Building_Door door && door.Open)
                {
                    continue;
                }

                float top = cover.def.fillPercent;
                if (top < shotHeight || (best != null && top <= coverTop))
                {
                    continue;
                }

                best = cover;
                coverTop = top;
            }

            return best;
        }

        private static bool ShouldReport(HashSet<ThingDef> reported, ThingDef def)
        {
            return Prefs.DevMode && UnityData.IsInMainThread && reported.Add(def);
        }

        private static string Height(float cells)
        {
            return $"{cells:0.00}, {cells * MetersPerCellHeight:0.00} m";
        }
    }
}