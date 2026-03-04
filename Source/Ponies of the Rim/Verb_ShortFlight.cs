using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim
{
    public class Verb_ShortFlight : Verb_CastAbility
    {
        private float cachedEffectiveRange = -1f;
        public override bool Available()
        {
            if (caster.Position.Roofed(caster.Map))
            {
                return false;
            }
            return base.Available();
        }
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                return JumpUtility.DoJump(CasterPawn, currentTarget, base.ReloadableCompSource, verbProps, ability, base.CurrentTarget);
            }
            return false;
        }

        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    if (base.EquipmentSource != null)
                    {
                        cachedEffectiveRange = base.EquipmentSource.GetStatValue(StatDefOf.JumpRange);
                    }
                    else
                    {
                        cachedEffectiveRange = verbProps.range;
                    }
                }
                return cachedEffectiveRange;
            }
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            float distanceSquared = (root - targ.Cell).LengthHorizontalSquared;
            return distanceSquared <= (EffectiveRange * EffectiveRange) && targ.Cell.Walkable(caster.Map);
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null)
            {
                return false;
            }
            if (!JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell))
            {
                return false;
            }
            if (target.Cell.Roofed(caster.Map))
            {
                return false;
            }
            if (OutOfRange(caster.Position, target.Cell, CellRect.SingleCell(target.Cell)))
            {
                return false;
            }
            if (!ReloadableUtility.CanUseConsideringQueuedJobs(CasterPawn, base.EquipmentSource))
            {
                return false;
            }
            Building building = target.Cell.GetEdifice(caster.Map);
            if (building != null)
            {
                return false;
            }
            return true;
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (ValidateTarget(target))
            {
                Job job = JobMaker.MakeJob(JobDefOf.CastJump, target.Cell);
                job.verbToUse = this;
                if (CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    FleckMaker.Static(target.Cell, CasterPawn.Map, FleckDefOf.FeedbackGoto);
                }
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (caster == null || caster.Spawned)
            {
                if (target.IsValid && JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell))
                {
                    GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
                }
                GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white,
                    (IntVec3 c) => c.Walkable(caster.Map) && JumpUtility.ValidJumpTarget(caster, caster.Map, c));
            }
        }
        public override void OnGUI(LocalTargetInfo target)
        {
            if (ValidateTarget(target, false))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }
    }
}