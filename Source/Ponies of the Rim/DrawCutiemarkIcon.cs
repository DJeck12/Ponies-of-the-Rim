using AlienRace;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class DrawCutiemarkIcon
    {
        public static void Draw(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, AlienPartGenerator.AlienComp alienComp, int variantIndex)
        {
            Widgets.DrawBox(rect);
            if (pawn == null || cutiemarkBodyAddon == null || alienComp?.addonVariants == null || variantIndex < 0 || variantIndex >= alienComp.addonVariants.Count)
            {
                return;
            }
            int sharedIndex = alienComp.addonVariants[variantIndex];
            DrawEastTexture(rect, cutiemarkBodyAddon.GetPath(pawn, ref sharedIndex, sharedIndex));
        }

        public static void DrawInSelector(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, ref int sharedIndex, int variant)
        {
            Widgets.DrawBox(rect);
            if (pawn == null || cutiemarkBodyAddon == null)
            {
                return;
            }
            DrawEastTexture(rect, cutiemarkBodyAddon.GetPath(pawn, ref sharedIndex, variant));
        }

        private static void DrawEastTexture(Rect rect, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            Texture2D texture2D = ContentFinder<Texture2D>.Get(path + "_east", reportFailure: false);
            if (texture2D != null)
            {
                GUI.DrawTexture(rect, texture2D);
            }
        }
    }
}