using RimWorld;
using Verse;
using UnityEngine;

namespace PoniesOfTheRim.Abilities
{
    public class GizmoIconShortFlightSwap : Command_Ability
    {
        public GizmoIconShortFlightSwap(Ability ability, Pawn pawn) : base(ability, pawn)
        {
            icon = ability.def.uiIcon;
            string raceIconPath = GetIconPathForRace(pawn);
            if (!raceIconPath.NullOrEmpty())
            {
                icon = ContentFinder<Texture2D>.Get(raceIconPath, reportFailure: false) ?? icon;
            }
        }

        private string GetIconPathForRace(Pawn pawn)
        {
            if (pawn.IsBatpony())
            {
                return "UI/Abilities/Glimpse";
            }

            return null;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }
    }
}