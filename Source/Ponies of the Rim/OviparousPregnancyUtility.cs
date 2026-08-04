using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_PregnancyUtility_ApplyBirthOutcome
    {
        public static bool Prefix(Pawn geneticMother, ref Thing __result)
        {
            if (!ModsConfig.BiotechActive)
            {
                return true;
            }
            if (geneticMother?.genes == null)
            {
                return true;
            }
            GeneDef ovip = Gene_Oviparous.OvipGeneDef;
            if (ovip == null)
            {
                Log.Error("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: GeneDef 'Pony_Oviparous' не найден!");
                return true;
            }
            if (!geneticMother.genes.HasActiveGene(ovip))
            {
                return true;
            }
            if (geneticMother.MapHeld == null)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: " + geneticMother.LabelShort +
                                " вне карты (караван) — ванильное рождение.");
                }
                return true;
            }

            Hediff source = Gene_Oviparous.FindParentSourceHediff(geneticMother);
            Gene_Oviparous.ExtractParents(source, out Pawn father, out GeneSet geneSet);

            Thing egg = Gene_Oviparous.TrySpawnEgg(geneticMother, father, geneSet);
            if (egg == null)
            {
                return true;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: яйцо заспавнено, ванильный исход заблокирован (отец: " +
                            (father?.LabelShort ?? "нет") + ").");
            }
            __result = egg;
            return false;
        }
    }
}