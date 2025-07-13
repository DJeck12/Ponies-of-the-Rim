using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class JumpUtility_CanHitTargetFrom_Patch
    {
        private static readonly Type patchType = typeof(JumpUtility_CanHitTargetFrom_Patch);
        static JumpUtility_CanHitTargetFrom_Patch()
        {
            Harmony harmonyInstance = new Harmony("Rimworld.Pony.PoniesOfTheRim");
            harmonyInstance.Patch(original: AccessTools.Method(typeof(JumpUtility), "CanHitTargetFrom"), prefix: new HarmonyMethod(patchType, "CanHitTargetFrom_Patch"));
        }
        public static bool CanHitTargetFrom_Patch(Pawn pawn, IntVec3 root, LocalTargetInfo targ, float range, ref bool __result)
        {
            if (pawn.verbTracker.AllVerbs.Any(v => v is Verb_ShortFlight))
            {
                float num = range * range;
                IntVec3 cell = targ.Cell;
                __result = (float)pawn.Position.DistanceToSquared(cell) <= num && cell.Walkable(pawn.Map);
                return false;
            }
            return true;
        }
    }
}
