using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
	public class ThoughtWorker_JoyousPony : ThoughtWorker
	{
		protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
		{
			if (!RelationsUtility.PawnsKnowEachOther(p, other))
			{
				return false;
			}
			return other.story.traits.HasTrait(Pony_DefOf.Pony_Joyous);
		}
	}
}