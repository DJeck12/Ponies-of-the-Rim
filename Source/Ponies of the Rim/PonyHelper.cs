using UnityEngine;
using Verse;
using RimWorld;

namespace PoniesOfTheRim
{
    internal static class PonyHelper
    {
        internal static bool IsPony(this Pawn pawn)
        {
            return pawn.def.defName.Contains("Pony_");
        }

        internal static bool IsEarthpony(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_EarthponyBody;
        }

        internal static bool IsUnicorn(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_UnicornBody;
        }

        internal static bool IsPegasus(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_PegasusBody;
        }

        internal static bool IsZebra(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_ZebraBody;
        }

        internal static bool IsCrystalpony(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_CrystalponyBody;
        }

        internal static bool IsBatpony(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_BatponyBody;
        }

        internal static bool IsAlicorn(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_AlicornBody;
        }

        internal static bool IsKirin(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_KirinBody;
        }

        internal static bool IsChangedling(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_ChangedlingBody;
        }

        internal static bool IsChangeling(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_ChangelingBody;
        }

        internal static bool IsDeer(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_DeerBody;
        }

        internal static bool IsGriffon(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_GriffonBody;
        }

        internal static bool IsSeapony(this Pawn pawn)
        {
            return pawn.kindDef.race.race.body == Pony_DefOf.Pony_SeaponyBody;
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