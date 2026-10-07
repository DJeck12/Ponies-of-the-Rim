using AlienRace;
using HarmonyLib;
using PoniesOfTheRim.Multiplayer;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_Pawn_Flying
    {
        private static readonly AccessTools.FieldRef<Pawn, Pawn_FlightTracker> flightRef = CreateFlightRef();

        private static AccessTools.FieldRef<Pawn, Pawn_FlightTracker> CreateFlightRef()
        {
            try { return AccessTools.FieldRefAccess<Pawn, Pawn_FlightTracker>("flight"); }
            catch { return null; }
        }

        public static void Postfix(Pawn __instance, ref bool __result)
        {
            try
            {
                if (PoniesOfTheRim.Compatibility.Patch_CE_CollisionVerticalLift.IsGroundedForCalculation(__instance))
                {
                    __result = false;
                    return;
                }

                if (__result) return;
                if (!PegasusFlightUtility.IsPegasusConstantFlight(__instance)) return;

                if (flightRef != null && flightRef(__instance) == null)
                {
                    PonyLog.WarnOnce("Patch_Pawn_Flying.NoFlightTracker",
                        $"Полёт: у пешки {__instance.LabelShortCap} нет Pawn.flight — флаг Flying не выставлен, " +
                        "чтобы не вызвать NRE в сторонних модах.");
                    return;
                }

                __result = true;
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой проверки, летит ли пешка.", e);
            }
        }
    }

    public static class Patch_FlightTracker_FlightTick
    {
        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, Pawn> pawnRef = CreatePawnRef();

        private static readonly FieldInfo fiFlightState =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightState");
        private static readonly FieldInfo fiCooldown =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightCooldownTicks");

        private static object _flyingEnumValue;
        private static readonly object BoxedZero = 0;

        private static AccessTools.FieldRef<Pawn_FlightTracker, Pawn> CreatePawnRef()
        {
            try { return AccessTools.FieldRefAccess<Pawn_FlightTracker, Pawn>("pawn"); }
            catch { return null; }
        }

        public static bool Prefix(Pawn_FlightTracker __instance)
        {
            try
            {
                if (pawnRef == null) return true;
                Pawn pawn = pawnRef(__instance);
                if (!PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                    return true;
                if (pawn.Dead || pawn.Downed)
                    return true;

                if (fiFlightState != null)
                {
                    if (_flyingEnumValue == null)
                        _flyingEnumValue = Enum.Parse(fiFlightState.FieldType, "Flying");
                    fiFlightState.SetValue(__instance, _flyingEnumValue);
                }

                if (fiCooldown != null)
                    fiCooldown.SetValue(__instance, BoxedZero);

                return true;
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой обновления полёта пешки.", e);
                return true;
            }
        }
    }


    public static class Patch_FlightTracker_ForceLand
    {
        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, Pawn> pawnRef = CreatePawnRef();

        private static AccessTools.FieldRef<Pawn_FlightTracker, Pawn> CreatePawnRef()
        {
            try { return AccessTools.FieldRefAccess<Pawn_FlightTracker, Pawn>("pawn"); }
            catch { return null; }
        }

        public static bool Prefix(Pawn_FlightTracker __instance)
        {
            try
            {
                if (pawnRef == null) return true;
                Pawn pawn = pawnRef(__instance);
                if (!PegasusFlightUtility.IsPegasusConstantFlight(pawn)) return true;
                if (pawn.Dead || pawn.Downed) return true;
                return false;
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой принудительной посадки.", e);
                return true;
            }
        }
    }

    public static class Patch_CostToMoveIntoCell
    {
        public static void Postfix(Pawn pawn, IntVec3 c, ref float __result)
        {
            var map = pawn?.Map;
            if (map == null) return;

            if (!PegasusFlightUtility.CanFlyToCell(pawn, c, map)) return;

            var edifice = c.GetEdifice(map);
            if (edifice?.def?.building != null && edifice.def.building.isNaturalRock)
                return;

            float baseTicks = (c.x != pawn.Position.x && c.z != pawn.Position.z)
                ? pawn.TicksPerMoveDiagonal
                : pawn.TicksPerMoveCardinal;

            if (__result <= baseTicks) return;

            float desired = baseTicks;

            var job = pawn.CurJob;
            if (job != null)
            {
                switch (job.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble: desired = Mathf.Max(desired * 3f, 60f); break;
                    case LocomotionUrgency.Walk: desired = Mathf.Max(desired * 2f, 50f); break;
                    case LocomotionUrgency.Jog: break;
                    case LocomotionUrgency.Sprint: desired = Mathf.RoundToInt(desired * 0.75f); break;
                }
            }

            desired = Mathf.Max(desired, 1f);
            __result = Mathf.Min(__result, desired);
        }
    }

    public static class Patch_PathFinder_CreateRequest
    {
        private static readonly MethodInfo CreateRequestOverload;
        private static readonly Type TuningNullableType;
        private static readonly Type TuningType;
        private static readonly FieldInfo FI_costBlockedWallBase;
        private static readonly FieldInfo FI_costBlockedWallExtraPerHitPoint;
        private static readonly FieldInfo FI_costBlockedDoor;
        private static readonly FieldInfo FI_costBlockedDoorPerHitPoint;
        private static readonly FieldInfo FI_costOffLordWalkGrid;
        private static readonly FieldInfo FI_costDanger;

        private static readonly object CachedTuningNullable;

        static Patch_PathFinder_CreateRequest()
        {
            foreach (var mi in typeof(PathFinder).GetMethods(AccessTools.all))
            {
                if (mi.Name != "CreateRequest") continue;
                var ps = mi.GetParameters();
                if (ps.Length >= 8 &&
                    ps[0].ParameterType == typeof(IntVec3) &&
                    ps[1].ParameterType == typeof(LocalTargetInfo) &&
                    ps[2].ParameterType == typeof(IntVec3?) &&
                    ps[3].ParameterType == typeof(TraverseParms) &&
                    ps[4].ParameterType.IsGenericType &&
                    ps[6].ParameterType == typeof(Pawn))
                {
                    CreateRequestOverload = mi;
                    TuningNullableType = ps[4].ParameterType;
                    TuningType = Nullable.GetUnderlyingType(TuningNullableType);
                    break;
                }
            }

            if (TuningType != null)
            {
                FI_costBlockedWallBase = TuningType.GetField("costBlockedWallBase");
                FI_costBlockedWallExtraPerHitPoint = TuningType.GetField("costBlockedWallExtraPerHitPoint");
                FI_costBlockedDoor = TuningType.GetField("costBlockedDoor");
                FI_costBlockedDoorPerHitPoint = TuningType.GetField("costBlockedDoorPerHitPoint");
                FI_costOffLordWalkGrid = TuningType.GetField("costOffLordWalkGrid");
                FI_costDanger = TuningType.GetField("costDanger");

                try
                {
                    object tuning = Activator.CreateInstance(TuningType);
                    FI_costBlockedWallBase?.SetValue(tuning, 0);
                    FI_costBlockedWallExtraPerHitPoint?.SetValue(tuning, 0f);
                    FI_costBlockedDoor?.SetValue(tuning, 0);
                    FI_costBlockedDoorPerHitPoint?.SetValue(tuning, 0f);
                    FI_costOffLordWalkGrid?.SetValue(tuning, 0);
                    FI_costDanger?.SetValue(tuning, 0);
                    CachedTuningNullable = Activator.CreateInstance(TuningNullableType, tuning);
                }
                catch
                {
                    CachedTuningNullable = null;
                }
            }
        }

        public static MethodBase FindTargetMethod()
        {
            foreach (var m in typeof(PathFinder).GetMethods(AccessTools.all))
            {
                if (m.Name != "CreateRequest") continue;
                var ps = m.GetParameters();
                if (ps.Length >= 7 &&
                    ps[0].ParameterType == typeof(IntVec3) &&
                    ps[1].ParameterType == typeof(LocalTargetInfo) &&
                    ps[2].ParameterType == typeof(IntVec3?) &&
                    ps[3].ParameterType == typeof(Pawn))
                    return m;
            }
            return null;
        }

        public static bool Prefix(PathFinder __instance, ref object __result,
            IntVec3 start, LocalTargetInfo target, IntVec3? dest,
            Pawn pawn, PathEndMode peMode, ref PathRequest.IPathGridCustomizer customizer)
        {
            if (!PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                return true;
            if (CreateRequestOverload == null || CachedTuningNullable == null)
                return true;
            if (!FlightCanReachTarget(pawn, start, target, peMode))
            {
                if (pawn.Drafted && PonyFlightCache.GetToggle(pawn)?.RejectOrderNextTick(pawn.CurJob) == true)
                    return true;

                if (PonyLog.Verbose)
                    PonyLog.TraceOnce("Flight.GroundPathFallback",
                        $"Полёт: {pawn.LabelShortCap} не долетит до {DescribeTarget(target)} (крыша, туман или скала на пути) — " +
                        "путь строится по земле. Сообщение выводится один раз.");
                return true;
            }

            var roofAndMountainBlocker = new PegasusFlightPathGridCustomizer(pawn.Map);

            if (!PegasusFlightUtility.CanFlyOverObstacles(pawn))
            {
                customizer = customizer != null
                    ? new CombinedPathGridCustomizer(customizer, roofAndMountainBlocker, pawn.Map)
                    : roofAndMountainBlocker;
                return true;
            }

            try
            {
                PathRequest.IPathGridCustomizer finalCustomizer = customizer != null
                    ? new CombinedPathGridCustomizer(customizer, roofAndMountainBlocker, pawn.Map)
                    : (PathRequest.IPathGridCustomizer)roofAndMountainBlocker;

                var tp = TraverseParms.For(
                    pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings,
                    canBashDoors: true, alwaysUseAvoidGrid: false,
                    canBashFences: true, avoidPersistentDanger: false);

                __result = CreateRequestOverload.Invoke(__instance,
                    new object[] { start, target, dest, tp, CachedTuningNullable, peMode, pawn, finalCustomizer });
                return false;
            }
            catch (Exception e)
            {
                PonyLog.ErrorCaught("Полёт: не удалось построить маршрут для летящей пешки.", e);
                return true;
            }
        }

        private static bool FlightCanReachTarget(Pawn pawn, IntVec3 start, LocalTargetInfo target, PathEndMode peMode)
        {
            Map map = pawn.Map;
            if (map == null || !target.IsValid)
                return true;

            try
            {
                return PegasusFlightUtility.CanFlyOverObstacles(pawn)
                    ? PegasusFlightReachability.CanReachByFlight(map, start, target, peMode)
                    : PegasusFlightReachability.CanReachUnderOpenSky(map, start, target, peMode);
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой проверки цели маршрута по воздуху — маршрут строится как раньше.", e);
                return true;
            }
        }

        private static string DescribeTarget(LocalTargetInfo target)
        {
            Thing thing = target.Thing;
            return thing != null ? $"{thing.LabelShortCap} ({thing.ThingID})" : target.Cell.ToString();
        }
    }

    public static class Patch_Reachability_CanReach
    {
        public static void Postfix(Reachability __instance, IntVec3 start, LocalTargetInfo dest,
            PathEndMode peMode, TraverseParms traverseParams, ref bool __result)
        {
            Pawn pawn = traverseParams.pawn;
            if (pawn == null)
            {
                pawn = PegasusFlightReachability.TakePatherContext();
                if (pawn == null || start != pawn.Position)
                    return;
            }

            if (__result || PegasusFlightReachability.GroundOnlyActive)
                return;
            if (traverseParams.mode != TraverseMode.ByPawn && traverseParams.mode != TraverseMode.PassDoors)
                return;
            if (!PegasusFlightUtility.IsFlyingOverObstacles(pawn))
                return;

            Map map = pawn.Map;
            if (map == null || map.reachability != __instance)
                return;

            try
            {
                if (PegasusFlightReachability.CanReachByFlight(map, start, dest, peMode))
                    __result = true;
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой проверки достижимости по воздуху — оставлен ванильный результат.", e);
            }
        }
    }

    public static class Patch_Reachability_CanReachMapEdge
    {
        private static bool _flipTraced;

        public static void Postfix(Reachability __instance, IntVec3 c, TraverseParms traverseParms, ref bool __result)
        {
            if (__result || PegasusFlightReachability.GroundOnlyActive)
                return;

            Pawn pawn = traverseParms.pawn;
            if (pawn == null)
                return;
            if (traverseParms.mode != TraverseMode.ByPawn && traverseParms.mode != TraverseMode.PassDoors)
                return;
            if (!PegasusFlightUtility.IsFlyingOverObstacles(pawn))
                return;

            Map map = pawn.Map;
            if (map == null || map.reachability != __instance)
                return;

            try
            {
                if (!PegasusFlightReachability.CanReachMapEdgeByFlight(map, c))
                    return;

                __result = true;

                if (!_flipTraced && PonyLog.Verbose)
                {
                    _flipTraced = true;
                    PonyLog.Trace($"Полёт: у {pawn.LabelShortCap} нет пути к краю карты по земле, но есть по воздуху — " +
                                  "CanReachMapEdge засчитан как «да» (сообщение выводится один раз).");
                }
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой проверки выхода к краю карты по воздуху — оставлен ванильный результат.", e);
            }
        }
    }

    public static class Patch_PathFollower_StartPath
    {
        public static MethodBase FindTargetMethod()
        {
            MethodInfo fallback = null;
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Pawn_PathFollower)))
            {
                if (m.Name != nameof(Pawn_PathFollower.StartPath))
                    continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length >= 2 &&
                    ps[0].ParameterType == typeof(LocalTargetInfo) &&
                    ps[1].ParameterType == typeof(PathEndMode))
                    return m;
                fallback ??= m;
            }
            return fallback;
        }

        public static void Prefix(Pawn ___pawn, out Pawn __state)
        {
            __state = PegasusFlightReachability.ExchangePatherContext(
                PegasusFlightUtility.IsPegasusConstantFlight(___pawn) ? ___pawn : null);
        }

        public static void Finalizer(Pawn __state)
        {
            PegasusFlightReachability.RestorePatherContext(__state);
        }
    }


    public static class Patch_GenGrid_WalkableBy
    {
        public static void Postfix(IntVec3 c, Map map, Pawn pawn, ref bool __result)
        {
            if (__result || map == null)
            {
                return;
            }
            if (PegasusFlightUtility.CanFlyToCell(pawn, c, map) && PegasusFlightUtility.CanFlyOverObstacles(pawn))
            {
                __result = true;
            }
        }
    }
    public static class Patch_CastPositionFinder_EvaluateCell
    {
        public static bool Prefix(IntVec3 c, ref CastPositionRequest ___req)
        {
            try
            {
                if (___req.maxRegions <= 0)
                    return true;

                Pawn caster = ___req.caster;
                if (caster == null || !caster.Spawned || !caster.Flying)
                    return true;

                return c.GetRegion(caster.Map, RegionType.Set_Passable) != null;
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой проверки клетки при поиске позиции для стрельбы.", e);
                return true;
            }
        }
    }

    public static class Patch_BuildingBlockingNextPathCell
    {
        public static void Postfix(Pawn ___pawn, IntVec3 ___nextCell, PawnPath ___curPath, ref Building __result)
        {
            if (__result == null || !PegasusFlightUtility.IsFlyingOverObstacles(___pawn))
            {
                return;
            }
            if (__result.def?.building != null && __result.def.building.isNaturalRock)
            {
                return;
            }
            if (!CanFlyOverPathCell(___pawn, ___nextCell, ___curPath))
            {
                return;
            }
            __result = null;
        }

        internal static bool CanFlyOverPathCell(Pawn pawn, IntVec3 cell, PawnPath path)
        {
            Map map = pawn?.Map;
            if (map == null || !cell.IsValid || !cell.InBounds(map) || !PegasusFlightUtility.CanFlyToCell(pawn, cell, map))
            {
                return false;
            }
            if (path == null || !path.Found || path.NodesLeftCount < 2 || path.Peek(0) != cell)
            {
                return true;
            }
            IntVec3 after = path.Peek(1);
            return !after.InBounds(map) || PegasusFlightUtility.CanFlyToCell(pawn, after, map);
        }
    }

    public static class Patch_NextCellDoor
    {
        public static void Postfix(Pawn ___pawn, IntVec3 ___nextCell, PawnPath ___curPath, ref Building_Door __result)
        {
            if (__result == null || !PegasusFlightUtility.IsFlyingOverObstacles(___pawn))
            {
                return;
            }
            if (!Patch_BuildingBlockingNextPathCell.CanFlyOverPathCell(___pawn, ___nextCell, ___curPath))
            {
                return;
            }
            __result = null;
        }
    }

    public static class Patch_TryEnterNextPathCell_BlockFog
    {
        private static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> pawnRef = CreatePawnRef();
        private static readonly AccessTools.FieldRef<Pawn_PathFollower, IntVec3> nextCellRef = CreateNextCellRef();

        private static AccessTools.FieldRef<Pawn_PathFollower, Pawn> CreatePawnRef()
        {
            try { return AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn"); }
            catch { return null; }
        }

        private static AccessTools.FieldRef<Pawn_PathFollower, IntVec3> CreateNextCellRef()
        {
            try { return AccessTools.FieldRefAccess<Pawn_PathFollower, IntVec3>("nextCell"); }
            catch { return null; }
        }

        public static bool Prefix(Pawn_PathFollower __instance)
        {
            if (pawnRef == null || nextCellRef == null) return true;

            Pawn pawn = pawnRef(__instance);
            if (!PegasusFlightUtility.IsPegasusConstantFlight(pawn)) return true;

            IntVec3 next = nextCellRef(__instance);
            if (!next.IsValid || pawn.Map == null) return true;

            if (next.Fogged(pawn.Map))
            {
                pawn.pather?.StopDead();
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true);

                Messages.Message(
                    $"{pawn.LabelShort} не может лететь в неразведанную область!",
                    pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (next.InBounds(pawn.Map) && next.Roofed(pawn.Map))
                return LandAtRoofEdge(pawn);

            return true;
        }

        private static bool LandAtRoofEdge(Pawn pawn)
        {
            CompPegasusFlightToggle toggle = PonyFlightCache.GetToggle(pawn);
            if (toggle == null)
                return true;

            bool overGround = pawn.Position.Walkable(pawn.Map);
            toggle.LandUnderRoof();

            if (PonyLog.Verbose)
                PonyLog.TraceOnce("Flight.LandAtRoofEdge",
                    $"Полёт: {pawn.LabelShortCap} сел у края крыши — " +
                    (overGround ? "дальше идёт пешком." : "висел над постройкой, перенесён на землю.") +
                    " Сообщение выводится один раз.");

            return overGround;
        }
    }


    public static class Patch_TraverseParms_AvoidFog
    {
        public static void Postfix(Pawn pawn, ref TraverseParms __result)
        {
            if (pawn != null && PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                __result.avoidFog = true;
        }
    }


    public static class Patch_GetPathContext
    {
        public static void Postfix(Pawn __instance, Pathing pathing, ref PathingContext __result)
        {
            if (PegasusFlightUtility.IsPegasusConstantFlight(__instance))
                __result = pathing.Normal;
        }
    }


    public static class Patch_BodyAddon_CanDrawAddon
    {
        private static int _threadReported;
        private static readonly string[] OurWingPathFragments = new string[4]
        {
            "wing_pegasus_", "wing_broken_", "wing_archotech_", "wing_bionic_"
        };

        private static readonly ConcurrentDictionary<string, bool> WingAddonVerdict =
            new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

        private static readonly Func<string, bool> ComputeIsOurWingDelegate = ComputeIsOurWing;

        public static void Postfix(AlienPartGenerator.BodyAddon __instance, Pawn pawn, ref bool __result)
        {

            PonyThreadGuard.ReportIfOffMain("BodyAddon.CanDrawAddon postfix", ref _threadReported);
            if (!__result || pawn == null || __instance == null || !pawn.HasWings())
            {
                return;
            }
            string path = __instance.path;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            if (!WingAddonVerdict.GetOrAdd(path, ComputeIsOurWingDelegate))
            {
                return;
            }
            CompPegasusFlightToggle toggle = PonyFlightCache.GetToggle(pawn);
            if (toggle != null && toggle.FlightEnabled)
            {
                __result = false;
            }
        }

        public static void ClearCache()
        {
            WingAddonVerdict.Clear();
        }

        private static bool ComputeIsOurWing(string path)
        {
            for (int i = 0; i < OurWingPathFragments.Length; i++)
            {
                if (path.IndexOf(OurWingPathFragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }


    public static class Patch_Pawn_ExposeData_SavePositionFix
    {
        private static readonly FieldInfo fiPositionInt =
            AccessTools.Field(typeof(Thing), "positionInt");

        private static readonly Dictionary<Pawn, IntVec3> savedOriginalPositions = new();

        public static void Prefix(Pawn __instance)
        {
            try
            {
                if (Scribe.mode != LoadSaveMode.Saving)
                    return;

                if (fiPositionInt == null)
                    return;

                var comp = __instance.GetComp<CompPegasusFlightToggle>();
                if (comp == null || !comp.FlightEnabled)
                    return;

                if (__instance.Map == null)
                    return;

                IntVec3 pos = __instance.Position;
                if (pos.Walkable(__instance.Map))
                    return;

                IntVec3 safeCell = FindSafeCellForSave(pos, __instance.Map);
                if (!safeCell.IsValid)
                    return;

                savedOriginalPositions[__instance] = pos;
                fiPositionInt.SetValue(__instance, safeCell);
            }
            catch (Exception e)
            {
                PonyLog.ErrorCaught("Полёт: сбой при сохранении позиции летящей пешки.", e);
            }
        }

        public static void Postfix(Pawn __instance)
        {
            try
            {
                if (savedOriginalPositions.TryGetValue(__instance, out IntVec3 original))
                {
                    savedOriginalPositions.Remove(__instance);
                    if (fiPositionInt != null)
                        fiPositionInt.SetValue(__instance, original);
                }
            }
            catch (Exception e)
            {
                PonyLog.ErrorCaught("Полёт: сбой при восстановлении позиции летящей пешки.", e);
            }
        }

        private static IntVec3 FindSafeCellForSave(IntVec3 center, Map map)
        {
            IntVec3 bestCell = IntVec3.Invalid;
            int bestScore = -1;

            for (int radius = 1; radius <= 15; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, radius == 1))
                {
                    if (!cell.InBounds(map) || !cell.Walkable(map))
                        continue;

                    int score = 0;
                    if (!cell.Fogged(map)) score += 2;
                    if (!cell.Roofed(map)) score += 1;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCell = cell;
                    }
                }

                if (bestCell.IsValid)
                    break;
            }

            if (!bestCell.IsValid)
            {
                CellFinder.TryFindRandomCellNear(center, map, 30,
                    c => c.Walkable(map) && !c.Fogged(map), out bestCell);
            }

            return bestCell;
        }
    }

    public static class Patch_SpawnSetup_EnsureNaturalWings
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            try
            {
                PonyFlightCache.Warmup(__instance);

                if (__instance?.health?.hediffSet == null) return;
                if (!__instance.HasWings()) return;
                EnsureWingsBoundToParts(__instance);
            }
            catch (Exception e)
            {
                PonyLog.ErrorCaught("Полёт: не удалось восстановить природные крылья пешки.", e);
            }
        }


        private static void EnsureWingsBoundToParts(Pawn pawn)
        {
            HediffDef naturalWingDef = DefDatabase<HediffDef>.GetNamed("Pony_NaturalWing", errorOnFail: false);
            if (naturalWingDef == null) return;

            var unbound = pawn.health.hediffSet.hediffs
                .Where(h => h.def?.defName == "Pony_NaturalWing" && h.Part == null)
                .ToList();

            foreach (Hediff h in unbound)
                pawn.health.RemoveHediff(h);

            EnsureWingOnPart(pawn, PegasusFlightUtility.LeftWingPartDef, naturalWingDef);
            EnsureWingOnPart(pawn, PegasusFlightUtility.RightWingPartDef, naturalWingDef);
        }

        private static void EnsureWingOnPart(Pawn pawn, string partDefName, HediffDef naturalWingDef)
        {
            BodyPartRecord part = pawn.RaceProps?.body?.AllParts?
                .FirstOrDefault(p => p.def?.defName == partDefName);
            if (part == null) return;
            if (pawn.health.hediffSet.PartIsMissing(part)) return;

            bool hasAnyWing = pawn.health.hediffSet.hediffs.Any(h =>
                h.Part != null &&
                h.Part.def?.defName == partDefName &&
                PegasusFlightUtility.IsWingHediff(h));

            if (hasAnyWing) return;

            pawn.health.AddHediff(naturalWingDef, part);
        }
    }

    public class PegasusFlightPathGridCustomizer : PathRequest.IPathGridCustomizer
    {
        private static readonly Dictionary<int, CachedGrid> gridCache = new();

        internal const int DisposeGraceTicks = 600;
        private const int RebuildIntervalTicks = 120;
        private const int NeverBuilt = -999999;

        private static readonly List<(int dueTick, NativeArray<ushort> grid)> pendingDisposal = new();

        private static int _offMainReported;
        private static bool InSimulation => MultiplayerCompat.TickCacheWritable;

        public static void ScheduleDispose(NativeArray<ushort> grid)
        {
            if (!grid.IsCreated) return;
            pendingDisposal.Add((Find.TickManager.TicksGame + DisposeGraceTicks, grid));
        }

        public static void FlushDueDisposals()
        {
            if (pendingDisposal.Count == 0) return;
            if (!InSimulation) return;

            int now = Find.TickManager.TicksGame;
            for (int i = pendingDisposal.Count - 1; i >= 0; i--)
            {
                var entry = pendingDisposal[i];
                if (entry.dueTick - now > DisposeGraceTicks)
                {
                    pendingDisposal[i] = (now + DisposeGraceTicks, entry.grid);
                    continue;
                }

                if (now >= entry.dueTick)
                {
                    if (entry.grid.IsCreated)
                        entry.grid.Dispose();
                    pendingDisposal.RemoveAt(i);
                }
            }
        }

        private readonly int mapId;

        public PegasusFlightPathGridCustomizer(Map map)
        {
            mapId = map.uniqueID;
            EnsureGrid(map);
        }

        public NativeArray<ushort> GetOffsetGrid()
        {
            if (gridCache.TryGetValue(mapId, out var cached) && cached.grid.IsCreated)
                return cached.grid;

            return default;
        }

        private static void EnsureGrid(Map map)
        {
            PonyThreadGuard.ReportIfOffMain(
                "PegasusFlightPathGridCustomizer.EnsureGrid", ref _offMainReported);

            FlushDueDisposals();

            int id = map.uniqueID;
            bool sim = InSimulation;
            int stamp = sim ? Find.TickManager.TicksGame : NeverBuilt;
            if (gridCache.TryGetValue(id, out var cached) && cached.grid.IsCreated && cached.IsFor(map))
            {
                if (!sim)
                    return;

                if (IsFresh(cached.builtAtTick, stamp))
                    return;
            }
            RebuildGrid(map, id, stamp);
        }

        internal static bool IsFresh(int builtAtTick, int now)
        {
            return now >= builtAtTick && now - builtAtTick < RebuildIntervalTicks;
        }

        private static void RebuildGrid(Map map, int mapId, int tick)
        {
            int numCells = map.cellIndices.NumGridCells;

            System.WeakReference<Map> mapRef = null;
            if (gridCache.TryGetValue(mapId, out var old))
            {
                if (old.grid.IsCreated)
                    ScheduleDispose(old.grid);
                if (old.IsFor(map))
                    mapRef = old.MapRef;
            }
            mapRef ??= new System.WeakReference<Map>(map);

            var grid = new NativeArray<ushort>(numCells, Allocator.Persistent);
            RoofGrid roofGrid = map.roofGrid;
            FogGrid fogGrid = map.fogGrid;
            EdificeGrid edificeGrid = map.edificeGrid;
            PathGrid pathGrid = map.pathing.Normal.pathGrid;
            TerrainGrid terrainGrid = map.terrainGrid;
            int sizeX = map.Size.x;

            for (int i = 0; i < numCells; i++)
            {
                bool blocked = roofGrid.Roofed(i) || fogGrid.IsFogged(i);

                if (!blocked)
                {
                    Building edifice = edificeGrid[i];
                    if (edifice != null && edifice.def?.building != null && edifice.def.building.isNaturalRock)
                        blocked = true;
                }

                if (!blocked && !pathGrid.WalkableFast(i))
                {
                    TerrainDef terrain = terrainGrid.TerrainAt(i);
                    if (terrain != null && terrain.passability == Traversability.Impassable)
                        blocked = true;
                    else if (PegasusFlightReachability.BordersRoofedRoom(map, i % sizeX, i / sizeX))
                        blocked = true;
                }

                grid[i] = blocked ? (ushort)10000 : (ushort)0;
            }

            gridCache[mapId] = new CachedGrid(grid, tick, mapRef);
        }

        public static void DisposeForMap(int mapId)
        {
            if (gridCache.TryGetValue(mapId, out var cached))
            {
                ScheduleDispose(cached.grid);
                gridCache.Remove(mapId);
            }
            FlushDueDisposals();
        }

        public static void DisposeAll()
        {
            var grids = new List<NativeArray<ushort>>(gridCache.Count + pendingDisposal.Count);
            foreach (CachedGrid cached in gridCache.Values)
                grids.Add(cached.grid);
            foreach (var entry in pendingDisposal)
                grids.Add(entry.grid);

            gridCache.Clear();
            pendingDisposal.Clear();

            int disposed = 0;
            for (int i = 0; i < grids.Count; i++)
            {
                NativeArray<ushort> grid = grids[i];
                if (!grid.IsCreated)
                    continue;
                grid.Dispose();
                disposed++;
            }

            if (disposed > 0)
                PonyLog.Trace($"Полёт: при выгрузке игры освобождено сеток полёта — {disposed}.");
        }

        public static void InvalidateCache(int mapId)
        {
            if (gridCache.TryGetValue(mapId, out var cached))
                gridCache[mapId] = cached.WithTick(NeverBuilt);
        }

        private readonly struct CachedGrid
        {
            public readonly NativeArray<ushort> grid;
            public readonly int builtAtTick;

            public readonly System.WeakReference<Map> MapRef;

            public CachedGrid(NativeArray<ushort> grid, int builtAtTick, System.WeakReference<Map> mapRef)
            {
                this.grid = grid;
                this.builtAtTick = builtAtTick;
                MapRef = mapRef;
            }

            public CachedGrid WithTick(int tick) => new CachedGrid(grid, tick, MapRef);

            public bool IsFor(Map map) =>
                MapRef != null && MapRef.TryGetTarget(out Map cachedMap) && ReferenceEquals(cachedMap, map);
        }
    }

    public class CombinedPathGridCustomizer : PathRequest.IPathGridCustomizer
    {
        private readonly PathRequest.IPathGridCustomizer first;
        private readonly PathRequest.IPathGridCustomizer second;
        private readonly int numCells;

        private NativeArray<ushort> combinedGrid;
        private int combinedAtTick;

        public CombinedPathGridCustomizer(
            PathRequest.IPathGridCustomizer first,
            PathRequest.IPathGridCustomizer second,
            Map map)
        {
            this.first = first;
            this.second = second;
            numCells = map.cellIndices.NumGridCells;
        }

        public NativeArray<ushort> GetOffsetGrid()
        {
            if (combinedGrid.IsCreated)
                return IsStillAlive(combinedAtTick, Find.TickManager.TicksGame) ? combinedGrid : default;

            NativeArray<ushort> a = first.GetOffsetGrid();
            NativeArray<ushort> b = second.GetOffsetGrid();
            TraceWrongSize(first, a);
            TraceWrongSize(second, b);

            switch (ChooseSource(a.IsCreated, a.Length, b.IsCreated, b.Length, numCells))
            {
                case GridSource.First:
                    return a;
                case GridSource.Second:
                    return b;
                case GridSource.Sum:
                    break;
                default:
                    return default;
            }

            combinedGrid = new NativeArray<ushort>(numCells, Allocator.Persistent);
            for (int i = 0; i < numCells; i++)
            {
                int sum = a[i] + b[i];
                combinedGrid[i] = sum > ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
            }
            combinedAtTick = Find.TickManager.TicksGame;
            PegasusFlightPathGridCustomizer.ScheduleDispose(combinedGrid);

            PonyLog.TraceOnce("CombinedPathGridCustomizer|" + first.GetType().FullName,
                $"Полёт: сетка полёта объединена с чужой ({first.GetType().FullName}).");
            return combinedGrid;
        }

        internal static bool IsStillAlive(int createdAtTick, int now) =>
            now >= createdAtTick && now - createdAtTick < PegasusFlightPathGridCustomizer.DisposeGraceTicks;

        private void TraceWrongSize(PathRequest.IPathGridCustomizer source, NativeArray<ushort> grid)
        {
            if (grid.IsCreated && grid.Length > 0 && grid.Length != numCells)
                PonyLog.TraceOnce("CombinedPathGridCustomizer.WrongSize|" + source.GetType().FullName,
                    $"Полёт: сетка {source.GetType().FullName} не совпадает с размером карты ({grid.Length} вместо {numCells}) — не используется.");
        }

        internal enum GridSource
        {
            None,
            First,
            Second,
            Sum
        }

        internal static GridSource ChooseSource(bool firstCreated, int firstLength,
            bool secondCreated, int secondLength, int numCells)
        {
            bool hasFirst = firstCreated && firstLength == numCells && numCells > 0;
            bool hasSecond = secondCreated && secondLength == numCells && numCells > 0;

            if (hasFirst && hasSecond)
                return GridSource.Sum;
            if (hasFirst)
                return GridSource.First;
            if (hasSecond)
                return GridSource.Second;
            return GridSource.None;
        }
    }


    public static class Patch_Game_DeinitAndRemoveMap
    {
        public static void Postfix(Map map)
        {
            if (map != null)
                PegasusFlightPathGridCustomizer.DisposeForMap(map.uniqueID);
        }
    }

    public static class Patch_Game_Dispose
    {
        public static void Postfix()
        {
            try
            {
                PegasusFlightPathGridCustomizer.DisposeAll();
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: не удалось освободить сетки полёта при выгрузке игры.", e);
            }
        }
    }


    public static class Patch_PawnDrawTracker_DrawPos_BodyBob
    {
        private static readonly AccessTools.FieldRef<Pawn_DrawTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_DrawTracker, Pawn>("pawn");

        private const float BobAmplitude = 0.012f;

        public static void Postfix(Pawn_DrawTracker __instance, ref Vector3 __result)
        {
            try
            {
                Pawn pawn = PawnRef(__instance);
                if (pawn == null || !pawn.HasWings())
                    return;

                CompPegasusFlightToggle toggle = PonyFlightCache.GetToggle(pawn);
                if (toggle == null || !toggle.FlightEnabled)
                    return;

                if (!pawn.Spawned || pawn.Map == null)
                    return;
                if (pawn.GetPosture() != PawnPosture.Standing)
                    return;

                __result.z += PegasusWingAnimation.BodyBobZ(BobAmplitude);
            }
            catch (Exception e)
            {
                PonyLog.WarnCaught("Полёт: сбой анимации покачивания в полёте.", e);
            }
        }
    }

}