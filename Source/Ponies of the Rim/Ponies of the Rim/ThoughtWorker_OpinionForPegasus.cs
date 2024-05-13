using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ThoughtWorker_OpinionForPegasus : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!(otherPawn.def.race.body == DefDatabase<BodyDef>.GetNamed("Pegasus")) || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }
            if (!p.story.traits.HasTrait(Pony_DefOf.Pony_RelationsForPegasus))
            {
                return false;
            }
            if (p.story.traits.DegreeOfTrait(Pony_DefOf.Pony_RelationsForPegasus) != -1)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.ActiveAtStage(1);
        }
    }
}
