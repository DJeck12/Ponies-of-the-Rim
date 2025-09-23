using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using System.Linq;
using Verse.Sound;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class DrawCharacterCardPatch
    {
        static DrawCharacterCardPatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(CharacterCardUtility), "DrawCharacterCard"), null, new HarmonyMethod(typeof(DrawCharacterCardPatch).GetMethod("CutiemarkIcon")));
        }

        [HarmonyPostfix]
        public static void CutiemarkIcon(Pawn pawn)
        {
            if (pawn == null || !PonyHelper.IsPony(pawn) ||
                pawn.IsMutant || pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse) || 
                pawn.Corpse.GetRotStage() == RotStage.Dessicated)
            {
                return;
            }

            bool isRandomPlusActive = ModLister.GetActiveModWithIdentifier("mastertea.RandomPlus") != null;
            float offsetX = isRandomPlusActive ? -50f : 0f;
            float num = CharacterCardUtility.PawnCardSize(pawn).x - 85f + 40f;
            float num2 = CharacterCardUtility.PawnCardSize(pawn).y - 500f + 100f;
            float num3 = CharacterCardUtility.PawnCardSize(pawn).y - 500f + 200f;
            Rect inRect = new Rect(0f, 150f, 80f, 80f);
            Rect inRect2 = new Rect(0f, 150f, 50f, 50f);
            Rect inRect3 = new Rect(150f, 150f, 80f, 80f);
            ThingDef_AlienRace pawnrace = (ThingDef_AlienRace)pawn.def;
            List<AlienPartGenerator.BodyAddon> list = pawnrace.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat<AlienPartGenerator.BodyAddon>(Utilities.UniversalBodyAddons).ToList();

            // Find specific addons based on race
            AlienPartGenerator.BodyAddon cutiemarkBodyAddon = null;
            AlienPartGenerator.BodyAddon tailBodyAddon = null;
            int cutieIndex = -1;
            int tailIndex = -1;

            if (pawn.IsEarthpony() || pawn.IsPegasus() || pawn.IsUnicorn() || pawn.IsCrystalpony() || pawn.IsBatpony() || pawn.IsAlicorn())
            {
                cutiemarkBodyAddon = list.Find(ba => ba.Name.Contains("Cutiemark"));
                
            }
            if (pawn.IsPony() && !pawn.IsZebra())
            {
                tailBodyAddon = list.Find(ba => ba.Name.Contains("Tail"));
            }
            else if (pawn.IsZebra())
            {
                cutiemarkBodyAddon = list.Find(ba => ba.Name.Contains("Zebra Cutiemark"));
                tailBodyAddon = list.Find(ba => ba.Name.Contains("Long tail"));
            }

            if (cutiemarkBodyAddon != null)
            {
                cutieIndex = list.IndexOf(cutiemarkBodyAddon);
            }
            if (tailBodyAddon != null)
            {
                tailIndex = list.IndexOf(tailBodyAddon);
            }

            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            Rect cutiemarkRect = new Rect(num + 230f + offsetX, num - 365f, inRect.width, inRect.height);
            Rect tailRect = new Rect(num + 230f + offsetX - 85f + inRect.width, num - 365f + 80f, 90f, 20f); // Tail button
            Rect rect2 = new Rect(450f + offsetX, num2, inRect2.width, inRect2.height);
            Rect rect3 = new Rect(500f + offsetX, num3, inRect3.width, inRect3.height);

            if (Find.WindowStack.currentlyDrawnWindow is not Dialog_InfoCard && Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                //if (pawn.DevelopmentalStage.Adult() && cutiemarkBodyAddon != null)
                //{
                    if (cutiemarkBodyAddon != null && cutieIndex >= 0)
                    {
                        DrawCutiemarkIcon.Draw(pawn, cutiemarkRect, cutiemarkBodyAddon, alienComp, cutieIndex);
                    }
                    if (Mouse.IsOver(cutiemarkRect) || DebugViewSettings.drawTooltipEdges)
                    {
                        TooltipHandler.TipRegion(cutiemarkRect, "PawnMakingUICutieMarkTip".Translate());
                    }
                    if (Widgets.ButtonInvisible(cutiemarkRect) && cutieIndex >= 0)
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Find.WindowStack.Add(new CutiemarkSelector(pawn, cutiemarkBodyAddon, alienComp, cutieIndex));
                    }
                //}

                if (tailBodyAddon != null && tailIndex >= 0)
                {
                    if (Widgets.ButtonText(tailRect, "Select Tail".Translate()))
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Find.WindowStack.Add(new TailSelector(pawn, tailBodyAddon, alienComp, tailIndex));
                        Log.Message($"Аддон: { tailBodyAddon.Name} Индекс: { tailIndex}");
                    }
                    if (Mouse.IsOver(tailRect) || DebugViewSettings.drawTooltipEdges)
                    {
                        TooltipHandler.TipRegion(tailRect, "Select a tail for this pony.".Translate());
                    }
                }
            }

            if (Find.CurrentMap != null && Find.WindowStack.currentlyDrawnWindow is not Dialog_InfoCard && Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                if (cutiemarkBodyAddon != null && cutieIndex >= 0)
                {
                    DrawCutiemarkIcon.Draw(pawn, rect2, cutiemarkBodyAddon, alienComp, cutieIndex);
                }
                if (Mouse.IsOver(rect2) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect2, "PawnMakingUICutieMarkTip".Translate());
                }
            }

            if (Find.WindowStack.WindowOfType<Dialog_InfoCard>() != null && Find.CurrentMap != null && Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                if (cutiemarkBodyAddon != null && cutieIndex >= 0)
                {
                    DrawCutiemarkIcon.Draw(pawn, rect3, cutiemarkBodyAddon, alienComp, cutieIndex);
                }
                if (Mouse.IsOver(rect3) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect3, "PawnMakingUICutieMarkTip".Translate());
                }
            }
        }
    }
}