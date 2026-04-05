using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueNameAssigner
    {
        public static void GeneratePawn_Postfix(Pawn __result)
        {
            if (__result == null) return;

            if (ModsConfig.BiotechActive && (__result.DevelopmentalStage.Baby()
                                          || __result.DevelopmentalStage.Newborn()))
                return;

            if (__result.ageTracker?.AgeBiologicalYearsFloat < 1f)
                return;

            if (!UniquePawnConfig.ByKindDef.TryGetValue(__result.kindDef.defName, out var config))
                return;

            ApplyCanonicalName(__result, config);
        }

        public static void ApplyCanonicalName(Pawn pawn, UniqueCharacterConfig config)
        {
            if (config.UseSingleName)
            {
                pawn.Name = new NameSingle(config.NickName);
            }
            else
            {
                pawn.Name = new NameTriple(
                    config.FirstName  ?? string.Empty,
                    config.NickName   ?? string.Empty,
                    config.LastName   ?? string.Empty
                );
            }
        }
    }
}