using RimWorld;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class EleganceQualityPatch
    {
        public static void GenerateQuality_Postfix(Pawn pawn, ref QualityCategory __result)
        {
            if (__result < QualityCategory.Legendary && pawn?.story?.traits != null && pawn.story.traits.HasTrait(Pony_DefOf.Pony_Elegance))
            {
                QualityCategory before = __result;
                __result = (QualityCategory)((int)__result + 1);
            }
        }
    }
}