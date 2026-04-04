using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class HediffGiver_AbilityHorn : HediffGiver
    {
        public AbilityDef ability;

        private static PoniesOfTheRimSettingsData _cachedSettings;
        private static PoniesOfTheRimSettingsData Settings =>
            _cachedSettings ??= LoadedModManager
                .GetMod<PoniesOfTheRimSettings>()
                .GetSettings<PoniesOfTheRimSettingsData>();

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (!Settings.IsAbilityEnabled(ability.defName))
            {
                pawn.abilities.RemoveAbility(ability);
                return;
            }

            int numNaturalHorn    = GetNumNaturalHorn(pawn, partsToAffect);
            int numProstheticHorn = GetNumProstheticHorn(pawn, partsToAffect);

            if (numNaturalHorn == 1 || numProstheticHorn == 1)
                pawn.abilities.GainAbility(ability);
            else if (numNaturalHorn == 0 && numProstheticHorn == 0)
                pawn.abilities.RemoveAbility(ability);

            HornCheck(pawn, partsToAffect);
        }

        public int GetNumNaturalHorn(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            return partsToAffect.Count(p =>
                !pawn.health.hediffSet.IsBionicOrImplant(p) &&
                pawn.RaceProps.body.AllParts.Any(part =>
                    part.def == p && !pawn.health.hediffSet.PartIsMissing(part)));
        }

        public int GetNumProstheticHorn(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            return partsToAffect.Count(p =>
                pawn.health.hediffSet.IsBionicOrImplant(p) &&
                pawn.RaceProps.body.AllParts.Any(part =>
                    part.def == p && !pawn.health.hediffSet.PartIsMissing(part)));
        }

        public bool HornCheck(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            foreach (BodyPartDef affectedPart in partsToAffect)
            {
                if (pawn.health.hediffSet.IsBionicOrImplant(affectedPart))
                    continue;

                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts
                    .Where(part => part.def == affectedPart))
                {
                    if (!pawn.health.hediffSet.PartIsMissing(record) &&
                        !pawn.health.hediffSet.HasHediff(hediff, record))
                    {
                        pawn.health.AddHediff(hediff, record);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}