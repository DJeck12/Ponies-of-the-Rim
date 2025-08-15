using System;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class PatchesForPonySettings
    {
        private static readonly Harmony harmony = new("Rimworld.Pony.PoniesOfTheRim");

        static PatchesForPonySettings()
        {
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(LoadedModManager), "ApplyPatches")]
        public static class LoadedModManager_ApplyPatches_Patch
        {
            public static void Prefix()
            {
                try
                {
                    var mod = LoadedModManager.GetMod<PoniesOfTheRimSettings>();
                    if (mod != null)
                    {
                        PoniesOfTheRimSettings.settings = mod.GetSettings<PoniesOfTheRimSettingsData>();
                    }
                    else
                    {
                        Log.Warning("PoniesOfTheRimSettings not found.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error initializing PoniesOfTheRim settings: {ex.Message}");
                }
            }
        }
    }
}