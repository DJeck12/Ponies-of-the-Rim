using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_Hediff_Pregnant_Tick
    {
        private static GeneDef _ovipGene;
        private static GeneDef OvipGene =>
            _ovipGene ??= DefDatabase<GeneDef>.GetNamed("Pony_Oviparous", errorOnFail: false);

        private static ThingDef _eggDef;
        private static ThingDef EggDef =>
            _eggDef ??= DefDatabase<ThingDef>.GetNamed("Pony_AvianEgg", errorOnFail: false);

        public static void Prefix(Hediff __instance)
        {
            Hediff_Pregnant hediff = __instance as Hediff_Pregnant;
            if (hediff == null)
                return;

            if (hediff.Severity < 1f)
                return;

            Pawn mother = hediff.pawn;

            Log.Message($"[PoniesOfTheRim] Tick.Prefix: severity >= 1, mother={mother?.LabelShort ?? "null"}");

            if (!ModsConfig.BiotechActive)
                return;

            if (mother?.genes == null)
            {
                Log.Message("[PoniesOfTheRim] Tick.Prefix: genes == null → пропуск.");
                return;
            }

            if (OvipGene == null)
            {
                Log.Error("[PoniesOfTheRim] Tick.Prefix: GeneDef 'Pony_Oviparous' не найден!");
                return;
            }

            bool hasGene = mother.genes.HasActiveGene(OvipGene);
            Log.Message($"[PoniesOfTheRim] Tick.Prefix: HasActiveGene(Pony_Oviparous) = {hasGene}");

            if (!hasGene)
                return;

            Pawn father = Traverse.Create(hediff).Field("father").GetValue<Pawn>();
            GeneSet geneSet = Traverse.Create(hediff).Field("geneSet").GetValue<GeneSet>();

            Log.Message($"[PoniesOfTheRim] Tick.Prefix: спавним яйцо. father={father?.LabelShort ?? "null"}");

            if (!TrySpawnEgg(mother, father, geneSet))
            {
                Log.Warning("[PoniesOfTheRim] Tick.Prefix: TrySpawnEgg вернул false → ванильное рождение.");
                return;
            }

            mother.health.RemoveHediff(hediff);

            Log.Message("[PoniesOfTheRim] Tick.Prefix: яйцо заспавнено, хеддиф удалён.");
        }

        private static bool TrySpawnEgg(Pawn mother, Pawn father, GeneSet geneSet)
        {
            if (mother?.Map == null)
            {
                Log.Warning(
                    $"[PoniesOfTheRim] TrySpawnEgg: {mother?.LabelShort ?? "null"} " +
                    "не на карте — яйцо не создано."
                );
                return false;
            }

            if (EggDef == null)
            {
                Log.Error("[PoniesOfTheRim] TrySpawnEgg: ThingDef 'Pony_AvianEgg' не найден!");
                return false;
            }

            Thing egg = ThingMaker.MakeThing(EggDef);
            Comp_EggHatcher comp = egg.TryGetComp<Comp_EggHatcher>();

            if (comp != null)
            {
                comp.mother   = mother;
                comp.father   = father;
                comp.xenotype = mother.genes?.Xenotype ?? XenotypeDefOf.Baseliner;
                comp.geneSet  = geneSet;
            }

            GenSpawn.Spawn(egg, mother.PositionHeld, mother.MapHeld);
            return true;
        }
    }
}