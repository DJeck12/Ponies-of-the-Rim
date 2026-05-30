using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    /// <summary>
    /// Хранит состояние диагональной рыси для каждой пешки.
    ///
    /// Рысь — две диагональные пары ног работают противофазно:
    ///   Триггер чётный   → правая передняя + левая задняя
    ///   Триггер нечётный → левая передняя + правая задняя
    ///
    /// Задняя нога приходит в позицию передней ноги ТОЙ ЖЕ СТОРОНЫ
    /// с предыдущего шага (поведение "tracking up"). Поэтому позиции
    /// левой и правой передней сохраняются отдельно.
    ///
    /// ConditionalWeakTable не держит сильную ссылку на пешку —
    /// записи удаляются автоматически при GC, ручная очистка не нужна.
    /// </summary>
    internal static class FootprintCycleTracker
    {
        private static readonly ConditionalWeakTable<Pawn, PawnFootState> _states
            = new ConditionalWeakTable<Pawn, PawnFootState>();

        /// <summary>
        /// Возвращает true если в этом триггере ведёт правая передняя
        /// (диагональ RF+LB), false — левая передняя (диагональ LF+RB).
        /// Сразу переключает фазу.
        /// </summary>
        internal static bool IsRightFrontAndAdvance(Pawn pawn)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            bool isRight = state.RightFrontNext;
            state.RightFrontNext = !state.RightFrontNext;
            return isRight;
        }

        /// <summary>Сохраняет позицию правой передней — задняя той же стороны придёт сюда.</summary>
        internal static void SaveFrontRight(Pawn pawn, Vector3 pos)
        {
            PawnFootState state = _states.GetOrCreateValue(pawn);
            state.FrontRightPos = pos;
            state.HasFrontRight = true;
        }

        /// <summary>Сохраняет позицию левой передней.</summary>
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
            public bool    RightFrontNext = true; // первый шаг — правая диагональ
            public Vector3 FrontLeftPos;
            public Vector3 FrontRightPos;
            public bool    HasFrontLeft;
            public bool    HasFrontRight;
        }
    }
}