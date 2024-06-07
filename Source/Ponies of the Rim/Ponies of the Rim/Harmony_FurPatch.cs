using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class FurTypePatch
    {
        static FurTypePatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(FurDef), "GetFurBodyGraphicPath"), null, new HarmonyMethod(typeof(FurTypePatch).GetMethod("FurPatch")));
        }

        [HarmonyPostfix]
        public static void FurPatch(Pawn pawn, ref string __result, FurDef __instance)
        {
            if (pawn.def.defName.Contains("Pony_"))
            {
                __result = "Things/Races/Pony/Bodies/Furskin/Naked_Pony";
            }
            else
            {
                for (int i = 0; i < __instance.bodyTypeGraphicPaths.Count; i++)
                {
                    if (__instance.bodyTypeGraphicPaths[i].bodyType == pawn.story.bodyType)
                    {
                        __result = __instance.bodyTypeGraphicPaths[i].texturePath;
                    }
                }
            }
        }
    }
}    









