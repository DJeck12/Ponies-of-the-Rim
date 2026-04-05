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

                    Log.Warning($"[PoniesOfTheRim] UniqueBirthFix: FallbackKindDefName " +
                                $"'{config.FallbackKindDefName}' для '{kindDef.defName}' не найден. " +
                                $"Используется авто-определение.");
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
                    Log.Warning($"[PoniesOfTheRim] UniqueBirthFix: Не найден обычный kindDef " +
                                $"для расы '{race.defName}'. Ребёнок '{kindDef.defName}' " +
                                $"может унаследовать внешность уникальной пешки. " +
                                $"Установите FallbackKindDefName в UniquePoniesConfig.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] UniqueBirthFix.GeneratePawn_Prefix: {ex.Message}");
            }
        }
    }
}