using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Genetics
{
    public static class Patch_PregnancyUtility_RacialGenes
    {
        private static readonly Dictionary<MethodBase, int[]> ParentIndices = new Dictionary<MethodBase, int[]>();

        public static void Register(Harmony harmony)
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }
            PonyRacialGeneUtility.BuildCache();

            int patched = 0;
            patched += PatchOverloads(harmony, "GetInheritedGeneSet", typeof(GeneSet), "GeneSet_Postfix");
            patched += PatchOverloads(harmony, "GetInheritedGenes", typeof(List<GeneDef>), "GeneList_Postfix");

            if (patched == 0)
            {
                Log.Error("[PoniesOfTheRim] Genetics: не пропатчен ни один метод наследования генов — гарантия расового гена не работает.");
            }
        }

        private static int PatchOverloads(Harmony harmony, string methodName, Type returnType, string postfixName)
        {
            List<MethodInfo> candidates = new List<MethodInfo>();
            try
            {
                List<MethodInfo> declared = AccessTools.GetDeclaredMethods(typeof(PregnancyUtility));
                for (int i = 0; i < declared.Count; i++)
                {
                    MethodInfo m = declared[i];
                    if (m.Name != methodName || m.ReturnType != returnType || m.IsGenericMethodDefinition)
                    {
                        continue;
                    }
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length < 2 || ps[0].ParameterType != typeof(Pawn) || ps[1].ParameterType != typeof(Pawn))
                    {
                        continue;
                    }
                    candidates.Add(m);
                }
            }
            catch (Exception e)
            {
                Log.Error("[PoniesOfTheRim] Genetics: ошибка поиска 'PregnancyUtility." + methodName + "':\n" + e);
                return 0;
            }

            if (candidates.Count == 0)
            {
                Log.Warning("[PoniesOfTheRim] Genetics: подходящих перегрузок 'PregnancyUtility." + methodName + "' не найдено — пропущено.");
                return 0;
            }

            HarmonyMethod postfix = new HarmonyMethod(typeof(Patch_PregnancyUtility_RacialGenes), postfixName);
            int count = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                MethodInfo target = candidates[i];
                string signature = methodName + "(" + DescribeParameters(target) + ")";
                try
                {
                    CacheParentIndices(target);
                    harmony.Patch(target, null, postfix);
                    count++;
                    Log.Message("[PoniesOfTheRim] Genetics: ✓ PregnancyUtility." + signature);
                }
                catch (Exception e)
                {
                    Log.Error("[PoniesOfTheRim] Genetics: ошибка патча 'PregnancyUtility." + signature + "':\n" + e);
                }
            }
            return count;
        }

        private static void CacheParentIndices(MethodInfo method)
        {
            int fatherIndex = 0;
            int motherIndex = 1;
            ParameterInfo[] ps = method.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType != typeof(Pawn))
                {
                    continue;
                }
                if (ps[i].Name == "father")
                {
                    fatherIndex = i;
                }
                else if (ps[i].Name == "mother")
                {
                    motherIndex = i;
                }
            }
            ParentIndices[method] = new int[2] { fatherIndex, motherIndex };
        }

        private static string DescribeParameters(MethodInfo method)
        {
            ParameterInfo[] ps = method.GetParameters();
            string[] parts = new string[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                parts[i] = ps[i].ParameterType.Name + " " + ps[i].Name;
            }
            return string.Join(", ", parts);
        }

        public static void GeneSet_Postfix(object[] __args, MethodBase __originalMethod, GeneSet __result)
        {
            if (__result == null)
            {
                return;
            }
            Pawn father;
            Pawn mother;
            ReadParents(__args, __originalMethod, out father, out mother);
            PonyRacialGeneUtility.Normalize(__result.GenesListForReading, mother, father);
        }

        public static void GeneList_Postfix(object[] __args, MethodBase __originalMethod, List<GeneDef> __result)
        {
            if (__result == null)
            {
                return;
            }
            Pawn father;
            Pawn mother;
            ReadParents(__args, __originalMethod, out father, out mother);
            PonyRacialGeneUtility.Normalize(__result, mother, father);
        }

        private static void ReadParents(object[] args, MethodBase method, out Pawn father, out Pawn mother)
        {
            father = null;
            mother = null;
            if (args == null || args.Length < 2)
            {
                return;
            }
            int fatherIndex = 0;
            int motherIndex = 1;
            int[] cached;
            if (method != null && ParentIndices.TryGetValue(method, out cached))
            {
                fatherIndex = cached[0];
                motherIndex = cached[1];
            }
            if (fatherIndex < args.Length)
            {
                father = args[fatherIndex] as Pawn;
            }
            if (motherIndex < args.Length)
            {
                mother = args[motherIndex] as Pawn;
            }
        }
    }
}