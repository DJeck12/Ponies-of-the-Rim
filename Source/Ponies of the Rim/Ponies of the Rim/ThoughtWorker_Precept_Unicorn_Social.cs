using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ThoughtWorker_Precept_Unicorn_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return otherPawn.def.defName == "Pony_Unicorn";
        }
    }
}
