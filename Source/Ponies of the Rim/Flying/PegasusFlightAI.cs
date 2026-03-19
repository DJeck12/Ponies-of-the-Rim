using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    public class CompProperties_PegasusAI : CompProperties
    {
        public CompProperties_PegasusAI()
        {
            compClass = typeof(CompPegasusAI);
        }
    }

    public class CompPegasusAI : ThingComp
    {
        private const float MIN_STAMINA_TO_START = 0.20f;

        private const int TERRAIN_PENALTY_THRESHOLD = 18;

        private const float APPROACH_FACTOR = 0.80f;

        private const float MAX_CHASE_FACTOR = 3.0f;

        private const int EVAL_INTERVAL = 30;

        private const int PATH_LOOKAHEAD = 16;

        private const int WATER_SAVING_THRESHOLD = 80;

        private bool _flyingForTerrain;
        private bool _flyingForCombat;
        private bool _flyingForWater;

        private int _kiteJobCooldownTick = -1;
        private const int KITE_JOB_COOLDOWN = 180;

        private Pawn Pawn => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _flyingForTerrain,    "potrAI_flyTerrain",    false);
            Scribe_Values.Look(ref _flyingForCombat,     "potrAI_flyCombat",     false);
            Scribe_Values.Look(ref _flyingForWater,      "potrAI_flyWater",      false);
            Scribe_Values.Look(ref _kiteJobCooldownTick, "potrAI_kiteCooldown",  -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
                return;

            if (!pawn.HasWings())
                return;

            if (pawn.IsColonistPlayerControlled)
                return;

            if (!pawn.IsHashIntervalTick(EVAL_INTERVAL))
                return;

            var toggle = pawn.TryGetComp<CompPegasusFlightToggle>();
            var timer  = pawn.TryGetComp<CompPegasusFlightTimer>();
            if (toggle == null)
                return;

            if (!CanPhysicallyFly(pawn, timer))
            {
                if (toggle.FlightEnabled)
                {
                    toggle.FlightEnabled = false;
                    _flyingForTerrain    = false;
                    _flyingForCombat     = false;
                    _flyingForWater      = false;
                }
                return;
            }

            bool wantTerrain  = EvaluateTerrain(pawn, toggle, timer);
            bool wantCombat   = EvaluateCombat(pawn, toggle, timer);
            bool wantWater    = EvaluateWaterPath(pawn, toggle, timer);
            bool wantBlocked  = EvaluateGroundBlocked(pawn, toggle, timer);
            bool wantRetreat  = EvaluateRetreat(pawn, toggle, timer);

            _flyingForTerrain = wantTerrain;
            _flyingForCombat  = wantCombat;
            _flyingForWater   = wantWater;

            bool shouldFly = wantTerrain || wantCombat || wantWater || wantBlocked || wantRetreat;

            if (shouldFly == toggle.FlightEnabled)
                return;

            toggle.FlightEnabled = shouldFly;

            if (shouldFly && wantCombat && HasStructureBlockingTarget(pawn))
            {
                if (pawn.pather != null && pawn.pather.Moving)
                    pawn.pather.StopDead();
            }

            int curTick = Find.TickManager.TicksGame;
            bool kiteOnCooldown = curTick - _kiteJobCooldownTick < KITE_JOB_COOLDOWN;

            if (!kiteOnCooldown)
            {
                IntVec3? kiteTarget = EvaluateKiting(pawn, toggle, timer);
                if (kiteTarget.HasValue)
                {
                    if (!toggle.FlightEnabled)
                        toggle.FlightEnabled = true;

                    Job gotoJob = JobMaker.MakeJob(JobDefOf.Goto, kiteTarget.Value);
                    gotoJob.locomotionUrgency = LocomotionUrgency.Sprint;
                    pawn.jobs?.StartJob(gotoJob, JobCondition.InterruptForced,
                        resumeCurJobAfterwards: false, cancelBusyStances: true);

                    _kiteJobCooldownTick = curTick;
                }
            }
        }

        private static bool CanPhysicallyFly(Pawn pawn, CompPegasusFlightTimer timer)
        {
            if (!PegasusFlightUtil.HasUsableWings(pawn))
                return false;

            if (pawn.Map == null || pawn.Position.Roofed(pawn.Map))
                return false;

            if (timer != null && !timer.CanFly)
                return false;

            return true;
        }

        private static bool EvaluateTerrain(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null)
                return false;

            if (!toggle.FlightEnabled && timer != null && timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                return false;

            PathGrid grid = pawn.Map.pathing.Normal.pathGrid;

            if (grid.Cost(pawn.Position) >= TERRAIN_PENALTY_THRESHOLD)
                return true;

            {
                PawnPath path = pawn.pather?.curPath;
                if (path != null && path.Found)
                {
                    int count = Mathf.Min(4, path.NodesLeftCount);
                    for (int i = 0; i < count; i++)
                    {
                        IntVec3 cell = path.Peek(i);
                        if (!cell.InBounds(pawn.Map) || cell.Roofed(pawn.Map)) continue;
                        if (grid.Cost(cell) >= TERRAIN_PENALTY_THRESHOLD)
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool EvaluateCombat(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            float weaponRange = GetRangedWeaponRange(pawn);
            if (weaponRange <= 0f)
                return false;

            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || !target.Spawned)
                return false;

            float dist        = pawn.Position.DistanceTo(target.Position);
            float desiredDist = weaponRange * APPROACH_FACTOR;

            if (dist <= desiredDist)
                return false;

            if (dist > weaponRange * MAX_CHASE_FACTOR)
                return false;

            if (!toggle.FlightEnabled && timer != null)
            {
                if (timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                    return false;

                float remainingTicks = timer.CurrentStaminaPercent * timer.MaxFlightDurationTicks;
                float flightRange    = remainingTicks / Mathf.Max(1f, pawn.TicksPerMoveCardinal);
                float neededRange    = (dist - desiredDist) * 1.3f;

                if (flightRange < neededRange)
                    return false;
            }

            return true;
        }

        private static bool EvaluateWaterPath(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null) return false;

            if (!toggle.FlightEnabled && timer != null && timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                return false;

            PawnPath path = pawn.pather?.curPath;
            if (path == null || !path.Found) return false;

            int nodesLeft = path.NodesLeftCount;
            if (nodesLeft == 0) return false;

            PathGrid grid     = pawn.Map.pathing.Normal.pathGrid;
            int      lookahead = Mathf.Min(PATH_LOOKAHEAD, nodesLeft);

            int normalCost = Mathf.RoundToInt(pawn.TicksPerMoveCardinal);
            int totalSaving = 0;

            for (int i = 0; i < lookahead; i++)
            {
                IntVec3 cell = path.Peek(i);
                if (!cell.InBounds(pawn.Map)) continue;

                if (cell.Roofed(pawn.Map)) continue;

                int cellCost = grid.Cost(cell);

                if (cellCost >= 10000) continue;

                int saving = cellCost - normalCost;
                if (saving > 0)
                    totalSaving += saving;
            }

            return totalSaving >= WATER_SAVING_THRESHOLD;
        }

        private static IntVec3? EvaluateKiting(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null) return null;
            if (timer != null && !timer.CanFly) return null;
            if (timer != null && !toggle.FlightEnabled && timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                return null;

            VerbProperties vp = PegasusKiteUtil.GetRangedVerbProps(pawn);
            if (vp == null) return null;

            float optRange = PegasusKiteUtil.GetOptimalAccuracyRange(vp);
            if (optRange < 1f) return null;

            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || !target.Spawned) return null;

            float dist    = pawn.Position.DistanceTo(target.Position);
            float lo      = optRange * 0.9f;
            float hi      = optRange * 1.1f;
            bool  inMelee = PegasusKiteUtil.IsInMeleeCombat(pawn);

            if (!inMelee && dist >= lo && dist <= hi) return null;

            IntVec3 pos = PegasusKiteUtil.FindKitePosition(
                pawn, target.Position, optRange, pawn.Map, walkableOnly: true);

            return pos.IsValid ? pos : (IntVec3?)null;
        }

        private static bool EvaluateRetreat(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null) return false;

            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.exitMapOnArrival)
                return true;

            PawnDuty duty = pawn.mindState?.duty;
            if (duty?.def != null)
            {
                string dutyName = duty.def.defName;
                if (dutyName.IndexOf("ExitMap", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (dutyName.Equals("Flee", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool EvaluateGroundBlocked(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null) return false;

            if (!toggle.FlightEnabled && timer != null && timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                return false;

            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || !target.Spawned) return false;

            float dist = pawn.Position.DistanceTo(target.Position);

            float weaponRange = GetRangedWeaponRange(pawn);
            float threshold   = weaponRange > 0f ? weaponRange * MAX_CHASE_FACTOR : 20f;
            if (dist > threshold) return false;

            var tp = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors);
            if (pawn.Map.reachability.CanReach(pawn.Position, target, PathEndMode.Touch, tp))
                return false;

            return true;
        }

        private static bool HasStructureBlockingTarget(Pawn pawn)
        {
            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || pawn.Map == null) return false;

            IntVec3 from = pawn.Position;
            IntVec3 to   = target.Position;
            int dx = to.x - from.x;
            int dz = to.z - from.z;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
            if (steps == 0) return false;

            for (int i = 1; i < steps; i++)
            {
                var cell = new IntVec3(
                    from.x + Mathf.RoundToInt(dx * i / (float)steps),
                    0,
                    from.z + Mathf.RoundToInt(dz * i / (float)steps));

                if (!cell.InBounds(pawn.Map)) continue;
                if (cell.Walkable(pawn.Map))  continue;

                var edifice = cell.GetEdifice(pawn.Map);
                if (edifice?.def?.building == null) continue;
                if (edifice.def.building.isNaturalRock) continue;

                return true;
            }
            return false;
        }

        private static float GetRangedWeaponRange(Pawn pawn)
        {
            ThingWithComps primary = pawn.equipment?.Primary;
            if (primary?.def?.Verbs == null)
                return 0f;

            foreach (VerbProperties vp in primary.def.Verbs)
            {
                if (!vp.IsMeleeAttack && vp.range > 1f)
                    return vp.range;
            }

            return 0f;
        }
    }
}