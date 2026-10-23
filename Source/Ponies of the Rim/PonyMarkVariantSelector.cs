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
    public class PonyMarkTracker : GameComponent
    {
        private HashSet<int> generatedFor = new HashSet<int>();

        public PonyMarkTracker(Game game)
        {
        }

        public static bool TryClaim(Pawn pawn)
        {
            if (pawn == null)
                return false;

            PonyMarkTracker tracker = Current.Game?.GetComponent<PonyMarkTracker>();
            if (tracker == null)
                return true;

            return tracker.generatedFor.Add(pawn.thingIDNumber);
        }

        public static void Release(Pawn pawn)
        {
            if (pawn == null)
                return;
            Current.Game?.GetComponent<PonyMarkTracker>()?.generatedFor.Remove(pawn.thingIDNumber);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref generatedFor, "potrMarksGeneratedFor", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && generatedFor == null)
                generatedFor = new HashSet<int>();
        }
    }

    public static class PonyMarkVariantSelector
    {
        private static FieldInfo _variantCountField;

        private static readonly HashSet<Pawn> _pendingRefresh = new HashSet<Pawn>();
        private static bool _callbackRegistered;

        public static void Initialize()
        {
            _variantCountField = AccessTools.Field(
                typeof(AlienPartGenerator.BodyAddon), "variantCount");

            if (_variantCountField == null)
                PonyLog.Error("Marks: BodyAddon.variantCount not found, body and head mark variants disabled");
        }

        public static void CompRenderNodes_Postfix(AlienComp __instance)
            => ApplyMarkVariantsSafe(__instance);

        public static void RegenerateAddonsForced_Postfix(AlienComp __instance)
            => ApplyMarkVariantsSafe(__instance);

        private static void ApplyMarkVariantsSafe(AlienComp comp)
        {
            try
            {
                ApplyMarkVariants(comp);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Marks: mark variants not applied", ex);
            }
        }

        private static void ApplyMarkVariants(AlienComp comp)
        {
            if (_variantCountField == null) return;
            if (!UnityData.IsInMainThread) return;

            Pawn pawn = comp.parent as Pawn;
            if (pawn?.def == null) return;
            if (pawn.thingIDNumber <= 0) return;

            MarkGenerationExtension ext =
                pawn.def.GetModExtension<MarkGenerationExtension>();
            if (ext == null) return;

            List<int> addonVariants = comp.addonVariants;
            if (addonVariants == null || addonVariants.Count == 0) return;

            ThingDef_AlienRace alienDef = pawn.def as ThingDef_AlienRace;
            if (alienDef == null) return;
            if (!PonyMarkTracker.TryClaim(pawn)) return;

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

                if (addonName == "Pony_Body" && ext.bodyMarkVariants > 0)
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
                else if (addonName == "Pony_Head")
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
                    PonyLog.WarnCaught($"Marks: graphics refresh failed for {pawn?.LabelShortCap}", ex);
                }
            }

            _pendingRefresh.Clear();
        }

        private static bool IsExcludedByBackstory(Pawn pawn, MarkGenerationExtension ext)
        {
            BackstoryDef adulthood = pawn.story?.Adulthood;
            return adulthood != null && ext.noMarkBackstories.Contains(adulthood.defName);
        }
    }
}