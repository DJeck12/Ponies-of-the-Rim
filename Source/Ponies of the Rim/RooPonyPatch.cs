using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyRooCompatPatch
    {
        private static readonly GeneDef UnguligradeGene =
            DefDatabase<GeneDef>.GetNamed("RBM_UnguligradeLegs", errorOnFail: false);

        public static bool DisableFurForPonyMinotaur(Pawn pawn, ref Graphic __result)
        {
            if (pawn?.story == null || !pawn.IsPony())
                return true;

            if (UnguligradeGene == null || pawn.genes?.HasActiveGene(UnguligradeGene) != true)
                return true;

            FurDef originalFur = pawn.story.furDef;
            pawn.story.furDef  = null;
            __result           = null;

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (pawn?.story != null)
                    pawn.story.furDef = originalFur;
            });

            return false;
        }
    }
}