using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ThoughtWorker_Precept_StrictRaces_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return otherPawn.def != def.GetModExtension<ThoughtExtension>().race;
        }
    }
}
