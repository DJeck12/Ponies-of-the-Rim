using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_JoyousPonyPresence : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			if (p.MapHeld == null)
			{
				return false;
			}
			if (!p.IsPlayerControlled && !p.IsPrisonerOfColony)
			{
				return false;
			}
			foreach (Pawn item in p.MapHeld.mapPawns.FreeColonistsSpawned)
			{
				if (item != p && item.story.traits.HasTrait(Pony_DefOf.Pony_Joyous) && !item.HostileTo(p))
				{
					return ThoughtState.ActiveWithReason(def.stages[0].description.Formatted(item.Named("PAWN")));
				}
			}
			return false;
		}
    }
}
