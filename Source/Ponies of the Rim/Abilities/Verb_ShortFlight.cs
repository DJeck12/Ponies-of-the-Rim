using PoniesOfTheRim.Flying;
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
            return distSq <= EffectiveRange * EffectiveRange && targ.Cell.Walkable(caster.Map) &&
                   FlightLineClear(caster.Map, root, targ.Cell);
        }

        internal static bool FlightLineClear(Map map, IntVec3 from, IntVec3 to)
        {
            if (map == null)
                return false;
            var cells = new ShortFlightCells(map);
            return LineClear(from, to, ref cells);
        }

        internal interface ICellCheck
        {
            bool Blocked(int x, int z);
        }

        private readonly struct ShortFlightCells : ICellCheck
        {
            private readonly Map map;
            private readonly PathGrid pathGrid;
            private readonly int sizeX;
            private readonly int sizeZ;

            public ShortFlightCells(Map map)
            {
                this.map = map;
                pathGrid = map.pathing.Normal.pathGrid;
                sizeX = map.Size.x;
                sizeZ = map.Size.z;
            }

            public bool Blocked(int x, int z)
            {
                if ((uint)x >= (uint)sizeX || (uint)z >= (uint)sizeZ)
                    return true;
                int i = z * sizeX + x;
                if (map.roofGrid.Roofed(i) || map.fogGrid.IsFogged(i))
                    return true;
                Building edifice = map.edificeGrid[i];
                if (edifice?.def?.building != null && edifice.def.building.isNaturalRock)
                    return true;
                // Стена или амбразура крытого помещения: рядом с ней пол под крышей.
                return !pathGrid.WalkableFast(i) && PegasusFlightReachability.BordersRoofedRoom(map, x, z);
            }
        }

        internal static bool LineClear<T>(IntVec3 from, IntVec3 to, ref T check) where T : struct, ICellCheck
        {
            int x = from.x, z = from.z;
            int dx = to.x - from.x, dz = to.z - from.z;
            int sx = dx < 0 ? -1 : 1, sz = dz < 0 ? -1 : 1;
            dx = dx < 0 ? -dx : dx;
            dz = dz < 0 ? -dz : dz;
            int ddx = 2 * dx, ddz = 2 * dz;

            if (ddx >= ddz)
            {
                int error = dx, errorPrev = dx;
                for (int i = 0; i < dx; i++)
                {
                    x += sx;
                    error += ddz;
                    if (error > ddx)
                    {
                        z += sz;
                        error -= ddx;
                        int sum = error + errorPrev;
                        if (sum < ddx)
                        {
                            if (check.Blocked(x, z - sz)) return false;
                        }
                        else if (sum > ddx)
                        {
                            if (check.Blocked(x - sx, z)) return false;
                        }
                        else if (check.Blocked(x, z - sz) && check.Blocked(x - sx, z))
                        {
                            return false;
                        }
                    }
                    if (check.Blocked(x, z)) return false;
                    errorPrev = error;
                }
            }
            else
            {
                int error = dz, errorPrev = dz;
                for (int i = 0; i < dz; i++)
                {
                    z += sz;
                    error += ddx;
                    if (error > ddz)
                    {
                        x += sx;
                        error -= ddz;
                        int sum = error + errorPrev;
                        if (sum < ddz)
                        {
                            if (check.Blocked(x - sx, z)) return false;
                        }
                        else if (sum > ddz)
                        {
                            if (check.Blocked(x, z - sz)) return false;
                        }
                        else if (check.Blocked(x - sx, z) && check.Blocked(x, z - sz))
                        {
                            return false;
                        }
                    }
                    if (check.Blocked(x, z)) return false;
                    errorPrev = error;
                }
            }
            return true;
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
            if (!FlightLineClear(caster.Map, caster.Position, target.Cell))
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

            if (target.IsValid && ValidateTarget(target, showMessages: false))
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