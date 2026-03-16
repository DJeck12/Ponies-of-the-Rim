using HarmonyLib;
using RimWorld;
using Verse;


namespace PoniesOfTheRim
{
    public static class ChildPonyBodyTypeFixPatch
    {
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