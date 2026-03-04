using Verse;

namespace PoniesOfTheRim
{
    public class CompProperties_EggHatcher : CompProperties
    {
        public float daysToHatch = 18f;

        public CompProperties_EggHatcher()
        {
            compClass = typeof(Comp_EggHatcher);
        }
    }
}