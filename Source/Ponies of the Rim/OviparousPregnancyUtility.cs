using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_PregnancyUtility_ApplyBirthOutcome
    {
        private static GeneDef _ovipGene;
        private static GeneDef OvipGene =>
            _ovipGene ??= DefDatabase<GeneDef>.GetNamed("Pony_Oviparous", errorOnFail: false);

        private static ThingDef _eggDef;
        private static ThingDef EggDef =>
            _eggDef ??= DefDatabase<ThingDef>.GetNamed("Pony_AvianEgg", errorOnFail: false);

        public static bool Prefix(Pawn geneticMother, ref Thing __result)
        {
            Log.Message($"[PoniesOfTheRim] ApplyBirthOutcome.Prefix вызван. geneticMother={geneticMother?.LabelShort ?? "null"}");

            if (!ModsConfig.BiotechActive)
                return true;

            Pawn mother = geneticMother;

            if (mother?.genes == null)
            {
                Log.Message("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: genes == null → пропуск.");
                return true;
            }

            if (OvipGene == null)
            {
                Log.Error("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: GeneDef 'Pony_Oviparous' не найден!");
                return true;
            }

            bool hasGene = mother.genes.HasActiveGene(OvipGene);
            Log.Message($"[PoniesOfTheRim] ApplyBirthOutcome.Prefix: HasActiveGene(Pony_Oviparous) = {hasGene}");

            if (!hasGene)
                return true;

            if (EggDef == null)
            {
                Log.Error("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: ThingDef 'Pony_AvianEgg' не найден!");
                return true;
            }

            var hediff = mother.health?.hediffSet?
                .GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;

            Pawn father  = hediff != null
                ? Traverse.Create(hediff).Field("father").GetValue<Pawn>()
                : null;
            GeneSet genes = hediff != null
                ? Traverse.Create(hediff).Field("geneSet").GetValue<GeneSet>()
                : null;

            Thing egg = ThingMaker.MakeThing(EggDef);
            Comp_EggHatcher comp = egg.TryGetComp<Comp_EggHatcher>();

            if (comp != null)
            {
                comp.mother   = mother;
                comp.father   = father;
                comp.xenotype = mother.genes?.Xenotype ?? XenotypeDefOf.Baseliner;
                comp.geneSet  = genes;
            }

            GenSpawn.Spawn(egg, mother.PositionHeld, mother.MapHeld);
            Log.Message("[PoniesOfTheRim] ApplyBirthOutcome.Prefix: яйцо заспавнено, ванильный исход заблокирован.");

            __result = egg;
            return false;
        }
    }
}