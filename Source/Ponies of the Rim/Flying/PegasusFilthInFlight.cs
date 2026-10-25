using System;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_FilthTracker_FilthInFlight
    {
        public static bool Prefix(Pawn ___pawn)
        {
            try
            {
                if (___pawn == null || !PegasusFlightUtility.IsPegasusConstantFlight(___pawn))
                {
                    return true;
                }
                if (PonyLog.Verbose)
                {
                    TraceSkipped(___pawn);
                }
                return false;
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: flight check for filth failed", ex);
                return true;
            }
        }

        private static void TraceSkipped(Pawn pawn)
        {
            TerrainDef terrain = pawn.Position.GetTerrain(pawn.Map);
            ThingDef filth = terrain?.generatedFilth;
            if (filth == null)
            {
                return;
            }
            PonyLog.TraceOnce("Flight.Filth.Skipped",
                $"Flight: {pawn.LabelShort} flying, no {filth.defName} picked up from {terrain.defName}");
        }
    }
}