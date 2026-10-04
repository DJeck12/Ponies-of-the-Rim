using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PoniesOfTheRim.Multiplayer;
using RimWorld;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    public static class PegasusFlightReachability
    {
        private const int RebuildIntervalTicks = 120;
        private const int NeverBuilt = int.MinValue;

        internal const byte FlagFlyable = 1;
        internal const byte FlagHardBlocked = 2;
        internal const byte FlagWalkable = 4;
        internal const byte FlagDoor = 8;

        internal const byte GroundNodeMask = FlagFlyable | FlagWalkable;

        private sealed class MapSnapshot
        {
            public int[] labels;
            public byte[] flags;
            public readonly HashSet<int> edgeLabels = new HashSet<int>();
            public int sizeX;
            public int sizeZ;
            public int builtAtTick = NeverBuilt;
            public bool built;
            public int version;

            public int[] groundLabels;
            public int groundVersion = -1;
        }

        private static readonly ConditionalWeakTable<Map, MapSnapshot> Snapshots =
            new ConditionalWeakTable<Map, MapSnapshot>();

        private static readonly ConditionalWeakTable<Map, MapSnapshot>.CreateValueCallback CreateSnapshot =
            _ => new MapSnapshot();

        private static int _groundOnlyDepth;
        private static Pawn _patherContext;
        private static int _rebuildOffMainReported;

        public static bool GroundOnlyActive => _groundOnlyDepth > 0;

        public static GroundOnlyScope GroundOnly()
        {
            _groundOnlyDepth++;
            return new GroundOnlyScope(true);
        }

        public struct GroundOnlyScope : IDisposable
        {
            private readonly bool active;

            internal GroundOnlyScope(bool active)
            {
                this.active = active;
            }

            public void Dispose()
            {
                if (active && _groundOnlyDepth > 0)
                    _groundOnlyDepth--;
            }
        }

        internal static Pawn ExchangePatherContext(Pawn pawn)
        {
            Pawn previous = _patherContext;
            _patherContext = pawn;
            return previous;
        }

        internal static void RestorePatherContext(Pawn previous)
        {
            _patherContext = previous;
        }

        internal static Pawn TakePatherContext()
        {
            Pawn pawn = _patherContext;
            if (pawn != null)
                _patherContext = null;
            return pawn;
        }

        public static bool CanReachByFlight(Map map, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode)
        {
            return CanReach(map, start, dest, peMode, overObstacles: true);
        }

        public static bool CanReachUnderOpenSky(Map map, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode)
        {
            return CanReach(map, start, dest, peMode, overObstacles: false);
        }

        private static bool CanReach(Map map, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, bool overObstacles)
        {
            if (map == null || !dest.IsValid)
                return false;
            if (!UnityData.IsInMainThread)
                return false;
            if (dest.HasThing && dest.Thing.Map != map)
                return false;
            if (!start.InBounds(map) || !dest.Cell.InBounds(map))
                return false;

            int minX, minZ, maxX, maxZ;
            bool touch;
            switch (peMode)
            {
                case PathEndMode.OnCell:
                    minX = maxX = dest.Cell.x;
                    minZ = maxZ = dest.Cell.z;
                    touch = false;
                    break;

                case PathEndMode.InteractionCell:
                    {
                        Thing thing = dest.Thing;
                        if (thing?.def == null || !thing.def.hasInteractionCell)
                            return false;
                        IntVec3 cell = thing.InteractionCell;
                        minX = maxX = cell.x;
                        minZ = maxZ = cell.z;
                        touch = false;
                        break;
                    }

                case PathEndMode.Touch:
                case PathEndMode.ClosestTouch:
                    {
                        CellRect rect = dest.HasThing ? dest.Thing.OccupiedRect() : CellRect.SingleCell(dest.Cell);
                        minX = rect.minX;
                        minZ = rect.minZ;
                        maxX = rect.maxX;
                        maxZ = rect.maxZ;
                        touch = true;
                        break;
                    }

                default:
                    return false;
            }

            if (!IsOpenSky(map, start.x, start.z))
                return false;
            if (!AnyOpenSkyLanding(map, minX, minZ, maxX, maxZ, touch))
                return false;

            MapSnapshot snap = overObstacles ? GetSnapshot(map) : GetGroundSnapshot(map);
            int[] labels = overObstacles ? snap.labels : snap.groundLabels;
            int startLabel = labels[start.z * snap.sizeX + start.x];
            if (startLabel == 0)
                return false;

            return touch
                ? CanTouchRect(labels, snap.flags, snap.sizeX, snap.sizeZ, minX, minZ, maxX, maxZ, startLabel)
                : CanLandAt(labels, snap.flags, snap.sizeX, snap.sizeZ, minX, minZ, startLabel);
        }

        public static bool CanReachMapEdgeByFlight(Map map, IntVec3 start)
        {
            if (map == null || !UnityData.IsInMainThread)
                return false;
            if (!start.InBounds(map) || !IsOpenSky(map, start.x, start.z))
                return false;

            MapSnapshot snap = GetSnapshot(map);
            int startLabel = snap.labels[start.z * snap.sizeX + start.x];
            return startLabel != 0 && snap.edgeLabels.Contains(startLabel);
        }

        private static bool AnyOpenSkyLanding(Map map, int minX, int minZ, int maxX, int maxZ, bool touch)
        {
            int pad = touch ? 1 : 0;
            for (int z = minZ - pad; z <= maxZ + pad; z++)
            {
                for (int x = minX - pad; x <= maxX + pad; x++)
                {
                    if (IsOpenSkyGround(map, x, z))
                        return true;
                }
            }
            return false;
        }

        private static bool IsOpenSkyGround(Map map, int x, int z)
        {
            return IsOpenSky(map, x, z) && map.pathing.Normal.pathGrid.WalkableFast(z * map.Size.x + x);
        }

        private static bool IsOpenSky(Map map, int x, int z)
        {
            IntVec3 size = map.Size;
            if ((uint)x >= (uint)size.x || (uint)z >= (uint)size.z)
                return false;
            int i = z * size.x + x;
            return !map.roofGrid.Roofed(i) && !map.fogGrid.IsFogged(i);
        }

        private static MapSnapshot GetSnapshot(Map map)
        {
            MapSnapshot snap = Snapshots.GetValue(map, CreateSnapshot);
            IntVec3 size = map.Size;
            bool usable = snap.built && snap.sizeX == size.x && snap.sizeZ == size.z;

            if (!MultiplayerCompat.TickCacheWritable)
            {
                if (!usable)
                    Rebuild(map, snap, NeverBuilt);
                return snap;
            }

            int now = Find.TickManager.TicksGame;
            if (usable && snap.builtAtTick != NeverBuilt &&
                now >= snap.builtAtTick && now - snap.builtAtTick < RebuildIntervalTicks)
                return snap;

            Rebuild(map, snap, now);
            return snap;
        }

        private static MapSnapshot GetGroundSnapshot(Map map)
        {
            MapSnapshot snap = GetSnapshot(map);
            int numCells = snap.sizeX * snap.sizeZ;
            if (snap.groundVersion != snap.version || snap.groundLabels == null || snap.groundLabels.Length != numCells)
            {
                if (snap.groundLabels == null || snap.groundLabels.Length != numCells)
                    snap.groundLabels = new int[numCells];
                BuildZones(snap.flags, GroundNodeMask, snap.sizeX, snap.sizeZ, snap.groundLabels);
                snap.groundVersion = snap.version;
            }
            return snap;
        }

        private static void Rebuild(Map map, MapSnapshot snap, int stamp)
        {
            PonyThreadGuard.ReportIfOffMain("PegasusFlightReachability.Rebuild", ref _rebuildOffMainReported);

            Stopwatch sw = PonyLog.Verbose ? Stopwatch.StartNew() : null;

            IntVec3 size = map.Size;
            int sizeX = size.x;
            int sizeZ = size.z;
            int numCells = sizeX * sizeZ;

            if (snap.labels == null || snap.labels.Length != numCells)
                snap.labels = new int[numCells];
            if (snap.flags == null || snap.flags.Length != numCells)
                snap.flags = new byte[numCells];

            FillFlags(map, snap.flags, numCells);
            int zones = BuildZones(snap.flags, sizeX, sizeZ, snap.labels);
            CollectEdgeLabels(snap.labels, snap.flags, sizeX, sizeZ, snap.edgeLabels);

            snap.sizeX = sizeX;
            snap.sizeZ = sizeZ;
            snap.builtAtTick = stamp;
            snap.built = true;
            snap.version++;

            if (sw != null)
            {
                sw.Stop();
                PonyLog.TraceOnce("PegasusFlightReachability.FirstBuild",
                    $"Полёт: зоны достижимости по воздуху построены — карта {sizeX}×{sizeZ}, " +
                    $"зон {zones}, {sw.Elapsed.TotalMilliseconds:F2} мс.");
            }
        }

        private static void FillFlags(Map map, byte[] flags, int numCells)
        {
            RoofGrid roofGrid = map.roofGrid;
            FogGrid fogGrid = map.fogGrid;
            EdificeGrid edificeGrid = map.edificeGrid;
            PathGrid pathGrid = map.pathing.Normal.pathGrid;

            for (int i = 0; i < numCells; i++)
            {
                bool walkable = pathGrid.WalkableFast(i);
                Building edifice = edificeGrid[i];
                ThingDef def = edifice?.def;
                bool naturalRock = def?.building != null && def.building.isNaturalRock;
                bool destroyableBlocker = !walkable && def != null && !naturalRock && def.destroyable && def.useHitPoints;

                byte f = 0;
                if (walkable)
                    f |= FlagWalkable;
                else if (!destroyableBlocker)
                    f |= FlagHardBlocked;

                if (edifice is Building_Door)
                    f |= FlagDoor;

                if (!naturalRock && (walkable || destroyableBlocker) && !roofGrid.Roofed(i) && !fogGrid.IsFogged(i))
                    f |= FlagFlyable;

                flags[i] = f;
            }
        }

        internal static int BuildZones(byte[] flags, int sizeX, int sizeZ, int[] labels)
        {
            return BuildZones(flags, FlagFlyable, sizeX, sizeZ, labels);
        }

        internal static int BuildZones(byte[] flags, byte nodeMask, int sizeX, int sizeZ, int[] labels)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                int row = z * sizeX;
                for (int x = 0; x < sizeX; x++)
                {
                    int i = row + x;
                    if ((flags[i] & nodeMask) != nodeMask)
                    {
                        labels[i] = -1;
                        continue;
                    }

                    bool south = z > 0 && (flags[i - sizeX] & nodeMask) == nodeMask;
                    bool west = x > 0 && (flags[i - 1] & nodeMask) == nodeMask;

                    if (south)
                    {
                        labels[i] = FindRoot(labels, i - sizeX);
                        if (west)
                            UnionRoots(labels, i, i - 1);
                    }
                    else if (west)
                    {
                        labels[i] = FindRoot(labels, i - 1);
                    }
                    else
                    {
                        labels[i] = i;
                    }
                }
            }

            int zones = 0;
            int numCells = sizeX * sizeZ;
            for (int i = 0; i < numCells; i++)
            {
                int p = labels[i];
                if (p < 0)
                {
                    labels[i] = 0;
                }
                else if (p == i)
                {
                    labels[i] = i + 1;
                    zones++;
                }
                else
                {
                    labels[i] = labels[p];
                }
            }
            return zones;
        }

        internal static void CollectEdgeLabels(int[] labels, byte[] flags, int sizeX, int sizeZ, HashSet<int> result)
        {
            result.Clear();
            if (sizeX <= 0 || sizeZ <= 0)
                return;

            int top = (sizeZ - 1) * sizeX;
            for (int x = 0; x < sizeX; x++)
            {
                AddLandableLabel(labels, flags, x, result);
                AddLandableLabel(labels, flags, top + x, result);
            }
            for (int z = 1; z < sizeZ - 1; z++)
            {
                int row = z * sizeX;
                AddLandableLabel(labels, flags, row, result);
                AddLandableLabel(labels, flags, row + sizeX - 1, result);
            }
        }

        private static void AddLandableLabel(int[] labels, byte[] flags, int i, HashSet<int> result)
        {
            int label = labels[i];
            if (label != 0 && (flags[i] & FlagWalkable) != 0)
                result.Add(label);
        }

        internal static bool CanLandAt(int[] labels, byte[] flags, int sizeX, int sizeZ, int x, int z, int startLabel)
        {
            if ((uint)x >= (uint)sizeX || (uint)z >= (uint)sizeZ)
                return false;
            int i = z * sizeX + x;
            return labels[i] == startLabel && (flags[i] & FlagWalkable) != 0;
        }

        internal static bool CanTouchRect(int[] labels, byte[] flags, int sizeX, int sizeZ,
            int minX, int minZ, int maxX, int maxZ, int startLabel)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (CanLandAt(labels, flags, sizeX, sizeZ, x, z, startLabel))
                        return true;
                }
            }

            for (int x = minX; x <= maxX; x++)
            {
                if (CanLandAt(labels, flags, sizeX, sizeZ, x, minZ - 1, startLabel) ||
                    CanLandAt(labels, flags, sizeX, sizeZ, x, maxZ + 1, startLabel))
                    return true;
            }
            for (int z = minZ; z <= maxZ; z++)
            {
                if (CanLandAt(labels, flags, sizeX, sizeZ, minX - 1, z, startLabel) ||
                    CanLandAt(labels, flags, sizeX, sizeZ, maxX + 1, z, startLabel))
                    return true;
            }

            return CornerTouch(labels, flags, sizeX, sizeZ, minX - 1, minZ - 1, minX, minZ - 1, minX - 1, minZ, startLabel)
                || CornerTouch(labels, flags, sizeX, sizeZ, minX - 1, maxZ + 1, minX, maxZ + 1, minX - 1, maxZ, startLabel)
                || CornerTouch(labels, flags, sizeX, sizeZ, maxX + 1, maxZ + 1, maxX, maxZ + 1, maxX + 1, maxZ, startLabel)
                || CornerTouch(labels, flags, sizeX, sizeZ, maxX + 1, minZ - 1, maxX, minZ - 1, maxX + 1, minZ, startLabel);
        }

        private static bool CornerTouch(int[] labels, byte[] flags, int sizeX, int sizeZ,
            int cornerX, int cornerZ, int ax, int az, int bx, int bz, int startLabel)
        {
            if (!CanLandAt(labels, flags, sizeX, sizeZ, cornerX, cornerZ, startLabel))
                return false;
            return IsOpenGround(flags, sizeX, sizeZ, ax, az) || IsOpenGround(flags, sizeX, sizeZ, bx, bz);
        }

        private static bool IsOpenGround(byte[] flags, int sizeX, int sizeZ, int x, int z)
        {
            if ((uint)x >= (uint)sizeX || (uint)z >= (uint)sizeZ)
                return false;
            byte f = flags[z * sizeX + x];
            return (f & FlagWalkable) != 0 && (f & FlagDoor) == 0;
        }

        private static int FindRoot(int[] parent, int x)
        {
            while (true)
            {
                int p = parent[x];
                if (p == x)
                    return x;
                int gp = parent[p];
                parent[x] = gp;
                x = gp;
            }
        }

        private static void UnionRoots(int[] parent, int a, int b)
        {
            int ra = FindRoot(parent, a);
            int rb = FindRoot(parent, b);
            if (ra == rb)
                return;
            if (ra < rb)
                parent[rb] = ra;
            else
                parent[ra] = rb;
        }
    }
}