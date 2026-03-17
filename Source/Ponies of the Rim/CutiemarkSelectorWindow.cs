using RimWorld;
using UnityEngine;
using Verse;
using AlienRace;
using Verse.Sound;

namespace PoniesOfTheRim
{
    public class CutiemarkSelector : Window
    {
        public Pawn pawn;
        public AlienPartGenerator.BodyAddon addon;
        private Vector2 addonsScrollPos;
        private int selectedIndexAddons;
        private readonly AlienPartGenerator.AlienComp alienComp;
        private readonly int variantIndex;

        public override Vector2 InitialSize => new Vector2(230f, 500f);

        public override string CloseButtonText => "PawnMakingUICloseButton".Translate();

        public CutiemarkSelector(
            Pawn pawn,
            AlienPartGenerator.BodyAddon addon,
            AlienPartGenerator.AlienComp alienComp,
            int variantIndex)
        {
            this.pawn        = pawn;
            this.addon       = addon;
            this.alienComp   = alienComp;
            this.variantIndex = variantIndex;

            this.selectedIndexAddons = (variantIndex >= 0 && variantIndex < alienComp.addonVariants.Count)
                ? alienComp.addonVariants[variantIndex]
                : 0;

            doCloseButton        = true;
            closeOnClickedOutside = false;
            doCloseX             = false;
            draggable            = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            inRect.height = 400f;
            inRect.width  = 193f;

            Rect viewRect = new Rect(0f, 0f, 150f, addon.variantCount * 154f);
            Widgets.BeginScrollView(inRect, ref addonsScrollPos, viewRect);

            for (int i = 0; i < addon.variantCount; i++)
            {
                Rect rect = new Rect(10f, i * 154f + 4f, 150f, 150f).ContractedBy(2f);

                if (i == selectedIndexAddons)
                    Widgets.DrawOptionSelected(rect);

                Widgets.DrawHighlightIfMouseover(rect);

                if (Widgets.ButtonInvisible(rect))
                {
                    selectedIndexAddons = i;
                    SoundDefOf.Click.PlayOneShotOnCamera();

                    if (variantIndex >= 0 && variantIndex < alienComp.addonVariants.Count)
                        alienComp.addonVariants[variantIndex] = selectedIndexAddons;

                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }

                int sharedIndex = i;
                DrawCutiemarkIcon.DrawInSelector(pawn, rect, addon, ref sharedIndex, i);
            }

            Widgets.EndScrollView();
        }
    }
}