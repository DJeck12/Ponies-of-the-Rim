using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace PoniesOfTheRim.Abilities
{
    public class HediffGiver_AbilityWings : HediffGiver
    {
        public AbilityDef ability;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            //TODO: Move this so is not called on every Interval
            //if (!LoadedModManager.GetMod<PoniesOfTheRimSettings>().GetSettings<PoniesOfTheRimSettingsData>().abilities)
            //{
            //    pawn.abilities.RemoveAbility(this.ability);
            //    return;
            //}

            var allParts = pawn.RaceProps.body.AllParts;

            int numNaturalWing = partsToAffect.Count(p => !pawn.health.hediffSet.IsBionicOrImplant(p) && allParts.Where(part => part.def == p).Any(part => !pawn.health.hediffSet.PartIsMissing(part)));
            int numProstheticWing = partsToAffect.Count(p => pawn.health.hediffSet.IsBionicOrImplant(p) && allParts.Where(part => part.def == p).Any(part => !pawn.health.hediffSet.PartIsMissing(part)));

            if (numNaturalWing == 2 || (numNaturalWing == 1 && numProstheticWing == 1) || numProstheticWing == 2)
            {
                pawn.abilities.GainAbility(this.ability);
            }
            else if ((numNaturalWing == 1 && numProstheticWing == 0) || (numNaturalWing == 0 && numProstheticWing == 1) || numNaturalWing == 0 && numProstheticWing == 0)
            {
                pawn.abilities.RemoveAbility(this.ability);
            }

            if (WingCheck(pawn, this.partsToAffect))
            {

            }
        }
        public int GetNumNaturalWing(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int numNotBionic = 0;

            foreach (BodyPartDef affectedPart in partsToAffect)
            {
                List<BodyPartRecord> partRecords = pawn.RaceProps.body.AllParts.FindAll(part => part.def == affectedPart);

                if (!pawn.health.hediffSet.IsBionicOrImplant(affectedPart))
                {
                    foreach (BodyPartRecord record in partRecords)
                    {
                        if (!pawn.health.hediffSet.PartIsMissing(record))
                        {
                            numNotBionic++;
                        }
                    }
                }
            }
            return numNotBionic;
        }
        public int GetNumProstheticWing(Pawn pawn, List<BodyPartDef> partsToAffect)
        {

            int numBionicParts = 0;

            foreach (BodyPartDef affectedPart in partsToAffect)
            {
                List<BodyPartRecord> partRecords = pawn.RaceProps.body.AllParts.FindAll(part => part.def == affectedPart);

                if (pawn.health.hediffSet.IsBionicOrImplant(affectedPart))
                {
                    foreach (BodyPartRecord record in partRecords)
                    {
                        if (!pawn.health.hediffSet.PartIsMissing(record))
                        {
                            numBionicParts++;

                        }
                    }
                }

            }
            return numBionicParts;
        }
        public bool WingCheck(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            foreach (BodyPartDef affectedPart in partsToAffect)
            {
                List<BodyPartRecord> partRecords = pawn.RaceProps.body.AllParts.FindAll(part => part.def == affectedPart);

                if (!pawn.health.hediffSet.IsBionicOrImplant(affectedPart))
                {
                    foreach (BodyPartRecord record in partRecords)
                    {
                        if (!pawn.health.hediffSet.PartIsMissing(record) && !pawn.health.hediffSet.HasHediff(this.hediff, record))
                        {
                            pawn.health.AddHediff(this.hediff, record);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
