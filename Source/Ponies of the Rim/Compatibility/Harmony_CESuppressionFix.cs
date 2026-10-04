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
            if (!(__instance?.parent is Pawn pawn) || pawn.Spawned)
                return true;

            if (!_traced && PonyLog.Verbose)
            {
                _traced = true;
                TraceFirstSkip(pawn);
            }
            return false;
        }
        private static void TraceFirstSkip(Pawn pawn)
        {
            try
            {
                PonyLog.Trace($"CE: подавление у {pawn.LabelShortCap} не тикает, пока пешки нет на карте " +
                              "(полёт, переноска, караван) — иначе CE падает с NullReferenceException. " +
                              "Сообщение выводится один раз.");
            }
            catch
            {
            }
        }
    }
}