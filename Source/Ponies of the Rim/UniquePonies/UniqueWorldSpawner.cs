using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public class UniqueOnceState : WorldComponent
    {
        public HashSet<string> placed = new();
        public bool processed;

        public UniqueOnceState(World world) : base(world) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref processed, "uniqueProcessed", false);
            Scribe_Collections.Look(ref placed, "uniquePlaced", LookMode.Value);
            placed ??= new HashSet<string>();
        }
    }

    public static class UniqueWorldSpawner
    {
        private static Dictionary<FactionDef, HashSet<ThingDef>> BuildFactionRaceMap()
        {
            var map = new Dictionary<FactionDef, HashSet<ThingDef>>();

            foreach (var fd in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (fd.pawnGroupMakers == null) continue;

                var races = new HashSet<ThingDef>();
                foreach (var pg in fd.pawnGroupMakers)
                {
                    if (pg?.options == null) continue;
                    foreach (var opt in pg.options)
                    {
                        var race = opt?.kind?.race;
                        if (race != null)
                            races.Add(race);
                    }
                }

                if (races.Count > 0)
                    map[fd] = races;
            }

            return map;
        }

        public static void FinalizeInit_Postfix()
        {
            try
            {
                SpawnUniquePawns();
            }
            catch (Exception ex)
            {
                PonyLog.ErrorCaught("Unique: unique pawns not placed in the world", ex);
            }
        }

        private static void SpawnUniquePawns()
        {
            var world = Find.World;
            if (world == null) return;

            var state = world.GetComponent<UniqueOnceState>();
            if (state == null || state.processed) return;

            var factionRaces = BuildFactionRaceMap();

            var uniques = UniquePawnConfig.Characters
                .Select(c => DefDatabase<PawnKindDef>.GetNamed(c.KindDefName, errorOnFail: false))
                .Where(k => k != null)
                .ToList();

            var playerPawns =
                PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists?.ToList()
                ?? new List<Pawn>();

            foreach (var kind in uniques)
            {
                if (state.placed.Contains(kind.defName))
                    continue;

                try
                {
                    TryPlace(kind, state, factionRaces, playerPawns);
                }
                catch (Exception ex)
                {
                    PonyLog.WarnCaught($"Unique: {kind.defName} not placed in the world", ex);
                }
            }

            state.processed = true;
        }

        private static void TryPlace(PawnKindDef kind, UniqueOnceState state,
            Dictionary<FactionDef, HashSet<ThingDef>> factionRaces, List<Pawn> playerPawns)
        {
            if (playerPawns.Any(p => p.kindDef == kind))
            {
                state.placed.Add(kind.defName);
                return;
            }

            var targetRace = kind.race;
            if (targetRace == null) return;

            var candidateFactions = Find.FactionManager.AllFactionsListForReading
                .Where(f =>
                    !f.IsPlayer &&
                    !f.Hidden &&
                    !f.defeated &&
                    factionRaces.TryGetValue(f.def, out var races) &&
                    races.Contains(targetRace))
                .ToList();

            if (candidateFactions.Count == 0)
                return;

            var targetFaction = candidateFactions.RandomElement();

            var req = new PawnGenerationRequest(
                kind,
                targetFaction,
                PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowPregnant: true,
                allowFood: true,
                allowAddictions: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceNoBackstory: false
            );

            var pawn = PawnGenerator.GeneratePawn(req);
            if (pawn == null)
                return;

            if (UniquePawnConfig.ByKindDef.TryGetValue(kind.defName, out var config))
                UniqueNameAssigner.ApplyCanonicalName(pawn, config);

            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            state.placed.Add(kind.defName);
        }
    }
}