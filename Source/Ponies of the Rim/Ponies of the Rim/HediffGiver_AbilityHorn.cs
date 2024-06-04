using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace PoniesOfTheRim
{
    public class HediffGiver_AbilityHorn : HediffGiver
    {
        public AbilityDef ability;


        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            int numNaturalHorn = GetNumNaturalHorn(pawn, this.partsToAffect);
            int numProstheticHorn = GetNumProstheticHorn(pawn, this.partsToAffect);

            if (numNaturalHorn == 1 || numProstheticHorn == 1)
            {
                pawn.abilities.GainAbility(this.ability);
            }
            else if (numNaturalHorn == 0 && numProstheticHorn == 0)
            {
                pawn.abilities.RemoveAbility(this.ability);
            }

            if (HornCheck(pawn, this.partsToAffect))
            {

            }
        }


        public int GetNumNaturalHorn(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            return partsToAffect.Count(p => !pawn.health.hediffSet.IsBionicOrImplant(p) && pawn.RaceProps.body.AllParts.Any(part => part.def == p && !pawn.health.hediffSet.PartIsMissing(part)));
        }


        public int GetNumProstheticHorn(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            return partsToAffect.Count(p => pawn.health.hediffSet.IsBionicOrImplant(p) && pawn.RaceProps.body.AllParts.Any(part => part.def == p && !pawn.health.hediffSet.PartIsMissing(part)));
        }

        public bool HornCheck(Pawn pawn, List<BodyPartDef> partsToAffect)
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











