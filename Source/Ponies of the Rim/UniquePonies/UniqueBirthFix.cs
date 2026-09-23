using System;
using System.Linq;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueBirthFix
    {
        public static void GeneratePawn_Prefix(ref PawnGenerationRequest request)
        {
            try
            {
                if (!request.AllowedDevelopmentalStages.Newborn())
                    return;

                var kindDef = request.KindDef;
                if (kindDef == null) return;

                if (!UniquePawnConfig.ByKindDef.TryGetValue(kindDef.defName, out var config))
                    return;

                if (!string.IsNullOrEmpty(config.FallbackKindDefName))
                {
                    var configured = DefDatabase<PawnKindDef>.GetNamed(
                        config.FallbackKindDefName, errorOnFail: false);

                    if (configured != null)
                    {
                        request.KindDef = configured;
                        return;
                    }

                    PonyLog.WarnOnce("UniqueBirthFix.MissingFallback|" + kindDef.defName,
                        $"Уникальные пони: FallbackKindDefName '{config.FallbackKindDefName}' для '{kindDef.defName}' " +
                        "не найден — используется автоопределение.");
                }

                var race = kindDef.race;
                if (race == null) return;

                var fallback = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .FirstOrDefault(k =>
                        k.race == race &&
                        !UniquePawnConfig.ByKindDef.ContainsKey(k.defName) &&
                        k != kindDef);

                if (fallback != null)
                {
                    request.KindDef = fallback;
                }
                else
                {
                    PonyLog.WarnOnce("UniqueBirthFix.NoFallback|" + kindDef.defName,
                        $"Уникальные пони: не найден обычный kindDef для расы '{race.defName}' — ребёнок " +
                        $"'{kindDef.defName}' может унаследовать внешность уникальной пешки. " +
                        "Укажите FallbackKindDefName в UniquePoniesConfig.");
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Уникальные пони: сбой при генерации новорождённого.", ex);
            }
        }
    }
}