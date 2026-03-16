using HarmonyLib;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyFoodGeneRemovalPatch
    {
        public static void PonyFoodGeneRemovalForGeneratePawn(Pawn __result)
        {
            if (!ModsConfig.BiotechActive || __result == null || __result.genes == null)
            {
                return;
            }

            var settings = LoadedModManager.GetMod<PoniesOfTheRimSettings>().GetSettings<PoniesOfTheRimSettingsData>();

            if (!settings.enableFoodGenes && __result.IsPony())
            {
                Gene herbivoreGene = __result.genes.GetGene(Pony_DefOf.Pony_Herbivore);
                if (herbivoreGene != null)
                {
                    __result.genes.RemoveGene(herbivoreGene);
                }

                Gene carnivoreGene = __result.genes.GetGene(Pony_DefOf.Pony_Carnivore);
                if (carnivoreGene != null)
                {
                    __result.genes.RemoveGene(carnivoreGene);
                }
            }
        }
    }
}