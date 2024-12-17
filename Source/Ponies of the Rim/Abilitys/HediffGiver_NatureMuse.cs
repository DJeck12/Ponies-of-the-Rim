using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace PoniesOfTheRim.Abilitys
{
    public class HediffGiver_NatureMuse : HediffGiver
    {
        public AbilityDef ability;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            //TODO: Move this so is not called on every Interval
            if (!LoadedModManager.GetMod<PoniesOfTheRimSettings>().GetSettings<PoniesOfTheRimSettingsData>().abilities)
            {
                pawn.abilities.RemoveAbility(this.ability);
                return;
            }
        }
    }
}
