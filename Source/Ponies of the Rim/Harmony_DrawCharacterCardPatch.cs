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
            if (pawn == null || !PonyHelper.IsPony(pawn) || !pawn.DevelopmentalStage.Adult() || pawn.IsMutant || pawn.health.hediffSet.HasHediff(HediffDefOf.ShamblerCorpse) || pawn.Corpse.GetRotStage() == RotStage.Dessicated)
            {
                return;
            }

            float num = CharacterCardUtility.PawnCardSize(pawn).x - 85f + 40f;
            float num2 = CharacterCardUtility.PawnCardSize(pawn).y - 500f + 100f;
            float num3 = CharacterCardUtility.PawnCardSize(pawn).y - 500f + 200f;
            Rect inRect = new Rect(0f, 150f, 80f, 80f);
            Rect inRect2 = new Rect(0f, 150f, 50f, 50f);
            Rect inRect3 = new Rect(150f, 150f, 80f, 80f);
            ThingDef_AlienRace pawnrace = (ThingDef_AlienRace)pawn.def;
            List<AlienPartGenerator.BodyAddon> list = pawnrace.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat<AlienPartGenerator.BodyAddon>(Utilities.UniversalBodyAddons).ToList();
            AlienPartGenerator.BodyAddon cutiemarkBodyAddon = list.Find(ba => ba.Name.Contains("Cutiemark"));
            AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            Rect rect = new Rect(num + 230f, num - 365f, inRect.width, inRect.height);
            Rect rect2 = new Rect(450f, num2, inRect2.width, inRect2.height);
            Rect rect3 = new Rect(500f, num3, inRect3.width, inRect3.height);

            if (Find.WindowStack.currentlyDrawnWindow is Dialog_InfoCard == false)
            {
                DrawCutiemarkIcon.Draw(pawn, rect, cutiemarkBodyAddon, alienComp);
                if (Mouse.IsOver(rect) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect, "PawnMakingUICutieMarkTip".Translate());
                }
                if (Widgets.ButtonInvisible(rect))
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Find.WindowStack.Add(new CutiemarkSelector(pawn,cutiemarkBodyAddon, alienComp));
                }
            }

            if (Find.CurrentMap != null && Find.WindowStack.currentlyDrawnWindow is Dialog_InfoCard == false)
            {
                DrawCutiemarkIcon.Draw(pawn, rect2, cutiemarkBodyAddon, alienComp);
                if (Mouse.IsOver(rect2) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect, "PawnMakingUICutieMarkTip".Translate());
                }
            }
            if (Find.WindowStack.WindowOfType<Dialog_InfoCard>() != null && Find.CurrentMap != null)
            {
                DrawCutiemarkIcon.Draw(pawn, rect3, cutiemarkBodyAddon, alienComp);
                if (Mouse.IsOver(rect3) || DebugViewSettings.drawTooltipEdges)
                {
                    TooltipHandler.TipRegion(rect, "PawnMakingUICutieMarkTip".Translate());
                }
            }
        }
    }
}
