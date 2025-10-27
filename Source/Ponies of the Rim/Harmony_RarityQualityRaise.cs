using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class RarityQualityRaise
    {
        static RarityQualityRaise()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(GenRecipe), "PostProcessProduct"), null, new HarmonyMethod(typeof(RarityQualityRaise).GetMethod("QualityRaisePostfix")));
        }

    [HarmonyPostfix]
        public static void QualityRaisePostfix(Thing product, Pawn worker)
        {
            if (worker?.kindDef?.defName == "Pony_Rarity")
            {
                CompQuality comp = product.TryGetComp<CompQuality>();
                if (comp != null && comp.Quality < QualityCategory.Legendary)
                {
                    Log.Message(comp.Quality);
                    comp.SetQuality((QualityCategory)((int)comp.Quality + 1), ArtGenerationContext.Colony);
                    Log.Message(comp.Quality);
                }
            }
        }
    }
}