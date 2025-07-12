using HarmonyLib;
using RimWorld;
using Verse;


namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class ChildPonyBodyTypeFixPatch
    {
        static ChildPonyBodyTypeFixPatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(Pawn_StoryTracker), "ExposeData"), null, new HarmonyMethod(typeof(ChildPonyBodyTypeFixPatch).GetMethod("ChildBodyTypeFixPatch")));
        }

        [HarmonyPostfix]
        public static void ChildBodyTypeFixPatch(ref Pawn_StoryTracker __instance, ref Pawn ___pawn, BodyTypeDef ___bodyType)
        {
			if (___pawn.genes == null || ___pawn.DevelopmentalStage != DevelopmentalStage.Child)
			{
				return;
			}
            if (ModsConfig.BiotechActive && ___pawn.DevelopmentalStage.Child() && ___pawn.IsPony())
            {
                __instance.bodyType = Pony_DefOf.PonyChild;
            }
        }
    }
}