using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class Patch_CE_SuppressionOffMap
    {
        private static bool _traced;

        public static MethodInfo TargetMethod()
        {
            Type comp = AccessTools.TypeByName("CombatExtended.CompSuppressable");
            if (comp == null)
                return null;

            return AccessTools.DeclaredMethod(comp, "CompTickInterval", new[] { typeof(int) })
                   ?? AccessTools.DeclaredMethod(comp, "CompTick", Type.EmptyTypes);
        }

        public static bool Prefix(ThingComp __instance)
        {
            if (!(__instance?.parent is Pawn pawn) || pawn.Spawned || !pawn.IsPony())
                return true;

            if (!_traced && PonyLog.Verbose)
            {
                _traced = true;
                PonyLog.Trace($"CE: suppression does not tick for {pawn.LabelShortCap} while off map");
            }
            return false;
        }
    }
}