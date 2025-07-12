using System.Linq;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class ScenPart_ConfigPage_ConfigureStartingPawns_KindDefsCustom : ScenPart_ConfigPage_ConfigureStartingPawns_KindDefs
    {
        [MustTranslate]
        public string customSummary;
        public override string Summary(Scenario scen)
        {
            return customSummary ?? ((string)"ScenPart_StartWithPawns".Translate(kindCounts.Select((PawnKindCount x) => x.Summary).ToCommaList(useAnd: true), pawnChoiceCount));
        }
    }
}