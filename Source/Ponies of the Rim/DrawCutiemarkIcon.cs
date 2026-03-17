using AlienRace;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public static class DrawCutiemarkIcon
    {
        public static void Draw(
            Pawn pawn,
            Rect rect,
            AlienPartGenerator.BodyAddon cutiemarkBodyAddon,
            AlienPartGenerator.AlienComp alienComp,
            int variantIndex)
        {
            Widgets.DrawBox(rect);

            if (cutiemarkBodyAddon == null) return;
            if (!PonyHelper.HasCutiemark(pawn)) return;                         
            if (variantIndex < 0 || variantIndex >= alienComp.addonVariants.Count) return;  

            int value = alienComp.addonVariants[variantIndex];
            string path = cutiemarkBodyAddon.GetPath(pawn, ref value, value, null);
            if (string.IsNullOrEmpty(path)) return;                             

            Texture2D image = ContentFinder<Texture2D>.Get(path + "_east", false);      
            if (image == null) return;                                          

            GUI.DrawTexture(rect, image);
        }

        public static void DrawInSelector(
            Pawn pawn,
            Rect rect,
            AlienPartGenerator.BodyAddon cutiemarkBodyAddon,
            ref int sharedIndex,
            int variant)
        {
            Widgets.DrawBox(rect);

            if (cutiemarkBodyAddon == null) return;
            if (!PonyHelper.HasCutiemark(pawn)) return;         

            string path = cutiemarkBodyAddon.GetPath(pawn, ref sharedIndex, variant, null);
            if (string.IsNullOrEmpty(path)) return;                    

            Texture2D image = ContentFinder<Texture2D>.Get(path + "_east", false); 
            if (image == null) return;                                      

            GUI.DrawTexture(rect, image);
        }
    }
}