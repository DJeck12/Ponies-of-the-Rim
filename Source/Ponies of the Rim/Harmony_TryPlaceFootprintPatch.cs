using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System;


namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class HoofprintPatch
    {
        private static readonly Type patchType = typeof(HoofprintPatch);
        static HoofprintPatch()
        {
            Harmony harmonyInstance = new Harmony("Rimworld.Pony.PoniesOfTheRim");
            harmonyInstance.Patch(original: AccessTools.Method(typeof(PawnFootprintMaker), "TryPlaceFootprint"), prefix: new HarmonyMethod(patchType, "TryPlaceHoofprint"));
        }

        public static bool TryPlaceHoofprint(ref Pawn ___pawn, ref Vector3 ___lastFootprintPlacePos, ref bool ___lastFootprintRight, ref Vector3 ___FootprintOffset)
        {
            Vector3 drawPos = ___pawn.Drawer.DrawPos;
            Vector3 normalized = (drawPos - ___lastFootprintPlacePos).normalized;
            float rot = normalized.AngleFlat();
            float angle = (___lastFootprintRight ? 90 : (-90));
            Vector3 vector = normalized.RotatedBy(angle) * 0.17f * Mathf.Sqrt(___pawn.BodySize);
            Vector3 vector2 = drawPos + ___FootprintOffset + vector;
            IntVec3 c = vector2.ToIntVec3();
            if (c.InBounds(___pawn.Map))
            {
                TerrainDef terrain = c.GetTerrain(___pawn.Map);
                if (terrain != null)
                {
                    if (terrain.takeSplashes)
                    {
                        FleckMaker.WaterSplash(vector2, ___pawn.Map, Mathf.Sqrt(___pawn.BodySize) * 2f, 1.5f);
                    }
                    if (___pawn.IsPony())
                    {
                        if (___pawn.RaceProps.makesFootprints && terrain.takeFootprints && ___pawn.Map.snowGrid.GetDepth(___pawn.Position) >= 0.4f)
                        {
                            PonyHelper.PlaceHoofprint(vector2, ___pawn.Map, rot);
                        }
                    }
                    else
                    {
                        if (___pawn.RaceProps.makesFootprints && terrain.takeFootprints && ___pawn.Map.snowGrid.GetDepth(___pawn.Position) >= 0.4f)
                        {
                            FleckMaker.PlaceFootprint(vector2, ___pawn.Map, rot);
                        }
                    }
                    
                }
            }
            ___lastFootprintPlacePos = drawPos;
            ___lastFootprintRight = !___lastFootprintRight;
            return false;
        }
    }
}









