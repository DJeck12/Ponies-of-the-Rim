using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlienRace;
using HarmonyLib;
using RimWorld;
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
            MethodInfo generatePawn = AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                new[] { typeof(PawnGenerationRequest) });
            TryPatch(generatePawn,
                prefix: new HarmonyMethod(typeof(UniqueBirthFix), "GeneratePawn_Prefix") { priority = 600 });
            TryPatch(generatePawn,
                postfix: new HarmonyMethod(typeof(UniqueBodyAddonAssigner), "GeneratePawn_Postfix") { priority = 600 });
            TryPatch(generatePawn,
                postfix: new HarmonyMethod(typeof(UniqueEquipmentAssigner), "GeneratePawn_Postfix") { priority = 400 });
            TryPatch(generatePawn,
                postfix: new HarmonyMethod(typeof(UniqueNameAssigner), "GeneratePawn_Postfix") { priority = 100 });

            TryPatch(UniqueCleanGeneration.OldAgeInjuriesTarget(),
                prefix: new HarmonyMethod(typeof(UniqueCleanGeneration), "OldAgeInjuries_Prefix") { priority = 600 });
            TryPatch(UniqueCleanGeneration.TechHediffsTarget(),
                prefix: new HarmonyMethod(typeof(UniqueCleanGeneration), "TechHediffs_Prefix") { priority = 600 });
            TryPatch(UniqueCleanGeneration.AddictionsTarget(),
                prefix: new HarmonyMethod(typeof(UniqueCleanGeneration), "Addictions_Prefix") { priority = 600 });

            TryPatch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoWindowContents"),
                prefix: new HarmonyMethod(typeof(UniqueStartingPawnUI), "DoWindowContents_Prefix"),
                postfix: new HarmonyMethod(typeof(UniqueStartingPawnUI), "DoWindowContents_Postfix"));
            TryPatch(AccessTools.Method(typeof(Game), "FinalizeInit"),
                postfix: new HarmonyMethod(typeof(UniqueWorldSpawner), "FinalizeInit_Postfix"));
            TryPatch(AccessTools.Method(typeof(SkillRecord), "Interval"),
                postfix: new HarmonyMethod(typeof(PerfectMemoryPatch), "Interval_Postfix"));
            TryPatch(AccessTools.Method(typeof(GenRecipe), "PostProcessProduct"),
                postfix: new HarmonyMethod(typeof(EleganceQualityPatch), "PostProcessProduct_Postfix"));
            TryPatch(AccessTools.Method(typeof(AlienRace.StylingStation), "DoAddonInfo",
                    new[] { typeof(UnityEngine.Rect), typeof(AlienPartGenerator.BodyAddon), typeof(List<AlienPartGenerator.BodyAddon>) }),
                postfix: new HarmonyMethod(typeof(StylingStationRefresh), nameof(StylingStationRefresh.DoAddonInfo_Postfix)));

            LongEventHandler.ExecuteWhenFinished(ForceInitUniversalAddons);

            PonyLog.Trace($"Unique: {_patched} patched, {_failed} failed");
        }

        private static void ForceInitUniversalAddons()
        {
            try
            {
                var addons = Utilities.UniversalBodyAddons;
                int variants = addons?.Sum(a => a.GetVariantCount()) ?? 0;
                PonyLog.Trace($"Addons: {addons?.Count ?? 0} universal, {variants} variants");
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Addons: HAR universal addons init failed, some may not render", ex);
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
                PonyLog.Error($"Unique: target of {Describe(prefix ?? postfix ?? transpiler)} not found, skipped");
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
                PonyLog.Error($"Unique: {Describe(prefix ?? postfix ?? transpiler)} failed on {original.DeclaringType?.Name}.{original.Name}\n{ex}");
            }
        }

        private static string Describe(HarmonyMethod patch)
        {
            MethodInfo method = patch?.method;
            return method == null ? "?" : method.DeclaringType?.Name + "." + method.Name;
        }
    }
}