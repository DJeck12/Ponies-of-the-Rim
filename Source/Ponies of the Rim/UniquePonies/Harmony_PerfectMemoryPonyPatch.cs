using RimWorld;

namespace PoniesOfTheRim.UniquePonies
{
    public static class PerfectMemoryPatch
    {
        public static void Interval_Postfix(SkillRecord __instance)
        {
            var pawn = __instance.Pawn;
            if (pawn?.story?.traits == null)
                return;

            if (!pawn.story.traits.HasTrait(Pony_DefOf.Pony_PerfectMemory))
                return;

                                    if (__instance.xpSinceLastLevel < 0f)
                __instance.xpSinceLastLevel = 0f;
        }
    }
}