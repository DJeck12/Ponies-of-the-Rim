using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class BodyTypePatch
    {
        static BodyTypePatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(PawnGenerator), "GetBodyTypeFor"), null, new HarmonyMethod(typeof(BodyTypePatch).GetMethod("GenerateBodyTypePatch")));
        }
        [HarmonyPostfix]
        public static void GenerateBodyTypePatch(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.def.defName.Contains("Pony_"))
            {

                if (ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile())
                {
                    if (pawn.DevelopmentalStage == DevelopmentalStage.Baby)
                    {
                        __result = Pony_DefOf.PonyBaby;
                    }
                    __result = Pony_DefOf.PonyChild;
                }
            }
        }
    }
}









