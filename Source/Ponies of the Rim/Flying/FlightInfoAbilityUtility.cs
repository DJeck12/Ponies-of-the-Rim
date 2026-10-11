using System.Collections.Generic;
using PoniesOfTheRim.Abilities;
using PoniesOfTheRim.Multiplayer;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class FlightInfoAbilityUtility
    {
        public static bool Sync(Pawn pawn)
        {
            Pawn_AbilityTracker tracker = pawn?.abilities;
            List<Ability> abilities = tracker?.abilities;
            if (abilities == null)
            {
                return false;
            }
            AbilityDef wanted = WantedFor(pawn.def);
            bool has = false;
            List<AbilityDef> stale = null;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (!(abilities[i] is Ability_FlightInfo info))
                {
                    continue;
                }
                if (info.def == wanted)
                {
                    has = true;
                }
                else
                {
                    (stale ?? (stale = new List<AbilityDef>(1))).Add(info.def);
                }
            }
            bool changed = false;

            if (stale != null)
            {
                for (int j = 0; j < stale.Count; j++)
                {
                    tracker.RemoveAbility(stale[j]);
                }
                changed = true;
            }

            if (wanted != null && !has && !MultiplayerCompat.InInterface)
            {
                tracker.GainAbility(wanted);
                changed = true;
            }
            return changed;
        }

        private static AbilityDef WantedFor(ThingDef def)
        {
            AbilityDef infoAbility = def?.GetCompProperties<CompPropertiesPegasusFlightToggle>()?.infoAbility;
            if (infoAbility?.abilityClass == null || !typeof(Ability_FlightInfo).IsAssignableFrom(infoAbility.abilityClass))
            {
                return null;
            }
            return infoAbility;
        }
    }
}