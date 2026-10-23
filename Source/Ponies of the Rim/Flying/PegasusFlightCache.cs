using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using PoniesOfTheRim.Multiplayer;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class PonyFlightCache
    {
        public readonly struct WingParts
        {
            public readonly BodyPartRecord Left;
            public readonly BodyPartRecord Right;

            public WingParts(BodyPartRecord left, BodyPartRecord right)
            {
                Left = left;
                Right = right;
            }
        }

        private sealed class PawnFlightData
        {
            public BodyDef body;
            public CompPegasusFlightToggle toggle;
            public CompPegasusFlightTimer timer;
            public int usableWingsCheckedAtTick = NeverChecked;
            public bool usableWings;
        }

        private const int NeverChecked = -999999;
        private const int UsableWingsTtlTicks = 30;

        private static readonly HashSet<BodyDef> WingedBodies = new HashSet<BodyDef>();
        private static readonly HashSet<BodyDef> CutiemarkBodies = new HashSet<BodyDef>();
        private static readonly Dictionary<BodyDef, WingParts> WingPartsByBody = new Dictionary<BodyDef, WingParts>();

        private static readonly ConditionalWeakTable<Pawn, PawnFlightData> PerPawn =
            new ConditionalWeakTable<Pawn, PawnFlightData>();

        private static readonly ConditionalWeakTable<Pawn, PawnFlightData>.CreateValueCallback CreateDataDelegate = CreateData;

        private static int _rebuildOffMainReported;

        public static void BuildStaticCaches()
        {
            WingedBodies.Clear();
            CutiemarkBodies.Clear();
            WingPartsByBody.Clear();

            AddBody(WingedBodies, Pony_DefOf.Pony_PegasusBody);
            AddBody(WingedBodies, Pony_DefOf.Pony_BatponyBody);
            AddBody(WingedBodies, Pony_DefOf.Pony_AlicornBody);
            AddBody(WingedBodies, Pony_DefOf.Pony_ChangedlingBody);
            AddBody(WingedBodies, Pony_DefOf.Pony_ChangelingBody);
            AddBody(WingedBodies, Pony_DefOf.Pony_GriffonBody);
            AddBody(WingedBodies, Pony_DefOf.Pony_HippogriffBody);

            AddBody(CutiemarkBodies, Pony_DefOf.Pony_EarthponyBody);
            AddBody(CutiemarkBodies, Pony_DefOf.Pony_UnicornBody);
            AddBody(CutiemarkBodies, Pony_DefOf.Pony_PegasusBody);
            AddBody(CutiemarkBodies, Pony_DefOf.Pony_ZebraBody);
            AddBody(CutiemarkBodies, Pony_DefOf.Pony_CrystalponyBody);
            AddBody(CutiemarkBodies, Pony_DefOf.Pony_BatponyBody);
            AddBody(CutiemarkBodies, Pony_DefOf.Pony_AlicornBody);

            foreach (BodyDef body in WingedBodies)
            {
                BodyPartRecord left = null;
                BodyPartRecord right = null;
                List<BodyPartRecord> parts = body.AllParts;
                for (int i = 0; i < parts.Count; i++)
                {
                    string defName = parts[i].def?.defName;
                    if (defName == PegasusFlightUtility.LeftWingPartDef)
                        left = parts[i];
                    else if (defName == PegasusFlightUtility.RightWingPartDef)
                        right = parts[i];

                    if (left != null && right != null)
                        break;
                }
                WingPartsByBody[body] = new WingParts(left, right);
            }

            if (PonyLog.Verbose)
            {
                int complete = 0;
                foreach (WingParts wings in WingPartsByBody.Values)
                {
                    if (wings.Left != null && wings.Right != null)
                        complete++;
                }
                PonyLog.Trace($"Flight: {WingedBodies.Count} winged bodies, {complete} with both wing parts");
            }
        }

        private static void AddBody(HashSet<BodyDef> set, BodyDef body)
        {
            if (body != null)
                set.Add(body);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWingedBody(BodyDef body)
        {
            return body != null && WingedBodies.Contains(body);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCutiemarkBody(BodyDef body)
        {
            return body != null && CutiemarkBodies.Contains(body);
        }

        public static bool HasWingsFast(Pawn pawn)
        {
            return IsWingedBody(pawn?.kindDef?.race?.race?.body);
        }

        public static bool HasCutiemarkFast(Pawn pawn)
        {
            return IsCutiemarkBody(pawn?.kindDef?.race?.race?.body);
        }

        private static PawnFlightData GetValidated(Pawn pawn, BodyDef currentBody)
        {
            PawnFlightData data = PerPawn.GetValue(pawn, CreateDataDelegate);
            if (data.body != currentBody)
                Rebuild(pawn, data, currentBody);
            return data;
        }

        private static PawnFlightData CreateData(Pawn pawn)
        {
            PawnFlightData data = new PawnFlightData();
            Rebuild(pawn, data, pawn.kindDef?.race?.race?.body);
            return data;
        }

        private static void Rebuild(Pawn pawn, PawnFlightData data, BodyDef body)
        {
            PonyThreadGuard.ReportIfOffMain("PonyFlightCache.Rebuild", ref _rebuildOffMainReported);
            data.toggle = pawn.TryGetComp<CompPegasusFlightToggle>();
            data.timer = pawn.TryGetComp<CompPegasusFlightTimer>();
            data.usableWingsCheckedAtTick = NeverChecked;
            Volatile.Write(ref data.body, body);
        }

        public static void Warmup(Pawn pawn)
        {
            if (pawn == null) return;

            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (IsWingedBody(body))
                GetValidated(pawn, body);
        }

        public static CompPegasusFlightToggle GetToggle(Pawn pawn)
        {
            if (pawn == null) return null;

            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (!IsWingedBody(body)) return null;
            return GetValidated(pawn, body).toggle;
        }

        public static CompPegasusFlightTimer GetTimer(Pawn pawn)
        {
            if (pawn == null) return null;

            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (!IsWingedBody(body)) return null;
            return GetValidated(pawn, body).timer;
        }

        public static bool IsPegasusConstantFlight(Pawn p)
        {
            if (p == null) return false;

            BodyDef body = p.kindDef?.race?.race?.body;
            if (!IsWingedBody(body)) return false;
            if (!p.Spawned || p.Dead || p.Downed) return false;

            PawnFlightData data = GetValidated(p, body);
            CompPegasusFlightToggle toggle = data.toggle;
            if (toggle == null || !toggle.FlightEnabled) return false;
            if (!UsableWingsCached(p, data)) return false;
            if (p.Position.Roofed(p.Map)) return false;
            return data.timer?.CanFly ?? true;
        }

        public static bool HasUsableWingsFast(Pawn pawn)
        {
            if (pawn == null) return false;

            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (!IsWingedBody(body)) return false;
            return UsableWingsCached(pawn, GetValidated(pawn, body));
        }

        private static bool UsableWingsCached(Pawn pawn, PawnFlightData d)
        {
            if (!UnityData.IsInMainThread || !MultiplayerCompat.TickCacheWritable)
            {
                if (d.usableWingsCheckedAtTick == NeverChecked)
                    return ComputeUsableWings(pawn);
                return d.usableWings;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame - d.usableWingsCheckedAtTick < UsableWingsTtlTicks)
                return d.usableWings;

            bool result = d.usableWings = ComputeUsableWings(pawn);
            Volatile.Write(ref d.usableWingsCheckedAtTick, ticksGame);
            return result;
        }

        private static bool ComputeUsableWings(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health?.hediffSet;
            BodyDef body = pawn.RaceProps?.body;
            if (hediffSet == null || body == null) return false;

            if (!WingPartsByBody.TryGetValue(body, out WingParts wings)) return true;
            if (wings.Left == null && wings.Right == null) return true;
            if (wings.Left == null || wings.Right == null) return false;

            return !hediffSet.PartIsMissing(wings.Left) && !hediffSet.PartIsMissing(wings.Right);
        }

        public static bool TryGetWingParts(BodyDef body, out WingParts parts)
        {
            if (body != null)
                return WingPartsByBody.TryGetValue(body, out parts);

            parts = default;
            return false;
        }
    }
}