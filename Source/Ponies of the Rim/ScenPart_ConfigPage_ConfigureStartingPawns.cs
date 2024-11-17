using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ScenPart_ConfigPage_ConfigureStartingPawnsCustom : ScenPart_ConfigPage_ConfigureStartingPawns
    {
        [MustTranslate]
        public string customSummary;
        public override string Summary(Scenario scen)
        {
            return customSummary ?? ((string)"ScenPart_StartWithPawns".Translate(pawnCount));
        }
    }
}
