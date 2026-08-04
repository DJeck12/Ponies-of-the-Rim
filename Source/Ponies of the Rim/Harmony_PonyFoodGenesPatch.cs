using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyFoodGenesPatch
    {
        public static void IngestedPonyFoodGenesPatch(Thing __instance, Pawn ingester)
        {
            if (!ModsConfig.BiotechActive) return;

            if (!__instance.def.IsNutritionGivingIngestible || __instance.def.IsDrug) return;

            if (__instance.def?.defName == "Pemmican") return;

            var settings = LoadedModManager.GetMod<PoniesOfTheRimSettings>().GetSettings<PoniesOfTheRimSettingsData>();

            if (!settings.enableFoodDebuffs || ingester?.genes == null)
            {
                return;
            }

            bool hasMeat = HasMeatOrCorpse(__instance);

            if (ingester.genes.HasActiveGene(Pony_DefOf.Pony_Herbivore) && hasMeat)
            {
                ingester.needs?.mood?.thoughts?.memories?.TryGainMemory(Pony_DefOf.Pony_AteMeatAsHerbivore);
            }

            bool hasPlant = HasPlant(__instance);

            if (ingester.genes.HasActiveGene(Pony_DefOf.Pony_Carnivore) && hasPlant)
            {
                ingester.needs?.mood?.thoughts?.memories?.TryGainMemory(Pony_DefOf.Pony_AtePlantAsCarnivore);
            }
        }

        private static bool HasMeatOrCorpse(Thing thing)
        {
            return !FoodUtility.AcceptableVegetarian(thing);
        }

        private static bool HasPlant(Thing thing)
        {
            return !FoodUtility.AcceptableCarnivore(thing);
        }
    }
}