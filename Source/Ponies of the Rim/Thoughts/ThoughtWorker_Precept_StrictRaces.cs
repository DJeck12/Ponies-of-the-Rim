using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_StrictRaces : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            Lord lord = p.GetLord();
            if (lord != null && lord.ownedPawns.Any((Pawn c) => c.def != def.GetModExtension<ThoughtExtension>().race && c.RaceProps.intelligence == Intelligence.Humanlike && c.IsFreeNonSlaveColonist == true))
            {
                return true;
            }
            Caravan car = p.GetCaravan();
            if (car != null && car.PawnsListForReading.Any((Pawn c) => c.def != def.GetModExtension<ThoughtExtension>().race && c.RaceProps.intelligence == Intelligence.Humanlike && c.IsFreeNonSlaveColonist == true))
            {
                return true;
            }
            Map map = p.MapHeld;
            if (map != null)
            {
                Faction fac = p.Faction;
                if (fac != null)
                {
                    if (map.mapPawns.SpawnedPawnsInFaction(fac).Any((Pawn c) => c.def != def.GetModExtension<ThoughtExtension>().race && c.RaceProps.intelligence == Intelligence.Humanlike && c.IsFreeNonSlaveColonist == true))
                    {
                        return true;
                    }
                }
                else if (map.mapPawns.AllPawnsSpawned.Any((Pawn c) => c.def != def.GetModExtension<ThoughtExtension>().race && !p.HostileTo(c) && c.RaceProps.intelligence == Intelligence.Humanlike && c.IsFreeNonSlaveColonist == true))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
