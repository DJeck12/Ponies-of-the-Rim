using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class PonyRawFoodFix
    {

        static PonyRawFoodFix()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(Thing), "Ingested"), null, new HarmonyMethod(typeof(PonyRawFoodFix).GetMethod("FoodFix")));
        }


        [HarmonyPostfix]
        public static void FoodFix(Pawn ingester, float nutritionWanted, ThingWithComps __instance)
        {
            if (ingester.IsPony() && __instance.def.IsMeat)
            {
                ThoughtDef PonyAteMeatThought = DefDatabase<ThoughtDef>.GetNamed("Pony_AteMeat");
                if (PonyAteMeatThought != null)
                {
                    ingester.needs.mood.thoughts.memories.TryGainMemory(PonyAteMeatThought);
                }
            }
        }
    }
}
