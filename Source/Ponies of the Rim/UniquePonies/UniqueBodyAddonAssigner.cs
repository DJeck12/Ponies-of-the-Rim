using System;
using System.Collections.Generic;
using System.Linq;
using AlienRace;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueBodyAddonAssigner
    {
        private static List<AlienPartGenerator.BodyAddon> GetAllAddons(ThingDef_AlienRace alienDef)
        {
            try
            {
                var raceAddons = alienDef.alienRace.generalSettings.alienPartGenerator.bodyAddons;
                var universal = Utilities.UniversalBodyAddons;
                if (universal == null) return null;
                return raceAddons.Concat(universal).ToList();
            }
            catch (Exception)
            {
                return null;     
            }
        }

        public static void GeneratePawn_Postfix(Pawn __result)
        {
            var pawn = __result;
            if (pawn == null) return;

            if (pawn.def is not ThingDef_AlienRace alienDef)
                return;

            var story = pawn.story;
            if (story == null) return;

            var adultName = story.Adulthood?.defName;

            if (!string.IsNullOrEmpty(adultName)
                && UniquePawnConfig.ByAdultBackstory.TryGetValue(adultName, out var config))
            {
                bool hasAnything = config.CutiemarkVariant.HasValue
                                || config.TailVariant.HasValue
                                || config.HeadVariant.HasValue
                                || config.BodyVariant.HasValue;

                if (hasAnything)
                {
                    var comp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
                    if (comp != null)
                    {
                        var addons = GetAllAddons(alienDef);
                        if (addons == null) return;

                        comp.addonVariants ??= new List<int>();

                        if (config.CutiemarkVariant.HasValue)
                            SetVariantByName(addons, comp, "Cutiemark", config.CutiemarkVariant.Value);
                        if (config.TailVariant.HasValue)
                            SetVariantByName(addons, comp, "Tail", config.TailVariant.Value);
                        if (config.HeadVariant.HasValue)
                            SetVariantByName(addons, comp, "Head", config.HeadVariant.Value);
                        if (config.BodyVariant.HasValue)
                            SetVariantByName(addons, comp, "Body", config.BodyVariant.Value);
                    }
                }
                return;
            }

            int cutieVariant = -1;

            if (!string.IsNullOrEmpty(adultName)
                && UniquePawnConfig.BackstoryCutiemark.TryGetValue(adultName, out int av))
            {
                cutieVariant = av;
            }
            else
            {
                var childName = story.Childhood?.defName;
                if (!string.IsNullOrEmpty(childName)
                    && UniquePawnConfig.BackstoryCutiemark.TryGetValue(childName, out int cv))
                {
                    cutieVariant = cv;
                }
            }

            if (cutieVariant >= 0)
            {
                var comp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
                if (comp != null)
                {
                    var addons = GetAllAddons(alienDef);
                    if (addons == null) return;

                    comp.addonVariants ??= new List<int>();
                    SetVariantByName(addons, comp, "Cutiemark", cutieVariant);
                }
            }
        }

        private static void SetVariantByName(
            List<AlienPartGenerator.BodyAddon> addons,
            AlienPartGenerator.AlienComp comp,
            string namePart,
            int variant)
        {
            if (variant < 0) return;

            var addon = addons.FirstOrDefault(a =>
                a?.Name?.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);

            if (addon == null) return;

            int addonIndex = addons.IndexOf(addon);
            if (addonIndex < 0) return;

            while (comp.addonVariants.Count <= addonIndex)
                comp.addonVariants.Add(0);

            comp.addonVariants[addonIndex] = variant;
        }
    }
}