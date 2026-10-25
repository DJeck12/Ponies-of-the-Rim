using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueCleanGeneration
    {
        public static MethodInfo OldAgeInjuriesTarget()
        {
            return FindPawnStep(typeof(AgeInjuryUtility), "GenerateRandomOldAgeInjuries");
        }

        public static MethodInfo TechHediffsTarget()
        {
            return FindPawnStep(typeof(PawnTechHediffsGenerator), "GenerateTechHediffsFor");
        }

        public static MethodInfo AddictionsTarget()
        {
            return FindPawnStep(typeof(PawnAddictionHediffsGenerator), "GenerateAddictionsAndTolerancesFor");
        }
        public static bool OldAgeInjuries_Prefix(Pawn pawn)
        {
            return !IsUnique(pawn);
        }
        public static bool TechHediffs_Prefix(Pawn pawn)
        {
            return !IsUnique(pawn) || !pawn.kindDef.techHediffsRequired.NullOrEmpty();
        }
        public static bool Addictions_Prefix(Pawn pawn)
        {
            return !IsUnique(pawn);
        }

        private static bool IsUnique(Pawn pawn)
        {
            PawnKindDef kind = pawn?.kindDef;
            return kind != null && UniquePawnConfig.ByKindDef.ContainsKey(kind.defName);
        }
        private static MethodInfo FindPawnStep(Type type, string methodName)
        {
            List<MethodInfo> methods = AccessTools.GetDeclaredMethods(type);
            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                if (!method.IsStatic || method.Name != methodName)
                {
                    continue;
                }
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Pawn))
                {
                    return method;
                }
            }
            return null;
        }
    }
}