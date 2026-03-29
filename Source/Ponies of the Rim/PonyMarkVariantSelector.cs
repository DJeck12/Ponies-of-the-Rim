using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using static AlienRace.AlienPartGenerator;

namespace PoniesOfTheRim
{
    public static class PonyMarkVariantSelector
    {
        private static FieldInfo _variantCountField;
        private static FieldInfo _adulthoodField;

        private static readonly HashSet<Pawn> _pendingRefresh = new HashSet<Pawn>();
        private static bool _callbackRegistered;

        public static void Initialize()
        {
            _variantCountField = AccessTools.Field(
                typeof(AlienPartGenerator.BodyAddon), "variantCount");

            _adulthoodField =
                AccessTools.Field(typeof(Pawn_StoryTracker), "adulthood") ??
                AccessTools.Field(typeof(Pawn_StoryTracker), "Adulthood");

            if (_variantCountField == null)
                Log.Error("[PoniesOfTheRim] PonyMarkVariantSelector: " +
                          "поле variantCount не найдено на BodyAddon.");
        }

        public static void CompRenderNodes_Postfix(AlienComp __instance)
            => ApplyMarkVariants(__instance);

        public static void RegenerateAddonsForced_Postfix(AlienComp __instance)
            => ApplyMarkVariants(__instance);

        private static void ApplyMarkVariants(AlienComp comp)
        {
            if (_variantCountField == null) return;

            Pawn pawn = comp.parent as Pawn;
            if (pawn?.def == null) return;

            MarkGenerationExtension ext =
                pawn.def.GetModExtension<MarkGenerationExtension>();
            if (ext == null) return;

            List<int> addonVariants = comp.addonVariants;
            if (addonVariants == null || addonVariants.Count == 0) return;

            ThingDef_AlienRace alienDef = pawn.def as ThingDef_AlienRace;
            if (alienDef == null) return;

            bool isExcluded =
                ext.noMarkBackstories.Count > 0 && IsExcludedByBackstory(pawn, ext);

            List<AlienPartGenerator.BodyAddon> allAddons =
                alienDef.alienRace.generalSettings.alienPartGenerator.bodyAddons
                    .Concat(Utilities.UniversalBodyAddons)
                    .ToList();

            bool changed = false;

            for (int i = 0; i < allAddons.Count && i < addonVariants.Count; i++)
            {
                AlienPartGenerator.BodyAddon addon = allAddons[i];
                string addonName = addon.Name;
                int desired = -1;

                if (addonName == "Body" && ext.bodyMarkVariants > 0)
                {
                    int variantCount = (int)_variantCountField.GetValue(addon);
                    if (variantCount <= 1) continue;

                    Rand.PushState(pawn.thingIDNumber ^ 0x1A2B3C);
                    bool hasMarks = !isExcluded && Rand.Value < ext.bodyMarkChance;
                    desired = hasMarks
                        ? Rand.RangeInclusive(1, Math.Min(ext.bodyMarkVariants, variantCount - 1))
                        : 0;
                    Rand.PopState();
                }
                else if (addonName == "Head")
                {
                    int maxVariants = pawn.gender == Gender.Female
                        ? ext.headMarkVariantsFemale
                        : ext.headMarkVariantsMale;
                    if (maxVariants <= 0) continue;

                    int variantCount = (int)_variantCountField.GetValue(addon);
                    if (variantCount <= 1) continue;

                    Rand.PushState(pawn.thingIDNumber ^ 0x4D5E6F);
                    bool hasMarks = !isExcluded && Rand.Value < ext.headMarkChance;
                    desired = hasMarks
                        ? Rand.RangeInclusive(1, Math.Min(maxVariants, variantCount - 1))
                        : 0;
                    Rand.PopState();
                }

                if (desired < 0) continue;

                if (addonVariants[i] != desired)
                {
                    addonVariants[i] = desired;
                    changed = true;
                }
            }

            if (!changed) return;

            _pendingRefresh.Add(pawn);
            PortraitsCache.SetDirty(pawn);

            if (!_callbackRegistered)
            {
                Application.onBeforeRender += ProcessPendingRefresh;
                _callbackRegistered = true;
            }
        }

        private static void ProcessPendingRefresh()
        {
            Application.onBeforeRender -= ProcessPendingRefresh;
            _callbackRegistered = false;

            foreach (Pawn pawn in _pendingRefresh)
            {
                try
                {
                    if (pawn?.Drawer?.renderer == null) continue;
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[PoniesOfTheRim] PonyMarkVariantSelector: " +
                                $"ошибка при обновлении графики {pawn?.Name}: {ex.Message}");
                }
            }

            _pendingRefresh.Clear();
        }

        private static bool IsExcludedByBackstory(Pawn pawn, MarkGenerationExtension ext)
        {
            if (_adulthoodField == null || pawn.story == null) return false;
            BackstoryDef adulthood = _adulthoodField.GetValue(pawn.story) as BackstoryDef;
            return adulthood != null && ext.noMarkBackstories.Contains(adulthood.defName);
        }
    }
}