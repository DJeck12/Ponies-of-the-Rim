using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_Pony_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return otherPawn.def.defName.Contains("Pony_");
        }
    }
}
