using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class PerfectPonyMemoryPatch
    {
        static PerfectPonyMemoryPatch()
        {
            var harmony = new Harmony("Rimworld.Pony.PoniesOfTheRim");
            var target = AccessTools.Method(typeof(SkillRecord), "Interval");
            var postfix = new HarmonyMethod(typeof(PerfectPonyMemoryPatch).GetMethod(nameof(SkillDecayPostfix)));
            harmony.Patch(target, null, postfix);
        }

        [HarmonyPostfix]
        public static void SkillDecayPostfix(SkillRecord __instance)
        {
            var pawn = __instance.Pawn;
            if (pawn?.story?.traits?.HasTrait(Pony_DefOf.Pony_PerfectMemory) == true)
            {
                if (__instance.xpSinceLastLevel < 0f)
                    __instance.xpSinceLastLevel = 0f;

                if (__instance.Level < __instance.GetUnclampedLevel())
                    __instance.Level = __instance.GetUnclampedLevel();
            }
        }
    }
}
