using System;
using PoniesOfTheRim.Flying;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class Patch_CE_GetCollisionBodyFactors
    {
        private static bool _errorLogged;

        public static void Postfix(Pawn pawn, ref Vector2 __result)
        {
            try
            {
                if (pawn == null)
                {
                    return;
                }

                if (!CombatExtendedCompatability.TryGetFlightExtension(pawn.def, out var ext))
                {
                    return;
                }

                if (ext.flyingWidthFactor == 1f && ext.flyingHeightFactor == 1f)
                {
                    return;
                }

                if (!PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                {
                    return;
                }

                __result.x *= ext.flyingWidthFactor;
                __result.y *= ext.flyingHeightFactor;
            }
            catch (Exception arg)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    Log.Warning($"[PoniesOfTheRim] CE GetCollisionBodyFactors postfix: {arg}");
                }
            }
        }
    }
}