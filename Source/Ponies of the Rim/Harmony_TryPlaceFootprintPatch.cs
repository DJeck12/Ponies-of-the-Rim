using RimWorld;
using Verse;
using UnityEngine;

namespace PoniesOfTheRim
{
    public static class HoofprintPatch
    {
        private const float LateralOffset       = 0.28f;
        private const float LongitudinalOffset   = 0.50f;
        private const float QuadrupedMinStepDist = LongitudinalOffset * 2f;
        private const float TrailOffsetMin = 0.02f;
        private const float TrailOffsetMax = 0.12f;
        private const float LateralJitter = 0.04f;
        private const float StalePositionThreshold = LongitudinalOffset * 6f;

        public static bool TryPlaceHoofprint(
            ref Pawn    ___pawn,
            ref Vector3 ___lastFootprintPlacePos,
            ref bool    ___lastFootprintRight,
            ref Vector3 ___FootprintOffset)
        {
            Pawn pawn = ___pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return false;
            if (!pawn.IsPony())
                return true;
            Vector3 drawPos   = pawn.Drawer.DrawPos;
            Vector3 dir       = (drawPos - ___lastFootprintPlacePos).normalized;
            float   rot       = dir.AngleFlat();
            float   sizeScale = Mathf.Sqrt(pawn.BodySize);
            float distSq = (drawPos - ___lastFootprintPlacePos).sqrMagnitude;
            if (distSq < QuadrupedMinStepDist * QuadrupedMinStepDist)
                return false;
            GetFootprintDefs(pawn, out FleckDef frontFleck, out FleckDef backFleck);
            bool rightFrontLeads = FootprintCycleTracker.IsRightFrontAndAdvance(pawn);

            Vector3 perpDir = dir.RotatedBy(90f);
            Vector3 lateralSide = perpDir * LateralOffset * sizeScale;
            Vector3 frontPos = drawPos + ___FootprintOffset
                             + dir * LongitudinalOffset * sizeScale
                             + (rightFrontLeads ? lateralSide : -lateralSide);

            TryPlaceAt(frontPos, pawn, rot, sizeScale, frontFleck);
            if (rightFrontLeads)
                FootprintCycleTracker.SaveFrontRight(pawn, frontPos);
            else
                FootprintCycleTracker.SaveFrontLeft(pawn, frontPos);
            bool hasSaved = rightFrontLeads
                ? FootprintCycleTracker.TryGetFrontLeft(pawn,  out Vector3 savedOpposite)
                : FootprintCycleTracker.TryGetFrontRight(pawn, out savedOpposite);

            if (hasSaved)
            {
                float ageSq = (drawPos - savedOpposite).sqrMagnitude;
                if (ageSq < StalePositionThreshold * StalePositionThreshold)
                {
                    Vector3 trailBack = dir * Rand.Range(TrailOffsetMin, TrailOffsetMax) * sizeScale;
                    Vector3 latJit    = perpDir * Rand.Range(-LateralJitter, LateralJitter) * sizeScale;
                    Vector3 backPos   = savedOpposite - trailBack + latJit;

                    TryPlaceAt(backPos, pawn, rot, sizeScale, backFleck);
                }
            }

            ___lastFootprintPlacePos = drawPos;
            ___lastFootprintRight    = !___lastFootprintRight;
            return false;
        }

        private static void GetFootprintDefs(Pawn pawn, out FleckDef front, out FleckDef back)
        {
            if (pawn.IsGriffon())
            {
                front = Pony_DefOf.Pony_Talonprint   ?? Pony_DefOf.Pony_Hoofprint;
                back  = Pony_DefOf.Pony_Pawprint ?? Pony_DefOf.Pony_Hoofprint;
            }
            else if (pawn.IsHippogriff())
            {
                front = Pony_DefOf.Pony_Talonprint ?? Pony_DefOf.Pony_Hoofprint;
                back  = Pony_DefOf.Pony_Hoofprint;
            }
            else
            {
                front = Pony_DefOf.Pony_Hoofprint;
                back  = Pony_DefOf.Pony_Hoofprint;
            }
        }

        private static void TryPlaceAt(
            Vector3 pos, Pawn pawn, float rot, float sizeScale, FleckDef fleckDef)
        {
            IntVec3 cell = pos.ToIntVec3();
            if (!cell.InBounds(pawn.Map)) return;

            TerrainDef terrain = cell.GetTerrain(pawn.Map);
            if (terrain == null) return;

            if (terrain.takeSplashes)
                FleckMaker.WaterSplash(pos, pawn.Map, sizeScale * 2f, 1.5f);

            if (pawn.RaceProps.makesFootprints
                && terrain.takeFootprints
                && pawn.Map.snowGrid.GetDepth(pawn.Position) >= 0.4f)
            {
                PonyHelper.PlaceFootprintFleck(pos, pawn.Map, rot, fleckDef);
            }
        }
    }
}