using System;
using System.Collections.Generic;
using Verse;

namespace Ponies_of_the_Rim
{
    public class HediffGiver_Always : HediffGiver
    {
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (HasHediffCount(pawn, this.hediff, this.countToAffect, false))
            {
                return;
            }

            if (GetNumPresentParts(pawn, this.partsToAffect) >= this.countToAffect)
            {
                if (base.TryApply(pawn, null))
                {
                    if (sendLetter)
                    {
                        base.SendLetter(pawn, cause);
                    }
                }
            }
        }

        public bool HasHediffCount(Pawn pawn, HediffDef def, int requiredCount = 1, bool mustBeVisible = false)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            int count = 0;
            for (int i = 0; i < hediffSet.hediffs.Count; i++)
            {
                if (hediffSet.hediffs[i].def == def && (!mustBeVisible || hediffSet.hediffs[i].Visible))
                {
                    count++;
                }
            }

            if (count >= requiredCount)
            {
                return true;
            }

            return false;
        }

        public int GetNumPresentParts(Pawn pawn, List<BodyPartDef> partsToAffect)
        {
            int numPresentParts = 0;

            foreach (BodyPartDef affectedPart in partsToAffect)
            {
                List<BodyPartRecord> partRecords = pawn.RaceProps.body.AllParts.FindAll(part => part.def == affectedPart);

                foreach (BodyPartRecord record in partRecords)
                {
                    if (!pawn.health.hediffSet.PartIsMissing(record))
                    {
                        numPresentParts++;
                    }
                }
            }

            return numPresentParts;
        }

        public bool sendLetter = false;
    }
}