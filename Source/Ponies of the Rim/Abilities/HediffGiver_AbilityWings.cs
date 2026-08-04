using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class HediffGiver_AbilityWings : HediffGiver
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
            int numNaturalWing = GetNumNaturalWing(pawn, partsToAffect);
            int numProstheticWing = GetNumProstheticWing(pawn, partsToAffect);
            if (numNaturalWing == 2 || (numNaturalWing == 1 && numProstheticWing == 1) || numProstheticWing == 2)
            {
                pawn.abilities.GainAbility(ability);
            }
            else if (!AbilityToggleEnforcer.IsGrantedByActiveSource(pawn, ability))
            {
                pawn.abilities.RemoveAbility(ability);
            }
            WingCheck(pawn, partsToAffect);
        }

        public int GetNumNaturalWing(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int count = 0;
            HediffSet set = pawn.health.hediffSet;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts)
                {
                    if (record.def == partDef && !set.PartIsMissing(record) && !set.PartHasProsthetic(record))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public int GetNumProstheticWing(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int count = 0;
            HediffSet set = pawn.health.hediffSet;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts)
                {
                    if (record.def == partDef && !set.PartIsMissing(record) && set.PartHasProsthetic(record))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public bool WingCheck(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            bool result = false;
            HediffSet set = pawn.health.hediffSet;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                foreach (BodyPartRecord item in pawn.RaceProps.body.AllParts.Where((BodyPartRecord p) => p.def == partDef))
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
                        result = true;
                    }
                }
            }
            return result;
        }
    }
}