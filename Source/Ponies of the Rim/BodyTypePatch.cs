using AlienRace;
using HarmonyLib;
using RimWorld;
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
            if (pawn.IsPony())
            {
                if (ModsConfig.BiotechActive)
                {
                    if (pawn.DevelopmentalStage.Baby() || pawn.DevelopmentalStage.Newborn())
                    {
                        __result = Pony_DefOf.PonyBaby;
                    }
                    if (pawn.DevelopmentalStage.Child())
                    {
                        __result = Pony_DefOf.PonyChild;
                    }
                }
                if (pawn.DevelopmentalStage.Adult())
                {
                    __result = Pony_DefOf.Pony;
                }
            }
        }
    }
}