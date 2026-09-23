using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    [StaticConstructorOnStartup]
    public static class UniquePawnsBootstrap
    {
        public const string HarmonyId = "Rimworld.PoniesOfTheRim.UniquePawns";

        private static readonly Harmony Harmony = new Harmony(HarmonyId);

        private static int _patched;
        private static int _failed;

        static UniquePawnsBootstrap()
        {
            var generatePawnMethod = AccessTools.Method(
                typeof(PawnGenerator),
                nameof(PawnGenerator.GeneratePawn),
                new[] { typeof(PawnGenerationRequest) });

            TryPatch(generatePawnMethod,
                prefix: new HarmonyMethod(typeof(UniqueBirthFix),
                    nameof(UniqueBirthFix.GeneratePawn_Prefix))
                { priority = Priority.High },
                label: "PawnGenerator.GeneratePawn (UniqueBirthFix)");

            TryPatch(generatePawnMethod,
                postfix: new HarmonyMethod(typeof(UniqueBodyAddonAssigner),
                    nameof(UniqueBodyAddonAssigner.GeneratePawn_Postfix))
                { priority = Priority.High },
                label: "PawnGenerator.GeneratePawn (UniqueBodyAddonAssigner)");

            TryPatch(generatePawnMethod,
                postfix: new HarmonyMethod(typeof(UniqueEquipmentAssigner),
                    nameof(UniqueEquipmentAssigner.GeneratePawn_Postfix))
                { priority = Priority.Normal },
                label: "PawnGenerator.GeneratePawn (UniqueEquipmentAssigner)");

            TryPatch(generatePawnMethod,
                postfix: new HarmonyMethod(typeof(UniqueNameAssigner),
                    nameof(UniqueNameAssigner.GeneratePawn_Postfix))
                { priority = Priority.VeryLow },
                label: "PawnGenerator.GeneratePawn (UniqueNameAssigner)");

            TryPatch(
                AccessTools.Method(typeof(Page_ConfigureStartingPawns),
                    nameof(Page_ConfigureStartingPawns.DoWindowContents)),
                prefix: new HarmonyMethod(typeof(UniqueStartingPawnUI),
                    nameof(UniqueStartingPawnUI.DoWindowContents_Prefix)),
                postfix: new HarmonyMethod(typeof(UniqueStartingPawnUI),
                    nameof(UniqueStartingPawnUI.DoWindowContents_Postfix)),
                label: "Page_ConfigureStartingPawns.DoWindowContents (unique)");

            TryPatch(
                AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)),
                postfix: new HarmonyMethod(typeof(UniqueWorldSpawner),
                    nameof(UniqueWorldSpawner.FinalizeInit_Postfix)),
                label: "Game.FinalizeInit (UniqueWorldSpawner)");

            TryPatch(
                AccessTools.Method(typeof(SkillRecord), "Interval"),
                postfix: new HarmonyMethod(typeof(PerfectMemoryPatch),
                    nameof(PerfectMemoryPatch.Interval_Postfix)),
                label: "SkillRecord.Interval (PerfectMemory)");

            TryPatch(
                AccessTools.Method(typeof(GenRecipe), "PostProcessProduct"),
                postfix: new HarmonyMethod(typeof(EleganceQualityPatch),
                    nameof(EleganceQualityPatch.PostProcessProduct_Postfix)),
                label: "GenRecipe.PostProcessProduct (EleganceQuality)");

            PatchStylingStationIfAvailable();

            LongEventHandler.ExecuteWhenFinished(ForceInitUniversalAddons);

            PonyLog.Trace($"Unique pawns: патчей установлено {_patched}, ошибок {_failed}.");
        }

        private static void TryPatch(
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            string label = "")
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"Unique pawns: метод не найден — '{label}'.");
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
                PonyLog.Error($"Unique pawns: ошибка патча '{label}':\n{ex}");
            }
        }

        private static void ForceInitUniversalAddons()
        {
            try
            {
                var addons = Utilities.UniversalBodyAddons;
                int total = addons?.Sum(a => a.GetVariantCount()) ?? 0;

                PonyLog.Trace($"Universal addons: {addons?.Count ?? 0} аддонов, {total} вариантов.");
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Не удалось пересобрать универсальные аддоны HAR — часть внешности может не отображаться.", ex);
            }
        }

        private static void PatchStylingStationIfAvailable()
        {
            try
            {
                var stylingType = AccessTools.TypeByName("AlienRace.StylingStation");
                if (stylingType == null) return;

                var doAddonInfo = AccessTools.Method(
                    stylingType,
                    "DoAddonInfo",
                    new[] { typeof(UnityEngine.Rect),
                            typeof(AlienPartGenerator.BodyAddon),
                            typeof(System.Collections.Generic.List<AlienPartGenerator.BodyAddon>) });

                if (doAddonInfo == null) return;

                TryPatch(
                    doAddonInfo,
                    postfix: new HarmonyMethod(typeof(StylingStationRefresh),
                        nameof(StylingStationRefresh.DoAddonInfo_Postfix)),
                    label: "StylingStation.DoAddonInfo (refresh)");
            }
            catch (Exception ex)
            {
                PonyLog.Warn($"Не удалось пропатчить HAR StylingStation (некритично): {ex.Message}");
            }
        }
    }
}