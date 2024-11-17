using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace PoniesOfTheRim
{
    public class HediffGiver_AbilityWings : HediffGiver
    {
        public AbilityDef ability;
<<<<<<< HEAD:Source/Ponies of the Rim/HediffGiver_AbilityWings.cs
=======


>>>>>>> 90cf756f714473384e712a6cc3016d28166dbf3c:Source/Ponies of the Rim/Ponies of the Rim/HediffGiver_AbilityWings.cs
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
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

<<<<<<< HEAD:Source/Ponies of the Rim/HediffGiver_AbilityWings.cs
=======

>>>>>>> 90cf756f714473384e712a6cc3016d28166dbf3c:Source/Ponies of the Rim/Ponies of the Rim/HediffGiver_AbilityWings.cs
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

<<<<<<< HEAD:Source/Ponies of the Rim/HediffGiver_AbilityWings.cs
=======

>>>>>>> 90cf756f714473384e712a6cc3016d28166dbf3c:Source/Ponies of the Rim/Ponies of the Rim/HediffGiver_AbilityWings.cs
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

<<<<<<< HEAD:Source/Ponies of the Rim/HediffGiver_AbilityWings.cs
=======

>>>>>>> 90cf756f714473384e712a6cc3016d28166dbf3c:Source/Ponies of the Rim/Ponies of the Rim/HediffGiver_AbilityWings.cs
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
<<<<<<< HEAD:Source/Ponies of the Rim/HediffGiver_AbilityWings.cs
=======











>>>>>>> 90cf756f714473384e712a6cc3016d28166dbf3c:Source/Ponies of the Rim/Ponies of the Rim/HediffGiver_AbilityWings.cs
