using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Ideology
{
    public sealed class FriendshipPreceptExtension : DefModExtension
    {
        public ThoughtDef recruitedPrisonerSocialThought;
        public ThoughtDef violenceMoodThought;
        public ThoughtDef violenceSocialThought;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (recruitedPrisonerSocialThought == null
                && violenceMoodThought == null && violenceSocialThought == null)
            {
                yield return "[PoniesOfTheRim] FriendshipPreceptExtension has no memory thoughts";
            }

            if (recruitedPrisonerSocialThought != null
                && !typeof(Thought_MemorySocial).IsAssignableFrom(recruitedPrisonerSocialThought.ThoughtClass))
            {
                yield return "[PoniesOfTheRim] recruitedPrisonerSocialThought must use Thought_MemorySocial";
            }

            if (violenceMoodThought != null
                && (!typeof(Thought_Memory).IsAssignableFrom(violenceMoodThought.ThoughtClass)
                    || typeof(Thought_MemorySocial).IsAssignableFrom(violenceMoodThought.ThoughtClass)))
            {
                yield return "[PoniesOfTheRim] violenceMoodThought must use a non-social Thought_Memory";
            }

            if (violenceSocialThought != null
                && !typeof(Thought_MemorySocial).IsAssignableFrom(violenceSocialThought.ThoughtClass))
            {
                yield return "[PoniesOfTheRim] violenceSocialThought must use Thought_MemorySocial";
            }
        }
    }
}
