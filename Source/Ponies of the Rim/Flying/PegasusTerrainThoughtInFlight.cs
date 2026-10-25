using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_MindState_TerrainThoughtInFlight
    {
        private static readonly FieldInfo TraversedThoughtField =
            AccessTools.Field(typeof(TerrainDef), nameof(TerrainDef.traversedThought));

        private static readonly MethodInfo FilterMethod =
            AccessTools.Method(typeof(Patch_MindState_TerrainThoughtInFlight), nameof(FilterTraversedThought));

        public static List<MethodInfo> ResolveTargets()
        {
            List<MethodInfo> targets = new List<MethodInfo>(1);
            if (TraversedThoughtField == null)
            {
                return targets;
            }
            List<MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(Pawn_MindState));
            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                if (!method.IsStatic && !method.IsAbstract && !method.ContainsGenericParameters && ReadsTraversedThought(method))
                {
                    targets.Add(method);
                }
            }
            return targets;
        }

        private static bool ReadsTraversedThought(MethodInfo method)
        {
            try
            {
                foreach (KeyValuePair<OpCode, object> instruction in PatchProcessor.ReadMethodBody(method))
                {
                    if (instruction.Key == OpCodes.Ldfld && instruction.Value is FieldInfo field && field == TraversedThoughtField)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                PonyLog.Trace($"Flight: IL of Pawn_MindState.{method.Name} not readable, skipped ({ex.Message})");
            }
            return false;
        }

        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            bool canPatch = TraversedThoughtField != null
                && FilterMethod != null
                && original != null
                && !original.IsStatic
                && original.DeclaringType == typeof(Pawn_MindState);
            int inserted = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (canPatch && instruction.LoadsField(TraversedThoughtField))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, FilterMethod);
                    inserted++;
                }
            }
            if (inserted == 0)
            {
                string name = original?.Name ?? "?";
                PonyLog.WarnOnce("Flight.TerrainThought.NotInserted|" + name,
                    $"Flight: no traversedThought read in Pawn_MindState.{name} (another mod's transpiler?), flying ponies get terrain thoughts");
            }
        }

        public static ThoughtDef FilterTraversedThought(ThoughtDef thought, Pawn_MindState mindState)
        {
            if (thought == null)
            {
                return null;
            }
            try
            {
                Pawn pawn = mindState?.pawn;
                if (pawn != null && PegasusFlightUtility.IsPegasusConstantFlight(pawn))
                {
                    if (PonyLog.Verbose)
                    {
                        PonyLog.TraceOnce("Flight.TerrainThought.Skipped|" + thought.defName,
                            $"Flight: {thought.defName} from {pawn.Position.GetTerrain(pawn.Map)?.defName ?? "?"} skipped, {pawn.LabelShort} is flying");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Flight: flight check for terrain thought failed, vanilla thought kept", ex);
            }
            return thought;
        }
    }
}