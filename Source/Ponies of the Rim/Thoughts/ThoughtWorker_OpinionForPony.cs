using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_OpinionForPony : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!otherPawn.IsPony() || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }
            if (!p.story.traits.HasTrait(Pony_DefOf.Pony_RelationsForPony))
            {
                return false;
            }
            if (p.story.traits.DegreeOfTrait(Pony_DefOf.Pony_RelationsForPony) != -1)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.ActiveAtStage(1);
        }
    }
}
