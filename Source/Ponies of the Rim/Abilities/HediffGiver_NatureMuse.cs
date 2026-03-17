using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class HediffGiver_NatureMuse : HediffGiver
    {
        public AbilityDef ability;
        public List<string> allowedRaces = new List<string>();

        private static PoniesOfTheRimSettingsData _cachedSettings;
        private static PoniesOfTheRimSettingsData Settings =>
            _cachedSettings ??= LoadedModManager
                .GetMod<PoniesOfTheRimSettings>()
                .GetSettings<PoniesOfTheRimSettingsData>();

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (!Settings.abilities)
            {
                pawn.abilities.RemoveAbility(ability);
                return;
            }

            if (!allowedRaces.NullOrEmpty() && !allowedRaces.Contains(pawn.def.defName))
            {
                pawn.abilities.RemoveAbility(ability);
                return;
            }

            pawn.abilities.GainAbility(ability);
        }
    }
}