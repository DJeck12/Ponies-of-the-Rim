using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_StrictRaces_Social : ThoughtWorker_Precept_Social
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
                            "[PoniesOfTheRim] ThoughtWorker_Precept_StrictRaces_Social: ThoughtExtension не найден на ThoughtDef '"
                            + def.defName + "'.", def.shortHash);
                    }
                }
                return cachedExtension;
            }
        }

        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            ThingDef race = Extension?.race;
            return race != null && otherPawn.def != race;
        }
    }
}