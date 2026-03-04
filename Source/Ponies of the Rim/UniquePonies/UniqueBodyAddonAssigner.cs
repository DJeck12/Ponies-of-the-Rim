
using System;
using System.Collections.Generic;
using System.Linq;
using AlienRace;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueBodyAddonAssigner
    {
        public static void GeneratePawn_Postfix(Pawn __result)
        {
            var pawn = __result;
            if (pawn == null) return;

            if (pawn.def is not ThingDef_AlienRace alienDef)
                return;

            var backstoryAdult = pawn.story?.Adulthood?.defName;
            if (string.IsNullOrEmpty(backstoryAdult))
                return;

            if (!UniquePawnConfig.ByAdultBackstory.TryGetValue(backstoryAdult, out var config))
                return;

            bool hasAnything = config.CutiemarkVariant.HasValue
                            || config.TailVariant.HasValue
                            || config.HeadVariant.HasValue
                            || config.BodyVariant.HasValue;

            if (!hasAnything) return;

            var comp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (comp == null) return;

            var addons = alienDef.alienRace.generalSettings.alienPartGenerator.bodyAddons
                .Concat(AlienRace.Utilities.UniversalBodyAddons)
                .ToList();

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