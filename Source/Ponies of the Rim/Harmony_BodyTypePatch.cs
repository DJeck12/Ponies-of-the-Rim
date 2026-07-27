using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyBodyTypePatch
    {
        public static void BodyTypePatch(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.IsPony())
            {

                if (ModsConfig.BiotechActive)
                {
                    if (pawn.DevelopmentalStage.Baby() || pawn.DevelopmentalStage.Newborn())
                    {
                        __result = Pony_DefOf.PonyBaby;
                    }
                    if (pawn.DevelopmentalStage.Child())
                    {
                        __result = Pony_DefOf.PonyChild;
                    }
                }
                if (pawn.DevelopmentalStage.Adult())
                {
                    __result = Pony_DefOf.Pony;
                }
            }
        }
    }
}