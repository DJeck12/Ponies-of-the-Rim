using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim.Genetics
{
    public static class Patch_PregnancyUtility_RacialGenes
    {
        public static void Postfix(List<GeneDef> __result, Pawn father, Pawn mother)
        {
            if (__result != null)
            {
                PonyRacialGeneUtility.Normalize(__result, mother, father);
            }
        }
    }
}