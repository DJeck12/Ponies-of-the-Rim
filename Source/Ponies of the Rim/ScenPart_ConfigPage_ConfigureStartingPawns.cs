using RimWorld;
using System;
using Verse;

namespace Ponies_of_the_Rim
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
