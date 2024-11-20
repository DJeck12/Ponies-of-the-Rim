using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ThoughtWorker_Precept_LeatherApparel : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            return ThoughtWorker_LeatherApparel.CurrentThoughtState(p);
        }
    }
}
