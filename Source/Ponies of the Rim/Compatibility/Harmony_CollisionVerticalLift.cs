using HarmonyLib;
using PoniesOfTheRim.Flying;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class Patch_CE_CollisionVerticalLift
    {
        [ThreadStatic]
        private static Pawn _groundedForCalculation;

        private static readonly AccessTools.FieldRef<Pawn, Pawn_FlightTracker> FlightRef = CreateFlightRef();

        private static bool _errorLogged;

        private static AccessTools.FieldRef<Pawn, Pawn_FlightTracker> CreateFlightRef()
        {
            try
            {
                return AccessTools.FieldRefAccess<Pawn, Pawn_FlightTracker>("flight");
            }
            catch
            {
                return null;
            }
        }

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

                Pawn_FlightTracker tracker = FlightRef?.Invoke(pawn);
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
            catch (Exception arg)
            {
                __state = 0f;
                _groundedForCalculation = null;
                LogOnce(arg);
            }
        }

        public static void Postfix(float __state, ref float shotHeight)
        {
            if (__state > 0f)
            {
                shotHeight += __state;
            }
        }

        public static void Finalizer()
        {
            _groundedForCalculation = null;
        }

        private static void LogOnce(Exception arg)
        {
            if (_errorLogged)
            {
                return;
            }

            _errorLogged = true;
            Log.Warning($"[PoniesOfTheRim] CE CollisionVertical lift patch: {arg}");
        }
    }
}