using Verse;


namespace PoniesOfTheRim
{
    internal static class PonyHelper
    {
        internal static bool IsPony(this Pawn pawn)
        {
            return pawn.kindDef.race.defName.Contains("Pony_");
        }
    }
}    









