using System;
using System.Collections.Generic;
using AlienRace;
using Verse;

namespace PoniesOfTheRim
{
    internal static class CutiemarkAddonResolver
    {
        internal readonly struct ResolvedAddon
        {
            public readonly AlienPartGenerator.BodyAddon Addon;

            public readonly int Index;

            public ResolvedAddon(AlienPartGenerator.BodyAddon addon, int index)
            {
                Addon = addon;
                Index = index;
            }

            public bool IsValid => Addon != null && Index >= 0;
        }
        private static readonly string[] CutiemarkAddonNames = { "Pony_Cutiemark", "Pony_Zebra_Cutiemark" };

        private static readonly string[] PairedTailAddonNames = { "Pony_Tail", "Pony_Long_Tail" };

        public static bool HasVisibleCutiemark(Pawn pawn)
        {
            return TryResolve(pawn, out _, out _);
        }

        public static bool TryResolve(Pawn pawn, out ResolvedAddon cutiemark, out ResolvedAddon tail)
        {
            cutiemark = default;
            tail = default;
            if (!(pawn?.def is ThingDef_AlienRace alienRace))
            {
                return false;
            }
            List<AlienPartGenerator.BodyAddon> raceAddons = alienRace.alienRace?.generalSettings?.alienPartGenerator?.bodyAddons;
            List<AlienPartGenerator.BodyAddon> universalAddons = Utilities.UniversalBodyAddons;
            for (int i = 0; i < CutiemarkAddonNames.Length; i++)
            {
                ResolvedAddon candidate = FindByName(raceAddons, universalAddons, CutiemarkAddonNames[i]);
                if (!candidate.IsValid || !IsCarriedBy(candidate.Addon, pawn))
                {
                    continue;
                }
                cutiemark = candidate;
                ResolvedAddon pairedTail = FindByName(raceAddons, universalAddons, PairedTailAddonNames[i]);
                if (pairedTail.IsValid && IsCarriedBy(pairedTail.Addon, pawn))
                {
                    tail = pairedTail;
                }
                return true;
            }
            return false;
        }

        private static ResolvedAddon FindByName(List<AlienPartGenerator.BodyAddon> raceAddons, List<AlienPartGenerator.BodyAddon> universalAddons, string name)
        {
            int index = 0;
            if (raceAddons != null)
            {
                for (int i = 0; i < raceAddons.Count; i++, index++)
                {
                    if (raceAddons[i]?.Name == name)
                    {
                        return new ResolvedAddon(raceAddons[i], index);
                    }
                }
            }
            if (universalAddons != null)
            {
                for (int i = 0; i < universalAddons.Count; i++, index++)
                {
                    if (universalAddons[i]?.Name == name)
                    {
                        return new ResolvedAddon(universalAddons[i], index);
                    }
                }
            }
            return default;
        }

        private static bool IsCarriedBy(AlienPartGenerator.BodyAddon addon, Pawn pawn)
        {
            try
            {
                return addon.CanDrawAddonStatic(pawn);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Карточка персонажа: не удалось проверить условия аддона '" + addon.Name + "' — кнопки метки и хвоста для этой пешки скрыты.", ex);
                return false;
            }
        }
    }
}