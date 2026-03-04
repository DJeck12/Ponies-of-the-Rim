using RimWorld;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class EleganceQualityPatch
    {
        public static void PostProcessProduct_Postfix(Thing product, Pawn worker)
        {
                        if (worker?.story?.traits == null)
                return;

            if (!worker.story.traits.HasTrait(Pony_DefOf.Pony_Elegance))
                return;

            CompQuality comp = product.TryGetComp<CompQuality>();
            if (comp == null)
                return;

            if (comp.Quality < QualityCategory.Legendary)
            {
                comp.SetQuality(
                    (QualityCategory)((int)comp.Quality + 1),
                    ArtGenerationContext.Colony
                );
            }
        }
    }
}