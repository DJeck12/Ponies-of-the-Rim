using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AlienRace;
using HarmonyLib;
using PoniesOfTheRim.UniquePonies;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class DrawCharacterCardPatch
    {
        private static readonly FieldInfo TmpStackElementsField = AccessTools.Field(typeof(CharacterCardUtility), "tmpStackElements");

        private static readonly FieldInfo TmpMaxStackHeightField = AccessTools.Field(typeof(CharacterCardUtility), "tmpMaxElementStackHeight");

        private static readonly Dictionary<int, CutiemarkAddonResolver.ResolvedAddon> CutiemarkCacheInGame = new Dictionary<int, CutiemarkAddonResolver.ResolvedAddon>();

        private static int _lastCacheClearTick = -1;

        private const int CacheTTLTicks = 120;

        private static Texture2D _hoverCutieTex;

        private static bool _showHoverPreview;

        private static Rect _hoverScreenRect;

        public static void CutiemarkIcon(Pawn pawn)
        {
            DrawHoverPreview();
            if (pawn == null || !(Find.WindowStack.currentlyDrawnWindow is Page_ConfigureStartingPawns))
            {
                return;
            }
            Rect cutiemarkRect = UniqueStartingPawnUI.LastCutiemarkRect;
            if (cutiemarkRect.width <= 0f || pawn.DevelopmentalStage == DevelopmentalStage.Newborn || pawn.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                return;
            }
            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp?.addonVariants == null)
            {
                return;
            }
            if (!CutiemarkAddonResolver.TryResolve(pawn, out CutiemarkAddonResolver.ResolvedAddon cutiemark, out CutiemarkAddonResolver.ResolvedAddon tail) || cutiemark.Index >= alienComp.addonVariants.Count)
            {
                return;
            }
            DrawCutiemarkIcon.Draw(pawn, cutiemarkRect, cutiemark.Addon, alienComp, cutiemark.Index);
            if (Mouse.IsOver(cutiemarkRect) || DebugViewSettings.drawTooltipEdges)
            {
                TooltipHandler.TipRegion(cutiemarkRect, "SelectCutiemark".Translate());
            }
            if (Widgets.ButtonInvisible(cutiemarkRect))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                Find.WindowStack.Add(new CutiemarkSelector(pawn, cutiemark.Addon, alienComp, cutiemark.Index));
            }
            if (!tail.IsValid || tail.Index >= alienComp.addonVariants.Count)
            {
                return;
            }
            Rect tailRect = UniqueStartingPawnUI.LastTailRect;
            if (Widgets.ButtonText(tailRect, "Select Tail"))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                Find.WindowStack.Add(new TailSelector(pawn, tail.Addon, alienComp, tail.Index));
            }
            if (Mouse.IsOver(tailRect) || DebugViewSettings.drawTooltipEdges)
            {
                TooltipHandler.TipRegion(tailRect, "Select a tail for this pony.".Translate());
            }
        }

        public static IEnumerable<CodeInstruction> DoTopStackTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            if (TmpMaxStackHeightField == null)
            {
                PonyLog.Warn("Карточка персонажа: поле tmpMaxElementStackHeight не найдено — метка не будет показана в строке информации пешки.");
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                }
                yield break;
            }
            List<CodeInstruction> codes = instructions.ToList();
            bool injected = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!injected && codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fieldInfo && fieldInfo == TmpMaxStackHeightField)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DrawCharacterCardPatch), "TryInjectCutiemarkInGame"));
                    injected = true;
                }
                yield return codes[i];
            }
            if (!injected)
            {
                PonyLog.Warn("Карточка персонажа: точка вставки в DoTopStack не найдена — метка не будет показана в строке информации пешки.");
            }
        }

        public static void TryInjectCutiemarkInGame(Pawn pawn, bool creationMode)
        {
            if (creationMode || pawn == null || pawn.IsMutant || pawn.DevelopmentalStage == DevelopmentalStage.Newborn || pawn.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                return;
            }
            if (pawn.health?.hediffSet?.HasHediff(HediffDefOf.ShamblerCorpse) == true || (pawn.Corpse != null && pawn.Corpse.GetRotStage() == RotStage.Dessicated))
            {
                return;
            }
            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp?.addonVariants == null)
            {
                return;
            }
            CutiemarkAddonResolver.ResolvedAddon cutiemark = ResolveCutiemarkAddon(pawn);
            if (!cutiemark.IsValid || cutiemark.Index >= alienComp.addonVariants.Count)
            {
                return;
            }
            if (TmpStackElementsField == null)
            {
                PonyLog.WarnOnce("DrawCharacterCardPatch.NoTmpStackElements", "Карточка персонажа: поле tmpStackElements не найдено — метка не будет показана.");
            }
            else if (TmpStackElementsField.GetValue(null) is List<GenUI.AnonymousStackElement> stackElements)
            {
                AlienPartGenerator.BodyAddon cutieAddon = cutiemark.Addon;
                int cutieIdx = cutiemark.Index;
                stackElements.Add(new GenUI.AnonymousStackElement
                {
                    drawer = delegate (Rect r)
                    {
                        DrawCutiemarkStackElement(r, pawn, cutieAddon, alienComp, cutieIdx);
                    },
                    width = 22f
                });
            }
        }

        private static void ClearCacheIfStale()
        {
            int num = ((Current.ProgramState == ProgramState.Playing) ? GenTicks.TicksGame : (-1));
            if (num < 0 || num - _lastCacheClearTick > CacheTTLTicks)
            {
                CutiemarkCacheInGame.Clear();
                _lastCacheClearTick = num;
            }
        }

        public static void InvalidateCache()
        {
            CutiemarkCacheInGame.Clear();
            _lastCacheClearTick = -1;
        }

        private static CutiemarkAddonResolver.ResolvedAddon ResolveCutiemarkAddon(Pawn pawn)
        {
            CutiemarkAddonResolver.ResolvedAddon cutiemark;
            int thingIDNumber = pawn.thingIDNumber;
            if (thingIDNumber <= 0)
            {
                CutiemarkAddonResolver.TryResolve(pawn, out cutiemark, out _);
                return cutiemark;
            }
            ClearCacheIfStale();
            if (CutiemarkCacheInGame.TryGetValue(thingIDNumber, out cutiemark))
            {
                return cutiemark;
            }
            CutiemarkAddonResolver.TryResolve(pawn, out cutiemark, out _);
            CutiemarkCacheInGame[thingIDNumber] = cutiemark;
            return cutiemark;
        }

        private static void DrawCutiemarkStackElement(Rect r, Pawn pawn, AlienPartGenerator.BodyAddon addon, AlienPartGenerator.AlienComp alienComp, int cutieIndex)
        {
            Color color = GUI.color;
            GUI.color = CharacterCardUtility.StackElementBackground;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            GUI.color = color;
            if (Mouse.IsOver(r))
            {
                Widgets.DrawHighlight(r);
            }
            Texture2D texture2D = null;
            try
            {
                int sharedIndex = alienComp.addonVariants[cutieIndex];
                string path = addon.GetPath(pawn, ref sharedIndex, sharedIndex);
                if (!path.NullOrEmpty())
                {
                    texture2D = ContentFinder<Texture2D>.Get(path + "_east", reportFailure: false);
                }
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Не удалось отрисовать метку на карточке персонажа.", ex);
            }
            if (texture2D != null)
            {
                GUI.DrawTexture(r.ContractedBy(1f), texture2D);
            }
            if (Mouse.IsOver(r) && texture2D != null)
            {
                Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(r.center.x, r.yMax));
                _hoverScreenRect = new Rect(vector.x - 64f, vector.y + 4f, 128f, 128f);
                _hoverCutieTex = texture2D;
                _showHoverPreview = true;
                TooltipHandler.TipRegion(r, "Pony_Cutiemark".Translate());
            }
        }

        public static void DrawHoverPreview()
        {
            if (!_showHoverPreview || _hoverCutieTex == null)
            {
                _showHoverPreview = false;
                return;
            }
            _showHoverPreview = false;
            Find.WindowStack.ImmediateWindow(1347375683, _hoverScreenRect, WindowLayer.Super, delegate
            {
                Rect position = new Rect(0f, 0f, _hoverScreenRect.width, _hoverScreenRect.height).ContractedBy(6f);
                GUI.DrawTexture(position, _hoverCutieTex);
            }, doBackground: true, absorbInputAroundWindow: false, 0f);
        }
    }
}