using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Abilities
{
    public class Verb_ShortFlight : Verb_CastAbility
    {
        private float cachedEffectiveRange = -1f;

        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    cachedEffectiveRange = EquipmentSource != null
                        ? EquipmentSource.GetStatValue(StatDefOf.JumpRange)
                        : verbProps.range;
                }
                return cachedEffectiveRange;
            }
        }

        public override bool Available()
        {
            if (caster.Position.Roofed(caster.Map))
                return false;
            return base.Available();
        }

        protected override bool TryCastShot()
        {
            if (!base.TryCastShot())
                return false;

            return JumpUtility.DoJump(CasterPawn, currentTarget, ReloadableCompSource, verbProps, ability, CurrentTarget);
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            float distSq = (root - targ.Cell).LengthHorizontalSquared;
            return distSq <= EffectiveRange * EffectiveRange && targ.Cell.Walkable(caster.Map);
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null)
                return false;
            if (!JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell))
                return false;
            if (target.Cell.Roofed(caster.Map))
                return false;
            if (OutOfRange(caster.Position, target.Cell, CellRect.SingleCell(target.Cell)))
                return false;
            if (!ReloadableUtility.CanUseConsideringQueuedJobs(CasterPawn, EquipmentSource))
                return false;
            if (!target.Cell.Walkable(caster.Map))
                return false;
            return true;
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (!ValidateTarget(target))
                return;
            StartShortFlightJobSynced(this, CasterPawn, target.Cell);
        }

        public static void StartShortFlightJobSynced(Verb verb, Pawn pawn, IntVec3 cell)
        {
            if (verb == null || pawn?.jobs == null || pawn.Map == null)
                return;

            Job job = JobMaker.MakeJob(JobDefOf.CastJump, cell);
            job.verbToUse = verb;

            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                FleckMaker.Static(cell, pawn.Map, FleckDefOf.FeedbackGoto);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (caster == null || !caster.Spawned)
                return;

            if (target.IsValid && JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell))
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);

            GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white,
                (IntVec3 c) => c.Walkable(caster.Map) && JumpUtility.ValidJumpTarget(caster, caster.Map, c));
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            if (ValidateTarget(target, showMessages: false))
                base.OnGUI(target);
            else
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
        }
    }
}