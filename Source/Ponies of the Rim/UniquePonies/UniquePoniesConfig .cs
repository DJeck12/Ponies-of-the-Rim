using System.Collections.Generic;

namespace PoniesOfTheRim.UniquePonies
{
    public class UniqueCharacterConfig
    {
        public string KindDefName;

        public bool UseSingleName;
        public string FirstName;
        public string NickName;
        public string LastName;

        public int? CutiemarkVariant;
        public int? TailVariant;
        public int? HeadVariant;
        public int? BodyVariant;

        public string AdultBackstoryDefName;

        public string FallbackKindDefName;
    }

    public static class UniquePawnConfig
    {
        public static readonly List<UniqueCharacterConfig> Characters = new()
        {
            new UniqueCharacterConfig
            {
                KindDefName           = "Pony_Applejack",
                UseSingleName         = true,
                NickName              = "Applejack",
                AdultBackstoryDefName = "Pony_Applejack_Adult",
                CutiemarkVariant      = 68,
                TailVariant           = 2,
                HeadVariant           = 0,
                BodyVariant           = 0,
                FallbackKindDefName   = "Pony_EarthponyColonist",       
            },
            new UniqueCharacterConfig
            {
                KindDefName           = "Pony_Rarity",
                UseSingleName         = true,
                NickName              = "Rarity",
                AdultBackstoryDefName = "Pony_Rarity_Adult",
                CutiemarkVariant      = 154,
                TailVariant           = 4,
                HeadVariant           = 0,
                BodyVariant           = 0,
                FallbackKindDefName   = "Pony_UnicornColonist",       
            },
            new UniqueCharacterConfig
            {
                KindDefName           = "Pony_Fluttershy",
                UseSingleName         = true,
                NickName              = "Fluttershy",
                AdultBackstoryDefName = "Pony_Fluttershy_Adult",
                CutiemarkVariant      = 159,
                TailVariant           = 5,
                HeadVariant           = 0,
                BodyVariant           = 0,
                FallbackKindDefName   = "Pony_PegasusColonist",       
            },
            new UniqueCharacterConfig
            {
                KindDefName           = "Pony_PinkiePie",
                UseSingleName         = false,
                FirstName             = "Pinkie",
                NickName              = "Pinkie Pie",
                LastName              = "Pie",
                AdultBackstoryDefName = "Pony_PinkiePie_Adult",
                CutiemarkVariant      = 155,
                TailVariant           = 3,
                HeadVariant           = 0,
                BodyVariant           = 0,
                FallbackKindDefName   = "Pony_EarthponyColonist",       
            },
            new UniqueCharacterConfig
            {
                KindDefName           = "Pony_RainbowDash",
                UseSingleName         = false,
                FirstName             = "Rainbow",
                NickName              = "Rainbow Dash",
                LastName              = "Dash",
                AdultBackstoryDefName = "Pony_RainbowDash_Adult",
                CutiemarkVariant      = 156,
                TailVariant           = 0,
                HeadVariant           = 0,
                BodyVariant           = 0,
                FallbackKindDefName   = "Pony_PegasusColonist",       
            },
            new UniqueCharacterConfig
            {
                KindDefName           = "Pony_TwilightSparkle",
                UseSingleName         = false,
                FirstName             = "Twilight",
                NickName              = "Twilight",
                LastName              = "Sparkle",
                AdultBackstoryDefName = "Pony_TwilightSparkle_Adult",
                CutiemarkVariant      = 153,
                TailVariant           = 1,
                HeadVariant           = 0,
                BodyVariant           = 0,
                FallbackKindDefName   = "Pony_UnicornColonist",       
            },
        };

        private static Dictionary<string, UniqueCharacterConfig> _byKindDef;
        public static Dictionary<string, UniqueCharacterConfig> ByKindDef
        {
            get
            {
                if (_byKindDef != null) return _byKindDef;
                _byKindDef = new Dictionary<string, UniqueCharacterConfig>();
                foreach (var c in Characters)
                    _byKindDef[c.KindDefName] = c;
                return _byKindDef;
            }
        }

        private static Dictionary<string, UniqueCharacterConfig> _byBackstory;
        public static Dictionary<string, UniqueCharacterConfig> ByAdultBackstory
        {
            get
            {
                if (_byBackstory != null) return _byBackstory;
                _byBackstory = new Dictionary<string, UniqueCharacterConfig>();
                foreach (var c in Characters)
                    if (!string.IsNullOrEmpty(c.AdultBackstoryDefName))
                        _byBackstory[c.AdultBackstoryDefName] = c;
                return _byBackstory;
            }
        }

        public static readonly Dictionary<string, int> BackstoryCutiemark = new()
        {
            { "Pony_EveryponyNightFanatic",      9 },
            { "Pony_EveryponyDayFanatic",       10 },
            { "Pony_EarthponyAnimalHandler",    35 },
            { "Pony_EarthponyZootranslator",    37 },
            { "Pony_PegasusShadowLeader",       52 },
            { "Pony_PegasusOfficerGuard",       61 },
            { "Pony_UnicornWarlock",            64 },
            { "Pony_UnicornCrystalMiner",       83 },
            { "Pony_EarthponyStoneFarmer",      93 },
            { "Pony_UnicornJeweler",            99 },
            { "Pony_EveryponyRoyalGuard",      104 },
            { "Pony_PegasusCook",             137 },
            { "Pony_EarthponyDruid",          151 },
            { "Pony_UnicornMagician",         152 },
            { "Pony_UnicornGemologist",        154 },
            { "Pony_EveryponyPartyPlanner",    155 },
            { "Pony_PegasusFlyingRacer",      156 },
            { "Pony_EarthponyAsceticCook",     173 },
            { "Pony_UnicornCourtWizard",       180 },
            { "Pony_EarthponyAlchemist",       187 },
            { "Pony_PegasusInventor",          234 },
            { "Pony_EveryponyPoacher",         238 },
            { "Pony_PegasusRescuer",           253 },
            { "Pony_PegasusPoultryFarmer",     256 },
            { "Pony_UnicornMagicTeacher",      258 },
            { "Pony_EarthponyFlorist",         275 },
            { "Pony_PegasusCloudSculptor",     276 },

            { "Pony_EveryponyDarkChild",         9 },
            { "Pony_EveryponyLightChild",       10 },
            { "Pony_EveryponyMagicStudent",     19 },
            { "Pony_UnicornYoungWizard",        19 },
            { "Pony_EveryponyUnicornFriend",    46 },
            { "Pony_UnicornPrince",           100 },
            { "Pony_UnicornGlitterFan",       106 },
            { "Pony_PegasusRainbowLover",      137 },
            { "Pony_EarthponyPreservationist", 151 },
            { "Pony_UnicornGiftedMagician",    153 },
            { "Pony_EveryponyFriendshipStudent",158 },
            { "Pony_EarthponyAnimalBonded",    159 },
            { "Pony_EveryponyApprentice",      180 },
            { "Pony_UnicornHornedKnight",      191 },
            { "Pony_PegasusYoungRacer",        230 },
            { "Pony_EarthponySacred",          249 },
            { "Pony_EarthponyForestFamily",    269 },
            { "Pony_PegasusCloudCatcher",      276 },
        };
    }
}