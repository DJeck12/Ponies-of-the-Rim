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
            //!pawn.DevelopmentalStage.Adult() 
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
            AlienPartGenerator.BodyAddon cutiemarkBodyAddon = list.Find(ba => ba.Name.Contains("Cutiemark"));
            AlienPartGenerator.BodyAddon tailBodyAddon = list.Find(ba => ba.Name.Contains("Tail"));
            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            Rect cutiemarkRect = new Rect(num + 230f + offsetX, num - 365f, inRect.width, inRect.height);
            Rect tailRect = new Rect(num + 230f + offsetX - 85f + inRect.width, num - 365f + 80f, 90f, 20f); // Tail button
            Rect rect2 = new Rect(450f + offsetX, num2, inRect2.width, inRect2.height);
            Rect rect3 = new Rect(500f + offsetX, num3, inRect3.width, inRect3.height);

            if (Find.WindowStack.currentlyDrawnWindow is not Dialog_InfoCard && Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                //if (pawn.DevelopmentalStage.Adult() && cutiemarkBodyAddon != null)
                //{
                    DrawCutiemarkIcon.Draw(pawn, cutiemarkRect, cutiemarkBodyAddon, alienComp);
                    if (Mouse.IsOver(cutiemarkRect) || DebugViewSettings.drawTooltipEdges)
                    {
                        TooltipHandler.TipRegion(cutiemarkRect, "PawnMakingUICutieMarkTip".Translate());
                    }
                    if (Widgets.ButtonInvisible(cutiemarkRect))
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Find.WindowStack.Add(new CutiemarkSelector(pawn, cutiemarkBodyAddon, alienComp));
                    }
                //}

                if (tailBodyAddon != null)
                {
                    if (Widgets.ButtonText(tailRect, "Select Tail".Translate()))
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Find.WindowStack.Add(new TailSelector(pawn, tailBodyAddon, alienComp));
                    }
                    if (Mouse.IsOver(tailRect) || DebugViewSettings.drawTooltipEdges)
                    {
                        TooltipHandler.TipRegion(tailRect, "Select a tail for this pony.".Translate());
                    }
                }
            }

            if (Find.CurrentMap != null && Find.WindowStack.currentlyDrawnWindow is not Dialog_InfoCard && Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                DrawCutiemarkIcon.Draw(pawn, rect2, cutiemarkBodyAddon, alienComp);
                if (Mouse.IsOver(rect2) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect2, "PawnMakingUICutieMarkTip".Translate());
                }
            }

            if (Find.WindowStack.WindowOfType<Dialog_InfoCard>() != null && Find.CurrentMap != null && Find.WindowStack.WindowOfType<Dialog_GrowthMomentChoices>() == null)
            {
                DrawCutiemarkIcon.Draw(pawn, rect3, cutiemarkBodyAddon, alienComp);
                if (Mouse.IsOver(rect3) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect3, "PawnMakingUICutieMarkTip".Translate());
                }
            }
        }
    }
}
