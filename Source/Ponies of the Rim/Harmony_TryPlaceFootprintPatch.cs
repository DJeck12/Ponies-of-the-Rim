using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace PoniesOfTheRim
{
    public static class HoofprintPatch
    {
        private const float LateralOffset       = 0.28f;
        private const float LongitudinalOffset   = 0.50f;
        private const float QuadrupedMinStepDist = LongitudinalOffset * 1.2f;

        private const float TrailOffsetMin = 0.03f;
        private const float TrailOffsetMax = 0.14f;

        private const float StaggerMin = 0.04f;
        private const float StaggerMax = 0.20f;

        private const float LateralJitter = 0.04f;

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

            bool isFront = FootprintCycleTracker.IsFrontAndAdvance(pawn);

            if (isFront)
            {
                float   stagger    = Rand.Range(StaggerMin, StaggerMax);
                Vector3 longOff    = dir * LongitudinalOffset * sizeScale;
                bool    rightLeads = ___lastFootprintRight;

                PlacePairPositions(drawPos, dir, sizeScale, longOff,
                    rightLeads, stagger, ___FootprintOffset,
                    out Vector3 posRight, out Vector3 posLeft);

                TryPlaceAt(posRight, pawn, rot, sizeScale, frontFleck);
                TryPlaceAt(posLeft,  pawn, rot, sizeScale, frontFleck);

                FootprintCycleTracker.SaveFrontPos(pawn, posLeft, posRight);
            }
            else
            {
                FootprintCycleTracker.GetFrontPos(pawn, out Vector3 savedLeft, out Vector3 savedRight);

                Vector3 trailBack  = dir * Rand.Range(TrailOffsetMin, TrailOffsetMax) * sizeScale;
                Vector3 perpDir    = dir.RotatedBy(90f);
                Vector3 lateralJit = perpDir * Rand.Range(-LateralJitter, LateralJitter) * sizeScale;

                float   stagger    = Rand.Range(StaggerMin, StaggerMax);
                Vector3 staggerVec = dir * stagger * sizeScale;
                bool rightLeads = !___lastFootprintRight;

                Vector3 posRight = savedRight - trailBack + lateralJit
                                 + (rightLeads ?  staggerVec : -staggerVec);
                Vector3 posLeft  = savedLeft  - trailBack + lateralJit
                                 + (rightLeads ? -staggerVec :  staggerVec);

                TryPlaceAt(posRight, pawn, rot, sizeScale, backFleck);
                TryPlaceAt(posLeft,  pawn, rot, sizeScale, backFleck);
            }

            ___lastFootprintPlacePos = drawPos;
            ___lastFootprintRight    = !___lastFootprintRight;
            return false;
        }

        private static void PlacePairPositions(
            Vector3 drawPos, Vector3 dir, float sizeScale,
            Vector3 longitudinal, bool rightLeads, float stagger,
            Vector3 footprintOffset,
            out Vector3 posRight, out Vector3 posLeft)
        {
            Vector3 lateralRight = dir.RotatedBy(90f)  * LateralOffset * sizeScale;
            Vector3 lateralLeft  = dir.RotatedBy(-90f) * LateralOffset * sizeScale;
            Vector3 staggerVec   = dir * stagger * sizeScale;
            Vector3 basePos      = drawPos + footprintOffset + longitudinal;

            posRight = basePos + lateralRight + (rightLeads ?  staggerVec : -staggerVec);
            posLeft  = basePos + lateralLeft  + (rightLeads ? -staggerVec :  staggerVec);
        }

        private static void GetFootprintDefs(Pawn pawn, out FleckDef front, out FleckDef back)
        {
            if (pawn.IsGriffon())
            {
                front = Pony_DefOf.Pony_Talonprint   ?? Pony_DefOf.Hoofprint;
                back  = Pony_DefOf.Pony_Pawprint ?? Pony_DefOf.Hoofprint;
            }
            else if (pawn.IsHippogriff())
            {
                front = Pony_DefOf.Pony_Talonprint ?? Pony_DefOf.Hoofprint;
                back  = Pony_DefOf.Hoofprint;
            }
            else
            {
                front = Pony_DefOf.Hoofprint;
                back  = Pony_DefOf.Hoofprint;
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