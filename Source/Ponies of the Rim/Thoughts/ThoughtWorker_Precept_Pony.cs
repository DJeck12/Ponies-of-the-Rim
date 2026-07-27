using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_Pony : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            Lord lord = p.GetLord();
            if (lord != null)
            {
                List<Pawn> owned = lord.ownedPawns;
                for (int i = 0; i < owned.Count; i++)
                {
                    if (owned[i].IsPony())
                    {
                        return true;
                    }
                }
            }
            Caravan car = p.GetCaravan();
            if (car != null)
            {
                List<Pawn> pawns = car.PawnsListForReading;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i].IsPony())
                    {
                        return true;
                    }
                }
            }
            Map map = p.MapHeld;
            if (map != null)
            {
                Faction fac = p.Faction;
                if (fac != null)
                {
                    List<Pawn> list = map.mapPawns.SpawnedPawnsInFaction(fac);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].IsPony())
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    List<Pawn> list = (List<Pawn>)map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < list.Count; i++)
                    {
                        Pawn c = list[i];
                        if (c.IsPony() && !p.HostileTo(c))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}