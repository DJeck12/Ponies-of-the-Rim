using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace PoniesOfTheRim
{
    public static class PonyHelper
    {
        private static readonly HashSet<string> KnownPonyDefNames = new HashSet<string>
        {
            "Pony_Earthpony",
            "Pony_Unicorn",
            "Pony_Pegasus",
            "Pony_Zebra",
            "Pony_Crystalpony",
            "Pony_Batpony",
            "Pony_Alicorn",
            "Pony_Kirin",
            "Pony_Changedling",
            "Pony_Changeling",
            "Pony_Deer",
            "Pony_Griffon",
            //"Pony_Seapony",
            "Pony_Hippogriff",
        };

        public static bool IsPony(this Pawn pawn)
        {
            return pawn?.def is AlienRace.ThingDef_AlienRace
                && KnownPonyDefNames.Contains(pawn.def.defName);
        }

        private static bool HasBody(this Pawn pawn, BodyDef body)
        {
            if (body == null) return false;
            return pawn?.kindDef?.race?.race?.body == body;
        }

        public static bool IsEarthpony(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_EarthponyBody);

        public static bool IsUnicorn(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_UnicornBody);

        public static bool IsPegasus(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_PegasusBody);

        public static bool IsZebra(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_ZebraBody);

        public static bool IsCrystalpony(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_CrystalponyBody);

        public static bool IsBatpony(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_BatponyBody);

        public static bool IsAlicorn(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_AlicornBody);

        public static bool IsKirin(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_KirinBody);

        public static bool IsChangedling(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_ChangedlingBody);

        public static bool IsChangeling(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_ChangelingBody);

        public static bool IsDeer(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_DeerBody);

        public static bool IsGriffon(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_GriffonBody);

        //public static bool IsSeapony(this Pawn pawn)
        //    => pawn.HasBody(Pony_DefOf.Pony_SeaponyBody);

        public static bool IsHippogriff(this Pawn pawn)
            => pawn.HasBody(Pony_DefOf.Pony_HippogriffBody);

        public static bool HasWings(this Pawn pawn)
        {
            return pawn.IsPegasus()
                || pawn.IsBatpony()
                || pawn.IsAlicorn()
                || pawn.IsChangedling()
                || pawn.IsChangeling()
                || pawn.IsGriffon()
                || pawn.IsHippogriff();
        }

        public static bool HasCutiemark(this Pawn pawn)
        {
            return pawn.IsEarthpony()
                || pawn.IsUnicorn()
                || pawn.IsPegasus()
                || pawn.IsZebra()
                || pawn.IsCrystalpony()
                || pawn.IsBatpony()
                || pawn.IsAlicorn();
        }

        public static bool HasOviparousGene(this Pawn pawn)
        {
            if (!ModsConfig.BiotechActive) return false;
            if (pawn?.genes == null) return false;

            return pawn.genes.HasActiveGene(
                DefDatabase<GeneDef>.GetNamed("Pony_Oviparous", errorOnFail: false));
        }

        public static void PlaceHoofprint(Vector3 loc, Map map, float rot)
        {
            PlaceFootprintFleck(loc, map, rot, Pony_DefOf.Pony_Hoofprint);
        }

        // Безопасно при fleckDef == null — ничего не происходит.
        public static void PlaceFootprintFleck(Vector3 loc, Map map, float rot, FleckDef fleckDef)
        {
            if (fleckDef == null) return;
            if (!loc.ShouldSpawnMotesAt(map)) return;

            FleckCreationData data = FleckMaker.GetDataStatic(loc, map, fleckDef, 0.5f);
            data.rotation = rot;
            map.flecks.CreateFleck(data);
        }
    }
}