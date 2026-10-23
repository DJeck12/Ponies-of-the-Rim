using System;
using System.Reflection;
using HarmonyLib;
using PoniesOfTheRim.Flying;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class Patch_CE_CollisionVerticalLift
    {
        [ThreadStatic]
        private static Pawn _groundedForCalculation;

        public static bool IsGroundedForCalculation(Pawn pawn)
        {
            return pawn != null && ReferenceEquals(pawn, _groundedForCalculation);
        }

        public static MethodInfo TargetMethod()
        {
            Type type = AccessTools.TypeByName("CombatExtended.CollisionVertical");
            return type == null ? null : AccessTools.Method(type, "CalculateHeightRange");
        }

        public static void Prefix(Thing thing, out float __state)
        {
            __state = 0f;

            try
            {
                if (!(thing is Pawn pawn))
                {
                    return;
                }

                if (!CombatExtendedCompatability.TryGetFlightExtension(pawn.def, out var ext) || !ext.neutralizeFlightLift)
                {
                    return;
                }

                if (!PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                {
                    return;
                }

                Pawn_FlightTracker tracker = pawn.flight;
                if (tracker == null)
                {
                    return;
                }

                float lift = 0.5f * tracker.PositionOffsetFactor;
                if (lift <= 0f)
                {
                    return;
                }

                __state = lift;
                _groundedForCalculation = pawn;
            }
            catch (Exception ex)
            {
                __state = 0f;
                _groundedForCalculation = null;
                PonyLog.WarnCaught("CE: flight lift neutralization failed", ex);
            }
        }

        public static void Postfix(float __state, Thing thing, ref FloatRange heightRange, ref float shotHeight)
        {
            if (__state <= 0f)
            {
                return;
            }

            float liftedShot = shotHeight + __state;
            Patch_CE_PonyCoverPeek.ApplyGrounded(thing as Pawn, ref heightRange, ref shotHeight);
            shotHeight = Math.Max(liftedShot, shotHeight);
        }

        public static void Finalizer()
        {
            _groundedForCalculation = null;
        }
    }
}