using System;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim
{
    public static class PatchesForPonySettings
    {
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