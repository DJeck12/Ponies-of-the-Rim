using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class RaceIconEntry
    {
        public string raceDef;
        public string iconPath;
    }

    public class AbilityRaceIconExtension : DefModExtension
    {
        public List<RaceIconEntry> raceIcons = new List<RaceIconEntry>();

        public string GetIconPathForRace(ThingDef raceDef)
        {
            if (raceDef == null || raceIcons.NullOrEmpty())
                return null;

            return raceIcons.FirstOrDefault(e => e.raceDef == raceDef.defName)?.iconPath;
        }
    }
}