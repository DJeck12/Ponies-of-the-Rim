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
                        $"Unique: fallback kind {config.FallbackKindDefName} for {kindDef.defName} not found, picking one by race");
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
                        $"Unique: no regular {race.defName} kind for a newborn of {kindDef.defName}, it may get the unique look");
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Unique: newborn kind fix failed", ex);
            }
        }
    }
}