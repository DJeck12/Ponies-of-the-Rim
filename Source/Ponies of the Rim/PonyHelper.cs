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
        public class PonyRaceExtension : DefModExtension
        {
            public bool hasMane = true;
            public static PonyRaceExtension Get(Def def)
            {
                List<DefModExtension> exts = def?.modExtensions;
                if (exts == null)
                {
                    return null;
                }
                for (int i = exts.Count - 1; i >= 0; i--)
                {
                    if (exts[i] is PonyRaceExtension ext)
                    {
                        return ext;
                    }
                }
                return null;
            }
        }

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
            "Pony_Hippogriff"
        };

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
            return def is ThingDef_AlienRace && KnownPonyDefNames.Contains(def.defName);
        }

        public static void BuildRaceCache()
        {
            HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
            List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                ThingDef thingDef = allDefsListForReading[i];
                if (thingDef is ThingDef_AlienRace && (thingDef.HasModExtension<PonyRaceExtension>() || KnownPonyDefNames.Contains(thingDef.defName)))
                {
                    hashSet.Add(thingDef);
                }
            }
            _ponyRaceCache = hashSet;
            if (Prefs.DevMode)
            {
                Log.Message("[PoniesOfTheRim] Кэш пони-рас: " + hashSet.Count + " записей.");
            }
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
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, fleckDef, 0.5f);
                dataStatic.rotation = rot;
                map.flecks.CreateFleck(dataStatic);
            }
        }
    }
}