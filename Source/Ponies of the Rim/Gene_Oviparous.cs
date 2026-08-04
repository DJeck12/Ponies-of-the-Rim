using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class Gene_Oviparous : Gene
    {
        private const int CheckIntervalTicks = 60;
        private static ThingDef _eggDef;
        private static GeneDef _ovipGeneDef;
        private static HediffDef _laborDef;
        private static HediffDef _laborPushingDef;
        private static bool _laborDefsResolved;

        public static ThingDef EggDef =>
            _eggDef ?? (_eggDef = DefDatabase<ThingDef>.GetNamed("Pony_AvianEgg", errorOnFail: false));

        public static GeneDef OvipGeneDef =>
            _ovipGeneDef ?? (_ovipGeneDef = DefDatabase<GeneDef>.GetNamed("Pony_Oviparous", errorOnFail: false));

        private static void ResolveLaborDefs()
        {
            if (_laborDefsResolved)
            {
                return;
            }
            _laborDefsResolved = true;
            _laborDef = DefDatabase<HediffDef>.GetNamed("PregnancyLabor", errorOnFail: false);
            _laborPushingDef = DefDatabase<HediffDef>.GetNamed("PregnancyLaborPushing", errorOnFail: false);
        }

        private static bool IsLaborHediff(HediffDef def)
        {
            ResolveLaborDefs();
            return def != null && (def == _laborDef || def == _laborPushingDef);
        }

        public override void Tick()
        {
            base.Tick();
            if (!Active || !pawn.IsHashIntervalTick(CheckIntervalTicks))
            {
                return;
            }

            if (!pawn.Spawned)
            {
                return;
            }

            Hediff_Pregnant pregnant = null;
            List<Hediff> hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                return;
            }
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Pregnant hp && hp.Severity >= 1f)
                {
                    pregnant = hp;
                    break;
                }
            }
            if (pregnant == null)
            {
                return;
            }

            ExtractParents(pregnant, out Pawn father, out GeneSet geneSet);
            Thing egg = TrySpawnEgg(pawn, father, geneSet);
            if (egg == null)
            {
                return;
            }

            pawn.health.RemoveHediff(pregnant);
            if (Prefs.DevMode)
            {
                Log.Message("[PoniesOfTheRim] Gene_Oviparous: " + pawn.LabelShort +
                            " отложила яйцо (отец: " + (father?.LabelShort ?? "нет") + ").");
            }
        }

        public static Hediff FindParentSourceHediff(Pawn mother)
        {
            List<Hediff> hediffs = mother?.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                return null;
            }
            Hediff labor = null;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h is Hediff_Pregnant)
                {
                    return h;
                }
                if (labor == null && IsLaborHediff(h.def))
                {
                    labor = h;
                }
            }
            return labor;
        }

        public static void ExtractParents(Hediff source, out Pawn father, out GeneSet geneSet)
        {
            father = null;
            geneSet = null;
            if (source == null)
            {
                return;
            }
            Type t = source.GetType();
            if (!ParentFieldCache.TryGetValue(t, out var fields))
            {
                fields = (AccessTools.Field(t, "father"), AccessTools.Field(t, "geneSet"));
                ParentFieldCache[t] = fields;
                if (Prefs.DevMode && (fields.father == null || fields.geneSet == null))
                {
                    Log.Warning("[PoniesOfTheRim] ExtractParents: у " + t.Name +
                                " не найдены поля father/geneSet — яйцо будет без этих данных.");
                }
            }
            father = fields.father?.GetValue(source) as Pawn;
            geneSet = fields.geneSet?.GetValue(source) as GeneSet;
        }

        private static readonly Dictionary<Type, (FieldInfo father, FieldInfo geneSet)> ParentFieldCache =
            new Dictionary<Type, (FieldInfo, FieldInfo)>();

        public static Thing TrySpawnEgg(Pawn mother, Pawn father, GeneSet geneSet)
        {
            if (mother?.MapHeld == null)
            {
                Log.Warning("[PoniesOfTheRim] TrySpawnEgg: " + (mother?.LabelShort ?? "null") +
                            " не на карте — яйцо не создано.");
                return null;
            }
            if (EggDef == null)
            {
                Log.Error("[PoniesOfTheRim] TrySpawnEgg: ThingDef 'Pony_AvianEgg' не найден!");
                return null;
            }
            Thing thing = ThingMaker.MakeThing(EggDef);
            Comp_EggHatcher hatcher = thing.TryGetComp<Comp_EggHatcher>();
            if (hatcher != null)
            {
                hatcher.mother = mother;
                hatcher.father = father;
                hatcher.xenotype = mother.genes?.Xenotype ?? XenotypeDefOf.Baseliner;
                hatcher.geneSet = geneSet;
            }
            GenSpawn.Spawn(thing, mother.PositionHeld, mother.MapHeld);
            return thing;
        }
    }

    public class GameComponent_OviparousGeneMigration : GameComponent
    {
        public GameComponent_OviparousGeneMigration(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            GeneDef def = Gene_Oviparous.OvipGeneDef;
            if (def == null || !ModsConfig.BiotechActive)
            {
                return;
            }
            int migrated = 0;
            List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
            List<Gene> stale = null;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn_GeneTracker tracker = pawns[i]?.genes;
                if (tracker == null)
                {
                    continue;
                }
                stale?.Clear();
                List<Gene> genes = tracker.GenesListForReading;
                for (int j = 0; j < genes.Count; j++)
                {
                    Gene g = genes[j];
                    if (g.def == def && !(g is Gene_Oviparous))
                    {
                        (stale ?? (stale = new List<Gene>(1))).Add(g);
                    }
                }
                if (stale == null || stale.Count == 0)
                {
                    continue;
                }
                for (int j = 0; j < stale.Count; j++)
                {
                    Gene old = stale[j];
                    bool xenogene = tracker.Xenogenes.Contains(old);
                    tracker.RemoveGene(old);
                    tracker.AddGene(def, xenogene);
                    migrated++;
                }
            }
            if (migrated > 0)
            {
                Log.Message("[PoniesOfTheRim] Миграция Pony_Oviparous: заменено " + migrated + " экз. гена старого класса из сейва.");
            }
        }
    }
}