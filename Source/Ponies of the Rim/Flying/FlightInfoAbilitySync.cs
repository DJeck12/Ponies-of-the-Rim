using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public class GameComponent_FlightInfoAbilitySync : GameComponent
    {
        public GameComponent_FlightInfoAbilitySync(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
            int changed = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                try
                {
                    if (FlightInfoAbilityUtility.Sync(pawn))
                    {
                        changed++;
                    }
                }
                catch (Exception ex)
                {
                    PonyLog.WarnCaught($"Flight: flight icon check failed for {pawn?.LabelShortCap ?? "?"}", ex);
                }
            }
            if (changed > 0)
            {
                PonyLog.Trace($"Flight: flight icon synced with race on {changed} pawns");
            }
        }
    }
}