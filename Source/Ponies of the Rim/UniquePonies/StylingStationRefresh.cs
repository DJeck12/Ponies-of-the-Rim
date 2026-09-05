using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class StylingStationRefresh
    {
        private static bool _cacheInitialized;
        private static FieldInfo _pawnField;

        private static readonly Dictionary<int, string> _signatureCache = new();
        private static readonly StringBuilder _sigBuilder = new StringBuilder(128);
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

            string curSig = BuildSignature(alienComp);
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

        private static string BuildSignature(AlienPartGenerator.AlienComp comp)
        {
            _sigBuilder.Length = 0;

            List<int> variants = comp.addonVariants;
            if (variants != null)
            {
                for (int i = 0; i < variants.Count; i++)
                {
                    _sigBuilder.Append(variants[i]).Append(',');
                }
            }

            _sigBuilder.Append('|');

            var colors = comp.addonColors;
            if (colors != null)
            {
                for (int i = 0; i < colors.Count; i++)
                {
                    AppendColor(colors[i]?.first);
                    AppendColor(colors[i]?.second);
                }
            }

            _sigBuilder.Append('|');

            var channels = comp.ColorChannels;
            if (channels != null)
            {
                foreach (var kv in channels)
                {
                    _sigBuilder.Append(kv.Key).Append(':');
                    AppendColor(kv.Value?.first);
                    AppendColor(kv.Value?.second);
                }
            }

            return _sigBuilder.ToString();
        }

        private static void AppendColor(Color? c)
        {
            if (!c.HasValue)
            {
                _sigBuilder.Append("-;");
                return;
            }
            Color v = c.Value;
            _sigBuilder.Append((int)(v.r * 255f)).Append('_')
                       .Append((int)(v.g * 255f)).Append('_')
                       .Append((int)(v.b * 255f)).Append('_')
                       .Append((int)(v.a * 255f)).Append(';');
        }
    }
}