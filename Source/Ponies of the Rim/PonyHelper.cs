using UnityEngine;
using Verse;
using RimWorld;

namespace PoniesOfTheRim
{
    internal static class PonyHelper
    {
        internal static bool IsPony(this Pawn pawn)
        {
            return pawn.kindDef.race.defName.Contains("Pony_");
        }
        internal static bool IsEarthpony(this Pawn pawn)
        {
            return pawn.kindDef.race.defName.Contains("Pony_Earthpony");
        }
        internal static bool IsUnicorn(this Pawn pawn)
        {
            return pawn.kindDef.race.defName.Contains("Pony_Unicorn");
        }
        internal static bool IsPegasus(this Pawn pawn)
        {
            return pawn.kindDef.race.defName.Contains("Pony_Pegasus");
        }

        public static void PlaceHoofprint(Vector3 loc, Map map, float rot)
        {
            if (loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, Pony_DefOf.Hoofprint, 0.5f);
                dataStatic.rotation = rot;
                map.flecks.CreateFleck(dataStatic);
            }
        }
    }
}    
