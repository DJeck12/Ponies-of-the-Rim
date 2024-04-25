﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Ponies_of_the_Rim
{
    [StaticConstructorOnStartup]
    public static class ScalableBeltPatch
    {
        static readonly Vector2 north = new Vector2(0, -0.15f);
        static readonly Vector2 east = new Vector2(-0.31f, -0.1f);
        static readonly Vector2 south = new Vector2(0, -0.15f);
        static readonly Vector2 west = new Vector2(0.31f, -0.1f);
        static readonly Vector2 scale = new Vector2(0.4f, 0.4f);

        private static readonly Type patchType = typeof(ScalableBeltPatch);
        static ScalableBeltPatch()
        {
            Harmony harmonyInstance = new Harmony("com.Pony.rimworld.mod");
            harmonyInstance.Patch(original: AccessTools.Method(typeof(WornGraphicData), "BeltScaleAt"), prefix: new HarmonyMethod(patchType, "BeltScaleAtPatch"));
            harmonyInstance.Patch(original: AccessTools.Method(typeof(WornGraphicData), "BeltOffsetAt"), prefix: new HarmonyMethod(patchType, "BeltOffsetAtPatch"));
        }

        public static bool BeltScaleAtPatch(ref Vector2 __result, ref WornGraphicData __instance, BodyTypeDef bodyType)
        {
            if (bodyType == BodyTypeDefOf.Pony)
            {
                __result = scale;
                return false;
            }
            return true;
        }

        public static bool BeltOffsetAtPatch(ref Vector2 __result, ref Rot4 facing, BodyTypeDef bodyType)
        {
            if (bodyType == BodyTypeDefOf.Pony)
            {
                switch (facing.AsInt)
                {
                    case 0:
                        __result = north;
                        break;
                    case 1:
                        __result = east;
                        break;
                    case 2:
                        __result = south;
                        break;
                    case 3:
                        __result = west;
                        break;
                }
                return false;
            }
            return true;
        }
    }
}
