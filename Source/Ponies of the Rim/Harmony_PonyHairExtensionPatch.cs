using AlienRace;
using HarmonyLib;
using PoniesOfTheRim.UniquePonies;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyHairExtensionPatch
    {
        public static void PonyTailPatch(Pawn pawn, AlienPartGenerator.BodyAddon __instance, ref int sharedIndex)
        {
            if (__instance.Name != "Pony_Tail") return;
            if (pawn == null || !pawn.IsPony()) return;
            if (pawn.story?.hairDef == null) return;

            if (UniquePawnConfig.ByKindDef.TryGetValue(
                    pawn.kindDef?.defName ?? string.Empty, out var config))
            {
                if (!config.TailVariant.HasValue) return;

                var tracker = UniqueTailInitTracker.Current;
                if (tracker != null && tracker.IsInitialized(pawn.thingIDNumber))
                    return;

                int correctVariant = config.TailVariant.Value;

                sharedIndex = correctVariant;

                if (pawn.def is ThingDef_AlienRace alienDef)
                {
                    var allAddons = alienDef.alienRace.generalSettings.alienPartGenerator.bodyAddons
                        .Concat(Utilities.UniversalBodyAddons)
                        .ToList();

                    int idx = allAddons.IndexOf(__instance);
                    if (idx >= 0)
                    {
                        var comp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
                        if (comp != null)
                        {
                            comp.addonVariants ??= new List<int>();
                            while (comp.addonVariants.Count <= idx)
                                comp.addonVariants.Add(0);
                            comp.addonVariants[idx] = correctVariant;
                        }
                    }
                }

                tracker?.MarkInitialized(pawn.thingIDNumber);
                return;
            }

            if (!pawn.story.hairDef.HasModExtension<PonyHairExtension>())
                return;

            var ext = pawn.story.hairDef.GetModExtension<PonyHairExtension>();
            if (ext?.ponyTailType == null) return;

            var indices = new List<int>();
            foreach (PonyTailType type in ext.ponyTailType)
            {
                if (type?.tailIndex != null)
                    indices.AddRange(type.tailIndex);
            }

            if (indices.Count == 0) return;

            sharedIndex = Rand.Element(indices.Distinct().ToArray());
        }
    }
}