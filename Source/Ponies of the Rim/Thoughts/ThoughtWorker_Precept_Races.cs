using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace PoniesOfTheRim.Thoughts
{
    public class ThoughtWorker_Precept_Races : ThoughtWorker_Precept
    {
        private ThoughtExtension cachedExtension;
        private bool extensionCached;

        private ThoughtExtension Extension
        {
            get
            {
                if (!extensionCached)
                {
                    extensionCached = true;
                    cachedExtension = def.GetModExtension<ThoughtExtension>();

                    if (cachedExtension == null)
                    {
                        Log.ErrorOnce(
                            "[PoniesOfTheRim] ThoughtWorker_Precept_Races: ThoughtExtension не найден на ThoughtDef '"
                            + def.defName + "'.", def.shortHash);
                    }
                }
                return cachedExtension;
            }
        }

        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            ThingDef race = Extension?.race;
            if (race == null)
            {
                return false;
            }
            Lord lord = p.GetLord();
            if (lord != null)
            {
                List<Pawn> owned = lord.ownedPawns;
                for (int i = 0; i < owned.Count; i++)
                {
                    Pawn c = owned[i];
                    if (c.def == race && c.IsFreeNonSlaveColonist)
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
                    Pawn c = pawns[i];
                    if (c.def == race && c.IsFreeNonSlaveColonist)
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
                        Pawn c = list[i];
                        if (c.def == race && c.IsFreeColonist)
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
                        if (c.def == race && !p.HostileTo(c) && c.IsFreeNonSlaveColonist)
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