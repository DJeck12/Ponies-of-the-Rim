using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PoniesOfTheRim.HarmonyPatches
{
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    public static class MainMenuDiscordIconPatch
    {
        private static Texture2D discordIcon;
        private const float IconSize = 48f;
        private const float Margin = 10f;
        private const string DiscordUrl = "https://discord.com/invite/Bwnh2SVV7S";
        private const string IconPath = "Transparent/Discord";

        static MainMenuDiscordIconPatch()
        {
            discordIcon = ContentFinder<Texture2D>.Get(IconPath, false);
        }

        [HarmonyPostfix]
        public static void Postfix(Rect rect)
        {
            Rect iconRect = new Rect(UI.screenWidth - IconSize - Margin, UI.screenHeight - IconSize - Margin, IconSize, IconSize);
            GUI.DrawTexture(iconRect, discordIcon);
            if (Widgets.ButtonInvisible(iconRect, doMouseoverSound: false))
            {
                Application.OpenURL(DiscordUrl);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            TooltipHandler.TipRegion(iconRect, "Join on Ponies of the Rim Discord!");
        }
    }
}