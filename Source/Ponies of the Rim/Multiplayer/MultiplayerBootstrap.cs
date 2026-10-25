using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using PoniesOfTheRim.Abilities;
using PoniesOfTheRim.Flying;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Multiplayer
{
    [StaticConstructorOnStartup]
    public static class MultiplayerBootstrap
    {
        public const string HarmonyId = "PoniesOfTheRim.Multiplayer";

        private static readonly Harmony Harmony = new(HarmonyId);
        public static bool Active { get; private set; }

        private static int _patched;
        private static int _failed;

        static MultiplayerBootstrap()
        {
            if (!MultiplayerCompat.Loaded)
                return;

            Active = true;

            RunStage(RegisterSyncMethods);
            RunStage(PatchStylingStationDummyPawn);
            if (Prefs.DevMode)
            {
                RunStage(PatchAbilityIdAudit);
            }

            PonyLog.Trace($"MP: {MultiplayerCompat.Registered.Count} sync methods, {_patched} patched, {_failed} failed");
        }

        private static void RegisterSyncMethods()
        {
            TrySync(typeof(CompPegasusFlightToggle), nameof(CompPegasusFlightToggle.SetFlightEnabledSynced));
            TrySync(typeof(Verb_ShortFlight), nameof(Verb_ShortFlight.StartShortFlightJobSynced));
        }

        private static void TrySync(Type type, string methodName)
        {
            if (!MultiplayerCompat.RegisterSyncMethod(type, methodName))
            {
                _failed++;
            }
        }

        private static void PatchStylingStationDummyPawn()
        {
            TryPatch(
                AccessTools.Constructor(typeof(Dialog_StylingStation), new[] { typeof(Pawn), typeof(Thing) }),
                prefix: new HarmonyMethod(typeof(Patch_StylingDialogDummyPawn_Genes), nameof(Patch_StylingDialogDummyPawn_Genes.Prefix)));
            TryPatch(
                AccessTools.Method(typeof(ExtendedGraphicsPawnWrapper), nameof(ExtendedGraphicsPawnWrapper.HasGene)),
                prefix: new HarmonyMethod(typeof(Patch_ExtendedGraphicsPawnWrapper_HasGene), nameof(Patch_ExtendedGraphicsPawnWrapper_HasGene.Prefix)));
        }

        private static void PatchAbilityIdAudit()
        {
            TryPatch(
                ResolveGainAbility(),
                postfix: new HarmonyMethod(typeof(Patch_AbilityGrantAudit), nameof(Patch_AbilityGrantAudit.Postfix)));
        }

        private static MethodInfo ResolveGainAbility()
        {
            return AccessTools.GetDeclaredMethods(typeof(Pawn_AbilityTracker))
                .Where(m => m.Name == "GainAbility")
                .Where(m =>
                {
                    ParameterInfo[] ps = m.GetParameters();
                    return ps.Length >= 1 && ps[0].ParameterType == typeof(AbilityDef);
                })
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault();
        }

        private static void RunStage(Action stage)
        {
            try
            {
                stage();
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"MP: {stage.Method.Name} aborted\n{ex}");
            }
        }

        private static void TryPatch(
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null)
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"MP: target of {Describe(prefix ?? postfix ?? transpiler)} not found, skipped");
                return;
            }
            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler);
                _patched++;
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"MP: {Describe(prefix ?? postfix ?? transpiler)} failed on {original.DeclaringType?.Name}.{original.Name}\n{ex}");
            }
        }

        private static string Describe(HarmonyMethod patch)
        {
            MethodInfo method = patch?.method;
            return method == null ? "?" : method.DeclaringType?.Name + "." + method.Name;
        }
    }

    public static class Patch_AbilityGrantAudit
    {
        private static readonly HashSet<string> Reported = new();

        public static void Postfix(Pawn_AbilityTracker __instance, AbilityDef def)
        {
            try
            {
                if (def == null || !MultiplayerCompat.InMultiplayer)
                    return;

                List<Ability> all = __instance?.abilities;
                if (all == null)
                    return;

                for (int i = 0; i < all.Count; i++)
                {
                    Ability ability = all[i];
                    if (ability?.def != def || ability.Id >= 0)
                        continue;

                    string key = $"{def.defName}|{__instance.pawn?.thingIDNumber ?? 0}";
                    if (!Reported.Add(key))
                        return;

                    PonyLog.Warn(
                        $"MP: {def.defName} granted to {__instance.pawn?.LabelShortCap ?? "?"} outside a synced context " +
                        $"(Id {ability.Id}, InInterface {MultiplayerCompat.InInterface}), will desync\n{Environment.StackTrace}");
                    return;
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("MP: ability grant audit failed", ex);
            }
        }
    }
}