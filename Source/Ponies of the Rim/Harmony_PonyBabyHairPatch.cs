using HarmonyLib;
using PoniesOfTheRim;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

[HarmonyPatch(typeof(PawnRenderNode_Hair), "GraphicFor")]
public static class PonyBabyHairPatch
{
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
