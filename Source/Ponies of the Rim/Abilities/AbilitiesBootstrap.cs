using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    [StaticConstructorOnStartup]
    public static class AbilitiesBootstrap
    {
        private const string HarmonyId = "PoniesOfTheRim.Abilities";

        private static readonly Harmony Harmony = new Harmony(HarmonyId);

        private static int _patched;
        private static int _failed;

        static AbilitiesBootstrap()
        {
            try
            {
                AbilityToggleEnforcer.BuildGrantToRacesCache();
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Abilities: grant cache not built\n{ex}");
            }

            TryPatch(AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
                postfix: new HarmonyMethod(typeof(AbilityToggleEnforcer), nameof(AbilityToggleEnforcer.SpawnSetup_Postfix)));
            TryPatch(AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)),
                postfix: new HarmonyMethod(typeof(AbilityToggleEnforcer), nameof(AbilityToggleEnforcer.FinalizeInit_Postfix)));

            PonyLog.Trace($"Abilities: {_patched} patched, {_failed} failed");
        }

        private static void TryPatch(
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null)
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"Abilities: target of {Describe(prefix ?? postfix)} not found, skipped");
                return;
            }
            try
            {
                Harmony.Patch(original, prefix, postfix);
                _patched++;
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Abilities: {Describe(prefix ?? postfix)} failed on {original.DeclaringType?.Name}.{original.Name}\n{ex}");
            }
        }

        private static string Describe(HarmonyMethod patch)
        {
            MethodInfo method = patch?.method;
            return method == null ? "?" : method.DeclaringType?.Name + "." + method.Name;
        }
    }
}