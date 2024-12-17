using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_Pony : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            Lord lord = p.GetLord();
            if (lord != null && lord.ownedPawns.Any((Pawn c) => c.def.defName.Contains("Pony_")))
            {
                return true;
            }
            Caravan car = p.GetCaravan();
            if (car != null && car.PawnsListForReading.Any((Pawn c) => c.def.defName.Contains("Pony_")))
            {
                return true;
            }
            Map map = p.MapHeld;
            if (map != null)
            {
                Faction fac = p.Faction;
                if (fac != null)
                {
                    if (map.mapPawns.SpawnedPawnsInFaction(fac).Any((Pawn c) => c.def.defName.Contains("Pony_")))
                    {
                        return true;
                    }
                }
                else if (map.mapPawns.AllPawnsSpawned.Any((Pawn c) => c.def.defName.Contains("Pony_") && !p.HostileTo(c)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
