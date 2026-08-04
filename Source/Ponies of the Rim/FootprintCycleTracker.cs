using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    internal static class FootprintCycleTracker
    {
        private static readonly ConditionalWeakTable<Pawn, PawnFootState> _states = new ConditionalWeakTable<Pawn, PawnFootState>();

        internal static bool IsRightFrontAndAdvance(Pawn pawn)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            bool isRight = state.RightFrontNext;
            state.RightFrontNext = !state.RightFrontNext;
            return isRight;
        }

        internal static void SaveFrontRight(Pawn pawn, Vector3 pos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            state.FrontRightPos = pos;
            state.HasFrontRight = true;
        }

        internal static void SaveFrontLeft(Pawn pawn, Vector3 pos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            state.FrontLeftPos = pos;
            state.HasFrontLeft = true;
        }

        internal static bool TryGetFrontRight(Pawn pawn, out Vector3 pos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            pos = state.FrontRightPos;
            return state.HasFrontRight;
        }

        internal static bool TryGetFrontLeft(Pawn pawn, out Vector3 pos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            pos = state.FrontLeftPos;
            return state.HasFrontLeft;
        }

        private sealed class PawnFootState
        {
            public bool    RightFrontNext = true;
            public Vector3 FrontLeftPos;
            public Vector3 FrontRightPos;
            public bool    HasFrontLeft;
            public bool    HasFrontRight;
        }
    }
}