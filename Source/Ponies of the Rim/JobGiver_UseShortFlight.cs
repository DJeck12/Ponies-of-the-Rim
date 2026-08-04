using System.Linq;
using PoniesOfTheRim.Flying;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim
{
    public class JobGiver_UseShortFlight : ThinkNode_JobGiver
    {
        private const float KITE_BAND = 0.10f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || !pawn.Spawned || pawn.Map == null)
                return null;

            if (pawn.IsColonistPlayerControlled)
                return null;

            Abilities.Verb_ShortFlight verb = pawn.abilities?.AllAbilitiesForReading
                .Select(a => a.verb)
                .OfType<Abilities.Verb_ShortFlight>()
                .FirstOrDefault(v => v.Available());

            if (verb == null)
                return null;

            VerbProperties rangedProps = PegasusKiteUtility.GetRangedVerbProps(pawn);

            return rangedProps != null
                ? TryGiveKiteJob(pawn, verb, rangedProps)
                : TryGiveMeleeJob(pawn, verb);
        }

        private static Job TryGiveMeleeJob(Pawn pawn, Abilities.Verb_ShortFlight verb)
        {
            Pawn enemy = FindEnemy(pawn, maxDist: verb.EffectiveRange * 2f);
            if (enemy == null) return null;

            IntVec3 best      = IntVec3.Invalid;
            float   bestScore = float.MaxValue;

            foreach (IntVec3 offset in GenAdj.AdjacentCellsAndInside)
            {
                IntVec3 c = enemy.Position + offset;
                if (!c.InBounds(pawn.Map) || c.Roofed(pawn.Map)) continue;
                if (!c.Walkable(pawn.Map))                         continue;
                if (!JumpUtility.ValidJumpTarget(pawn, pawn.Map, c)) continue;
                if (!verb.CanHitTargetFrom(pawn.Position, c))      continue;

                float score = pawn.Position.DistanceTo(c);
                if (score < bestScore) { bestScore = score; best = c; }
            }

            return best.IsValid ? MakeJob(verb, best) : null;
        }

        private static Job TryGiveKiteJob(
            Pawn pawn, Abilities.Verb_ShortFlight verb, VerbProperties rangedProps)
        {
            Pawn enemy = FindEnemy(pawn, maxDist: rangedProps.range * 3f);
            if (enemy == null) return null;

            float optRange = PegasusKiteUtility.GetOptimalAccuracyRange(rangedProps);
            if (optRange < 1f) return null;

            float dist    = pawn.Position.DistanceTo(enemy.Position);
            float lo      = optRange * (1f - KITE_BAND);
            float hi      = optRange * (1f + KITE_BAND);
            bool  inMelee = PegasusKiteUtility.IsInMeleeCombat(pawn);

            if (!inMelee && dist >= lo && dist <= hi)
                return null;

            IntVec3 kitePos = PegasusKiteUtility.FindKitePosition(
                pawn, enemy.Position, optRange, pawn.Map, walkableOnly: true);

            if (!kitePos.IsValid) return null;

            if (verb.CanHitTargetFrom(pawn.Position, kitePos)
                && JumpUtility.ValidJumpTarget(pawn, pawn.Map, kitePos))
                return MakeJob(verb, kitePos);

            IntVec3 mid = FindIntermediateCell(pawn, kitePos, verb);
            return mid.IsValid ? MakeJob(verb, mid) : null;
        }

        private static IntVec3 FindIntermediateCell(
            Pawn pawn, IntVec3 kitePos, Abilities.Verb_ShortFlight verb)
        {
            Vector3 dir = (kitePos.ToVector3() - pawn.Position.ToVector3()).normalized;

            IntVec3 best   = IntVec3.Invalid;
            float bestDot  = float.MinValue;

            foreach (IntVec3 c in GenRadial.RadialCellsAround(
                pawn.Position, verb.EffectiveRange, useCenter: false))
            {
                if (!c.InBounds(pawn.Map) || c.Roofed(pawn.Map)) continue;
                if (!c.Walkable(pawn.Map))                         continue;
                if (!verb.CanHitTargetFrom(pawn.Position, c))      continue;
                if (!JumpUtility.ValidJumpTarget(pawn, pawn.Map, c)) continue;

                Vector3 toC = (c.ToVector3() - pawn.Position.ToVector3()).normalized;
                float   dot = Vector3.Dot(dir, toC);

                if (dot > 0.5f && dot > bestDot) { bestDot = dot; best = c; }
            }
            return best;
        }

        private static Pawn FindEnemy(Pawn pawn, float maxDist)
            => AttackTargetFinder.BestAttackTarget(
                   pawn, TargetScanFlags.NeedReachable, maxDist: maxDist) as Pawn;

        private static Job MakeJob(Abilities.Verb_ShortFlight verb, IntVec3 dest)
        {
            Job job = JobMaker.MakeJob(JobDefOf.CastJump, dest);
            job.verbToUse = verb;
            return job;
        }
    }
}