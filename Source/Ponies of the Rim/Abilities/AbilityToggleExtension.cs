using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class AbilityToggleExtension : DefModExtension
    {
        public string settingLabel = null;
        public bool defaultEnabled = true;
        public List<string> grantToRaces = new List<string>();
    }
}