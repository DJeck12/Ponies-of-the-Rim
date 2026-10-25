using System;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_PawnGenerator_FlightInfoAbility
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null)
            {
                return;
            }
            try
            {
                FlightInfoAbilityUtility.Sync(__result);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: flight info icon not set on a generated pawn", ex);
            }
        }
    }
}