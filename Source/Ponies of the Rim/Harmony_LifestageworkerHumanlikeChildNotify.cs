using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_LifeStageWorker_HumanlikeChild_Notify
    {
        public static void Postfix(Pawn pawn)
        {
            EggHatchStylePreserver.TryRestore(pawn);
        }
    }
}