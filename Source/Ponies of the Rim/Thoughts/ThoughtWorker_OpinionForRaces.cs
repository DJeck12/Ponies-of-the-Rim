using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_OpinionForRaces : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!(otherPawn.def.race.body == def.GetModExtension<ThoughtExtension>().body || !RelationsUtility.PawnsKnowEachOther(p, otherPawn)))
            {
                return false;
            }
            if (!p.story.traits.HasTrait(def.GetModExtension<ThoughtExtension>().trait))
            {
                return false;
            }
            if (p.story.traits.DegreeOfTrait(def.GetModExtension<ThoughtExtension>().trait) != -1)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.ActiveAtStage(1);
        }
    }
}
