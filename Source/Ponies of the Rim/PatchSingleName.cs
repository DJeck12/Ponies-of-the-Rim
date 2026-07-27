using System.Reflection;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim
{
    internal static class Patch_DialogNamePawn_SingleName
    {
        public static void Ctor_Postfix(Dialog_NamePawn __instance, Pawn pawn)
        {
            if (pawn?.Name is not NameSingle nameSingle)
                return;

            string singleName = nameSingle.ToStringShort;

            FieldInfo nickField = AccessTools.Field(typeof(Dialog_NamePawn), "curNickName");
            if (nickField != null)
            {
                nickField.SetValue(__instance, singleName);
                return;
            }

            Log.Warning($"[PoniesOfTheRim] Patch_DialogNamePawn_SingleName: " +
                        $"не удалось найти поле curNickName в Dialog_NamePawn. " +
                        $"Поле ника при переименовании '{singleName}' останется пустым.");
        }

        public static void IsValid_Postfix(NameTriple __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!__instance.Nick.NullOrEmpty() && __instance.Last.NullOrEmpty())
                __result = true;
        }
    }
}