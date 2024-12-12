using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{
    public class PonyHairExtension : DefModExtension
    {
        public int[] shortTailPool = new int[] { 12, 13, 14, 15, 19 };
        public int[] waveyTailPool = new int[] { 0, 8, 11, 14, 18 };
        public int[] curlyTailPool = new int[] { 3, 7, 15, 17 };
        public int[] royalTailPool = new int[] { 4, 10 };
        public List<int> customPool;

        public bool shortTail;
        public bool waveyTail;
        public bool curlyTail;
        public bool royalTail;
    }
}
