using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class Crystalpony_CrystalizeGraphics_Bootstrap
    {
        public static class Patch_AlienComp_CompRenderNodes
        {
            public static void Postfix(AlienPartGenerator.AlienComp __instance)
            {
                CrystalponyGraphicBaker.TryBakeAddons(__instance);
            }
        }

        public static class Patch_AlienComp_RegenerateAddonsForced
        {
            public static void Postfix(AlienPartGenerator.AlienComp __instance)
            {
                CrystalponyGraphicBaker.TryBakeAddons(__instance);
            }
        }

        public static class Patch_PawnRenderNodeHair_GraphicForPawn
        {
            public static void Postfix(Pawn pawn, ref Graphic __result)
            {
                CrystalponyGraphicBaker.TryCrystalizeHair(pawn, ref __result);
            }
        }

                                        
        private static int refreshFramesLeft = -1;

        public static class Patch_ConfigureStartingPawns_PreOpen
        {
            public static void Postfix()
            {
                refreshFramesLeft = 2;
            }
        }

        public static class Patch_ConfigureStartingPawns_DoWindowContents
        {
            public static void Postfix()
            {
                if (refreshFramesLeft <= 0) return;
                refreshFramesLeft--;
                if (refreshFramesLeft == 0)
                {
                    PortraitsCache.Clear();
                }
            }
        }

        
        internal static class CrystalponyPortraitFix
{
    private static readonly HashSet<int> queued = new();

            public static void MarkDirtyLater(Pawn pawn)
            {
                if (pawn == null) return;
                int key = pawn.thingIDNumber;
                if (!queued.Add(key)) return;

                                refreshFramesLeft = 2;

                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    queued.Remove(key);
                    if (pawn == null) return;
                    PortraitsCache.SetDirty(pawn);
                });
            }
        }

        [StaticConstructorOnStartup]
        internal static class CrystalponyGraphicBaker
        {
            private const string TargetRaceDefName = "Pony_Crystalpony";
            private const string CrystalOverlayPath = "Races/Addons/Crystal/CrystalOverlay";
            private const string BakedSuffix = "_CrystalponyBaked";

            private static readonly HashSet<string> CrystalizeAddons =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Pony_Body", "Pony_Head", "Pony_Tail",
                    "Pony_Right_Ear", "Pony_Left_Ear", "Pony_Cutiemark"
                };

            
            private static readonly FieldInfo FI_nodeProps =
                AccessTools.Field(typeof(AlienPartGenerator.AlienComp), "nodeProps");

            private static readonly MethodInfo MI_memberwiseClone =
                AccessTools.Method(typeof(object), "MemberwiseClone");

            private static readonly FieldInfo FI_graphicMulti_mats =
                AccessTools.Field(typeof(Graphic_Multi), "mats");

            private static Type cachedPropsType;
            private static FieldInfo cachedF_addon;
            private static FieldInfo cachedF_graphic;
            private static FieldInfo cachedF_node;

            private static bool ResolvePropsFields(Type propsType)
            {
                if (propsType == cachedPropsType)
                    return cachedF_addon != null && cachedF_graphic != null;

                cachedPropsType = propsType;
                cachedF_addon   = AccessTools.Field(propsType, "addon");
                cachedF_graphic = AccessTools.Field(propsType, "graphic");
                cachedF_node    = AccessTools.Field(propsType, "node");
                return cachedF_addon != null && cachedF_graphic != null;
            }

            private static readonly Dictionary<Type, MethodInfo> updateGraphicCache = new();

            private static ThingDef cachedTargetDef;
            private static bool targetDefResolved;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsCrystalPony(Pawn pawn)
            {
                if (pawn?.def == null) return false;
                if (!targetDefResolved)
                {
                    cachedTargetDef = DefDatabase<ThingDef>.GetNamed(TargetRaceDefName, errorOnFail: false);
                    targetDefResolved = true;
                }
                return pawn.def == cachedTargetDef;
            }

            
            private readonly struct MatCloneKey : IEquatable<MatCloneKey>
            {
                public readonly int srcMatId;
                public readonly int finalTexId;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public MatCloneKey(int m, int t) { srcMatId = m; finalTexId = t; }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool Equals(MatCloneKey o) =>
                    srcMatId == o.srcMatId && finalTexId == o.finalTexId;

                public override bool Equals(object obj) =>
                    obj is MatCloneKey o && Equals(o);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override int GetHashCode()
                {
                    unchecked { return (srcMatId * 397) ^ finalTexId; }
                }
            }

            private readonly struct OverlayKey : IEquatable<OverlayKey>
            {
                public readonly int baseId, overlayId, ox, oy;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public OverlayKey(int b, int o, int x, int y)
                {
                    baseId = b; overlayId = o; ox = x; oy = y;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool Equals(OverlayKey other) =>
                    baseId == other.baseId && overlayId == other.overlayId
                    && ox == other.ox && oy == other.oy;

                public override bool Equals(object obj) =>
                    obj is OverlayKey other && Equals(other);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override int GetHashCode()
                {
                    unchecked
                    {
                        int h = baseId;
                        h = (h * 397) ^ overlayId;
                        h = (h * 397) ^ ox;
                        h = (h * 397) ^ oy;
                        return h;
                    }
                }
            }

            private static Texture2D crystalCached;
            private static readonly Dictionary<OverlayKey, Texture2D> overlayCache =
                new Dictionary<OverlayKey, Texture2D>(256);

            private const int MaxCacheSize = 4096;

                                    
            public static void TryBakeAddons(AlienPartGenerator.AlienComp comp)
            {
                try
                {
                    if (comp == null) return;

                    Pawn pawn = comp.parent as Pawn;
                    if (!IsCrystalPony(pawn)) return;

                    var nodePropsList = FI_nodeProps?.GetValue(comp) as IList;
                    if (nodePropsList == null || nodePropsList.Count == 0) return;

                    Type propsType = nodePropsList[0].GetType();
                    if (!ResolvePropsFields(propsType)) return;

                    Texture2D crystal = GetCrystalOverlay();
                    if (crystal == null) return;

                    bool changedAnything = false;

                    for (int i = 0; i < nodePropsList.Count; i++)
                    {
                        object p = nodePropsList[i];

                        var addon = cachedF_addon.GetValue(p) as AlienPartGenerator.BodyAddon;
                        if (addon == null) continue;

                        string addonName = (addon.Name ?? "").Trim();
                        if (!CrystalizeAddons.Contains(addonName)) continue;

                        var g = cachedF_graphic.GetValue(p) as Graphic;
                        if (g == null) continue;
                        if (IsAlreadyBaked(g)) continue;

                        if (TryApplyCrystalOverlayToGraphic(g, crystal, out var newG))
                        {
                            cachedF_graphic.SetValue(p, newG);
                            TryUpdateNodeGraphic(cachedF_node, p);
                            changedAnything = true;
                        }
                    }

                    if (changedAnything)
                    {
                        CrystalponyPortraitFix.MarkDirtyLater(pawn);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[CrystalponyGraphicBaker] addon bake failed: {e}");
                }
            }

            public static void TryCrystalizeHair(Pawn pawn, ref Graphic hairGraphic)
            {
                try
                {
                    if (pawn == null || hairGraphic == null) return;
                    if (!IsCrystalPony(pawn)) return;
                    if (IsAlreadyBaked(hairGraphic)) return;

                    Texture2D crystal = GetCrystalOverlay();
                    if (crystal == null) return;

                    if (TryApplyCrystalOverlayToGraphic(hairGraphic, crystal, out var newG))
                    {
                        hairGraphic = newG;
                        CrystalponyPortraitFix.MarkDirtyLater(pawn);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[CrystalponyGraphicBaker] hair bake failed: {e}");
                }
            }

                                    
            private static Texture2D GetCrystalOverlay()
            {
                if (crystalCached != null) return crystalCached;
                crystalCached = ContentFinder<Texture2D>.Get(CrystalOverlayPath, reportFailure: false);
                return crystalCached;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsAlreadyBaked(Graphic g)
            {
                if (g == null) return false;
                if (!TryGetMats(g, out var mats) || mats.Length == 0) return false;

                var m0 = mats[0];
                return m0 != null
                    && m0.name != null
                    && m0.name.EndsWith(BakedSuffix, StringComparison.Ordinal);
            }

            private static bool TryGetMats(Graphic g, out Material[] mats)
            {
                mats = null;
                if (g is not Graphic_Multi || FI_graphicMulti_mats == null) return false;
                mats = FI_graphicMulti_mats.GetValue(g) as Material[];
                return mats != null;
            }

            private static void TryUpdateNodeGraphic(FieldInfo f_node, object props)
            {
                if (f_node == null) return;

                object nodeObj = f_node.GetValue(props);
                if (nodeObj == null) return;

                Type nodeType = nodeObj.GetType();
                if (!updateGraphicCache.TryGetValue(nodeType, out var mi))
                {
                    mi = AccessTools.Method(nodeType, "UpdateGraphic");
                    updateGraphicCache[nodeType] = mi;
                }
                mi?.Invoke(nodeObj, null);
            }

            private static bool TryApplyCrystalOverlayToGraphic(
                Graphic src, Texture2D crystal, out Graphic result)
            {
                result = null;

                if (src == null || crystal == null) return false;
                if (!TryGetMats(src, out var mats) || mats.Length != 4) return false;

                var newMats = new Material[4];
                var cloneCache = new Dictionary<MatCloneKey, Material>(16);

                bool anyApplied = false;

                for (int dir = 0; dir < 4; dir++)
                {
                    var baseMat = mats[dir];
                    if (baseMat == null) { newMats[dir] = null; continue; }

                    var baseTex = baseMat.mainTexture as Texture2D;

                    if (baseTex == null)
                    {
                        var k0 = new MatCloneKey(baseMat.GetInstanceID(), 0);
                        if (!cloneCache.TryGetValue(k0, out var nm0))
                        {
                            nm0 = UnityEngine.Object.Instantiate(baseMat);
                            nm0.name = baseMat.name + BakedSuffix;
                            cloneCache[k0] = nm0;
                        }

                        newMats[dir] = nm0;
                        continue;
                    }

                    var over = ApplyOverlayCached(baseTex, crystal, 0, 0);
                    if (over != null) anyApplied = true;

                    Texture2D finalTex = over ?? baseTex;

                    var key = new MatCloneKey(baseMat.GetInstanceID(), finalTex.GetInstanceID());
                    if (!cloneCache.TryGetValue(key, out var nm))
                    {
                        nm = UnityEngine.Object.Instantiate(baseMat);
                        nm.name = baseMat.name + BakedSuffix;
                        nm.mainTexture = finalTex;
                        cloneCache[key] = nm;
                    }

                    newMats[dir] = nm;
                }

                if (!anyApplied) return false;

                result = (Graphic)MI_memberwiseClone.Invoke(src, null);
                FI_graphicMulti_mats.SetValue(result, newMats);
                return true;
            }

            private static Texture2D ApplyOverlayCached(
                Texture2D baseTex, Texture2D overlay, int ox, int oy)
            {
                if (baseTex == null || overlay == null) return null;

                var key = new OverlayKey(
                    baseTex.GetInstanceID(), overlay.GetInstanceID(), ox, oy);

                if (overlayCache.TryGetValue(key, out var cached) && cached != null)
                    return cached;

                if (overlayCache.Count >= MaxCacheSize)
                {
                    Log.Warning("[CrystalponyGraphicBaker] Cache limit reached, clearing old entries");
                    overlayCache.Clear();
                }

                var t = CombineTexturesAlpha(baseTex, overlay, ox, oy);
                if (t != null) overlayCache[key] = t;

                return t;
            }

            private static Texture2D MakeReadable(Texture2D src)
            {
                if (src == null) return null;
                return src.isReadable ? src : TextureAtlasHelper.MakeReadableTextureInstance(src);
            }

            private static Texture2D CombineTexturesAlpha(
                Texture2D background, Texture2D overlay, int startX, int startY)
            {
                Texture2D bg = null;
                Texture2D ov = null;

                try
                {
                    bg = MakeReadable(background);
                    ov = MakeReadable(overlay);

                    if (bg == null || ov == null) return null;

                    int w = bg.width;
                    int h = bg.height;

                    var bgPx = bg.GetPixels32();
                    var ovPx = ov.GetPixels32();
                    int ovW = ov.width;
                    int ovH = ov.height;

                    var outPx = new Color32[bgPx.Length];

                    for (int y = 0; y < h; y++)
                    {
                        int row = y * w;

                        for (int x = 0; x < w; x++)
                        {
                            int idx = row + x;
                            var baseC = bgPx[idx];

                            if (baseC.a == 0)
                            {
                                outPx[idx] = baseC;
                                continue;
                            }

                            int oxx = x - startX;
                            int oyy = y - startY;

                            if ((uint)oxx < (uint)ovW && (uint)oyy < (uint)ovH)
                            {
                                var overC = ovPx[oyy * ovW + oxx];
                                byte a = overC.a;

                                if (a == 0)
                                {
                                    outPx[idx] = baseC;
                                }
                                else
                                {
                                    int invA = 255 - a;
                                    byte r = (byte)((baseC.r * invA + overC.r * a) / 255);
                                    byte g = (byte)((baseC.g * invA + overC.g * a) / 255);
                                    byte b = (byte)((baseC.b * invA + overC.b * a) / 255);
                                    outPx[idx] = new Color32(r, g, b, baseC.a);
                                }
                            }
                            else
                            {
                                outPx[idx] = baseC;
                            }
                        }
                    }

                    var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    outTex.hideFlags = HideFlags.HideAndDontSave;
                    outTex.SetPixels32(outPx);
                    outTex.Apply(false, false);
                    return outTex;
                }
                catch (Exception e)
                {
                    Log.Warning($"[CrystalponyGraphicBaker] texture combine failed: {e}");
                    return null;
                }
                finally
                {
                    if (bg != null && bg != background) UnityEngine.Object.Destroy(bg);
                    if (ov != null && ov != overlay)     UnityEngine.Object.Destroy(ov);
                }
            }
        }
    }
}