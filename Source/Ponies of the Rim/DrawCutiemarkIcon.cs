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
            if (cutiemarkBodyAddon != null && variantIndex >= 0 &&
                (pawn.IsEarthpony() || pawn.IsUnicorn() || pawn.IsPegasus() || pawn.IsZebra() || pawn.IsCrystalpony() || pawn.IsBatpony() || pawn.IsAlicorn()))
            {
                int value = alienComp.addonVariants[variantIndex];
                Texture2D image = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value, value, null) + "_east");
                GUI.DrawTexture(rect, image);
            }
        }

        public static void DrawInSelector(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, ref int sharedIndex, int variant)
        {
            Widgets.DrawBox(rect);
            if (cutiemarkBodyAddon != null && (pawn.IsEarthpony() || pawn.IsUnicorn() || pawn.IsPegasus() || pawn.IsZebra() || pawn.IsCrystalpony() || pawn.IsBatpony() || pawn.IsAlicorn()))
            {
                Texture2D image = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref sharedIndex, variant, null) + "_east");
                GUI.DrawTexture(rect, image);
            }
        }
    }
}