using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class PegasusKiteUtility
    {
        private static bool _cached;
        private static FieldInfo _fiTouch;
        private static FieldInfo _fiShort;
        private static FieldInfo _fiMedium;
        private static FieldInfo _fiLong;

        private static void EnsureFields()
        {
            if (_cached) return;
            _cached = true;
            var t = typeof(VerbProperties);
            const BindingFlags F = BindingFlags.Public | BindingFlags.Instance;
            _fiTouch = t.GetField("accuracyTouch", F);
            _fiShort = t.GetField("accuracyShort", F);
            _fiMedium = t.GetField("accuracyMedium", F);
            _fiLong = t.GetField("accuracyLong", F);
        }

        public const float MIN_KITE_DIST = 5f;

        public static float GetOptimalAccuracyRange(VerbProperties vp)
        {
            float maxRange = vp.range;
            if (maxRange < 1f) return 0f;

            EnsureFields();

            if (_fiTouch == null)
                return Mathf.Clamp(maxRange * 0.66f, MIN_KITE_DIST, maxRange);

            float aTouch = (float)(_fiTouch.GetValue(vp) ?? 0f);
            float aShort = (float)(_fiShort.GetValue(vp) ?? 0f);
            float aMedium = (float)(_fiMedium.GetValue(vp) ?? 0f);
            float aLong = (float)(_fiLong.GetValue(vp) ?? 0f);

            float bestAcc = -1f;
            float bestDist = 0f;

            void Check(float acc, float dist)
            {
                float d = Mathf.Min(dist, maxRange);
                if (d < MIN_KITE_DIST) return;
                if (acc > bestAcc) { bestAcc = acc; bestDist = d; }
            }

            Check(aShort, 12f);
            Check(aMedium, 25f);
            Check(aLong, Mathf.Min(40f, maxRange));

            if (bestDist >= MIN_KITE_DIST) return bestDist;

            return Mathf.Min(MIN_KITE_DIST, maxRange);
        }

        public static VerbProperties GetRangedVerbProps(Pawn pawn)
        {
            ThingWithComps primary = pawn.equipment?.Primary;
            if (primary?.def?.Verbs == null) return null;

            foreach (VerbProperties vp in primary.def.Verbs)
            {
                if (!vp.IsMeleeAttack && vp.range > 1f)
                    return vp;
            }
            return null;
        }

        public static bool IsInMeleeCombat(Pawn pawn)
        {
            if (pawn.Map == null) return false;
            foreach (IntVec3 adj in GenAdj.AdjacentCells)
            {
                IntVec3 c = pawn.Position + adj;
                if (!c.InBounds(pawn.Map)) continue;
                List<Thing> things = pawn.Map.thingGrid.ThingsListAtFast(c);
                for (int i = 0; i < things.Count; i++)
                {
                    Pawn other = things[i] as Pawn;
                    if (other != null && other.HostileTo(pawn) && !other.Dead && !other.Downed)
                        return true;
                }
            }
            return false;
        }

        public static IntVec3 FindKitePosition(
            Pawn pawn, IntVec3 enemyPos, float targetDist,
            Map map, bool walkableOnly = true)
        {
            Vector3 dir = pawn.Position.ToVector3() - enemyPos.ToVector3();
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.right;
            else dir = dir.normalized;

            IntVec3 ideal = (enemyPos.ToVector3() + dir * targetDist).ToIntVec3();

            bool Valid(IntVec3 c)
            {
                if (!c.InBounds(map) || c.Roofed(map)) return false;
                if (walkableOnly) return c.Walkable(map);
                return !PegasusFlightUtility.IsImpassableMountain(c, map);
            }

            if (Valid(ideal)) return ideal;

            IntVec3 best = IntVec3.Invalid;
            float bestDelta = float.MaxValue;
            foreach (IntVec3 c in GenRadial.RadialCellsAround(ideal, 4f, true))
            {
                if (!Valid(c)) continue;
                float delta = Mathf.Abs(c.DistanceTo(enemyPos) - targetDist);
                if (delta < bestDelta) { bestDelta = delta; best = c; }
            }
            return best;
        }
    }
}