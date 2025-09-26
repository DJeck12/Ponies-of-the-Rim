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
        public static BodyDef Pony_ZebraBody;
        public static BodyDef Pony_CrystalponyBody;
        public static BodyDef Pony_BatponyBody;
        public static BodyDef Pony_AlicornBody;
        public static BodyDef Pony_KirinBody;
        public static BodyDef Pony_ChangedlingBody;
        public static BodyDef Pony_ChangelingBody;
        public static BodyDef Pony_DeerBody;
        public static BodyDef Pony_GriffonBody;
        public static BodyDef Pony_HippogriffBody;
        public static BodyDef Pony_SeaponyBody;

        public static TraitDef Pony_RelationsForPony;
        public static ThoughtDef Pony_OpinionForPony;

        public static ThoughtDef Pony_AteMeatAsHerbivore;
        public static ThoughtDef Pony_AtePlantAsCarnivore;



        [MayRequireBiotech]
        public static GeneDef Pony_Cutiemark;



        [MayRequireBiotech]
        public static GeneDef Pony_Herbivore;
        [MayRequireBiotech]
        public static GeneDef Pony_Carnivore;
    }
}
