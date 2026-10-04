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
                    PonyLog.WarnCaught("Сценарии: не удалось прочитать запросы стартовых пешек — сброс пропущен.", ex);
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
                PonyLog.TraceOnce("ScenarioReset.NoStaticList", "Сценарии: StartingPawnUtility не хранит запросы стартовых пешек в статическом поле — сбрасывать нечего.");
            }
            else if (cleared > 0)
            {
                PonyLog.Trace("Сценарии: перед настройкой «" + scenario + "» сброшено запросов стартовых пешек от прошлой настройки: " + cleared + " (" + JoinSummary(summary) + ").");
            }
            PawnKindDef restriction = ReadHarRestriction();
            if (restriction != null)
            {
                PonyLog.Trace("Сценарии: в HAR выбрана стартовая раса «" + (restriction.label ?? restriction.defName) + "» (" + restriction.defName + "). "
                    + "HAR применяет её в каждом сценарии, где этот вид указан в startingColonists фракции игрока (даже с шансом 0), "
                    + "пока на иконке рядом с «Случайно» не выбрано «Нет».");
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
                    key = (request.KindDef?.defName ?? "без вида") + " / " + (request.Faction?.def?.defName ?? "без фракции");
                }
                catch (Exception)
                {
                    key = "не прочитан";
                }
                int count;
                summary.TryGetValue(key, out count);
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
                builder.Append(pair.Key).Append(" ×").Append(pair.Value);
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
                PonyLog.TraceOnce("ScenarioReset.HarRestrictionRead", "Сценарии: не удалось прочитать выбор стартовой расы HAR: " + ex.Message);
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
                PonyLog.WarnCaught("Сценарии: не удалось найти запросы стартовых пешек в StartingPawnUtility — сброс при смене сценария отключён.", ex);
                return new FieldInfo[0];
            }
        }

        private static AccessTools.FieldRef<PawnKindDef> ResolveHarStartingKindRestriction()
        {
            try
            {
                FieldInfo field = AccessTools.Field(typeof(AlienRace.HarmonyPatches), "startingPawnKindRestriction");
                if (field == null || !field.IsStatic || field.FieldType != typeof(PawnKindDef))
                {
                    PonyLog.Trace("Сценарии: поле HAR startingPawnKindRestriction не найдено — диагностика выбора стартовой расы отключена.");
                    return null;
                }
                return AccessTools.StaticFieldRefAccess<PawnKindDef>(field);
            }
            catch (Exception ex)
            {
                PonyLog.Trace("Сценарии: нет доступа к выбору стартовой расы HAR (" + ex.Message + ") — диагностика отключена.");
                return null;
            }
        }
    }
}