using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{

    public class PonyRaceExtension : DefModExtension
    {
        public bool hasMane = true;
        public static PonyRaceExtension Get(Def def)
        {
            List<DefModExtension> exts = def?.modExtensions;
            if (exts == null)
            {
                return null;
            }
            for (int i = exts.Count - 1; i >= 0; i--)
            {
                if (exts[i] is PonyRaceExtension ext)
                {
                    return ext;
                }
            }
            return null;
        }
    }
}