using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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
        internal const float COMFORT_MIN_STAMINA_TO_START = 0.70f;
        internal const float COMFORT_MIN_STAMINA_TO_CONTINUE = 0.50f;

        internal const float TACTICAL_FLIGHT_MAX_SHARE = 0.35f;

        private const int TERRAIN_PENALTY_THRESHOLD = 18;

        private const float APPROACH_FACTOR = 0.80f;

        private const float MAX_CHASE_FACTOR = 3.0f;

        private const int EVAL_INTERVAL = 30;

        private const int PATH_LOOKAHEAD = 16;

        private const int WATER_SAVING_THRESHOLD = 80;

        private bool _flyingForTerrain;
        private bool _flyingForCombat;
        private bool _flyingForWater;

        private PegasusManeuverKind _maneuver;
        private IntVec3 _maneuverDest = IntVec3.Invalid;
        private int _maneuverEndTick = -1;
        private bool _maneuverSoleReason;
        private int _disengageReadyTick = -1;
        private int _coverReadyTick = -1;
        private int _maneuverEndedTick = -1;
        private bool _airborneBeforeManeuver;

        private Pawn Pawn => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _flyingForTerrain, "potrAI_flyTerrain", false);
            Scribe_Values.Look(ref _flyingForCombat, "potrAI_flyCombat", false);
            Scribe_Values.Look(ref _flyingForWater, "potrAI_flyWater", false);
            Scribe_Values.Look(ref _maneuver, "potrAI_maneuver", PegasusManeuverKind.None);
            Scribe_Values.Look(ref _maneuverDest, "potrAI_maneuverDest", IntVec3.Invalid);
            Scribe_Values.Look(ref _maneuverEndTick, "potrAI_maneuverEnd", -1);
            Scribe_Values.Look(ref _maneuverSoleReason, "potrAI_maneuverSole", false);
            Scribe_Values.Look(ref _disengageReadyTick, "potrAI_disengageReady", -1);
            Scribe_Values.Look(ref _coverReadyTick, "potrAI_coverReady", -1);
            Scribe_Values.Look(ref _maneuverEndedTick, "potrAI_maneuverEndedTick", -1);
            Scribe_Values.Look(ref _airborneBeforeManeuver, "potrAI_airborneBeforeManeuver", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            if (_maneuver != PegasusManeuverKind.None)
                TickManeuver(pawn);

            if (!pawn.IsHashIntervalTick(EVAL_INTERVAL))
                return;

            if (!pawn.Spawned || pawn.Dead || pawn.Downed)
                return;

            if (!pawn.HasWings())
                return;

            if (pawn.IsColonistPlayerControlled)
                return;

            var toggle = PonyFlightCache.GetToggle(pawn);
            var timer = PonyFlightCache.GetTimer(pawn);
            if (toggle == null)
                return;

            if (!CanPhysicallyFly(pawn, timer))
            {
                if (toggle.FlightEnabled)
                {
                    toggle.FlightEnabled = false;
                    _flyingForTerrain = false;
                    _flyingForCombat = false;
                    _flyingForWater = false;
                    PegasusFlightUtility.SafeLand(pawn);
                }

                if (_maneuver != PegasusManeuverKind.None)
                    AbortManeuver(pawn);
                return;
            }

            bool airborne = toggle.FlightEnabled &&
                            (_maneuver == PegasusManeuverKind.None || _airborneBeforeManeuver);

            bool wantTerrain = EvaluateTerrain(pawn, airborne, timer);
            bool wantCombat = EvaluateCombat(pawn, airborne, timer);
            bool wantWater = EvaluateWaterPath(pawn, airborne, timer);
            bool wantBlocked = EvaluateGroundBlocked(pawn, airborne, timer);
            bool wantRetreat = EvaluateRetreat(pawn, airborne, timer);
            bool wantTrapped = EvaluateTrappedOnGround(pawn, airborne, timer);
            bool wantHostilesByAir = EvaluateHostilesOnlyByAir(pawn, airborne, timer);

            _flyingForTerrain = wantTerrain;
            _flyingForCombat = wantCombat;
            _flyingForWater = wantWater;

            bool otherReasons = wantTerrain || wantCombat || wantWater || wantBlocked || wantRetreat || wantTrapped ||
                                wantHostilesByAir;

            bool wantManeuver = UpdateManeuver(pawn, toggle, timer);
            _maneuverSoleReason = wantManeuver && !otherReasons;
            bool shouldFly = otherReasons || wantManeuver;

            if (shouldFly == toggle.FlightEnabled)
                return;

            toggle.FlightEnabled = shouldFly;
            if (!shouldFly)
                PegasusFlightUtility.SafeLand(pawn);

            if (shouldFly && wantHostilesByAir)
                pawn.jobs?.CheckForJobOverride();

            if (shouldFly && wantCombat && PegasusFlightUtility.CanFlyOverObstacles(pawn) && HasStructureBlockingTarget(pawn))
            {
                if (pawn.pather != null && pawn.pather.Moving && IsCombatMoveJob(pawn.CurJob))
                    pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        private static bool IsCombatMoveJob(Job job)
        {
            return job != null && job.def == JobDefOf.Goto &&
                   (job.jobGiver is JobGiver_AIFightEnemy || job.jobGiver is JobGiver_AIGotoNearestHostile);
        }

        private bool UpdateManeuver(Pawn pawn, CompPegasusFlightToggle toggle, CompPegasusFlightTimer timer)
        {
            if (_maneuver != PegasusManeuverKind.None)
                return true;
            if (!PegasusCombatManeuvers.Enabled || !PegasusCombatManeuvers.CanManeuver(pawn))
                return false;

            Verb verb = PegasusCombatManeuvers.GetRangedVerb(pawn);
            if (verb == null)
                return false;

            int now = Find.TickManager.TicksGame;
            float stamina = timer?.CurrentStaminaPercent ?? 1f;

            if (now == _maneuverEndedTick)
                return false;

            if (now >= _disengageReadyTick && stamina >= PegasusCombatManeuvers.MIN_STAMINA_DISENGAGE)
            {
                Pawn threat = PegasusCombatManeuvers.FindMeleeThreat(pawn);
                if (threat != null)
                {
                    if (PegasusCombatManeuvers.TryFindDisengageCell(pawn, threat, verb, timer, out IntVec3 dest))
                    {
                        StartManeuver(pawn, toggle, PegasusManeuverKind.Disengage, dest, threat);
                        return true;
                    }
                    _disengageReadyTick = now + PegasusCombatManeuvers.RETRY_AFTER_FAILED_SEARCH_TICKS;
                }
            }

            if (now >= _coverReadyTick && stamina >= PegasusCombatManeuvers.MIN_STAMINA_COVER &&
                PegasusCombatManeuvers.UnderFireInOpen(pawn, out Thing shooter, out float cover))
            {
                if (PegasusCombatManeuvers.TryFindCoverCell(pawn, shooter, cover, verb, timer, out IntVec3 dest))
                {
                    StartManeuver(pawn, toggle, PegasusManeuverKind.Cover, dest, shooter);
                    return true;
                }
                _coverReadyTick = now + PegasusCombatManeuvers.RETRY_AFTER_FAILED_SEARCH_TICKS;
            }

            return false;
        }

        private void StartManeuver(Pawn pawn, CompPegasusFlightToggle toggle, PegasusManeuverKind kind, IntVec3 dest,
            Thing threat)
        {
            _airborneBeforeManeuver = toggle.FlightEnabled;
            toggle.FlightEnabled = true;

            Job job = JobMaker.MakeJob(PegasusCombatManeuvers.ManeuverJobDef, dest);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.expiryInterval = PegasusCombatManeuvers.MANEUVER_TIMEOUT_TICKS;
            job.checkOverrideOnExpire = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false,
                cancelBusyStances: true);

            _maneuver = kind;
            _maneuverDest = dest;
            _maneuverEndTick = Find.TickManager.TicksGame + PegasusCombatManeuvers.MANEUVER_TIMEOUT_TICKS;

            if (PonyLog.Verbose)
            {
                string what = kind == PegasusManeuverKind.Disengage ? "отрыв от ближнего боя" : "рывок в укрытие";
                PonyLog.TraceOnce("Flight.Maneuver." + kind,
                    $"Полёт: манёвр «{what}» — {pawn.LabelShortCap} летит на {pawn.Position.DistanceTo(dest):F0} клеток " +
                    $"(угроза: {threat?.LabelShortCap ?? "?"}). Сообщение выводится один раз.");
            }
        }

        private void TickManeuver(Pawn pawn)
        {
            bool ownJob = IsManeuverJob(pawn);
            bool alive = pawn.Spawned && !pawn.Dead && !pawn.Downed;
            if (ownJob && alive && Find.TickManager.TicksGame <= _maneuverEndTick)
                return;

            bool landNow = _maneuverSoleReason && alive && pawn.Position.Walkable(pawn.Map);
            EndManeuver();

            if (landNow)
            {
                CompPegasusFlightToggle toggle = PonyFlightCache.GetToggle(pawn);
                if (toggle != null && toggle.FlightEnabled)
                    toggle.FlightEnabled = false;
            }

            if (ownJob && alive)
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
        }

        private void AbortManeuver(Pawn pawn)
        {
            bool ownJob = IsManeuverJob(pawn);
            EndManeuver();
            if (ownJob && pawn.Spawned && !pawn.Dead && !pawn.Downed)
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
        }

        private bool IsManeuverJob(Pawn pawn)
        {
            Job job = pawn.CurJob;
            return job != null && job.def == PegasusCombatManeuvers.ManeuverJobDef && job.targetA.Cell == _maneuverDest;
        }

        private void EndManeuver()
        {
            int now = Find.TickManager.TicksGame;
            if (_maneuver == PegasusManeuverKind.Disengage)
                _disengageReadyTick = now + PegasusCombatManeuvers.COOLDOWN_DISENGAGE_TICKS;
            else if (_maneuver == PegasusManeuverKind.Cover)
                _coverReadyTick = now + PegasusCombatManeuvers.COOLDOWN_COVER_TICKS;

            _maneuver = PegasusManeuverKind.None;
            _maneuverDest = IntVec3.Invalid;
            _maneuverEndTick = -1;
            _maneuverSoleReason = false;
            _maneuverEndedTick = now;
            _airborneBeforeManeuver = false;
        }

        private static bool CanPhysicallyFly(Pawn pawn, CompPegasusFlightTimer timer)
        {
            if (!PegasusFlightUtility.HasUsableWings(pawn))
                return false;

            if (pawn.Map == null || pawn.Position.Roofed(pawn.Map))
                return false;

            if (timer != null && !timer.CanFly)
                return false;

            return true;
        }

        private static bool HasStaminaForComfortFlight(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            if (timer == null)
                return true;

            return ComfortFlightStaminaAllows(airborne, timer.CurrentStaminaPercent,
                airborne && !pawn.Position.Walkable(pawn.Map));
        }

        internal static bool ComfortFlightStaminaAllows(bool airborne, float stamina, bool overObstacle)
        {
            if (!airborne)
                return stamina >= COMFORT_MIN_STAMINA_TO_START;
            return stamina >= COMFORT_MIN_STAMINA_TO_CONTINUE || overObstacle;
        }

        private static bool EvaluateTerrain(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null)
                return false;

            if ((pawn.pather == null || !pawn.pather.Moving) && pawn.Position.Walkable(pawn.Map))
                return false;

            if (!HasStaminaForComfortFlight(pawn, airborne, timer))
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

        private static float TacticalFlightRange(Pawn pawn, CompPegasusFlightTimer timer)
        {
            return Mathf.Min(PegasusFlightUtility.FlightRangeCells(pawn, timer),
                PegasusFlightUtility.FlightCellsForStamina(pawn, timer, TACTICAL_FLIGHT_MAX_SHARE));
        }

        private static bool EvaluateCombat(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            float weaponRange = GetRangedWeaponRange(pawn);
            if (weaponRange <= 0f)
                return false;

            if (pawn.pather == null || !pawn.pather.Moving)
                return false;

            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || !target.Spawned)
                return false;

            float dist = pawn.Position.DistanceTo(target.Position);
            float desiredDist = weaponRange * APPROACH_FACTOR;

            if (dist <= desiredDist)
                return false;

            if (dist > weaponRange * MAX_CHASE_FACTOR)
                return false;

            if (!airborne && timer != null)
            {
                if (timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                    return false;

                float flightRange = TacticalFlightRange(pawn, timer);
                float neededRange = (dist - desiredDist) * 1.3f;

                if (flightRange < neededRange)
                    return false;
            }

            return true;
        }

        private static bool EvaluateWaterPath(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null) return false;

            if (!HasStaminaForComfortFlight(pawn, airborne, timer))
                return false;

            PawnPath path = pawn.pather?.curPath;
            if (path == null || !path.Found) return false;

            int nodesLeft = path.NodesLeftCount;
            if (nodesLeft == 0) return false;

            PathGrid grid = pawn.Map.pathing.Normal.pathGrid;
            int lookahead = Mathf.Min(PATH_LOOKAHEAD, nodesLeft);

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

        private static bool EvaluateRetreat(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
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

        private static bool EvaluateTrappedOnGround(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null || !PegasusFlightUtility.CanFlyOverObstacles(pawn))
                return false;

            if (!airborne && timer != null && timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                return false;

            if (!LordDigsOutWhenTrapped(pawn))
                return false;

            if (!IsGroundTrappedFromMapEdge(pawn))
                return false;

            return PegasusFlightReachability.CanReachMapEdgeByFlight(pawn.Map, pawn.Position);
        }

        private static bool LordDigsOutWhenTrapped(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            return lord != null && ToilDigsOutWhenTrapped(lord.CurLordToil, lord.Graph);
        }

        internal static bool ToilDigsOutWhenTrapped(LordToil toil, StateGraph graph)
        {
            List<Transition> transitions = graph?.transitions;
            if (toil == null || transitions == null)
                return false;

            for (int i = 0; i < transitions.Count; i++)
            {
                Transition transition = transitions[i];
                if (transition?.sources == null || transition.triggers == null || !transition.sources.Contains(toil))
                    continue;

                List<Trigger> triggers = transition.triggers;
                for (int j = 0; j < triggers.Count; j++)
                {
                    if (triggers[j] is Trigger_PawnCannotReachMapEdge)
                        return true;
                }
            }
            return false;
        }

        private static bool EvaluateHostilesOnlyByAir(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            Map map = pawn.Map;
            if (map == null || pawn.InMentalState || !PegasusFlightUtility.CanFlyOverObstacles(pawn))
                return false;

            DutyDef duty = pawn.mindState?.duty?.def;
            if (duty == null || !DutyGoesToNearestHostile(duty))
                return false;

            float maxDist = float.MaxValue;
            if (!airborne && timer != null)
            {
                if (timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                    return false;
                maxDist = TacticalFlightRange(pawn, timer) / 1.3f;
            }

            using (PegasusFlightReachability.GroundOnly())
            {
                Thing current = pawn.mindState.enemyTarget;
                if (current != null && current.Spawned &&
                    pawn.CanReach(current, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                    return false;

                Thing byAir = null;
                List<IAttackTarget> targets = map.attackTargetsCache.GetPotentialTargetsFor(pawn);
                for (int i = 0; i < targets.Count; i++)
                {
                    IAttackTarget target = targets[i];
                    if (target.ThreatDisabled(pawn) || !AttackTargetFinder.IsAutoTargetable(target))
                        continue;

                    Thing thing = target.Thing;
                    if (thing == null || !thing.Spawned || thing.Map != map)
                        continue;
                    if (thing is Pawn other && !other.IsCombatant())
                        continue;

                    if (pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
                        return false;

                    if (byAir == null && pawn.Position.DistanceTo(thing.Position) <= maxDist &&
                        PegasusFlightReachability.CanReachByFlight(map, pawn.Position, thing, PathEndMode.OnCell))
                        byAir = thing;
                }

                if (byAir == null)
                    return false;

                if (!airborne && !_hostilesByAirTraced && PonyLog.Verbose)
                {
                    _hostilesByAirTraced = true;
                    PonyLog.Trace($"Полёт: {pawn.LabelShortCap} не может дойти до врагов по земле — взлетает " +
                                  $"(по воздуху достижим {byAir.LabelShortCap}). Сообщение выводится один раз.");
                }
                return true;
            }
        }

        private static bool _hostilesByAirTraced;

        private static readonly Dictionary<DutyDef, bool> GoesToNearestHostileByDuty = new Dictionary<DutyDef, bool>();

        private static bool DutyGoesToNearestHostile(DutyDef duty)
        {
            if (!GoesToNearestHostileByDuty.TryGetValue(duty, out bool result))
            {
                result = GoesToNearestHostileBeforeDigging(duty.thinkNode);
                GoesToNearestHostileByDuty[duty] = result;
            }
            return result;
        }

        internal static bool GoesToNearestHostileBeforeDigging(ThinkNode root)
        {
            return FirstAssaultNode(root, 0) > 0;
        }

        private static int FirstAssaultNode(ThinkNode node, int depth)
        {
            if (node == null || depth > 32)
                return 0;
            if (node is JobGiver_AIGotoNearestHostile)
                return 1;
            if (node is JobGiver_AISapper || node is JobGiver_AIBreaching)
                return -1;

            List<ThinkNode> subNodes = node.subNodes;
            if (subNodes == null)
                return 0;
            for (int i = 0; i < subNodes.Count; i++)
            {
                int verdict = FirstAssaultNode(subNodes[i], depth + 1);
                if (verdict != 0)
                    return verdict;
            }
            return 0;
        }

        private static bool IsGroundTrappedFromMapEdge(Pawn pawn)
        {
            using (PegasusFlightReachability.GroundOnly())
                return !pawn.CanReachMapEdge();
        }

        private static bool EvaluateGroundBlocked(Pawn pawn, bool airborne, CompPegasusFlightTimer timer)
        {
            if (pawn.Map == null) return false;

            if (!PegasusFlightUtility.CanFlyOverObstacles(pawn)) return false;

            if (!airborne && timer != null && timer.CurrentStaminaPercent < MIN_STAMINA_TO_START)
                return false;

            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || !target.Spawned) return false;

            float dist = pawn.Position.DistanceTo(target.Position);

            float weaponRange = GetRangedWeaponRange(pawn);
            float threshold = weaponRange > 0f ? weaponRange * MAX_CHASE_FACTOR : 20f;
            if (dist > threshold) return false;
            var tp = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors);
            bool reachableOnGround;
            using (PegasusFlightReachability.GroundOnly())
                reachableOnGround = pawn.Map.reachability.CanReach(pawn.Position, target, PathEndMode.Touch, tp);

            return !reachableOnGround;
        }

        private static bool HasStructureBlockingTarget(Pawn pawn)
        {
            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || pawn.Map == null) return false;

            IntVec3 from = pawn.Position;
            IntVec3 to = target.Position;
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
                if (cell.Walkable(pawn.Map)) continue;

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