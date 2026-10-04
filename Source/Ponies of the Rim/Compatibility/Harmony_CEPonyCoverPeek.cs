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
        private static bool _errorLogged;

        public static void Postfix(Thing thing, ref FloatRange heightRange, ref float shotHeight)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null || !PonyHelper.IsPonyRace(pawn.def))
            {
                return;
            }

            try
            {
                Apply(pawn, ref heightRange, ref shotHeight, false);
            }
            catch (Exception arg)
            {
                LogErrorOnce(arg);
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
            catch (Exception arg)
            {
                LogErrorOnce(arg);
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

            float coverTop;
            Thing cover = FindHighestBlockingCover(pawn, shotHeight, out coverTop);
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
                    Log.Message($"[PoniesOfTheRim] CE: {CoverLabel(cover)} высотой {coverTop:0.00} ({coverTop * MetersPerCellHeight:0.00} м) " +
                                $"выше досягаемости {pawn.def.defName}: ствол поднимается максимум до {maxShot:0.00} " +
                                $"({maxShot * MetersPerCellHeight:0.00} м). Стрелять вплотную из-за него пони не сможет. " +
                                "Сообщение выводится один раз на тип укрытия.");
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
                Log.Message($"[PoniesOfTheRim] CE: {pawn.def.defName}{(inFlight ? " (в полёте, тело на земле)" : "")} выглядывает из-за {CoverLabel(cover)} высотой {coverTop:0.00} " +
                            $"({coverTop * MetersPerCellHeight:0.00} м): ствол {oldShot:0.000} → {shotHeight:0.000}, " +
                            $"верх силуэта {oldTop:0.000} → {heightRange.max:0.000} ({heightRange.max * MetersPerCellHeight:0.00} м). " +
                            "Сообщение выводится один раз на тип укрытия.");
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

                Building_Door door = cover as Building_Door;
                if (door != null && door.Open)
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

        private static void LogErrorOnce(Exception arg)
        {
            if (_errorLogged)
            {
                return;
            }

            _errorLogged = true;
            Log.Warning($"[PoniesOfTheRim] CE CoverPeek: {arg}");
        }

        private static bool ShouldReport(HashSet<ThingDef> reported, ThingDef def)
        {
            return Prefs.DevMode && UnityData.IsInMainThread && reported.Add(def);
        }

        private static string CoverLabel(Thing cover)
        {
            return "«" + (cover.def.label ?? cover.def.defName) + "» (" + cover.def.defName + ")";
        }
    }
}