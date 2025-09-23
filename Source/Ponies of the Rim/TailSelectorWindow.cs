using RimWorld;
using UnityEngine;
using Verse;
using AlienRace;
using Verse.Sound;

namespace PoniesOfTheRim
{
    public class TailSelector : Window
    {
        public Pawn pawn;
        public AlienPartGenerator.BodyAddon addon;
        private Vector2 tailsScrollPos;
        private int selectedIndexTails;
        readonly AlienPartGenerator.AlienComp alienComp;
        readonly int variantIndex;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(230f, 500f);
            }
        }

        public override string CloseButtonText
        {
            get
            {
                return "PawnMakingUICloseButton".Translate();
            }
        }

        public TailSelector(Pawn pawn, AlienPartGenerator.BodyAddon addon, AlienPartGenerator.AlienComp alienComp, int variantIndex)
        {
            this.pawn = pawn;
            this.addon = addon;
            this.alienComp = alienComp;
            this.variantIndex = variantIndex;
            this.selectedIndexTails = alienComp.addonVariants[variantIndex];
        }

        public override void DoWindowContents(Rect inRect)
        {
            inRect.height = 400f;
            inRect.width = 193f;
            doCloseButton = true;
            closeOnClickedOutside = false;
            doCloseX = false;
            draggable = true;

            Rect viewRect = new Rect(0f, 0f, 150f, addon.variantCount * 154f);
            Widgets.BeginScrollView(inRect, ref tailsScrollPos, viewRect);
            int num2 = -1;
            for (int i = 0; i < addon.variantCount; i++)
            {
                num2++;
                Rect rect = new Rect(10f, (float)num2 * 154f + 4f, 150f, 150f).ContractedBy(2f);
                if (i == selectedIndexTails)
                {
                    Widgets.DrawOptionSelected(rect);
                }
                Widgets.DrawHighlightIfMouseover(rect);
                if (Widgets.ButtonInvisible(rect))
                {
                    selectedIndexTails = i;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    alienComp.addonVariants[variantIndex] = selectedIndexTails;
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                int sharedIndex = i;
                DrawCutiemarkIcon.DrawInSelector(pawn, rect, addon, ref sharedIndex, i);
            }
            Widgets.EndScrollView();
        }
    }
}