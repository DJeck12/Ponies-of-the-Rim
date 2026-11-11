using RimWorld;

namespace PoniesOfTheRim.UniquePonies
{
    public static class PerfectMemoryPatch
    {
        public static bool Interval_Prefix(SkillRecord __instance)
        {
            TraitSet traits = __instance.Pawn?.story?.traits;
            return traits == null || !traits.HasTrait(Pony_DefOf.Pony_PerfectMemory);
        }
    }
}