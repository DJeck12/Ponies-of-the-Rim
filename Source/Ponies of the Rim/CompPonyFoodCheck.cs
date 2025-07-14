using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
	public class CompPonyFoodCheck : ThingComp
	{
        public override void PostIngested(Pawn ingester)
        {
            if (ingester.IsPony() && ContainsMeat(parent))
            {
                ThoughtDef PonyAteMeatThought = DefDatabase<ThoughtDef>.GetNamed("Pony_AteMeat");
                if (PonyAteMeatThought != null)
                {
                    ingester.needs.mood.thoughts.memories.TryGainMemory(PonyAteMeatThought);
                }
            }
        }

		private bool ContainsMeat(Thing thing)
        {
            if (thing is ThingWithComps thingWithComps)
            {
                CompIngredients comp = thingWithComps.GetComp<CompIngredients>();
                if (comp != null)
                {
                    return comp.ingredients.Any(def => def.IsMeat);
                }
            }
            return false;
        }
	}
}
