using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_PathFollower_SnowInFlight
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_PathFollower), "pawn");

        private static readonly MethodInfo AddDepthMethod = ResolveAddDepth();

        private static readonly MethodInfo FilterMethod =
            AccessTools.Method(typeof(Patch_PathFollower_SnowInFlight), nameof(FilterSnowDepthChange));

        private static MethodInfo ResolveAddDepth()
        {
            Type gridType = AccessTools.Field(typeof(Map), "snowGrid")?.FieldType;
            if (gridType == null)
            {
                return null;
            }
            return AccessTools.Method(gridType, "AddDepth", new[] { typeof(IntVec3), typeof(float) });
        }

        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            bool canPatch = PawnField != null && AddDepthMethod != null && FilterMethod != null
                && original != null && !original.IsStatic && original.DeclaringType == typeof(Pawn_PathFollower);
            int inserted = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (canPatch && instruction.Calls(AddDepthMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                    yield return new CodeInstruction(OpCodes.Ldfld, PawnField);
                    yield return new CodeInstruction(OpCodes.Call, FilterMethod);
                    inserted++;
                }
                yield return instruction;
            }
            if (inserted == 0)
            {
                PonyLog.WarnOnce("Flight.Snow.NotInserted",
                    "Flight: no snowGrid.AddDepth call in Pawn_PathFollower.TryEnterNextPathCell (another mod's transpiler?), flying ponies trample snow");
            }
        }

        public static float FilterSnowDepthChange(float depthChange, Pawn pawn)
        {
            if (depthChange >= 0f)
            {
                return depthChange;
            }
            try
            {
                if (pawn != null && PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                {
                    if (PonyLog.Verbose)
                    {
                        PonyLog.TraceOnce("Flight.Snow.Skipped", $"Flight: {pawn.LabelShort} does not trample snow while flying");
                    }
                    return 0f;
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: flight check for snow trampling failed", ex);
            }
            return depthChange;
        }
    }
}