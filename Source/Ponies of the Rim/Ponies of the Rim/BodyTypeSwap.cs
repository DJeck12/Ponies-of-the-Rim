using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class BodyTypeSwap
    {
        private static readonly Type patchType;
        static BodyTypeSwap()
        {
            patchType = typeof(BodyTypeSwap);
            Harmony harmony = new Harmony("Rimworld.Pony.PoniesOfTheRim");
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateBodyType"), null, new HarmonyMethod(patchType, "GenerateBodyTypePostfix"));
        }
        [HarmonyPostfix]
        public static void GenerateBodyTypePostfix(Pawn __instance)
        {
            if (__instance.def.defName.Contains("Pony_"))
            {
                if (__instance.ageTracker.CurLifeStage.defName == "PonyAdult" || __instance.ageTracker.CurLifeStage.defName == "PonyTeenager")
                {
                    __instance.story.bodyType = Pony_DefOf.Pony;
                }
                if (__instance.ageTracker.CurLifeStage.defName == "PonyPreTeenager" || __instance.ageTracker.CurLifeStage.defName == "PonyChild")
                {
                    __instance.story.bodyType = Pony_DefOf.PonyChild;
                }
                if (__instance.ageTracker.CurLifeStage.defName == "PonyBaby")
                {
                    __instance.story.bodyType = Pony_DefOf.PonyBaby;
                }
            }
        }
    }
}
