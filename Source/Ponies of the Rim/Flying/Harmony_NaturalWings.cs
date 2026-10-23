using System;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_PawnGenerator_NaturalWings
    {
        public static void Postfix(Pawn __result)
        {
            if (__result?.health?.hediffSet == null || __result.Dead || !__result.HasWings())
                return;

            try
            {
                Patch_SpawnSetup_EnsureNaturalWings.EnsureWingsBoundToParts(__result);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: natural wings not added on generation, they will be added on spawn", ex);
            }
        }
    }
}