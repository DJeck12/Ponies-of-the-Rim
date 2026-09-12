using AlienRace;
using PoniesOfTheRim.UniquePonies;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyHairExtensionPatch
    {
        private static readonly Dictionary<HairDef, int[]> _allowedTailsCache = new Dictionary<HairDef, int[]>();

        private static int _addonVariantsOffMainReported;
        private static int _randOffMainReported;
        private static int _tailsCacheOffMainReported;

        public static void PonyTailPatch(Pawn pawn, AlienPartGenerator.BodyAddon __instance, ref int sharedIndex)
        {
            if (__instance.Name != "Pony_Tail" || pawn == null || !pawn.IsPony() || pawn.story?.hairDef == null)
            {
                return;
            }

            if (UniquePawnConfig.ByKindDef.TryGetValue(pawn.kindDef?.defName ?? string.Empty, out var config))
            {
                if (!config.TailVariant.HasValue)
                {
                    return;
                }
                UniqueTailInitTracker tracker = UniqueTailInitTracker.Current;
                if (tracker != null && tracker.IsInitialized(pawn.thingIDNumber))
                {
                    return;
                }
                int variant = (sharedIndex = config.TailVariant.Value);
                if (pawn.def is ThingDef_AlienRace alienRaceDef)
                {
                    List<AlienPartGenerator.BodyAddon> addons = alienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons
                        .Concat(Utilities.UniversalBodyAddons).ToList();
                    int addonIndex = addons.IndexOf(__instance);
                    if (addonIndex >= 0)
                    {
                        AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
                        if (alienComp != null)
                        {
                            PonyThreadGuard.ReportIfOffMain(
                                "PonyHairExtensionPatch.PonyTailPatch (запись alienComp.addonVariants)",
                                ref _addonVariantsOffMainReported);

                            if (alienComp.addonVariants == null)
                            {
                                alienComp.addonVariants = new List<int>();
                            }
                            while (alienComp.addonVariants.Count <= addonIndex)
                            {
                                alienComp.addonVariants.Add(0);
                            }
                            alienComp.addonVariants[addonIndex] = variant;
                        }
                    }
                }
                tracker?.MarkInitialized(pawn.thingIDNumber);
            }
            else
            {
                int[] allowed = GetAllowedTails(pawn.story.hairDef);
                if (allowed == null || allowed.Length == 0)
                {
                    return;
                }
                for (int i = 0; i < allowed.Length; i++)
                {
                    if (allowed[i] == sharedIndex)
                    {
                        return;
                    }
                }
                PonyThreadGuard.ReportIfOffMain(
                    "PonyHairExtensionPatch.PonyTailPatch (Rand.PushState)",
                    ref _randOffMainReported);

                Rand.PushState(pawn.thingIDNumber ^ 0x7A11);
                sharedIndex = allowed[Rand.Range(0, allowed.Length)];
                Rand.PopState();
            }
        }

        private static int[] GetAllowedTails(HairDef hairDef)
        {
            if (_allowedTailsCache.TryGetValue(hairDef, out int[] cached))
            {
                return cached;
            }
            PonyThreadGuard.ReportIfOffMain(
                "PonyHairExtensionPatch.GetAllowedTails (запись кэша)",
                ref _tailsCacheOffMainReported);

            int[] result = null;
            PonyHairExtension ext = hairDef.GetModExtension<PonyHairExtension>();
            if (ext?.ponyTailType != null)
            {
                List<int> list = new List<int>();
                foreach (PonyTailType tailType in ext.ponyTailType)
                {
                    if (tailType?.tailIndex == null)
                    {
                        continue;
                    }
                    foreach (int idx in tailType.tailIndex)
                    {
                        if (!list.Contains(idx))
                        {
                            list.Add(idx);
                        }
                    }
                }
                if (list.Count > 0)
                {
                    result = list.ToArray();
                }
            }
            _allowedTailsCache[hairDef] = result;
            return result;
        }
    }
}