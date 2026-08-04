using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_OpinionForRaces : ThoughtWorker
    {
        private ThoughtExtension cachedExtension;
        private bool extensionCached;

        private ThoughtExtension Extension
        {
            get
            {
                if (!extensionCached)
                {
                    extensionCached = true;
                    cachedExtension = def.GetModExtension<ThoughtExtension>();

                    if (cachedExtension == null)
                    {
                        Log.ErrorOnce(
                            "[PoniesOfTheRim] ThoughtWorker_OpinionForRaces: ThoughtExtension не найден на ThoughtDef '"
                            + def.defName + "'.", def.shortHash);
                    }
                }
                return cachedExtension;
            }
        }

        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            var extension = Extension;
            if (extension == null || extension.bodies.NullOrEmpty())
            {
                return false;
            }

            if (!extension.bodies.Contains(otherPawn.def.race.body) || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }

            if (!p.story.traits.HasTrait(extension.trait))
            {
                return false;
            }

            if (p.story.traits.DegreeOfTrait(extension.trait) != -1)
            {
                return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.ActiveAtStage(1);
        }
    }
}