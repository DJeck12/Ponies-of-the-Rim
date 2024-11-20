using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using System;
using Verse.Sound;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class StylingStationPatch
    {
        private static readonly Type patchType = typeof(StylingStationPatch);
        static StylingStationPatch()
        {
            Harmony harmonyInstance = new Harmony("Rimworld.Pony.PoniesOfTheRim");
            harmonyInstance.Patch(original: AccessTools.Method(typeof(StylingStation), "DoAddonList"), prefix: new HarmonyMethod(patchType, "DoAddonList_PonyPatch"));
            harmonyInstance.Patch(original: AccessTools.Method(typeof(StylingStation), "DoAddonInfo"), prefix: new HarmonyMethod(patchType, "DoAddonInfo_PonyPatch"));
        }

        public static bool DoAddonList_PonyPatch(ref Rect inRect, ref List<AlienPartGenerator.BodyAddon> addons, ref int ___selectedIndexAddons, ref Vector2 ___addonsScrollPos, ref Pawn ___pawn, ref AlienPartGenerator.AlienComp ___alienComp)
        {
            int num = addons.Count((AlienPartGenerator.BodyAddon ba) => ba.userCustomizable);
            if (___selectedIndexAddons >= addons.Count)
            {
                ___selectedIndexAddons = -1;
            }
            Widgets.DrawMenuSection(inRect);
            if (num <= 0)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(inRect, "HAR.NoAddonsForList".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return false;
            }
            Rect viewRect = new Rect(0f, 0f, 250f, num * 54 + 4);
            Widgets.BeginScrollView(inRect, ref ___addonsScrollPos, viewRect);
            int num2 = -1;
            for (int i = 0; i < addons.Count; i++)
            {
                if (!addons[i].userCustomizable || !addons[i].CanDrawAddonStatic(___pawn))
                {
                    continue;
                }
                num2++;
                Rect rect = new Rect(10f, (float)num2 * 54f + 4f, 240f, 50f).ContractedBy(2f);
                if (i == ___selectedIndexAddons)
                {
                    Widgets.DrawOptionSelected(rect);
                }
                else
                {
                    GUI.color = Widgets.WindowBGFillColor;
                    GUI.DrawTexture(rect, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    bool flag = false;
                    int num3 = num2;
                    while (num3 >= 0 && addons[num3].linkVariantIndexWithPrevious)
                    {
                        num3--;
                        if (___selectedIndexAddons == num3)
                        {
                            flag = true;
                        }
                    }
                    for (num3 = i + 1; num3 <= addons.Count - 1 && addons[num3].linkVariantIndexWithPrevious; num3++)
                    {
                        if (___selectedIndexAddons == num3)
                        {
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        GUI.color = new ColorInt(135, 135, 135).ToColor;
                        Widgets.DrawBox(rect);
                        GUI.color = Color.white;
                    }
                }
                Widgets.DrawHighlightIfMouseover(rect);
                if (Widgets.ButtonInvisible(rect))
                {
                    ___selectedIndexAddons = i;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                Rect position = rect.LeftPartPixels(rect.height).ContractedBy(2f);
                int sharedIndex = ___alienComp.addonVariants[i];
                if (___pawn.IsPony())
                {
                    Texture2D image = ContentFinder<Texture2D>.Get(addons[i].GetPath(___pawn, ref sharedIndex, sharedIndex) + "_east");
                    GUI.color = Widgets.MenuSectionBGFillColor;
                    GUI.DrawTexture(position, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    GUI.DrawTexture(position, image);
                    rect.xMin += rect.height;
                    Widgets.Label(rect.ContractedBy(4f), addons[i].Name);
                }
                else
                {
                    Texture2D image = ContentFinder<Texture2D>.Get(addons[i].GetPath(___pawn, ref sharedIndex, sharedIndex) + "_south");
                    GUI.color = Widgets.MenuSectionBGFillColor;
                    GUI.DrawTexture(position, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    GUI.DrawTexture(position, image);
                    rect.xMin += rect.height;
                    Widgets.Label(rect.ContractedBy(4f), addons[i].Name);
                }

                if (addons[i].linkVariantIndexWithPrevious)
                {
                    GUI.color = new ColorInt(135, 135, 135).ToColor;
                    GUI.DrawTexture(new Rect(rect.x - rect.height - 6f, rect.center.y, 6f, 2f), BaseContent.WhiteTex);
                    GUI.DrawTexture(new Rect(rect.x - rect.height - 6f, (num2 - 1) * 54 + 27, 6f, 2f), BaseContent.WhiteTex);
                    GUI.DrawTexture(new Rect(rect.x - rect.height - 6f, (num2 - 1) * 54 + 27, 2f, 56f), BaseContent.WhiteTex);
                    GUI.color = Color.white;
                }
                AlienPartGenerator.ExposableValueTuple<Color, Color> channel = ___alienComp.GetChannel(addons[i].ColorChannel);
                ValueTuple<Color, Color> valueTuple = new ValueTuple<Color, Color>(___alienComp.addonColors[i].first ?? addons[i].colorOverrideOne ?? channel.first, ___alienComp.addonColors[i].second ?? addons[i].colorOverrideTwo ?? channel.second);
                Rect rect2 = new Rect(rect.xMax - 44f, rect.yMax - 22f, 18f, 18f);
                Widgets.DrawLightHighlight(rect2);
                Widgets.DrawBoxSolid(rect2.ContractedBy(2f), valueTuple.Item1);
                Rect rect3 = new Rect(rect.xMax - 22f, rect.yMax - 22f, 18f, 18f);
                Widgets.DrawLightHighlight(rect3);
                Widgets.DrawBoxSolid(rect3.ContractedBy(2f), valueTuple.Item2);
            }
            Widgets.EndScrollView();
            return false;
        }

        public static bool DoAddonInfo_PonyPatch(ref Rect inRect, AlienPartGenerator.BodyAddon addon, ref List<AlienPartGenerator.BodyAddon> addons, ref AlienPartGenerator.AlienComp ___alienComp, ref int ___selectedIndexAddons, ref bool ___editingFirstColor, ref Vector2 ___colorsScrollPos, ref ThingDef_AlienRace ___alienRaceDef, ref Texture2D ___ClearTex, ref Vector2 ___variantsScrollPos, ref Pawn ___pawn)
        {
            AlienPartGenerator.ExposableValueTuple<Color, Color> channel = ___alienComp.GetChannel(addon.ColorChannel);
            ValueTuple<Color, Color> valueTuple = new ValueTuple<Color, Color>(___alienComp.addonColors[___selectedIndexAddons].first ?? addon.colorOverrideOne ?? channel.first, ___alienComp.addonColors[___selectedIndexAddons].second ?? addon.colorOverrideTwo ?? channel.second);
            Rect rect2;
            if (addon.allowColorOverride)
            {
                List<Color> list = StylingStation.AvailableColors(addon);
                List<Color> list2 = StylingStation.AvailableColors(addon, false);
                if (list.Any() || list2.Any())
                {
                    Rect rect = inRect.BottomPart(0.4f);
                    inRect.yMax -= rect.height;
                    Widgets.DrawMenuSection(rect);
                    Color clear = Color.clear;
                    List<Color> list3 = (___editingFirstColor ? list : list2);
                    List<Color> list4 = new List<Color>(1 + list3.Count);
                    list4.Add(clear);
                    list4.AddRange(list3);
                    List<Color> list5 = list4;
                    rect = rect.ContractedBy(6f);
                    Vector2 size = new Vector2(18f, 18f);
                    rect2 = new Rect(0f, 0f, rect.width - 16f, (Mathf.Ceil((float)list5.Count / ((rect.width - 14f) / size.x)) + 1f) * size.y + 35f);
                    Widgets.BeginScrollView(rect, ref ___colorsScrollPos, rect2);
                    Rect rect3 = rect2.TopPartPixels(30f).ContractedBy(4f);
                    rect2.yMin += 30f;
                    Widgets.Label(rect3, "HAR.Colors".Translate());
                    if (list.Any())
                    {
                        Rect rect4 = new Rect(rect3.xMax - 44f, rect3.y, 18f, 18f);
                        Widgets.DrawLightHighlight(rect4);
                        Widgets.DrawHighlightIfMouseover(rect4);
                        Widgets.DrawBoxSolid(rect4.ContractedBy(2f), valueTuple.Item1);
                        if (___editingFirstColor)
                        {
                            Widgets.DrawBox(rect4);
                        }
                        if (Widgets.ButtonInvisible(rect4))
                        {
                            ___editingFirstColor = true;
                        }
                    }
                    else
                    {
                        ___editingFirstColor = false;
                    }
                    if (list2.Any())
                    {
                        Rect rect4 = new Rect(rect3.xMax - 22f, rect3.y, 18f, 18f);
                        Widgets.DrawLightHighlight(rect4);
                        Widgets.DrawHighlightIfMouseover(rect4);
                        Widgets.DrawBoxSolid(rect4.ContractedBy(2f), valueTuple.Item2);
                        if (!___editingFirstColor)
                        {
                            Widgets.DrawBox(rect4);
                        }
                        if (Widgets.ButtonInvisible(rect4))
                        {
                            ___editingFirstColor = false;
                        }
                    }
                    else
                    {
                        ___editingFirstColor = true;
                    }
                    Rect rect5 = rect2.TopPartPixels(30f).LeftHalf().ContractedBy(4f);
                    rect2.yMin += 30f;
                    if (Widgets.ButtonText(rect5, "HAR.RandomizeColors".Translate()))
                    {
                        AlienPartGenerator.ExposableValueTuple<Color, Color> exposableValueTuple = ___alienComp.GenerateChannel(___alienRaceDef.alienRace.generalSettings.alienPartGenerator.colorChannels.Find((AlienPartGenerator.ColorChannelGenerator ccg) => ccg.name == addon.ColorChannel));
                        if (___editingFirstColor)
                        {
                            ___alienComp.addonColors[___selectedIndexAddons].first = exposableValueTuple.first;
                        }
                        else
                        {
                            ___alienComp.addonColors[___selectedIndexAddons].second = exposableValueTuple.second;
                        }
                    }
                    Vector2 position = new Vector2(0f, 60f);
                    for (int i = 0; i < list5.Count; i++)
                    {
                        Color color = list5[i];
                        Rect rect6 = new Rect(position, size).ContractedBy(1f);
                        Widgets.DrawLightHighlight(rect6);
                        Widgets.DrawHighlightIfMouseover(rect6);
                        if (i == 0)
                        {
                            Widgets.DrawTextureFitted(rect6.ContractedBy(1f), ___ClearTex, 1f);
                        }
                        else
                        {
                            Widgets.DrawBoxSolid(rect6.ContractedBy(1f), color);
                        }
                        if (___editingFirstColor)
                        {
                            if (valueTuple.Item1.IndistinguishableFrom(color))
                            {
                                Widgets.DrawBox(rect6);
                            }
                            if (Widgets.ButtonInvisible(rect6))
                            {
                                ___alienComp.addonColors[___selectedIndexAddons].first = ((i == 0) ? null : new Color?(color));
                            }
                        }
                        else
                        {
                            if (valueTuple.Item2.IndistinguishableFrom(color))
                            {
                                Widgets.DrawBox(rect6);
                            }
                            if (Widgets.ButtonInvisible(rect6))
                            {
                                ___alienComp.addonColors[___selectedIndexAddons].second = ((i == 0) ? null : new Color?(color));
                            }
                        }
                        position.x += size.x;
                        if (position.x + size.x >= rect2.xMax)
                        {
                            position.y += size.y;
                            position.x = 0f;
                        }
                    }
                    Widgets.EndScrollView();
                }
            }
            int variantCountMax = addon.VariantCountMax;
            int num = 4;
            float num2 = inRect.width - 20f;
            float num3;
            for (num3 = num2 / (float)num; num3 > 92f; num3 = num2 / (float)num)
            {
                num++;
            }
            rect2 = new Rect(0f, 0f, num2, Mathf.Ceil((float)variantCountMax / (float)num) * num3);
            Widgets.DrawMenuSection(inRect);
            Widgets.BeginScrollView(inRect, ref ___variantsScrollPos, rect2);
            for (int j = 0; j < variantCountMax; j++)
            {
                Rect rect7 = new Rect((float)(j % num) * num3, Mathf.Floor((float)j / (float)num) * num3, num3, num3).ContractedBy(2f);
                int sharedIndex = j;
                GUI.color = Widgets.WindowBGFillColor;
                GUI.DrawTexture(rect7, (Texture)BaseContent.WhiteTex);
                GUI.color = Color.white;
                Widgets.DrawHighlightIfMouseover(rect7);
                if (___alienComp.addonVariants[___selectedIndexAddons] == j)
                {
                    Widgets.DrawBox(rect7);
                }
                if (___pawn.IsPony())
                {
                    Texture2D texture2D = ContentFinder<Texture2D>.Get(addon.GetPath(___pawn, ref sharedIndex, j) + "_east", false);
                    if (texture2D != null)
                    {
                        GUI.DrawTexture(rect7, (Texture)texture2D);
                    }
                }
                else
                {
                    Texture2D texture2D = ContentFinder<Texture2D>.Get(addon.GetPath(___pawn, ref sharedIndex, j) + "_south", false);
                    if (texture2D != null)
                    {
                        GUI.DrawTexture(rect7, (Texture)texture2D);
                    }
                }
                if (Widgets.ButtonInvisible(rect7))
                {
                    ___alienComp.addonVariants[___selectedIndexAddons] = j;
                    sharedIndex = ___selectedIndexAddons;
                    while (sharedIndex >= 0 && addons[sharedIndex].linkVariantIndexWithPrevious)
                    {
                        sharedIndex--;
                        ___alienComp.addonVariants[sharedIndex] = j;
                    }
                    for (sharedIndex = ___selectedIndexAddons + 1; sharedIndex <= addons.Count - 1 && addons[sharedIndex].linkVariantIndexWithPrevious; sharedIndex++)
                    {
                        ___alienComp.addonVariants[sharedIndex] = j;
                    }
                }
            }
            Widgets.EndScrollView();
            return false;
        }
    }
}









