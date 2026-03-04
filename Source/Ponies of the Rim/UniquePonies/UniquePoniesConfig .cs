using System.Collections.Generic;

namespace PoniesOfTheRim.UniquePonies
{
        public class UniqueCharacterConfig
    {
        public string KindDefName;

                public bool UseSingleName;             public string FirstName;
        public string NickName;                public string LastName;

                public int? CutiemarkVariant;
        public int? TailVariant;
        public int? HeadVariant;
        public int? BodyVariant;

                public string AdultBackstoryDefName;
    }

            public static class UniquePawnConfig
    {
        public static readonly List<UniqueCharacterConfig> Characters = new()
        {
            new UniqueCharacterConfig
            {
                KindDefName = "Pony_Applejack",
                UseSingleName = true,
                NickName = "Applejack",
                AdultBackstoryDefName = "Pony_Applejack_Adult",
                CutiemarkVariant = 67,
                TailVariant = 2,
                HeadVariant = 0,
                BodyVariant = 0,
            },
            new UniqueCharacterConfig
            {
                KindDefName = "Pony_Rarity",
                UseSingleName = true,
                NickName = "Rarity",
                AdultBackstoryDefName = "Pony_Rarity_Adult",
                                CutiemarkVariant = null,
                TailVariant = null,
                HeadVariant = null,
                BodyVariant = null,
            },
            new UniqueCharacterConfig
            {
                KindDefName = "Pony_Fluttershy",
                UseSingleName = true,
                NickName = "Fluttershy",
                AdultBackstoryDefName = "Pony_Fluttershy_Adult",
                CutiemarkVariant = null,
                TailVariant = null,
                HeadVariant = null,
                BodyVariant = null,
            },
            new UniqueCharacterConfig
            {
                KindDefName = "Pony_PinkiePie",
                UseSingleName = false,
                FirstName = "Pinkie",
                NickName = "Pinkie Pie",
                LastName = "Pie",
                AdultBackstoryDefName = "Pony_PinkiePie_Adult",
                CutiemarkVariant = null,
                TailVariant = null,
                HeadVariant = null,
                BodyVariant = null,
            },
            new UniqueCharacterConfig
            {
                KindDefName = "Pony_RainbowDash",
                UseSingleName = false,
                FirstName = "Rainbow",
                NickName = "Rainbow Dash",
                LastName = "Dash",
                AdultBackstoryDefName = "Pony_RainbowDash_Adult",
                CutiemarkVariant = null,
                TailVariant = null,
                HeadVariant = null,
                BodyVariant = null,
            },
            new UniqueCharacterConfig
            {
                KindDefName = "Pony_TwilightSparkle",
                UseSingleName = false,
                FirstName = "Twilight",
                NickName = "Twilight",
                LastName = "Sparkle",
                AdultBackstoryDefName = "Pony_TwilightSparkle_Adult",
                CutiemarkVariant = null,
                TailVariant = null,
                HeadVariant = null,
                BodyVariant = null,
            },
                                                                                                                                                                                };

        private static Dictionary<string, UniqueCharacterConfig> _byKindDef;
        public static Dictionary<string, UniqueCharacterConfig> ByKindDef
        {
            get
            {
                if (_byKindDef == null)
                {
                    _byKindDef = new Dictionary<string, UniqueCharacterConfig>();
                    foreach (var c in Characters)
                        _byKindDef[c.KindDefName] = c;
                }
                return _byKindDef;
            }
        }

        private static Dictionary<string, UniqueCharacterConfig> _byBackstory;
        public static Dictionary<string, UniqueCharacterConfig> ByAdultBackstory
        {
            get
            {
                if (_byBackstory == null)
                {
                    _byBackstory = new Dictionary<string, UniqueCharacterConfig>();
                    foreach (var c in Characters)
                    {
                        if (!string.IsNullOrEmpty(c.AdultBackstoryDefName))
                            _byBackstory[c.AdultBackstoryDefName] = c;
                    }
                }
                return _byBackstory;
            }
        }
    }
}