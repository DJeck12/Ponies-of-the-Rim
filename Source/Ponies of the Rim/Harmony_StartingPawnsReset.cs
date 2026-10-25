using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class Patch_StartingPawns_ScenarioReset
    {
        private const string TargetMethodName = "BeginScenarioConfiguration";

        private static readonly FieldInfo[] RequestListFields = FindRequestListFields();

        private static readonly AccessTools.FieldRef<PawnKindDef> HarStartingKindRestriction = ResolveHarStartingKindRestriction();

        public static bool HasStaticRequestList => RequestListFields.Length > 0;

        public static MethodInfo ResolveTarget()
        {
            List<MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(Page_SelectScenario));
            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                if (!method.IsStatic || method.Name != TargetMethodName)
                {
                    continue;
                }
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(Scenario))
                {
                    return method;
                }
            }
            return null;
        }

        public static void Postfix()
        {
            bool verbose = PonyLog.Verbose;
            Dictionary<string, int> summary = verbose ? new Dictionary<string, int>() : null;
            int cleared = 0;
            for (int i = 0; i < RequestListFields.Length; i++)
            {
                List<PawnGenerationRequest> requests;
                try
                {
                    requests = RequestListFields[i].GetValue(null) as List<PawnGenerationRequest>;
                }
                catch (Exception ex)
                {
                    PonyLog.WarnCaught("Scenarios: starting pawn requests not readable, reset skipped", ex);
                    continue;
                }
                if (requests == null || requests.Count == 0)
                {
                    continue;
                }
                if (summary != null)
                {
                    CountRequests(requests, summary);
                }
                cleared += requests.Count;
                requests.Clear();
            }
            if (verbose)
            {
                ReportState(cleared, summary);
            }
        }

        private static void ReportState(int cleared, Dictionary<string, int> summary)
        {
            string scenario = Current.Game?.Scenario?.name ?? "?";
            if (!HasStaticRequestList)
            {
                PonyLog.TraceOnce("ScenarioReset.NoStaticList", "Scenarios: StartingPawnUtility keeps no static request list, nothing to reset");
            }
            else if (cleared > 0)
            {
                PonyLog.Trace($"Scenarios: {cleared} starting pawn requests from the previous setup cleared before {scenario} ({JoinSummary(summary)})");
            }
            PawnKindDef restriction = ReadHarRestriction();
            if (restriction != null)
            {
                PonyLog.Trace($"Scenarios: HAR starting race restriction is {restriction.defName}, it applies to every scenario that lists this kind");
            }
        }

        private static void CountRequests(List<PawnGenerationRequest> requests, Dictionary<string, int> summary)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                string key;
                try
                {
                    PawnGenerationRequest request = requests[i];
                    key = (request.KindDef?.defName ?? "no kind") + "/" + (request.Faction?.def?.defName ?? "no faction");
                }
                catch (Exception)
                {
                    key = "unreadable";
                }
                summary.TryGetValue(key, out int count);
                summary[key] = count + 1;
            }
        }

        private static string JoinSummary(Dictionary<string, int> summary)
        {
            if (summary == null || summary.Count == 0)
            {
                return "-";
            }
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in summary)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(pair.Key).Append(" x").Append(pair.Value);
            }
            return builder.ToString();
        }

        private static PawnKindDef ReadHarRestriction()
        {
            if (HarStartingKindRestriction == null)
            {
                return null;
            }
            try
            {
                return HarStartingKindRestriction();
            }
            catch (Exception ex)
            {
                PonyLog.TraceOnce("ScenarioReset.HarRestrictionRead", $"Scenarios: HAR starting race restriction not readable ({ex.Message})");
                return null;
            }
        }

        private static FieldInfo[] FindRequestListFields()
        {
            try
            {
                List<FieldInfo> result = new List<FieldInfo>();
                FieldInfo[] fields = typeof(StartingPawnUtility).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].FieldType == typeof(List<PawnGenerationRequest>))
                    {
                        result.Add(fields[i]);
                    }
                }
                return result.ToArray();
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Scenarios: request lists in StartingPawnUtility not found, reset on scenario change disabled", ex);
                return Array.Empty<FieldInfo>();
            }
        }

        private static AccessTools.FieldRef<PawnKindDef> ResolveHarStartingKindRestriction()
        {
            try
            {
                FieldInfo field = AccessTools.Field(typeof(AlienRace.HarmonyPatches), "startingPawnKindRestriction");
                if (field == null || !field.IsStatic || field.FieldType != typeof(PawnKindDef))
                {
                    PonyLog.Trace("Scenarios: HAR startingPawnKindRestriction not found, starting race diagnostics off");
                    return null;
                }
                return AccessTools.StaticFieldRefAccess<PawnKindDef>(field);
            }
            catch (Exception ex)
            {
                PonyLog.Trace($"Scenarios: HAR startingPawnKindRestriction not accessible ({ex.Message}), starting race diagnostics off");
                return null;
            }
        }
    }
}