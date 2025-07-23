using AlienRace;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class OffsetForPonyEarsPatch
    {
        static OffsetForPonyEarsPatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(AlienPawnRenderNodeWorker_BodyAddon), "OffsetFor"), null, new HarmonyMethod(typeof(OffsetForPonyEarsPatch).GetMethod("OffsetForPonyEars")));
        }

        [HarmonyPostfix]
        public static void OffsetForPonyEars(ref Vector3 __result, PawnRenderNode node, PawnDrawParms parms)
            {
            if (node.Props is AlienPawnRenderNodeProperties_BodyAddon props)
            {
                AlienPartGenerator.BodyAddon addon = props.addon;
                Pawn pawn = parms.pawn;
                if (pawn.IsPony() && parms.facing == Rot4.South)
                {
                    bool wearingHeadgear = pawn.apparel.WornApparel.Any(a => a.def.apparel.bodyPartGroups.Any(bpg => bpg == BodyPartGroupDefOf.FullHead || bpg == BodyPartGroupDefOf.UpperHead));
                    float desiredLayerOffset;
                    if (addon.Name == "Left ear")
                    {
                        desiredLayerOffset = wearingHeadgear ? -0.268f : -0.278f;
                    }
                    else
                    {
                        return;
                    }
                    __result.y = (addon.inFrontOfBody ? 0.3f : -0.3f) + desiredLayerOffset;
                }
            }
        }
    }
}
