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

        static MultiplayerBootstrap()
        {
            if (!MultiplayerCompat.Loaded)
                return;

            Active = true;
            Log.Message("[PoniesOfTheRim] Multiplayer: обнаружен — устанавливаю патч совместимости.");

            RunStage(RegisterSyncMethods, "sync-методы");
            RunStage(PatchStylingStationDummyPawn, "стайлинг-станция (дубль пешки)");
            RunStage(PatchAbilityIdAudit, "аудит выдачи способностей");

            Log.Message($"[PoniesOfTheRim] Multiplayer: патч совместимости готов, sync-методов — {MultiplayerCompat.Registered.Count}.");
        }

        private static void RegisterSyncMethods()
        {
            TrySync(typeof(CompPegasusFlightToggle), nameof(CompPegasusFlightToggle.SetFlightEnabledSynced));
            TrySync(typeof(Verb_ShortFlight), nameof(Verb_ShortFlight.StartShortFlightJobSynced));
        }

        private static void TrySync(Type type, string methodName)
        {
            if (MultiplayerCompat.RegisterSyncMethod(type, methodName))
                Log.Message($"[PoniesOfTheRim] Multiplayer: ✓ sync {type.Name}.{methodName}");
            else
                Log.Warning($"[PoniesOfTheRim] Multiplayer: ✗ sync {type.Name}.{methodName} — метод не зарегистрирован.");
        }

        private static void PatchStylingStationDummyPawn()
        {
            TryPatch(
                AccessTools.Constructor(typeof(Dialog_StylingStation), new Type[2] { typeof(Pawn), typeof(Thing) }),
                prefix: new HarmonyMethod(typeof(Patch_StylingDialogDummyPawn_Genes), nameof(Patch_StylingDialogDummyPawn_Genes.Prefix)),
                label: "Dialog_StylingStation..ctor (гены дубля)");

            TryPatch(
                AccessTools.Method(typeof(ExtendedGraphicsPawnWrapper), nameof(ExtendedGraphicsPawnWrapper.HasGene)),
                prefix: new HarmonyMethod(typeof(Patch_ExtendedGraphicsPawnWrapper_HasGene), nameof(Patch_ExtendedGraphicsPawnWrapper_HasGene.Prefix)),
                label: "ExtendedGraphicsPawnWrapper.HasGene (нулгард)");
        }

        private static void PatchAbilityIdAudit()
        {
            TryPatch(
                ResolveGainAbility(),
                postfix: new HarmonyMethod(typeof(Patch_AbilityGrantAudit), nameof(Patch_AbilityGrantAudit.Postfix)),
                label: "Pawn_AbilityTracker.GainAbility (аудит Id)");
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

        private static void RunStage(Action stage, string label)
        {
            try
            {
                stage();
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Multiplayer: этап '{label}' завершился с ошибкой:\n{ex}");
            }
        }

        private static void TryPatch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null, string label = "")
        {
            if (original == null)
            {
                Log.Error($"[PoniesOfTheRim] Multiplayer: метод не найден — '{label}'.");
                return;
            }
            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler);
                Log.Message($"[PoniesOfTheRim] Multiplayer: ✓ {label}");
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Multiplayer: ошибка патча '{label}':\n{ex}");
            }
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

                    Log.Warning(
                        $"[PoniesOfTheRim] Multiplayer: способность '{def.defName}' выдана пешке " +
                        $"'{__instance.pawn?.LabelShortCap ?? "?"}' вне синхронизированного контекста " +
                        $"(Id = {ability.Id}, InInterface = {MultiplayerCompat.InInterface}). " +
                        $"Такой Id локален и приведёт к рассинхрону. Стек вызова:\n{Environment.StackTrace}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] Multiplayer: Patch_AbilityGrantAudit.Postfix: {ex.Message}");
            }
        }
    }
}