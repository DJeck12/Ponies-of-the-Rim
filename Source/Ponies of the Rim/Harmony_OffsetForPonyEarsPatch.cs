using System.Collections.Generic;
using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class OffsetForPonyEarsPatch
    {
        public static void OffsetForPonyEars(ref Vector3 __result, PawnRenderNode node, PawnDrawParms parms)
        {
            if (!(node.Props is AlienPawnRenderNodeProperties_BodyAddon bodyAddonProps))
            {
                return;
            }
            AlienPartGenerator.BodyAddon addon = bodyAddonProps.addon;
            if (addon == null || addon.Name != "Pony_Left_Ear")
            {
                return;
            }
            Pawn pawn = parms.pawn;
            if (pawn == null || !pawn.IsPony() || parms.facing != Rot4.South)
            {
                return;
            }

            bool headgear = WearsHeadgear(pawn);
            float num = (!headgear && !pawn.IsKirin()) ? (-0.278f) : (-0.268f);
            __result.y = (addon.inFrontOfBody ? 0.3f : (-0.3f)) + num;
        }

        private static bool WearsHeadgear(Pawn pawn)
        {
            List<Apparel> worn = pawn.apparel?.WornApparel;
            if (worn == null)
            {
                return false;
            }
            for (int i = 0; i < worn.Count; i++)
            {
                List<BodyPartGroupDef> groups = worn[i].def?.apparel?.bodyPartGroups;
                if (groups == null)
                {
                    continue;
                }
                for (int j = 0; j < groups.Count; j++)
                {
                    BodyPartGroupDef g = groups[j];
                    if (g == BodyPartGroupDefOf.FullHead || g == BodyPartGroupDefOf.UpperHead)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}