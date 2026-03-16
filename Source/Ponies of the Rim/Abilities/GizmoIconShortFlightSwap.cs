using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class GizmoIconShortFlightSwap : Command_Ability
    {
        public GizmoIconShortFlightSwap(Ability ability, Pawn pawn)
            : base(ability, pawn)
        {
            AbilityRaceIconExtension ext = ability.def.GetModExtension<AbilityRaceIconExtension>();
            if (ext == null)
                return;

            string iconPath = ext.GetIconPathForRace(pawn.def);
            if (iconPath.NullOrEmpty())
                return;

            Texture2D loaded = ContentFinder<Texture2D>.Get(iconPath, reportFailure: false);
            if (loaded != null)
                icon = loaded;
        }
    }
}