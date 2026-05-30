using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace PoniesOfTheRim
{
    /// <summary>
    /// Prefix-патч на PawnFootprintMaker.TryPlaceFootprint.
    ///
    /// Мод-совместимость:
    ///   - Для пешек, не принадлежащих нашему моду, возвращаем true →
    ///     оригинальный метод и патчи других модов выполняются в штатном режиме.
    ///   - Для наших пешек возвращаем false → управление полностью у нас.
    ///
    /// Аллюр — рысь (trot):
    ///   Диагональные пары ног работают противофазно.
    ///     Триггер чётный   → правая передняя + левая задняя
    ///     Триггер нечётный → левая передняя + правая задняя
    ///   Задняя нога каждой стороны приходит в позицию ПЕРЕДНЕЙ ТОЙ ЖЕ
    ///   стороны с предыдущего шага (tracking up) — реальное поведение
    ///   лошади на снегу для экономии энергии.
    ///
    ///   Обычные пони:  перед = копыто,  зад = копыто
    ///   Гиппогриф:     перед = коготь,  зад = копыто
    ///   Грифон:        перед = коготь,  зад = лапа льва
    /// </summary>
    public static class HoofprintPatch
    {
        // Боковое расстояние от центра пешки до ноги (полуширина).
        private const float LateralOffset       = 0.28f;
        // Расстояние от центра пешки до передней ноги вдоль движения.
        // Длина "туловища" примерно LongitudinalOffset * 2 — это же расстояние
        // должно пройти между триггерами, чтобы задняя попала в след передней.
        private const float LongitudinalOffset   = 0.50f;
        // Минимальное расстояние между триггерами. ≈ длина туловища, чтобы
        // диагональная пара выглядела как реальный шаг рыси.
        private const float QuadrupedMinStepDist = LongitudinalOffset * 2f;

        // Насколько задняя нога не доходит до точного следа передней.
        // Маленький разброс: задняя почти точно накрывает передний след.
        private const float TrailOffsetMin = 0.02f;
        private const float TrailOffsetMax = 0.12f;

        // Лёгкое боковое отклонение задней ноги от следа передней.
        private const float LateralJitter = 0.04f;

        // Если сохранённая позиция передней ноги слишком далеко от текущей
        // позиции пешки (поворот, телепорт, долгий простой) — игнорируем её.
        private const float StalePositionThreshold = LongitudinalOffset * 6f;

        public static bool TryPlaceHoofprint(
            ref Pawn    ___pawn,
            ref Vector3 ___lastFootprintPlacePos,
            ref bool    ___lastFootprintRight,
            ref Vector3 ___FootprintOffset)
        {
            Pawn pawn = ___pawn;

            // ── Защита от null-ref при деспауне ───────────────────────────────
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return false;

            // ── Мод-совместимость: не наша пешка — не трогаем ─────────────────
            if (!pawn.IsPony())
                return true;

            // ── Общие вычисления ──────────────────────────────────────────────
            Vector3 drawPos   = pawn.Drawer.DrawPos;
            Vector3 dir       = (drawPos - ___lastFootprintPlacePos).normalized;
            float   rot       = dir.AngleFlat();
            float   sizeScale = Mathf.Sqrt(pawn.BodySize);

            // ── Проверка минимального расстояния ─────────────────────────────
            float distSq = (drawPos - ___lastFootprintPlacePos).sqrMagnitude;
            if (distSq < QuadrupedMinStepDist * QuadrupedMinStepDist)
                return false;

            // ── Выбор текстур передних и задних лап ──────────────────────────
            GetFootprintDefs(pawn, out FleckDef frontFleck, out FleckDef backFleck);

            // ── Какая диагональ ведёт в этом шаге ────────────────────────────
            bool rightFrontLeads = FootprintCycleTracker.IsRightFrontAndAdvance(pawn);

            Vector3 perpDir = dir.RotatedBy(90f);
            Vector3 lateralSide = perpDir * LateralOffset * sizeScale;

            // ── Передняя нога: новая позиция впереди по движению ─────────────
            Vector3 frontPos = drawPos + ___FootprintOffset
                             + dir * LongitudinalOffset * sizeScale
                             + (rightFrontLeads ? lateralSide : -lateralSide);

            TryPlaceAt(frontPos, pawn, rot, sizeScale, frontFleck);

            // Запоминаем позицию ЭТОЙ передней — её "примет" задняя той же
            // стороны через шаг (одна диагональ пропускается между ними).
            if (rightFrontLeads)
                FootprintCycleTracker.SaveFrontRight(pawn, frontPos);
            else
                FootprintCycleTracker.SaveFrontLeft(pawn, frontPos);

            // ── Задняя нога: в позицию передней ПРОТИВОПОЛОЖНОЙ стороны ──────
            // Если ведёт правая передняя → задняя левая, и идёт она в место
            // где была левая передняя с предыдущего шага.
            bool hasSaved = rightFrontLeads
                ? FootprintCycleTracker.TryGetFrontLeft(pawn,  out Vector3 savedOpposite)
                : FootprintCycleTracker.TryGetFrontRight(pawn, out savedOpposite);

            if (hasSaved)
            {
                // Защита от устаревшей позиции (поворот, телепорт, простой).
                float ageSq = (drawPos - savedOpposite).sqrMagnitude;
                if (ageSq < StalePositionThreshold * StalePositionThreshold)
                {
                    Vector3 trailBack = dir * Rand.Range(TrailOffsetMin, TrailOffsetMax) * sizeScale;
                    Vector3 latJit    = perpDir * Rand.Range(-LateralJitter, LateralJitter) * sizeScale;
                    Vector3 backPos   = savedOpposite - trailBack + latJit;

                    TryPlaceAt(backPos, pawn, rot, sizeScale, backFleck);
                }
            }
            // Если сохранённой позиции нет (самый первый шаг или после простоя) —
            // размещаем только переднюю. Это естественно для начала движения.

            ___lastFootprintPlacePos = drawPos;
            ___lastFootprintRight    = !___lastFootprintRight;
            return false;
        }

        /// <summary>
        /// Возвращает FleckDef для передних и задних лап в зависимости от расы.
        /// </summary>
        private static void GetFootprintDefs(Pawn pawn, out FleckDef front, out FleckDef back)
        {
            if (pawn.IsGriffon())
            {
                front = Pony_DefOf.Pony_Talonprint   ?? Pony_DefOf.Pony_Hoofprint;
                back  = Pony_DefOf.Pony_Pawprint ?? Pony_DefOf.Pony_Hoofprint;
            }
            else if (pawn.IsHippogriff())
            {
                front = Pony_DefOf.Pony_Talonprint ?? Pony_DefOf.Pony_Hoofprint;
                back  = Pony_DefOf.Pony_Hoofprint;
            }
            else
            {
                front = Pony_DefOf.Pony_Hoofprint;
                back  = Pony_DefOf.Pony_Hoofprint;
            }
        }

        /// <summary>
        /// Размещает один отпечаток с проверкой границ, рельефа и глубины снега.
        /// </summary>
        private static void TryPlaceAt(
            Vector3 pos, Pawn pawn, float rot, float sizeScale, FleckDef fleckDef)
        {
            IntVec3 cell = pos.ToIntVec3();
            if (!cell.InBounds(pawn.Map)) return;

            TerrainDef terrain = cell.GetTerrain(pawn.Map);
            if (terrain == null) return;

            if (terrain.takeSplashes)
                FleckMaker.WaterSplash(pos, pawn.Map, sizeScale * 2f, 1.5f);

            if (pawn.RaceProps.makesFootprints
                && terrain.takeFootprints
                && pawn.Map.snowGrid.GetDepth(pawn.Position) >= 0.4f)
            {
                PonyHelper.PlaceFootprintFleck(pos, pawn.Map, rot, fleckDef);
            }
        }
    }
}