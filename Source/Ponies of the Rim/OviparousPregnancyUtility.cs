using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_PregnancyUtility_ApplyBirthOutcome
    {
        public static bool Prefix(Pawn geneticMother, ref Thing __result)
        {
            if (!ModsConfig.BiotechActive || geneticMother?.genes == null)
            {
                return true;
            }
            GeneDef ovip = Gene_Oviparous.OvipGeneDef;
            if (ovip == null || !geneticMother.genes.HasActiveGene(ovip))
            {
                return true;
            }
            if (geneticMother.MapHeld == null)
            {
                PonyLog.Trace($"Eggs: {geneticMother.LabelShort} is off map, vanilla birth");
                return true;
            }

            Hediff source = Gene_Oviparous.FindParentSourceHediff(geneticMother);
            Gene_Oviparous.ExtractParents(source, out Pawn father, out GeneSet geneSet);

            Thing egg = Gene_Oviparous.TrySpawnEgg(geneticMother, father, geneSet);
            if (egg == null)
            {
                return true;
            }

            PonyLog.Trace($"Eggs: {geneticMother.LabelShort} laid an egg instead of giving birth (father: {father?.LabelShort ?? "none"})");
            __result = egg;
            return false;
        }
    }
}