using System;
using System.Collections.Generic;
using PoniesOfTheRim.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    public enum PegasusManeuverKind : byte
    {
        None,
        Disengage,
        Cover,
    }

    public static class PegasusCombatManeuvers
    {
        internal const float MIN_STAMINA_DISENGAGE = 0.15f;
        internal const float MIN_STAMINA_COVER = 0.30f;

        internal const int COOLDOWN_DISENGAGE_TICKS = 180;
        internal const int COOLDOWN_COVER_TICKS = 300;
        internal const int RETRY_AFTER_FAILED_SEARCH_TICKS = 90;
        internal const int MANEUVER_TIMEOUT_TICKS = 180;
        internal const float MELEE_THREAT_SCAN_RADIUS = 4.9f;
        internal const float DISENGAGE_PREFERRED_DIST = 9f;
        internal const float DISENGAGE_MIN_DIST_FROM_ATTACKER = 5f;
        internal static readonly float[] DisengageRadii = { 6f, 8f, 10f };
        internal const float AWAY_MIN_DOT = -0.17f;
        internal const float HOSTILE_CLEARANCE = 4f;
        internal const int UNDER_FIRE_WINDOW_TICKS = 120;
        internal const float IN_OPEN_BLOCK_CHANCE = 0.2f;
        internal const float COVER_MIN_GAIN = 0.15f;
        internal const float COVER_MAX_DASH = 12f;
        internal const float PATH_LENGTH_MARGIN = 1.2f;
        internal const float STAMINA_MARGIN = 0.02f;
        private static readonly float[] DirX =
        {
            1f, 0.9238795f, 0.7071068f, 0.3826834f, 0f, -0.3826834f, -0.7071068f, -0.9238795f,
            -1f, -0.9238795f, -0.7071068f, -0.3826834f, 0f, 0.3826834f, 0.7071068f, 0.9238795f,
        };
        private static readonly float[] DirZ =
        {
            0f, 0.3826834f, 0.7071068f, 0.9238795f, 1f, 0.9238795f, 0.7071068f, 0.3826834f,
            0f, -0.3826834f, -0.7071068f, -0.9238795f, -1f, -0.9238795f, -0.7071068f, -0.3826834f,
        };

        private static readonly List<IntVec3> TmpCandidates = new List<IntVec3>();
        private static readonly List<IntVec3> TmpHostiles = new List<IntVec3>();
        private static JobDef _maneuverJobDef;

        internal static JobDef ManeuverJobDef =>
            _maneuverJobDef ??= DefDatabase<JobDef>.GetNamedSilentFail("Pony_FlightManeuver") ?? JobDefOf.Goto;
        public static bool Enabled
        {
            get
            {
                PoniesOfTheRimSettingsData settings = PoniesOfTheRimSettings.settings;
                if (settings != null && !settings.pegasusCombatManeuversAI)
                    return false;
                return !CombatExtendedCompatability.Active;
            }
        }

        internal static bool CanManeuver(Pawn pawn)
        {
            if (pawn.jobs == null || pawn.mindState == null || pawn.InMentalState)
                return false;
            if (pawn.carryTracker?.CarriedThing != null)
                return false;
            if (pawn.stances?.stunner != null && pawn.stances.stunner.Stunned)
                return false;

            Job job = pawn.CurJob;
            if (job == null)
                return false;
            JobDef def = job.def;
            if (def == JobDefOf.Wait_Combat || def == JobDefOf.AttackStatic || def == JobDefOf.AttackMelee)
                return true;
            return def == JobDefOf.Goto && job.jobGiver is JobGiver_AIFightEnemy;
        }

        internal static Verb GetRangedVerb(Pawn pawn)
        {
            Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (verb?.verbProps == null || verb.verbProps.IsMeleeAttack || verb.verbProps.range <= 1f)
                return null;
            return verb;
        }

        internal static Pawn FindMeleeThreat(Pawn pawn)
        {
            Pawn_MindState mind = pawn.mindState;
            if (mind.meleeThreat != null && mind.MeleeThreatStillThreat)
                return mind.meleeThreat;

            Map map = pawn.Map;
            IntVec3 root = pawn.Position;
            int cells = GenRadial.NumCellsInRadius(MELEE_THREAT_SCAN_RADIUS);
            for (int i = 1; i < cells; i++)
            {
                IntVec3 c = root + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                    continue;

                List<Thing> things = map.thingGrid.ThingsListAtFast(c);
                for (int j = 0; j < things.Count; j++)
                {
                    if (!(things[j] is Pawn other) || other == pawn || other.Dead || other.Downed)
                        continue;
                    Job job = other.CurJob;
                    if (job == null || job.def != JobDefOf.AttackMelee || job.targetA.Thing != pawn)
                        continue;
                    if (other.HostileTo(pawn))
                        return other;
                }
            }
            return null;
        }

        internal static bool TryFindDisengageCell(Pawn pawn, Pawn threat, Verb rangedVerb,
            CompPegasusFlightTimer timer, out IntVec3 dest)
        {
            dest = IntVec3.Invalid;
            Map map = pawn.Map;
            float maxDist = AffordableFlightCells(pawn, timer) / PATH_LENGTH_MARGIN;
            if (maxDist < DisengageRadii[0])
                return false;

            float farthest = DisengageRadii[DisengageRadii.Length - 1];
            CollectHostilePositions(pawn, farthest + HOSTILE_CLEARANCE + 1f, TmpHostiles);
            PawnDuty duty = pawn.mindState.duty;
            bool overObstacles = PegasusFlightUtility.CanFlyOverObstacles(pawn);
            Thing shooter = RangedEnemyTarget(pawn, threat);
            IntVec3 attackerPos = threat.Position;

            bool Usable(IntVec3 c)
            {
                if (!IsLandingCell(pawn, map, c, duty))
                    return false;
                if (!ClearOfHostiles(c, TmpHostiles, HOSTILE_CLEARANCE))
                    return false;
                return CanFlyThere(pawn, map, c, overObstacles);
            }

            float CoverAt(IntVec3 c) =>
                shooter != null ? CoverUtility.CalculateOverallBlockChance(c, shooter.Position, map) : 0f;

            bool AttackerMustDetour(IntVec3 c) => LineBlockedForWalkers(map, attackerPos, c);

            bool CanShootAttackerFrom(IntVec3 c) => rangedVerb.CanHitTargetFrom(c, threat);

            return TryPickDisengageCell(pawn.Position, attackerPos, maxDist, Usable, CoverAt, AttackerMustDetour,
                CanShootAttackerFrom, out dest);
        }

        internal static bool TryPickDisengageCell(IntVec3 pawnPos, IntVec3 attackerPos, float maxDist,
            Func<IntVec3, bool> usable, Func<IntVec3, float> coverAt, Func<IntVec3, bool> attackerMustDetour,
            Func<IntVec3, bool> canShootAttackerFrom, out IntVec3 best)
        {
            best = IntVec3.Invalid;

            float awayX = pawnPos.x - attackerPos.x;
            float awayZ = pawnPos.z - attackerPos.z;
            float awayLen = Mathf.Sqrt(awayX * awayX + awayZ * awayZ);
            bool anyDirection = awayLen < 0.5f;
            if (!anyDirection)
            {
                awayX /= awayLen;
                awayZ /= awayLen;
            }

            List<IntVec3> seen = TmpCandidates;
            seen.Clear();
            float bestScore = float.MinValue;

            for (int r = 0; r < DisengageRadii.Length; r++)
            {
                float radius = DisengageRadii[r];
                if (radius > maxDist)
                    break;

                for (int d = 0; d < DirX.Length; d++)
                {
                    if (!anyDirection && DirX[d] * awayX + DirZ[d] * awayZ < AWAY_MIN_DOT)
                        continue;

                    IntVec3 c = new IntVec3(pawnPos.x + Mathf.RoundToInt(DirX[d] * radius), 0,
                        pawnPos.z + Mathf.RoundToInt(DirZ[d] * radius));
                    if (seen.Contains(c))
                        continue;
                    seen.Add(c);

                    float fromAttacker = Distance(c, attackerPos);
                    if (fromAttacker < DISENGAGE_MIN_DIST_FROM_ATTACKER)
                        continue;
                    if (!usable(c))
                        continue;

                    float score = -Mathf.Abs(fromAttacker - DISENGAGE_PREFERRED_DIST)
                                  - 0.1f * Distance(c, pawnPos)
                                  + 4f * coverAt(c);
                    if (attackerMustDetour(c))
                        score += 3f;
                    if (canShootAttackerFrom(c))
                        score += 1.5f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }
            }
            return best.IsValid;
        }

        internal static bool UnderFireInOpen(Pawn pawn, out Thing shooter, out float cover)
        {
            shooter = null;
            cover = 1f;
            Pawn_MindState mind = pawn.mindState;
            if (Find.TickManager.TicksGame - mind.lastRangedHarmTick > UNDER_FIRE_WINDOW_TICKS)
                return false;

            Thing target = mind.enemyTarget;
            if (target == null || !target.Spawned || target.Map != pawn.Map || !IsRangedThreat(target))
                return false;

            cover = CoverUtility.CalculateOverallBlockChance(pawn, target.Position, pawn.Map);
            if (cover >= IN_OPEN_BLOCK_CHANCE)
                return false;

            shooter = target;
            return true;
        }

        internal static bool TryFindCoverCell(Pawn pawn, Thing shooter, float currentCover, Verb rangedVerb,
            CompPegasusFlightTimer timer, out IntVec3 dest)
        {
            dest = IntVec3.Invalid;
            Map map = pawn.Map;
            float maxDash = Mathf.Min(COVER_MAX_DASH, AffordableFlightCells(pawn, timer) / PATH_LENGTH_MARGIN);
            if (maxDash < 2f)
                return false;

            CollectHostilePositions(pawn, maxDash + HOSTILE_CLEARANCE + 1f, TmpHostiles);
            PawnDuty duty = pawn.mindState.duty;
            bool overObstacles = PegasusFlightUtility.CanFlyOverObstacles(pawn);

            CastPositionRequest request = new CastPositionRequest
            {
                caster = pawn,
                target = shooter,
                verb = rangedVerb,
                maxRangeFromTarget = rangedVerb.EffectiveRange,
                wantCoverFromTarget = true,
                maxRangeFromCaster = maxDash,
                validator = c => IsLandingCell(pawn, map, c, duty) && ClearOfHostiles(c, TmpHostiles, HOSTILE_CLEARANCE) &&
                                 CanFlyThere(pawn, map, c, overObstacles),
            };

            if (!CastPositionFinder.TryFindCastPosition(request, out IntVec3 found) || found == pawn.Position)
                return false;

            float newCover = CoverUtility.CalculateOverallBlockChance(found, shooter.Position, map);
            if (newCover < currentCover + COVER_MIN_GAIN)
                return false;

            dest = found;
            return true;
        }

        internal static float AffordableFlightCells(Pawn pawn, CompPegasusFlightTimer timer)
        {
            if (timer == null)
                return float.MaxValue;
            float share = timer.CurrentStaminaPercent - CompPegasusFlightTimer.MinStaminaToFly - STAMINA_MARGIN;
            return PegasusFlightUtility.FlightCellsForStamina(pawn, timer, share);
        }

        private static bool IsLandingCell(Pawn pawn, Map map, IntVec3 c, PawnDuty duty)
        {
            if (!c.InBounds(map) || c.Roofed(map) || c.Fogged(map) || !c.Standable(map))
                return false;
            if (FireUtility.ContainsStaticFire(c, map))
                return false;
            if (!map.pawnDestinationReservationManager.CanReserve(c, pawn, false))
                return false;
            return InDutyArea(duty, c);
        }

        internal static bool InDutyArea(PawnDuty duty, IntVec3 c)
        {
            if (duty == null || duty.radius <= 0f || !duty.focus.IsValid)
                return true;
            return c.InHorDistOf(duty.focus.Cell, duty.radius);
        }

        private static void CollectHostilePositions(Pawn pawn, float radius, List<IntVec3> into)
        {
            into.Clear();
            float radiusSq = radius * radius;
            IntVec3 root = pawn.Position;
            List<IAttackTarget> targets = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            for (int i = 0; i < targets.Count; i++)
            {
                IAttackTarget target = targets[i];
                Thing thing = target.Thing;
                if (thing == null || !thing.Spawned)
                    continue;
                if (thing is Pawn other && (other.Dead || other.Downed))
                    continue;
                if (target.ThreatDisabled(pawn))
                    continue;
                if ((thing.Position - root).LengthHorizontalSquared > radiusSq)
                    continue;
                into.Add(thing.Position);
            }
        }

        private static bool ClearOfHostiles(IntVec3 c, List<IntVec3> hostiles, float clearance)
        {
            float clearanceSq = clearance * clearance;
            for (int i = 0; i < hostiles.Count; i++)
            {
                if ((hostiles[i] - c).LengthHorizontalSquared < clearanceSq)
                    return false;
            }
            return true;
        }

        private static bool CanFlyThere(Pawn pawn, Map map, IntVec3 c, bool overObstacles)
        {
            return overObstacles
                ? PegasusFlightReachability.CanReachByFlight(map, pawn.Position, c, PathEndMode.OnCell)
                : StraightOpenLine(map, pawn.Position, c);
        }

        private static bool StraightOpenLine(Map map, IntVec3 from, IntVec3 to)
        {
            int dx = to.x - from.x;
            int dz = to.z - from.z;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
            for (int i = 1; i <= steps; i++)
            {
                IntVec3 c = new IntVec3(from.x + Mathf.RoundToInt(dx * i / (float)steps), 0,
                    from.z + Mathf.RoundToInt(dz * i / (float)steps));
                if (!c.InBounds(map) || !c.Walkable(map) || c.Roofed(map) || c.Fogged(map) || c.GetDoor(map) != null)
                    return false;
            }
            return true;
        }

        private static bool LineBlockedForWalkers(Map map, IntVec3 from, IntVec3 to)
        {
            int dx = to.x - from.x;
            int dz = to.z - from.z;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
            for (int i = 1; i < steps; i++)
            {
                IntVec3 c = new IntVec3(from.x + Mathf.RoundToInt(dx * i / (float)steps), 0,
                    from.z + Mathf.RoundToInt(dz * i / (float)steps));
                if (c.InBounds(map) && !c.Walkable(map))
                    return true;
            }
            return false;
        }

        private static Thing RangedEnemyTarget(Pawn pawn, Pawn meleeThreat)
        {
            Thing target = pawn.mindState.enemyTarget;
            if (target == null || target == meleeThreat || !target.Spawned || target.Map != pawn.Map)
                return null;
            return IsRangedThreat(target) ? target : null;
        }

        private static bool IsRangedThreat(Thing thing)
        {
            if (thing is Pawn pawn)
                return GetRangedVerb(pawn) != null;
            return thing is Building_Turret;
        }

        private static float Distance(IntVec3 a, IntVec3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}