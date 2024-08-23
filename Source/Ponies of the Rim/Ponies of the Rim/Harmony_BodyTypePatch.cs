using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class PonyBodyTypePatch
    {
        static PonyBodyTypePatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(HarmonyPatches), "CheckBodyType"), null, new HarmonyMethod(typeof(PonyBodyTypePatch).GetMethod("BodyTypePatch")));
        }

        [HarmonyPostfix]
        public static void BodyTypePatch(Pawn pawn, ref BodyTypeDef __result)
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









