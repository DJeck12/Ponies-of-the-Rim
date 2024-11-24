using AlienRace;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class DrawCutiemarkIcon
    {
        public static void Draw(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, AlienPartGenerator.AlienComp alienComp)
        {
            Widgets.DrawBox(rect);
            if (pawn.IsEarthpony())
            {
                int variantE = alienComp.addonVariants[3];
                Texture2D image = ContentFinder<Texture2D>.Get(cutiemarkBodyAddon.GetPath(pawn, ref variantE, variantE) + "_east");
                GUI.DrawTexture(rect, image);
            }
            if (pawn.IsUnicorn())
            {
                int variantU = alienComp.addonVariants[4];
                Texture2D image = ContentFinder<Texture2D>.Get(cutiemarkBodyAddon.GetPath(pawn, ref variantU, variantU) + "_east");
                GUI.DrawTexture(rect, image);
            }
            if (pawn.IsPegasus())
            {
                int variantP = alienComp.addonVariants[5];
                Texture2D image = ContentFinder<Texture2D>.Get(cutiemarkBodyAddon.GetPath(pawn, ref variantP, variantP) + "_east");
                GUI.DrawTexture(rect, image);
            }
        }
        public static void DrawInSelector(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, ref int sharedIndex, int variant)
        {
            Widgets.DrawBox(rect);
            if (pawn.IsEarthpony())
            {
                Texture2D image = ContentFinder<Texture2D>.Get(cutiemarkBodyAddon.GetPath(pawn, ref sharedIndex, variant) + "_east");
                GUI.DrawTexture(rect, image);
            }
            if (pawn.IsUnicorn())
            {
                Texture2D image = ContentFinder<Texture2D>.Get(cutiemarkBodyAddon.GetPath(pawn, ref sharedIndex, variant) + "_east");
                GUI.DrawTexture(rect, image);
            }
            if (pawn.IsPegasus())
            {
                Texture2D image = ContentFinder<Texture2D>.Get(cutiemarkBodyAddon.GetPath(pawn, ref sharedIndex, variant) + "_east");
                GUI.DrawTexture(rect, image);
            }
        }
    }
}
