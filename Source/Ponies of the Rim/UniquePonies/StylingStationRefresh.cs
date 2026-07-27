using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class StylingStationRefresh
    {
        private static bool _cacheInitialized;
        private static FieldInfo _pawnField;

                private static readonly Dictionary<int, string> _signatureCache = new();
        private static int _cleanupCounter;
        private const int CleanupInterval = 300;

        private static void InitCacheIfNeeded()
        {
            if (_cacheInitialized) return;
            _cacheInitialized = true;

            try
            {
                var stylingType = AccessTools.TypeByName("AlienRace.StylingStation");
                if (stylingType != null)
                    _pawnField = AccessTools.Field(stylingType, "pawn");
            }
            catch (Exception ex)
            {
                Log.Warning($"[PoniesOfTheRim] StylingStation reflection init failed: {ex.Message}");
            }
        }

        public static void DoAddonInfo_Postfix()
        {
            InitCacheIfNeeded();
            if (_pawnField == null || !(_pawnField.GetValue(null) is Pawn pawn))
            {
                return;
            }
            if (!pawn.IsPony())
            {
                return;
            }
            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp == null)
            {
                return;
            }

            var curSig = string.Join(",", alienComp.addonVariants ?? new List<int>());
            int pawnId = pawn.thingIDNumber;

            if (!_signatureCache.TryGetValue(pawnId, out var prev) || prev != curSig)
            {
                _signatureCache[pawnId] = curSig;
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(pawn);
            }

            if (++_cleanupCounter >= CleanupInterval)
            {
                _cleanupCounter = 0;
                if (_signatureCache.Count > 20)
                    _signatureCache.Clear();
            }
        }
    }
}