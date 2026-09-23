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

        private static int _patched;
        private static int _failed;

        static CombatExtendedBootstrap()
        {
            Harmony = new Harmony("rimworld.poniesoftherim.combatextended");

            if (!CombatExtendedCompatability.Active)
            {
                PonyLog.Trace("Combat Extended не обнаружен — патч совместимости пропущен.");
                return;
            }

            PonyLog.Trace("CE Bootstrap: запуск.");

            CombatExtendedCompatability.BuildFlightExtensionCache();
            RegisterCollisionPatches();

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                CombatExtendedCompatability.EnsureRaceComps();

                if (Prefs.DevMode)
                {
                    CombatExtendedCompatability.AuditRaces();
                }

                PonyLog.Trace($"CE Bootstrap: завершён, патчей установлено {_patched}, ошибок {_failed}.");
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
                _failed++;
                PonyLog.Error("CE Bootstrap: метод не найден — '" + label + "'.");
                return;
            }

            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
                _patched++;
            }
            catch (Exception arg)
            {
                _failed++;
                PonyLog.Error($"CE Bootstrap: ошибка патча '{label}':\n{arg}");
            }
        }
    }
}