using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class PonyBabyHairPatch
    {
        private static List<HairDef> _ponyHairsCache;

        private static List<HairDef> PonyHairs => _ponyHairsCache ?? (_ponyHairsCache = DefDatabase<HairDef>.AllDefsListForReading.Where((HairDef h) => IsPonyHair(h)).ToList());

        private static bool IsPonyHair(HairDef h)
        {
            if (h == null)
            {
                return false;
            }
            return h.HasModExtension<PonyHairExtension>() || (h.defName != null && h.defName.StartsWith("Pony_"));
        }

        private static bool HasMane(Pawn pawn)
        {
            return PonyRaceExtension.Get(pawn.def)?.hasMane ?? true;
        }

        public static void EnsurePonyBabyHair(Pawn pawn)
        {
            if (pawn?.story != null && !IsPonyHair(pawn.story.hairDef))
            {
                List<HairDef> ponyHairs = PonyHairs;
                if (ponyHairs.Count != 0)
                {
                    Rand.PushState(pawn.thingIDNumber ^ 0x5EED);
                    pawn.story.hairDef = ponyHairs[Rand.Range(0, ponyHairs.Count)];
                    Rand.PopState();
                }
            }
        }

        public static void GeneratePawn_Postfix(Pawn __result)
        {
            if (__result == null || !__result.IsPony() || (__result.DevelopmentalStage != DevelopmentalStage.Baby && __result.DevelopmentalStage != DevelopmentalStage.Newborn))
            {
                return;
            }
            if (!HasMane(__result))
            {
                if (__result.story != null && __result.story.hairDef != HairDefOf.Bald)
                {
                    __result.story.hairDef = HairDefOf.Bald;
                }
                return;
            }
            EnsurePonyBabyHair(__result);
        }

        public static void PonyBabyHairPostfix(Pawn pawn, ref Graphic __result)
        {
            if (pawn == null || !pawn.IsPony() || (pawn.DevelopmentalStage != DevelopmentalStage.Baby && pawn.DevelopmentalStage != DevelopmentalStage.Newborn))
            {
                return;
            }
            if (!HasMane(pawn))
            {
                return;
            }
            HairDef hairDef = pawn.story?.hairDef;
            if (!IsPonyHair(hairDef))
            {
                EnsurePonyBabyHair(pawn);
                hairDef = pawn.story?.hairDef;
                if (!IsPonyHair(hairDef))
                {
                    return;
                }
            }
            __result = hairDef.GraphicFor(pawn, pawn.story?.HairColor ?? Color.white);
        }
    }
}