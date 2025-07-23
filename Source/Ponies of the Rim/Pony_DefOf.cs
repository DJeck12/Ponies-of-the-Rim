using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [DefOf]
    public static class Pony_DefOf
    {
        public static FleckDef Hoofprint;

        public static BodyTypeDef Pony;
        public static BodyTypeDef PonyChild;
        public static BodyTypeDef PonyBaby;

        public static BodyDef Pony_EarthponyBody;
        public static BodyDef Pony_PegasusBody;
        public static BodyDef Pony_UnicornBody;

        public static TraitDef Pony_RelationsForPony;
        public static ThoughtDef Pony_OpinionForPony;

        public static ThoughtDef Pony_AteMeat;

        [MayRequireBiotech]
        public static GeneDef Pony_Cutiemark;

        

        //[MayRequireBiotech]
        //public static GeneDef Pony_Herbivore;
        //[MayRequireBiotech]
        //public static GeneDef Pony_Carnivore;
    }
}
