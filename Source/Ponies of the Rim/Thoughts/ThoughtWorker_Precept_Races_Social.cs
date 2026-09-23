using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_Races_Social : ThoughtWorker_Precept_Social
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
                        PonyLog.ErrorOnce("ThoughtWorker_Precept_Races_Social|" + def.defName,
                            "Мысли: у ThoughtDef '" + def.defName + "' нет ThoughtExtension — мысль не будет работать.");
                    }
                }
                return cachedExtension;
            }
        }

        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            ThingDef race = Extension?.race;
            return race != null && otherPawn.def == race;
        }
    }
}