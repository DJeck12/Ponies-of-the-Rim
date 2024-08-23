using UnityEngine;
using Verse;
using static RimWorld.FleckMaker;


namespace PoniesOfTheRim
{
    internal static class PonyHelper
    {
        internal static bool IsPony(this Pawn pawn)
        {
            return pawn.kindDef.race.defName.Contains("Pony_");
        }
        public static void PlaceHoofprint(Vector3 loc, Map map, float rot)
        {
            if (loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData dataStatic = GetDataStatic(loc, map, Pony_DefOf.Hoofprint, 0.5f);
                dataStatic.rotation = rot;
                map.flecks.CreateFleck(dataStatic);
            }
        }
    }
}    









