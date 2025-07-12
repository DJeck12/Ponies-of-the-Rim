using RimWorld;
using System.Linq;
using Verse.AI;
using Verse;

namespace PoniesOfTheRim
{
    public class JobGiver_UseShortFlight : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || !pawn.Spawned)
                return null;

            Verb verb = pawn.verbTracker.AllVerbs.FirstOrDefault(v => v is Verb_ShortFlight);
            if (verb == null || !verb.Available())
                return null;

            IntVec3 targetCell = FindJumpTarget(pawn);
            if (targetCell.IsValid && verb.CanHitTargetFrom(pawn.Position, targetCell))
            {
                Job job = JobMaker.MakeJob(JobDefOf.CastJump, targetCell);
                job.verbToUse = verb;
                return job;
            }
            return null;
        }

        private IntVec3 FindJumpTarget(Pawn pawn)
        {
            Pawn nearestEnemy = (Pawn)AttackTargetFinder.BestAttackTarget(
                pawn, TargetScanFlags.NeedReachable, maxDist: 15f);
            if (nearestEnemy != null)
            {
                IntVec3 targetCell = RCellFinder.RandomWanderDestFor(
                    pawn, nearestEnemy.Position, 5f, (Pawn p, IntVec3 c, IntVec3 r) => 
                        c.Walkable(p.Map) && !c.Roofed(p.Map) && JumpUtility.ValidJumpTarget(p, p.Map, c), 
                    Danger.Deadly);
                if (targetCell.IsValid)
                {
                    return targetCell;
                }
            }
            return IntVec3.Invalid;
        }
    }
}