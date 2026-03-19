using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyBabyHairPatch
    {
        public static void PonyBabyHairPostfix(Pawn pawn, ref Graphic __result)
        {
            if (pawn.DevelopmentalStage != DevelopmentalStage.Baby
                && pawn.DevelopmentalStage != DevelopmentalStage.Newborn)
                return;

            if (!pawn.IsPony())
                return;

            if (pawn.story?.hairDef != null
                && pawn.story.hairDef.defName.StartsWith("Pony_"))
            {
                __result = pawn.story.hairDef.GraphicFor(pawn, pawn.story.HairColor);
                return;
            }

            List<HairDef> ponyHairs = DefDatabase<HairDef>.AllDefsListForReading
                .Where(h => h.defName != null && h.defName.StartsWith("Pony_"))
                .ToList();

            if (ponyHairs.Count == 0)
                return;

            HairDef chosen = ponyHairs.RandomElement();

            if (pawn.story != null)
                pawn.story.hairDef = chosen;

            __result = chosen.GraphicFor(pawn, pawn.story?.HairColor ?? UnityEngine.Color.white);
        }
    }
}