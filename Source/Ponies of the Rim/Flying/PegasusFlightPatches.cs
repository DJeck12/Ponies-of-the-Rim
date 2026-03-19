using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
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
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            try
            {
                if (__result) return;
                if (!PegasusFlightUtil.IsPegasusConstantFlight(__instance)) return;
                __result = true;
            }
            catch (Exception e)
            {
                Log.Warning($"[PoniesOfTheRim] Patch_Pawn_Flying.Postfix: {e}");
            }
        }
    }

    public static class Patch_FlightTracker_FlightTick
    {
        private static readonly FieldInfo fiPawn =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");
        private static readonly FieldInfo fiFlightState =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightState");
        private static readonly FieldInfo fiCooldown =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightCooldownTicks");

        private static object _flyingEnumValue;

        public static bool Prefix(Pawn_FlightTracker __instance)
        {
            try
            {
                var pawn = fiPawn?.GetValue(__instance) as Pawn;
                if (!PegasusFlightUtil.IsPegasusConstantFlight(pawn))
                    return true;
                if (pawn == null || pawn.Dead || pawn.Downed)
                    return true;

                if (fiFlightState != null)
                {
                    if (_flyingEnumValue == null)
                        _flyingEnumValue = Enum.Parse(fiFlightState.FieldType, "Flying");

                    object cur = fiFlightState.GetValue(__instance);
                    if (!Equals(cur, _flyingEnumValue))
                        fiFlightState.SetValue(__instance, _flyingEnumValue);
                }

                if (fiCooldown != null)
                    fiCooldown.SetValue(__instance, 0);

                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"[PoniesOfTheRim] Patch_FlightTracker_FlightTick.Prefix: {e}");
                return true;
            }
        }
    }


    public static class Patch_FlightTracker_ForceLand
    {
        private static readonly FieldInfo fiPawn =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        public static bool Prefix(Pawn_FlightTracker __instance)
        {
            try
            {
                var pawn = fiPawn?.GetValue(__instance) as Pawn;
                if (!PegasusFlightUtil.IsPegasusConstantFlight(pawn)) return true;
                if (pawn == null || pawn.Dead || pawn.Downed) return true;
                return false;
            }
            catch (Exception e)
            {
                Log.Warning($"[PoniesOfTheRim] Patch_FlightTracker_ForceLand.Prefix: {e}");
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

            if (!PegasusFlightUtil.CanFlyToCell(pawn, c, map)) return;

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
                    case LocomotionUrgency.Amble:  desired = Mathf.Max(desired * 3f, 60f);       break;
                    case LocomotionUrgency.Walk:   desired = Mathf.Max(desired * 2f, 50f);       break;
                    case LocomotionUrgency.Jog:    break;
                    case LocomotionUrgency.Sprint: desired = Mathf.RoundToInt(desired * 0.75f);  break;
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
                FI_costBlockedWallBase            = TuningType.GetField("costBlockedWallBase");
                FI_costBlockedWallExtraPerHitPoint = TuningType.GetField("costBlockedWallExtraPerHitPoint");
                FI_costBlockedDoor                 = TuningType.GetField("costBlockedDoor");
                FI_costBlockedDoorPerHitPoint      = TuningType.GetField("costBlockedDoorPerHitPoint");
                FI_costOffLordWalkGrid             = TuningType.GetField("costOffLordWalkGrid");
                FI_costDanger                      = TuningType.GetField("costDanger");
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
            Pawn pawn, PathEndMode peMode, PathRequest.IPathGridCustomizer customizer)
        {
            if (!PegasusFlightUtil.IsPegasusConstantFlight(pawn))
                return true;
            if (CreateRequestOverload == null || TuningType == null)
                return true;

            var roofAndMountainBlocker = new PegasusFlightPathGridCustomizer(pawn.Map);

            CombinedPathGridCustomizer combined = null;
            try
            {
                PathRequest.IPathGridCustomizer finalCustomizer = customizer != null
                    ? (combined = new CombinedPathGridCustomizer(customizer, roofAndMountainBlocker))
                    : (PathRequest.IPathGridCustomizer)roofAndMountainBlocker;

                var tp = TraverseParms.For(
                    pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings,
                    canBashDoors: true, alwaysUseAvoidGrid: false,
                    canBashFences: true, avoidPersistentDanger: false);

                object tuning = Activator.CreateInstance(TuningType);

                FI_costBlockedWallBase?.SetValue(tuning, 0);
                FI_costBlockedWallExtraPerHitPoint?.SetValue(tuning, 0f);
                FI_costBlockedDoor?.SetValue(tuning, 0);
                FI_costBlockedDoorPerHitPoint?.SetValue(tuning, 0f);
                FI_costOffLordWalkGrid?.SetValue(tuning, 0);
                FI_costDanger?.SetValue(tuning, 0);

                object tuningNullable = Activator.CreateInstance(TuningNullableType, tuning);

                __result = CreateRequestOverload.Invoke(__instance,
                    new object[] { start, target, dest, tp, tuningNullable, peMode, pawn, finalCustomizer });
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"[PoniesOfTheRim] Patch_PathFinder_CreateRequest.Prefix: {e}");
                return true;
            }
            finally
            {
                combined?.Dispose();
            }
        }
    }


    public static class Patch_GenGrid_WalkableBy
    {
        public static bool Prefix(IntVec3 c, Map map, Pawn pawn, ref bool __result)
        {
            if (!PegasusFlightUtil.CanFlyToCell(pawn, c, map))
                return true;

            __result = true;
            return false;
        }
    }

    public static class Patch_BuildingBlockingNextPathCell
    {
        public static bool Prefix(Pawn ___pawn, ref Building __result)
        {
            if (!PegasusFlightUtil.IsPegasusConstantFlight(___pawn))
                return true;

            var nextCell = ___pawn.pather?.nextCell ?? IntVec3.Invalid;
            if (nextCell.IsValid && ___pawn.Map != null)
            {
                var edifice = nextCell.GetEdifice(___pawn.Map);
                if (edifice != null && edifice.def?.building != null && edifice.def.building.isNaturalRock)
                    return true;
            }

            __result = null;
            return false;
        }
    }

    public static class Patch_NextCellDoor
    {
        private static readonly FieldInfo fiPawn =
            AccessTools.Field(typeof(Pawn_PathFollower), "pawn");

        public static bool Prefix(Pawn_PathFollower __instance, ref Building_Door __result)
        {
            var pawn = fiPawn?.GetValue(__instance) as Pawn;
            if (!PegasusFlightUtil.IsPegasusConstantFlight(pawn))
                return true;

            __result = null;
            return false;
        }
    }


    public static class Patch_TryEnterNextPathCell_BlockFog
    {
        private static readonly FieldInfo fiPawn =
            AccessTools.Field(typeof(Pawn_PathFollower), "pawn");
        private static readonly FieldInfo fiNextCell =
            AccessTools.Field(typeof(Pawn_PathFollower), "nextCell");

        public static bool Prefix(Pawn_PathFollower __instance)
        {
            if (fiPawn == null || fiNextCell == null) return true;

            var pawn = fiPawn.GetValue(__instance) as Pawn;
            if (!PegasusFlightUtil.IsPegasusConstantFlight(pawn)) return true;

            var next = (IntVec3)fiNextCell.GetValue(__instance);
            if (!next.IsValid || pawn?.Map == null) return true;

            if (next.Fogged(pawn.Map))
            {
                pawn.pather?.StopDead();
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true);

                Messages.Message(
                    $"{pawn.LabelShort} не может лететь в неразведанную область!",
                    pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            return true;
        }
    }


    public static class Patch_TraverseParms_AvoidFog
    {
        public static void Postfix(Pawn pawn, ref TraverseParms __result)
        {
            if (pawn != null && PegasusFlightUtil.IsPegasusConstantFlight(pawn))
                __result.avoidFog = true;
        }
    }


    public static class Patch_GetPathContext
    {
        public static void Postfix(Pawn __instance, Pathing pathing, ref PathingContext __result)
        {
            if (PegasusFlightUtil.IsPegasusConstantFlight(__instance))
                __result = pathing.Normal;
        }
    }


    public static class Patch_RenderNode_ForceWingsRefresh
    {
        public static void Prefix(PawnRenderNode __instance)
        {
            if (__instance is PonyRenderNodePegasusWings)
                __instance.requestRecache = true;
        }
    }


    public static class Patch_BodyAddon_CanDrawAddon
    {
        private static readonly string[] OurWingPathFragments =
        {
            "wing_pegasus_",
            "wing_broken_",
            "wing_archotech_",
            "wing_bionic_",
        };

        public static void Postfix(AlienPartGenerator.BodyAddon __instance, Pawn pawn, ref bool __result)
        {
            if (!__result || pawn == null) return;

            var path = __instance.path;
            if (string.IsNullOrEmpty(path)) return;

            string pathLower = path.ToLowerInvariant();
            bool isOurWing = false;

            for (int i = 0; i < OurWingPathFragments.Length; i++)
            {
                if (pathLower.Contains(OurWingPathFragments[i]))
                {
                    isOurWing = true;
                    break;
                }
            }

            if (!isOurWing) return;
            if (!pawn.HasWings()) return;

            var comp = pawn.TryGetComp<CompPegasusFlightToggle>();
            if (comp != null && comp.FlightEnabled)
                __result = false;
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
                Log.Error($"[PoniesOfTheRim] ExposeData save position fix Prefix failed: {e}");
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
                Log.Error($"[PoniesOfTheRim] ExposeData save position fix Postfix failed: {e}");
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
                if (__instance?.health?.hediffSet == null) return;
                if (!__instance.HasWings()) return;
                EnsureWingsBoundToParts(__instance);
            }
            catch (Exception e)
            {
                Log.Error($"[PoniesOfTheRim] EnsureNaturalWings failed: {e}");
            }
        }

        private static void EnsureWingsBoundToParts(Pawn pawn)
        {
            HediffDef naturalWingDef = DefDatabase<HediffDef>.GetNamed("NaturalWing", errorOnFail: false);
            if (naturalWingDef == null) return;

            var unbound = pawn.health.hediffSet.hediffs
                .Where(h => h.def?.defName == "NaturalWing" && h.Part == null)
                .ToList();

            foreach (Hediff h in unbound)
                pawn.health.RemoveHediff(h);

            EnsureWingOnPart(pawn, PegasusFlightUtil.LeftWingPartDef, naturalWingDef);
            EnsureWingOnPart(pawn, PegasusFlightUtil.RightWingPartDef, naturalWingDef);
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
                h.def != null &&
                PegasusFlightUtil.WingHediffDefs.Contains(h.def.defName));

            if (hasAnyWing) return;

            pawn.health.AddHediff(naturalWingDef, part);
        }
    }

    public class PegasusFlightPathGridCustomizer : PathRequest.IPathGridCustomizer
    {
        private static readonly Dictionary<int, CachedGrid> gridCache = new();

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
            int id = map.uniqueID;
            int tick = Find.TickManager.TicksGame;

            if (gridCache.TryGetValue(id, out var cached))
            {
                if (cached.grid.IsCreated && tick - cached.builtAtTick < 120)
                    return;
            }

            RebuildGrid(map, id, tick);
        }

        private static void RebuildGrid(Map map, int mapId, int tick)
        {
            int numCells = map.cellIndices.NumGridCells;

            if (gridCache.TryGetValue(mapId, out var old) && old.grid.IsCreated)
                old.grid.Dispose();

            var grid = new NativeArray<ushort>(numCells, Allocator.Persistent);

            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = map.cellIndices.IndexToCell(i);
                bool blocked = false;

                if (cell.Roofed(map))
                    blocked = true;

                if (!blocked && cell.Fogged(map))
                    blocked = true;

                if (!blocked)
                {
                    var edifice = cell.GetEdifice(map);
                    if (edifice != null && edifice.def?.building != null && edifice.def.building.isNaturalRock)
                        blocked = true;
                }

                if (!blocked && !cell.Walkable(map))
                {
                    var terrain = cell.GetTerrain(map);
                    if (terrain != null && terrain.passability == Traversability.Impassable)
                        blocked = true;
                }

                grid[i] = blocked ? (ushort)10000 : (ushort)0;
            }

            gridCache[mapId] = new CachedGrid(grid, tick);
        }

        public static void DisposeForMap(int mapId)
        {
            if (gridCache.TryGetValue(mapId, out var cached))
            {
                if (cached.grid.IsCreated)
                    cached.grid.Dispose();
                gridCache.Remove(mapId);
            }
        }

        public static void InvalidateCache(int mapId)
        {
            if (gridCache.TryGetValue(mapId, out var cached))
                gridCache[mapId] = new CachedGrid(cached.grid, -9999);
        }

        private readonly struct CachedGrid
        {
            public readonly NativeArray<ushort> grid;
            public readonly int builtAtTick;

            public CachedGrid(NativeArray<ushort> grid, int builtAtTick)
            {
                this.grid = grid;
                this.builtAtTick = builtAtTick;
            }
        }
    }

    public class CombinedPathGridCustomizer : PathRequest.IPathGridCustomizer, IDisposable
    {
        private NativeArray<ushort> combinedGrid;

        public CombinedPathGridCustomizer(
            PathRequest.IPathGridCustomizer first,
            PathRequest.IPathGridCustomizer second)
        {
            var grid1 = first.GetOffsetGrid();
            var grid2 = second.GetOffsetGrid();

            combinedGrid = new NativeArray<ushort>(grid1.Length, Allocator.TempJob);

            for (int i = 0; i < combinedGrid.Length; i++)
            {
                int combined = grid1[i] + grid2[i];
                combinedGrid[i] = (ushort)Mathf.Min(combined, ushort.MaxValue);
            }
        }

        public NativeArray<ushort> GetOffsetGrid() => combinedGrid;

        public void Dispose()
        {
            if (combinedGrid.IsCreated)
                combinedGrid.Dispose();
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


    public static class Patch_PawnCapacityUtility_BodyCanEverDoCapacity
    {
        private const string FlightCapDefName = "Pegasus_Flight";

        public static void Postfix(BodyDef bodyDef, PawnCapacityDef capacity, ref bool __result)
        {
            try
            {
                if (!__result || capacity?.defName != FlightCapDefName)
                    return;

                if (bodyDef?.AllParts == null)
                {
                    __result = false;
                    return;
                }

                bool hasWingPart = false;
                foreach (BodyPartRecord part in bodyDef.AllParts)
                {
                    var defName = part.def?.defName;
                    if (defName == PegasusFlightUtil.LeftWingPartDef ||
                        defName == PegasusFlightUtil.RightWingPartDef)
                    {
                        hasWingPart = true;
                        break;
                    }
                }

                if (!hasWingPart)
                    __result = false;
            }
            catch (Exception e)
            {
                Log.Warning($"[PoniesOfTheRim] Patch_PawnCapacityUtility_BodyCanEverDoCapacity.Postfix: {e}");
            }
        }
    }


    public static class Patch_ITab_Pawn_Health_FillTab
    {
        private static readonly FieldInfo FiSize =
            AccessTools.Field(typeof(ITab), "size");

        private static readonly PropertyInfo PropSelPawn =
            AccessTools.Property(typeof(ITab), "SelPawn");

        private const float ExtraHeight = 26f;

        private static Vector2 _originalSize;
        private static bool    _sizeChanged;

        public static void Prefix(ITab __instance)
        {
            _sizeChanged = false;
            try
            {
                if (FiSize == null) return;

                Pawn pawn = PropSelPawn?.GetValue(__instance) as Pawn;
                if (pawn == null || !pawn.HasWings()) return;

                _originalSize = (Vector2)FiSize.GetValue(__instance);
                FiSize.SetValue(__instance,
                    new Vector2(_originalSize.x, _originalSize.y + ExtraHeight));
                _sizeChanged = true;
            }
            catch (Exception e)
            {
                Log.Warning($"[PoniesOfTheRim] Patch_ITab_Pawn_Health_FillTab.Prefix: {e}");
            }
        }

        public static void Postfix(ITab __instance)
        {
            if (!_sizeChanged || FiSize == null) return;
            _sizeChanged = false;
            try
            {
                FiSize.SetValue(__instance, _originalSize);
            }
            catch (Exception e)
            {
                Log.Warning($"[PoniesOfTheRim] Patch_ITab_Pawn_Health_FillTab.Postfix: {e}");
            }
        }
    }
}