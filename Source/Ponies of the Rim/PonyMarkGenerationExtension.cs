using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{
    public class MarkGenerationExtension : DefModExtension
    {
        public float bodyMarkChance    = 0f;
        public int   bodyMarkVariants  = 0;
        public float headMarkChance    = 0f;
        public int headMarkVariantsMale   = 0;
        public int headMarkVariantsFemale = 0;
        public List<string> noMarkBackstories = new List<string>();
    }
}