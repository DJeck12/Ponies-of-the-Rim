using HarmonyLib;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class PonyFoodGeneRemovalPatch
    {
        static PonyFoodGeneRemovalPatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), [typeof(PawnGenerationRequest)]), null, new HarmonyMethod(typeof(PonyFoodGeneRemovalPatch).GetMethod("PonyFoodGeneRemovalForGeneratePawn")));
        }

        [HarmonyPostfix]
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