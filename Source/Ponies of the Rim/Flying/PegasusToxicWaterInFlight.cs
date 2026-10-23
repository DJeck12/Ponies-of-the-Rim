using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_ToxicUtility_ToxicWaterInFlight
    {
        private static readonly FieldInfo ToxicBuildupFactorField =
            AccessTools.Field(typeof(TerrainDef), nameof(TerrainDef.toxicBuildupFactor));

        private static readonly MethodInfo FilterMethod =
            AccessTools.Method(typeof(Patch_ToxicUtility_ToxicWaterInFlight), nameof(FilterToxicBuildupFactor));

        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            bool canPatch = ToxicBuildupFactorField != null && FilterMethod != null && FirstArgumentIsPawn(original);
            int inserted = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (canPatch && instruction.LoadsField(ToxicBuildupFactorField))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, FilterMethod);
                    inserted++;
                }
            }
            if (inserted == 0)
            {
                PonyLog.WarnOnce("Flight.ToxicWater.NotInserted",
                    "Flight: no toxicBuildupFactor read in ToxicUtility.PawnToxicTickInterval (another mod's transpiler?), flying ponies get toxic buildup from water below");
            }
        }

        private static bool FirstArgumentIsPawn(MethodBase original)
        {
            if (original == null || !original.IsStatic)
            {
                return false;
            }
            ParameterInfo[] parameters = original.GetParameters();
            return parameters.Length > 0 && parameters[0].ParameterType == typeof(Pawn);
        }

        public static float FilterToxicBuildupFactor(float factor, Pawn pawn)
        {
            if (factor <= 0f)
            {
                return factor;
            }
            try
            {
                if (pawn != null && PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                {
                    if (PonyLog.Verbose)
                    {
                        PonyLog.TraceOnce("Flight.ToxicWater.Skipped",
                            $"Flight: {pawn.LabelShort} flying over {pawn.Position.GetTerrain(pawn.Map)?.defName ?? "?"}, no toxic buildup");
                    }
                    return 0f;
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: flight check for toxic buildup failed", ex);
            }
            return factor;
        }
    }
}