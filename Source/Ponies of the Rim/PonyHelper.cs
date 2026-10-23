using AlienRace;
using PoniesOfTheRim.Flying;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyHelper
    {
        private static HashSet<ThingDef> _ponyRaceCache;

        public static bool PartHasProsthetic(this HediffSet set, BodyPartRecord record)
        {
            if (set?.hediffs == null || record == null)
            {
                return false;
            }
            List<Hediff> hediffs = set.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.Part == record && (hediff is Hediff_AddedPart || hediff.def.countsAsAddedPartOrImplant))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPonyRace(ThingDef def)
        {
            if (_ponyRaceCache != null)
            {
                return def != null && _ponyRaceCache.Contains(def);
            }
            return IsPonyRaceDef(def);
        }

        public static void BuildRaceCache()
        {
            HashSet<ThingDef> races = new HashSet<ThingDef>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                if (IsPonyRaceDef(defs[i]))
                {
                    races.Add(defs[i]);
                }
            }
            _ponyRaceCache = races;
            PonyLog.Trace($"Races: {races.Count} cached");
        }

        private static bool IsPonyRaceDef(ThingDef def)
        {
            return def is ThingDef_AlienRace && def.HasModExtension<PonyRaceExtension>();
        }

        public static bool IsPony(this Pawn pawn)
        {
            return pawn != null && IsPonyRace(pawn.def);
        }

        private static bool HasBody(this Pawn pawn, BodyDef body)
        {
            if (body == null)
            {
                return false;
            }
            return pawn?.kindDef?.race?.race?.body == body;
        }

        public static bool IsEarthpony(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_EarthponyBody);
        }

        public static bool IsUnicorn(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_UnicornBody);
        }

        public static bool IsPegasus(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_PegasusBody);
        }

        public static bool IsZebra(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_ZebraBody);
        }

        public static bool IsCrystalpony(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_CrystalponyBody);
        }

        public static bool IsBatpony(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_BatponyBody);
        }

        public static bool IsAlicorn(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_AlicornBody);
        }

        public static bool IsKirin(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_KirinBody);
        }

        public static bool IsChangedling(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_ChangedlingBody);
        }

        public static bool IsChangeling(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_ChangelingBody);
        }

        public static bool IsDeer(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_DeerBody);
        }

        public static bool IsGriffon(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_GriffonBody);
        }

        public static bool IsHippogriff(this Pawn pawn)
        {
            return pawn.HasBody(Pony_DefOf.Pony_HippogriffBody);
        }

        public static bool HasWings(this Pawn pawn)
        {
            return PonyFlightCache.HasWingsFast(pawn);
        }

        public static bool HasCutiemark(this Pawn pawn)
        {
            return PonyFlightCache.HasCutiemarkFast(pawn);
        }

        public static bool HasOviparousGene(this Pawn pawn)
        {
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            if (pawn?.genes == null)
            {
                return false;
            }
            return pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("Pony_Oviparous", errorOnFail: false));
        }

        public static void PlaceHoofprint(Vector3 loc, Map map, float rot)
        {
            PlaceFootprintFleck(loc, map, rot, Pony_DefOf.Pony_Hoofprint);
        }

        public static void PlaceFootprintFleck(Vector3 loc, Map map, float rot, FleckDef fleckDef)
        {
            if (fleckDef != null && loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData data = FleckMaker.GetDataStatic(loc, map, fleckDef, 0.5f);
                data.rotation = rot;
                map.flecks.CreateFleck(data);
            }
        }
    }
}