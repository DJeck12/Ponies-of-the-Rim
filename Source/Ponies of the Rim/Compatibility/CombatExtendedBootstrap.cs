using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    [StaticConstructorOnStartup]
    public static class CombatExtendedBootstrap
    {
        public static readonly Harmony Harmony;

        static CombatExtendedBootstrap()
        {
            Harmony = new Harmony("rimworld.poniesoftherim.combatextended");

            if (!CombatExtendedCompatability.Active)
            {
                Log.Message("[PoniesOfTheRim] Combat Extended не обнаружен — патч совместимости пропущен.");
                return;
            }

            Log.Message("[PoniesOfTheRim] CE Bootstrap: запуск.");

            CombatExtendedCompatability.BuildFlightExtensionCache();
            RegisterCollisionPatches();

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                CombatExtendedCompatability.EnsureRaceComps();
                CombatExtendedCompatability.AuditRaces();
                Log.Message("[PoniesOfTheRim] CE Bootstrap: завершён.");
            });
        }

        private static void RegisterCollisionPatches()
        {
            TryPatch(
                AccessTools.Method("CombatExtended.CE_Utility:GetCollisionBodyFactors"),
                null,
                new HarmonyMethod(typeof(Patch_CE_GetCollisionBodyFactors), "Postfix"),
                null,
                "CE_Utility.GetCollisionBodyFactors (габариты в полёте)");

            TryPatch(
                Patch_CE_CollisionVerticalLift.TargetMethod(),
                new HarmonyMethod(typeof(Patch_CE_CollisionVerticalLift), "Prefix"),
                new HarmonyMethod(typeof(Patch_CE_CollisionVerticalLift), "Postfix"),
                null,
                "CollisionVertical.CalculateHeightRange (подъём в полёте)",
                new HarmonyMethod(typeof(Patch_CE_CollisionVerticalLift), "Finalizer"));
        }

        private static void TryPatch(MethodInfo original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, string label = "", HarmonyMethod finalizer = null)
        {
            if (original == null)
            {
                Log.Error("[PoniesOfTheRim] CE Bootstrap: метод не найден — '" + label + "'.");
                return;
            }

            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
                Log.Message("[PoniesOfTheRim] CE Bootstrap: ✓ " + label);
            }
            catch (Exception arg)
            {
                Log.Error($"[PoniesOfTheRim] CE Bootstrap: ошибка патча '{label}':\n{arg}");
            }
        }
    }
}