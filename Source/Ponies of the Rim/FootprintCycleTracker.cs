using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    internal static class FootprintCycleTracker
    {
        private static readonly ConditionalWeakTable<Pawn, PawnFootState> _states
            = new ConditionalWeakTable<Pawn, PawnFootState>();

        internal static bool IsFrontAndAdvance(Pawn pawn)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            bool isFront = state.IsFront;
            state.IsFront = !state.IsFront;
            return isFront;
        }

        internal static void SaveFrontPos(Pawn pawn, Vector3 leftPos, Vector3 rightPos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            state.FrontLeftPos  = leftPos;
            state.FrontRightPos = rightPos;
        }

        internal static void GetFrontPos(Pawn pawn, out Vector3 leftPos, out Vector3 rightPos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            leftPos  = state.FrontLeftPos;
            rightPos = state.FrontRightPos;
        }

        private sealed class PawnFootState
        {
            public bool    IsFront      = true;
            public Vector3 FrontLeftPos;
            public Vector3 FrontRightPos;
        }
    }
}