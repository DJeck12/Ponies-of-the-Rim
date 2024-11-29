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
        private static Vector2 addonsScrollPos;
        private static int selectedIndexAddons = 0;
        AlienPartGenerator.AlienComp alienComp;
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
			    return "OK".Translate();
		    }
	    }
        public CutiemarkSelector(Pawn pawn, AlienPartGenerator.BodyAddon addon, AlienPartGenerator.AlienComp alienComp)
        {
            this.pawn = pawn;
            this.addon = addon;
            this.alienComp = alienComp;
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
		    Widgets.BeginScrollView(inRect, ref addonsScrollPos, viewRect);
            int num2 = -1;
            for (int i = 0; i < addon.variantCount; i++)
            {
                num2++;
                Rect rect = new Rect(10f, (float)num2 * 154f + 4f, 150f, 150f).ContractedBy(2f);
                if (i == selectedIndexAddons)
			    {
				    Widgets.DrawOptionSelected(rect);
			    }
                Widgets.DrawHighlightIfMouseover(rect);
                if (Widgets.ButtonInvisible(rect))
			    {
				    selectedIndexAddons = i;
				    SoundDefOf.Click.PlayOneShotOnCamera();
			    }
                int sharedIndex = i;
                DrawCutiemarkIcon.DrawInSelector(pawn, rect, addon,ref sharedIndex, i);
                if (pawn.IsEarthpony())
                {
                    alienComp.addonVariants[4] = selectedIndexAddons;
                }
                if (pawn.IsUnicorn())
                {
                    alienComp.addonVariants[5] = selectedIndexAddons;
                }
                if (pawn.IsPegasus())
                {
                    alienComp.addonVariants[6] = selectedIndexAddons;
                }
            }
            Widgets.EndScrollView();
        }
    }
}
