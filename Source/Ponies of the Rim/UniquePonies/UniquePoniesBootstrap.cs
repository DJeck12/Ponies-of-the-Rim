using System;
using HarmonyLib;
using RimWorld;
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

                Log.Message("[PoniesOfTheRim] Unique pawns & traits system initialized.");
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Failed to initialize unique pawns system: {ex}");
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