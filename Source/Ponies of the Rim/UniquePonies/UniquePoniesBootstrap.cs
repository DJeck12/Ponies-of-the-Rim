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
        public const string HarmonyId = "Rimworld.Pony.PoniesOfTheRim.UniquePawns";

        static UniquePawnsBootstrap()
        {
            var harmony = new Harmony(HarmonyId);

            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(Page_ConfigureStartingPawns),
                        nameof(Page_ConfigureStartingPawns.DoWindowContents)),
                    postfix: new HarmonyMethod(typeof(UniqueStartingPawnUI),
                        nameof(UniqueStartingPawnUI.DoWindowContents_Postfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(PawnGenerator),
                        nameof(PawnGenerator.GeneratePawn),
                        new[] { typeof(PawnGenerationRequest) }),
                    postfix: new HarmonyMethod(typeof(UniqueBodyAddonAssigner),
                        nameof(UniqueBodyAddonAssigner.GeneratePawn_Postfix))
                        { priority = Priority.High }
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(PawnGenerator),
                        nameof(PawnGenerator.GeneratePawn),
                        new[] { typeof(PawnGenerationRequest) }),
                    postfix: new HarmonyMethod(typeof(UniqueEquipmentAssigner),
                        nameof(UniqueEquipmentAssigner.GeneratePawn_Postfix))
                        { priority = Priority.Normal }
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)),
                    postfix: new HarmonyMethod(typeof(UniqueWorldSpawner),
                        nameof(UniqueWorldSpawner.FinalizeInit_Postfix))
                );

                PatchStylingStationIfAvailable(harmony);

                harmony.Patch(
                    original: AccessTools.Method(typeof(SkillRecord), "Interval"),
                    postfix: new HarmonyMethod(typeof(PerfectMemoryPatch),
                        nameof(PerfectMemoryPatch.Interval_Postfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(GenRecipe), "PostProcessProduct"),
                    postfix: new HarmonyMethod(typeof(EleganceQualityPatch),
                        nameof(EleganceQualityPatch.PostProcessProduct_Postfix))
                );

                LongEventHandler.ExecuteWhenFinished(ForceInitUniversalAddons);

                Log.Message("[PoniesOfTheRim] Unique pawns & traits system initialized.");
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Failed to initialize unique pawns system: {ex}");
            }
        }

        private static void ForceInitUniversalAddons()
        {
            try
            {
                var field = AccessTools.Field(typeof(Utilities), "universalBodyAddons");
                if (field == null)
                {
                    Log.Warning("[PoniesOfTheRim] Could not find Utilities.universalBodyAddons field.");
                    return;
                }

                field.SetValue(null, null);

                var addons = Utilities.UniversalBodyAddons;

                int total = addons?.Sum(a => a.GetVariantCount()) ?? 0;
                Log.Message($"[PoniesOfTheRim] Universal addons initialized: {addons?.Count ?? 0} addons, {total} total variants.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] ForceInitUniversalAddons failed: {ex.Message}");
            }
        }

        private static void PatchStylingStationIfAvailable(Harmony harmony)
        {
            try
            {
                var stylingType = AccessTools.TypeByName("AlienRace.StylingStation");
                if (stylingType == null) return;

                var doAddonInfo = AccessTools.Method(stylingType, "DoAddonInfo");
                if (doAddonInfo == null) return;

                harmony.Patch(
                    doAddonInfo,
                    postfix: new HarmonyMethod(typeof(StylingStationRefresh),
                        nameof(StylingStationRefresh.DoAddonInfo_Postfix))
                );
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] Could not patch HAR StylingStation (non-critical): {ex.Message}");
            }
        }
    }
}