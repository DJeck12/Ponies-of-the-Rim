using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace PoniesOfTheRim.Flying
{

    public static class PonyFlightCache
    {
        // ------------------------------------------------------------------
        // Статические кэши по дефам. Заполняются один раз после загрузки дефов.
        // ------------------------------------------------------------------

        private static readonly HashSet<BodyDef> WingedBodies = new HashSet<BodyDef>();

        private static readonly HashSet<BodyDef> CutiemarkBodies = new HashSet<BodyDef>();

        private static readonly Dictionary<BodyDef, WingParts> WingPartsByBody = new Dictionary<BodyDef, WingParts>();

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
                    string dn = parts[i].def?.defName;
                    if (dn == PegasusFlightUtility.LeftWingPartDef)
                    {
                        left = parts[i];
                    }
                    else if (dn == PegasusFlightUtility.RightWingPartDef)
                    {
                        right = parts[i];
                    }
                    if (left != null && right != null)
                    {
                        break;
                    }
                }
                WingPartsByBody[body] = new WingParts(left, right);
            }

            if (Prefs.DevMode)
            {
                Log.Message($"[PoniesOfTheRim] PonyFlightCache: крылатых тел — {WingedBodies.Count}, записей частей крыльев — {WingPartsByBody.Count}.");
            }
        }

        private static void AddBody(HashSet<BodyDef> set, BodyDef body)
        {
            if (body != null)
            {
                set.Add(body);
            }
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

        private sealed class PawnFlightData
        {

            public BodyDef body;

            public CompPegasusFlightToggle toggle;

            public CompPegasusFlightTimer timer;

            public int usableWingsCheckedAtTick = NeverChecked;

            public bool usableWings;
        }

        private static readonly ConditionalWeakTable<Pawn, PawnFlightData> PerPawn = new ConditionalWeakTable<Pawn, PawnFlightData>();
        private static readonly ConditionalWeakTable<Pawn, PawnFlightData>.CreateValueCallback CreateDataDelegate = CreateData;
        private const int NeverChecked = -999999;
        private const int UsableWingsTtlTicks = 30;

        private static PawnFlightData GetValidated(Pawn pawn, BodyDef currentBody)
        {
            PawnFlightData data = PerPawn.GetValue(pawn, CreateDataDelegate);
            if (!ReferenceEquals(data.body, currentBody))
            {
                Rebuild(pawn, data, currentBody);
            }
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
            data.toggle = pawn.TryGetComp<CompPegasusFlightToggle>();
            data.timer = pawn.TryGetComp<CompPegasusFlightTimer>();
            data.usableWingsCheckedAtTick = NeverChecked;
            data.body = body;
        }

        public static CompPegasusFlightToggle GetToggle(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }
            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (!IsWingedBody(body))
            {
                return null;
            }
            return GetValidated(pawn, body).toggle;
        }

        public static CompPegasusFlightTimer GetTimer(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }
            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (!IsWingedBody(body))
            {
                return null;
            }
            return GetValidated(pawn, body).timer;
        }

        public static bool IsPegasusConstantFlight(Pawn p)
        {
            if (p == null)
            {
                return false;
            }
            BodyDef body = p.kindDef?.race?.race?.body;
            if (!IsWingedBody(body))
            {
                return false;
            }
            if (!p.Spawned || p.Dead || p.Downed)
            {
                return false;
            }
            PawnFlightData d = GetValidated(p, body);
            CompPegasusFlightToggle toggle = d.toggle;
            if (toggle == null || !toggle.FlightEnabled)
            {
                return false;
            }
            if (!UsableWingsCached(p, d))
            {
                return false;
            }
            if (p.Position.Roofed(p.Map))
            {
                return false;
            }
            CompPegasusFlightTimer timer = d.timer;
            return timer == null || timer.CanFly;
        }

        public static bool HasUsableWingsFast(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }
            BodyDef body = pawn.kindDef?.race?.race?.body;
            if (!IsWingedBody(body))
            {
                return false;
            }
            return UsableWingsCached(pawn, GetValidated(pawn, body));
        }

        private static bool UsableWingsCached(Pawn pawn, PawnFlightData d)
        {
            int now = Find.TickManager.TicksGame;
            if (now - d.usableWingsCheckedAtTick < UsableWingsTtlTicks)
            {
                return d.usableWings;
            }
            d.usableWingsCheckedAtTick = now;
            d.usableWings = ComputeUsableWings(pawn);
            return d.usableWings;
        }

        private static bool ComputeUsableWings(Pawn pawn)
        {
            HediffSet set = pawn.health?.hediffSet;
            BodyDef body = pawn.RaceProps?.body;
            if (set == null || body == null)
            {
                return false;
            }
            if (!WingPartsByBody.TryGetValue(body, out WingParts wp))
            {

                return true;
            }
            if (wp.Left == null && wp.Right == null)
            {
                return true;
            }
            if (wp.Left == null || wp.Right == null)
            {
                return false;
            }
            return !set.PartIsMissing(wp.Left) && !set.PartIsMissing(wp.Right);
        }

        public static bool TryGetWingParts(BodyDef body, out WingParts parts)
        {
            if (body != null)
            {
                return WingPartsByBody.TryGetValue(body, out parts);
            }
            parts = default;
            return false;
        }
    }
}