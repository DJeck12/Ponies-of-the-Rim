using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace PoniesOfTheRim.Abilities
{
    public class HediffGiver_NatureMuse : HediffGiver
    {
        public AbilityDef ability;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
                        if (!LoadedModManager.GetMod<PoniesOfTheRimSettings>().GetSettings<PoniesOfTheRimSettingsData>().abilities)
            {
                pawn.abilities.RemoveAbility(this.ability);
                return;
            }
        }
    }
}