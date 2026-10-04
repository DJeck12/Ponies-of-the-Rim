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
            Harmony = new Harmony(HarmonyId);

            MethodInfo generatePawn = AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) });
            TryPatch(generatePawn, new HarmonyMethod(typeof(UniqueBirthFix), "GeneratePawn_Prefix") { priority = 600 }, null, null, "PawnGenerator.GeneratePawn (UniqueBirthFix)");
            TryPatch(generatePawn, null, new HarmonyMethod(typeof(UniqueBodyAddonAssigner), "GeneratePawn_Postfix") { priority = 600 }, null, "PawnGenerator.GeneratePawn (UniqueBodyAddonAssigner)");
            TryPatch(generatePawn, null, new HarmonyMethod(typeof(UniqueEquipmentAssigner), "GeneratePawn_Postfix") { priority = 400 }, null, "PawnGenerator.GeneratePawn (UniqueEquipmentAssigner)");
            TryPatch(generatePawn, null, new HarmonyMethod(typeof(UniqueNameAssigner), "GeneratePawn_Postfix") { priority = 100 }, null, "PawnGenerator.GeneratePawn (UniqueNameAssigner)");

            TryPatch(UniqueCleanGeneration.OldAgeInjuriesTarget(), new HarmonyMethod(typeof(UniqueCleanGeneration), "OldAgeInjuries_Prefix") { priority = 600 }, null, null, "AgeInjuryUtility.GenerateRandomOldAgeInjuries (unique clean)");
            TryPatch(UniqueCleanGeneration.TechHediffsTarget(), new HarmonyMethod(typeof(UniqueCleanGeneration), "TechHediffs_Prefix") { priority = 600 }, null, null, "PawnTechHediffsGenerator.GenerateTechHediffsFor (unique clean)");
            TryPatch(UniqueCleanGeneration.AddictionsTarget(), new HarmonyMethod(typeof(UniqueCleanGeneration), "Addictions_Prefix") { priority = 600 }, null, null, "PawnAddictionHediffsGenerator.GenerateAddictionsAndTolerancesFor (unique clean)");

            TryPatch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoWindowContents"), new HarmonyMethod(typeof(UniqueStartingPawnUI), "DoWindowContents_Prefix"), new HarmonyMethod(typeof(UniqueStartingPawnUI), "DoWindowContents_Postfix"), null, "Page_ConfigureStartingPawns.DoWindowContents (unique)");
            TryPatch(AccessTools.Method(typeof(Game), "FinalizeInit"), null, new HarmonyMethod(typeof(UniqueWorldSpawner), "FinalizeInit_Postfix"), null, "Game.FinalizeInit (UniqueWorldSpawner)");
            TryPatch(AccessTools.Method(typeof(SkillRecord), "Interval"), null, new HarmonyMethod(typeof(PerfectMemoryPatch), "Interval_Postfix"), null, "SkillRecord.Interval (PerfectMemory)");
            TryPatch(AccessTools.Method(typeof(GenRecipe), "PostProcessProduct"), null, new HarmonyMethod(typeof(EleganceQualityPatch), "PostProcessProduct_Postfix"), null, "GenRecipe.PostProcessProduct (EleganceQuality)");
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