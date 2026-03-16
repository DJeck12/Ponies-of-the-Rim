using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AlienRace;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class DrawCharacterCardPatch
    {
        private static readonly bool IsRandomPlusActive =
            ModLister.GetActiveModWithIdentifier("mastertea.RandomPlus") != null;
        private static readonly bool IsPersonalitiesActive =
            ModLister.GetActiveModWithIdentifier("hahkethomemah.simplepersonalities") != null;

        private static readonly FieldInfo TmpStackElementsField =
            AccessTools.Field(typeof(CharacterCardUtility), "tmpStackElements");
        private static readonly FieldInfo TmpMaxStackHeightField =
            AccessTools.Field(typeof(CharacterCardUtility), "tmpMaxElementStackHeight");

        private static readonly Dictionary<int, AddonCacheEntry> CutiemarkCacheInGame =
            new Dictionary<int, AddonCacheEntry>();
        private static int _lastCacheClearTick = -1;
        private const int CacheTTLTicks = 120;

        private struct AddonCacheEntry
        {
            public AlienPartGenerator.BodyAddon addon;
            public int variantIndex;
        }

        public static void CutiemarkIcon(Pawn pawn)
        {
            DrawHoverPreview();

            if (pawn == null || !PonyHelper.IsPony(pawn) ||
                pawn.IsMutant ||
                pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse) ||
                (pawn.Corpse != null && pawn.Corpse.GetRotStage() == RotStage.Dessicated))
            {
                return;
            }

            if (Find.CurrentMap != null)
                return;

            float offsetX = (IsRandomPlusActive && !IsPersonalitiesActive) ? -50f : 0f;
            bool isStartingPawnsPage = Find.WindowStack?.currentlyDrawnWindow is Page_ConfigureStartingPawns;
            float cutieYOff = (IsPersonalitiesActive && isStartingPawnsPage) ? 24f : 0f;
            float tailXOff  = (IsPersonalitiesActive && isStartingPawnsPage) ? 206f : 0f;
            float tailYOff  = (IsPersonalitiesActive && isStartingPawnsPage) ? -100f : 0f;

            float num = CharacterCardUtility.PawnCardSize(pawn).x - 85f + 40f;

            Rect inRect = new Rect(0f, 150f, 80f, 80f);

            ThingDef_AlienRace pawnrace = (ThingDef_AlienRace)pawn.def;
            List<AlienPartGenerator.BodyAddon> list = pawnrace.alienRace.generalSettings
                .alienPartGenerator.bodyAddons
                .Concat(Utilities.UniversalBodyAddons).ToList();

            AlienPartGenerator.BodyAddon cutiemarkBodyAddon = null;
            AlienPartGenerator.BodyAddon tailBodyAddon = null;
            int cutieIndex = -1;
            int tailIndex = -1;

            if (pawn.IsZebra())
            {
                cutiemarkBodyAddon = list.Find(ba => ba.Name == "Zebra Cutiemark");
                tailBodyAddon = list.Find(ba => ba.Name == "Long tail");
            }
            else if (pawn.IsEarthpony() || pawn.IsPegasus() || pawn.IsUnicorn() ||
                     pawn.IsCrystalpony() || pawn.IsBatpony() || pawn.IsAlicorn())
            {
                cutiemarkBodyAddon = list.Find(ba => ba.Name == "Cutiemark");
                tailBodyAddon = list.Find(ba => ba.Name == "Tail");
            }

            if (cutiemarkBodyAddon != null)
                cutieIndex = list.IndexOf(cutiemarkBodyAddon);
            if (tailBodyAddon != null)
                tailIndex = list.IndexOf(tailBodyAddon);

            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp == null)
                return;

            float cutieScale = (IsPersonalitiesActive && isStartingPawnsPage) ? 0.9f : 1f;
            float cutieW = inRect.width * cutieScale;
            float cutieH = inRect.height * cutieScale;
            float cutieShiftX = (inRect.width - cutieW) * 0.5f;
            float cutieShiftY = (inRect.height - cutieH) * 0.5f;

            Rect cutiemarkRect = new Rect(
                num + 230f + offsetX + cutieShiftX,
                num - 365f + cutieYOff + cutieShiftY,
                cutieW, cutieH);

            Rect tailRect = new Rect(
                num + 230f + offsetX - 85f + inRect.width + tailXOff,
                num - 365f + 80f + tailYOff,
                90f, 20f);

            if (Find.WindowStack.currentlyDrawnWindow is not Dialog_InfoCard &&
                Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                if (cutiemarkBodyAddon != null && cutieIndex >= 0)
                {
                    if (pawn.DevelopmentalStage == DevelopmentalStage.Newborn ||
                        pawn.DevelopmentalStage == DevelopmentalStage.Baby)
                        return;
                    DrawCutiemarkIcon.Draw(pawn, cutiemarkRect, cutiemarkBodyAddon, alienComp, cutieIndex);
                }
                if (Mouse.IsOver(cutiemarkRect) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(cutiemarkRect, "SelectCutiemark".Translate());
                }
                if (Widgets.ButtonInvisible(cutiemarkRect) && cutieIndex >= 0)
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Find.WindowStack.Add(new CutiemarkSelector(pawn, cutiemarkBodyAddon, alienComp, cutieIndex));
                }

                if (tailBodyAddon != null && tailIndex >= 0)
                {
                    if (Widgets.ButtonText(tailRect, "Select Tail"))
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Find.WindowStack.Add(new TailSelector(pawn, tailBodyAddon, alienComp, tailIndex));
                    }
                    if (Mouse.IsOver(tailRect) || DebugViewSettings.drawTooltipEdges)
                    {
                        TooltipHandler.TipRegion(tailRect, "Select a tail for this pony.".Translate());
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> DoTopStackTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            bool injected = false;

            for (int i = 0; i < codes.Count; i++)
            {
                if (!injected &&
                    codes[i].opcode == OpCodes.Ldsfld &&
                    codes[i].operand is FieldInfo fi &&
                    fi == TmpMaxStackHeightField)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);  
                    yield return new CodeInstruction(OpCodes.Ldarg_2);  
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(DrawCharacterCardPatch), nameof(TryInjectCutiemarkInGame)));
                    injected = true;
                }
                yield return codes[i];
            }

            if (!injected)
            {
                Log.Warning("[PoniesOfTheRim] DoTopStack transpiler: injection point not found. " +
                            "In-game cutiemark will not appear in the info row.");
            }
        }

        public static void TryInjectCutiemarkInGame(Pawn pawn, bool creationMode)
        {
            if (creationMode)
                return;

            if (pawn == null || !PonyHelper.IsPony(pawn))
                return;
            if (pawn.DevelopmentalStage == DevelopmentalStage.Newborn ||
                pawn.DevelopmentalStage == DevelopmentalStage.Baby)
                return;
            if (pawn.IsMutant)
                return;
            if (pawn.health?.hediffSet?.HasHediff(HediffDefOf.ShamblerCorpse) == true)
                return;
            if (pawn.Corpse != null && pawn.Corpse.GetRotStage() == RotStage.Dessicated)
                return;

            var alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp == null)
                return;

            var cutie = ResolveCutiemarkAddon(pawn);
            if (cutie.addon == null || cutie.variantIndex < 0 ||
                cutie.variantIndex >= alienComp.addonVariants.Count)
                return;

            var tmpStack = (List<GenUI.AnonymousStackElement>)TmpStackElementsField.GetValue(null);
            var cutieAddon = cutie.addon;
            int cutieIdx = cutie.variantIndex;

            tmpStack.Add(new GenUI.AnonymousStackElement
            {
                drawer = delegate(Rect r)
                {
                    DrawCutiemarkStackElement(r, pawn, cutieAddon, alienComp, cutieIdx);
                },
                width = 22f
            });
        }

        private static void ClearCacheIfStale()
        {
            int tick = Current.ProgramState == ProgramState.Playing ? GenTicks.TicksGame : -1;
            if (tick < 0 || tick - _lastCacheClearTick > CacheTTLTicks)
            {
                CutiemarkCacheInGame.Clear();
                _lastCacheClearTick = tick;
            }
        }

        private static AddonCacheEntry ResolveCutiemarkAddon(Pawn pawn)
        {
            ClearCacheIfStale();
            int id = pawn.thingIDNumber;
            if (CutiemarkCacheInGame.TryGetValue(id, out var cached))
                return cached;

            var result = FindAddonByExactName(pawn,
                pawn.IsZebra() ? "Zebra Cutiemark" : "Cutiemark");
            CutiemarkCacheInGame[id] = result;
            return result;
        }

        private static AddonCacheEntry FindAddonByExactName(Pawn pawn, string exactName)
        {
            if (!(pawn.def is ThingDef_AlienRace alienRace))
                return default;

            var raceAddons = alienRace.alienRace.generalSettings.alienPartGenerator.bodyAddons;
            var universalAddons = Utilities.UniversalBodyAddons;

            int index = 0;
            foreach (var addon in raceAddons)
            {
                if (addon.Name == exactName)
                    return new AddonCacheEntry { addon = addon, variantIndex = index };
                index++;
            }
            foreach (var addon in universalAddons)
            {
                if (addon.Name == exactName)
                    return new AddonCacheEntry { addon = addon, variantIndex = index };
                index++;
            }
            return default;
        }

        private static Texture2D _hoverCutieTex;
        private static bool _showHoverPreview;
        private static Rect _hoverScreenRect;

        private static void DrawCutiemarkStackElement(Rect r, Pawn pawn,
            AlienPartGenerator.BodyAddon addon, AlienPartGenerator.AlienComp alienComp,
            int cutieIndex)
        {
            Color saved = GUI.color;
            GUI.color = CharacterCardUtility.StackElementBackground;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            GUI.color = saved;

            if (Mouse.IsOver(r))
                Widgets.DrawHighlight(r);

            Texture2D cutieTex = null;
            try
            {
                int value = alienComp.addonVariants[cutieIndex];
                string path = addon.GetPath(pawn, ref value, value, null);
                if (!path.NullOrEmpty())
                    cutieTex = ContentFinder<Texture2D>.Get(path + "_east", false);
            }
            catch (Exception)
            {
            }

            if (cutieTex != null)
                GUI.DrawTexture(r.ContractedBy(1f), cutieTex);

            if (Mouse.IsOver(r) && cutieTex != null)
            {
                Vector2 screenPos = GUIUtility.GUIToScreenPoint(new Vector2(r.center.x, r.yMax));

                const float previewSize = 128f;
                _hoverScreenRect = new Rect(
                    screenPos.x - previewSize / 2f,
                    screenPos.y + 4f,
                    previewSize,
                    previewSize);
                _hoverCutieTex = cutieTex;
                _showHoverPreview = true;

                TooltipHandler.TipRegion(r, "Cutiemark".Translate());
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

            Find.WindowStack.ImmediateWindow(
                0x504F5243,                
                _hoverScreenRect,          
                WindowLayer.Super,         
                delegate                   
                {
                    const float padding = 6f;
                    Rect inner = new Rect(0f, 0f,
                        _hoverScreenRect.width, _hoverScreenRect.height)
                        .ContractedBy(padding);
                    GUI.DrawTexture(inner, _hoverCutieTex);
                },
                true,                      
                false,                     
                0f);                       
        }
    }
}