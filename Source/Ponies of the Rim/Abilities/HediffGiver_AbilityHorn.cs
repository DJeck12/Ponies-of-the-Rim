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
                if (!AbilityToggleEnforcer.IsGrantedByActiveSource(pawn, ability))
                {
                    pawn.abilities.RemoveAbility(ability);
                }
                return;
            }
            int numNaturalHorn = GetNumNaturalHorn(pawn, partsToAffect);
            int numProstheticHorn = GetNumProstheticHorn(pawn, partsToAffect);
            if (numNaturalHorn == 1 || numProstheticHorn == 1)
            {
                pawn.abilities.GainAbility(ability);
            }
            else if (numNaturalHorn == 0 && numProstheticHorn == 0 &&
                     !AbilityToggleEnforcer.IsGrantedByActiveSource(pawn, ability))
            {
                pawn.abilities.RemoveAbility(ability);
            }
            HornCheck(pawn, partsToAffect);
        }

        public int GetNumNaturalHorn(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int num = 0;
            HediffSet set = pawn.health.hediffSet;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts)
                {
                    if (record.def == partDef && !set.PartIsMissing(record) && !set.PartHasProsthetic(record))
                    {
                        num++;
                    }
                }
            }
            return num;
        }

        public int GetNumProstheticHorn(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int num = 0;
            HediffSet set = pawn.health.hediffSet;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts)
                {
                    if (record.def == partDef && !set.PartIsMissing(record) && set.PartHasProsthetic(record))
                    {
                        num++;
                    }
                }
            }
            return num;
        }

        public bool HornCheck(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            HediffSet set = pawn.health.hediffSet;
            foreach (BodyPartDef affectedPart in partsToAffect)
            {
                foreach (BodyPartRecord item in pawn.RaceProps.body.AllParts.Where((BodyPartRecord part) => part.def == affectedPart))
                {
                    if (set.PartIsMissing(item))
                    {
                        continue;
                    }
                    if (set.PartHasProsthetic(item))
                    {
                        Hediff stacked = set.hediffs.FirstOrDefault((Hediff h) => h.Part == item && h.def == hediff);
                        if (stacked != null)
                        {
                            pawn.health.RemoveHediff(stacked);
                        }
                        continue;
                    }
                    if (!set.HasHediff(hediff, item))
                    {
                        pawn.health.AddHediff(hediff, item);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}