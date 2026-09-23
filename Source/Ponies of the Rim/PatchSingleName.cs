using Verse;

namespace PoniesOfTheRim
{
    internal static class Patch_DialogNamePawn_SingleName
    {
        public static void IsValid_Postfix(NameTriple __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!__instance.Nick.NullOrEmpty() && __instance.Last.NullOrEmpty())
                __result = true;
        }
    }
}