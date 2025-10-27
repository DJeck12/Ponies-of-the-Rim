using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class PonyBabyHairPatch
    {
        static PonyBabyHairPatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(PawnRenderNode_Hair), "GraphicFor"), null, new HarmonyMethod(typeof(PonyBabyHairPatch).GetMethod("PonyBabyHairPostfix")));
        }

        [HarmonyPostfix]
        public static void PonyBabyHairPostfix(Pawn pawn, ref Graphic __result)
        {
            if ((pawn.DevelopmentalStage == DevelopmentalStage.Baby || pawn.DevelopmentalStage == DevelopmentalStage.Newborn)
                && pawn.IsPony())
            {
                List<HairDef> ponyHairs = DefDatabase<HairDef>.AllDefsListForReading.Where(h => h.defName != null && h.defName.StartsWith("Pony_")).ToList();
                if (ponyHairs.Count > 0)
                {
                    HairDef chosen = ponyHairs.RandomElement();
                    __result = chosen.GraphicFor(pawn, pawn.story.HairColor);
                }
            }

        }
    }
}