using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class HediffGiver_AbilityWings : HediffGiver
    {
        public AbilityDef ability;

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (!LoadedModManager.GetMod<PoniesOfTheRimSettings>().GetSettings<PoniesOfTheRimSettingsData>().abilities)
            {
                pawn.abilities.RemoveAbility(ability);
                return;
            }

            int numNaturalWing    = GetNumNaturalWing(pawn, partsToAffect);
            int numProstheticWing = GetNumProstheticWing(pawn, partsToAffect);

            bool hasEnoughWings = numNaturalWing == 2
                || (numNaturalWing == 1 && numProstheticWing == 1)
                || numProstheticWing == 2;

            if (hasEnoughWings)
                pawn.abilities.GainAbility(ability);
            else
                pawn.abilities.RemoveAbility(ability);

            WingCheck(pawn, partsToAffect);
        }

        public int GetNumNaturalWing(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int count = 0;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                if (pawn.health.hediffSet.IsBionicOrImplant(partDef))
                    continue;

                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts.Where(p => p.def == partDef))
                {
                    if (!pawn.health.hediffSet.PartIsMissing(record))
                        count++;
                }
            }
            return count;
        }

        public int GetNumProstheticWing(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int count = 0;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                if (!pawn.health.hediffSet.IsBionicOrImplant(partDef))
                    continue;

                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts.Where(p => p.def == partDef))
                {
                    if (!pawn.health.hediffSet.PartIsMissing(record))
                        count++;
                }
            }
            return count;
        }

        public bool WingCheck(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            bool anyAdded = false;
            foreach (BodyPartDef partDef in partsToAffect)
            {
                if (pawn.health.hediffSet.IsBionicOrImplant(partDef))
                    continue;

                foreach (BodyPartRecord record in pawn.RaceProps.body.AllParts.Where(p => p.def == partDef))
                {
                    if (!pawn.health.hediffSet.PartIsMissing(record)
                        && !pawn.health.hediffSet.HasHediff(hediff, record))
                    {
                        pawn.health.AddHediff(hediff, record);
                        anyAdded = true;
                    }
                }
            }
            return anyAdded;
        }
    }
}