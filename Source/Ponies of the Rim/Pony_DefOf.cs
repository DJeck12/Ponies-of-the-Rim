using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [DefOf]
    public static class Pony_DefOf
    {
        public static FleckDef Hoofprint;
        public static FleckDef Pony_Talonprint;
        public static FleckDef Pony_Pawprint; 

        public static BodyTypeDef Pony;
        public static BodyTypeDef PonyChild;
        public static BodyTypeDef PonyBaby;

        public static BodyDef Pony_EarthponyBody;
        public static BodyDef Pony_PegasusBody;
        public static BodyDef Pony_UnicornBody;

        public static TraitDef Pony_RelationsForPony;

        public static ThoughtDef Pony_OpinionForPony;

        public static ThoughtDef Pony_AteMeatAsHerbivore;
        public static ThoughtDef Pony_AtePlantAsCarnivore;

        public static TraitDef Pony_PerfectMemory;
        public static TraitDef Pony_Joyous;
        public static TraitDef Pony_Elegance;


        [MayRequireBiotech]
        public static GeneDef Pony_Cutiemark;
        [MayRequireBiotech]
        public static GeneDef Pony_Herbivore;
        [MayRequireBiotech]
        public static GeneDef Pony_Carnivore;
        
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_ZebraBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_CrystalponyBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_BatponyBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_AlicornBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_KirinBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_ChangedlingBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_ChangelingBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_DeerBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_GriffonBody;
        [MayRequire("Pony.PoniesOfTheRim.Races")]
        public static BodyDef Pony_HippogriffBody;
        //[MayRequire("Pony.PoniesOfTheRim.Races")]
        //public static BodyDef Pony_SeaponyBody;

    }
}
