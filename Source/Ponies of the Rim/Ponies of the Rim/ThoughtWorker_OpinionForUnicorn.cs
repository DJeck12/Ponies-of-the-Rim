using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ThoughtWorker_OpinionForUnicorn : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!(otherPawn.def.race.body == DefDatabase<BodyDef>.GetNamed("Pony")) || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }
            if (!p.story.traits.HasTrait(Pony_DefOf.Pony_RelationsForUnicorn))
            {
                return false;
            }
            if (p.story.traits.DegreeOfTrait(Pony_DefOf.Pony_RelationsForUnicorn) != -1)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.ActiveAtStage(1);
        }
    }
}
