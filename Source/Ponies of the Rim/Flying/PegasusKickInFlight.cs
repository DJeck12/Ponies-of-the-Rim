using System;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_TerrainVerbs_KickInFlight
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (!__result || p == null)
            {
                return;
            }
            try
            {
                if (PegasusFlightUtility.IsPegasusConstantFlight(p))
                {
                    __result = false;
                    if (PonyLog.Verbose)
                    {
                        PonyLog.TraceOnce("Flight.TerrainVerb.Blocked",
                            $"Flight: {p.LabelShort} flying, melee verbs from {p.Position.GetTerrain(p.Map)?.defName ?? "?"} unavailable");
                    }
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: flight check for terrain melee verbs failed", ex);
            }
        }
    }
}