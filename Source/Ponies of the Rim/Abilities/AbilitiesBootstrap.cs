using HarmonyLib;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    [StaticConstructorOnStartup]
    public static class AbilitiesBootstrap
    {
        private const string HarmonyId = "PoniesOfTheRim.Abilities";

        static AbilitiesBootstrap()
        {
            Harmony harmony = new Harmony(HarmonyId);

            TryPatch(() => AbilityToggleEnforcer.Register(harmony),
                     "AbilityToggleEnforcer");
        }

        private static void TryPatch(System.Action patchAction, string patchName)
        {
            try
            {
                patchAction();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Abilities: ошибка регистрации патча '{patchName}': {ex}");
            }
        }
    }
}