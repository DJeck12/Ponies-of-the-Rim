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
                Log.Error($"[PoniesOfTheRim] Failed to spawn unique pawns: {ex}");
            }
        }

        private static void SpawnUniquePawns()
        {
            var world = Find.World;
            if (world == null) return;

            var state = world.GetComponent<UniqueOnceState>();
            if (state == null || state.processed) return;
            state.processed = true;

            var factionRaces = BuildFactionRaceMap();

            var uniques = UniquePawnConfig.Characters
                .Select(c => DefDatabase<PawnKindDef>.GetNamedSilentFail(c.KindDefName))
                .Where(k => k != null)
                .ToList();

            var playerPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists?.ToList()
                              ?? new List<Pawn>();

            foreach (var kind in uniques)
            {
                if (state.placed.Contains(kind.defName))
                    continue;

                bool chosenByPlayer = playerPawns.Any(p => p.kindDef == kind);
                if (chosenByPlayer)
                {
                    state.placed.Add(kind.defName);
                    continue;
                }

                var targetRace = kind.race;
                if (targetRace == null) continue;

                var candidateFactions = Find.FactionManager.AllFactionsListForReading
                    .Where(f =>
                        f != Faction.OfPlayer &&
                        !f.Hidden &&
                        !f.IsPlayer &&
                        !f.defeated &&
                        factionRaces.TryGetValue(f.def, out var races) &&
                        races.Contains(targetRace))
                    .ToList();

                if (candidateFactions.Count == 0)
                    continue;

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
                if (pawn != null)
                {
                    ForceUniqueName(pawn, kind);
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                    state.placed.Add(kind.defName);
                }
            }
        }

        private static void ForceUniqueName(Pawn pawn, PawnKindDef kind)
        {
            if (!UniquePawnConfig.ByKindDef.TryGetValue(kind.defName, out var config))
                return;

            if (config.UseSingleName)
            {
                pawn.Name = new NameSingle(config.NickName);
            }
            else
            {
                pawn.Name = new NameTriple(
                    config.FirstName,
                    config.NickName,
                    config.LastName
                );
            }
        }
    }
}