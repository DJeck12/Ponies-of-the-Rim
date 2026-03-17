using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class MainMenuDiscordIconPatch
    {
        private static Texture2D discordIcon;
        private const string DiscordUrl = "https://discord.com/invite/Bwnh2SVV7S";
        private const string IconPath = "Meta/Discord";

        static MainMenuDiscordIconPatch()
        {
            discordIcon = ContentFinder<Texture2D>.Get(IconPath, false);
        }

        public static void Postfix()
        {
            if (discordIcon == null)
            {
                return;
            }

            float iconX = 24f;
            float iconY = (float)UI.screenHeight - 88f - 96f;
            Rect iconRect = new Rect(iconX, iconY, 64f, 64f);

            float bgX = iconX - 16f;
            float bgY = (float)UI.screenHeight - 96f - 8f - 96f;
            Rect bgRect = new Rect(bgX, bgY, 96f, 96f);

            Widgets.DrawWindowBackground(bgRect);

            bool hovered = Mouse.IsOver(iconRect);
            if (hovered)
            {
                Widgets.DrawHighlight(iconRect);
            }

            GUI.DrawTexture(iconRect.ContractedBy(2f), discordIcon);

            if (Widgets.ButtonInvisible(iconRect, doMouseoverSound: false))
            {
                Application.OpenURL(DiscordUrl);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            TooltipHandler.TipRegion(iconRect, "Join on Ponies of the Rim Discord!");

            if (hovered)
            {
                DrawDiscordInfo(new Vector2(Mathf.Min(iconRect.xMax + 16f, (float)UI.screenWidth - 350f), iconRect.yMax));
            }
        }

        private static void DrawDiscordInfo(Vector2 offset)
        {
            string label = "Ponies of the Rim Discord";
            string description = "Join our community on Discord to discuss the mod, share experiences, and get updates!";

            float num = 16f;
            float b = 0f;

            Text.Font = GameFont.Medium;
            float num4 = Text.CalcHeight(label, 350f - num * 2f);
            Text.Font = GameFont.Small;
            string text = "Click to join the server";
            float num5 = Text.CalcHeight(description, 350f - num * 2f);
            float num6 = Text.CalcHeight(text, 350f - num * 2f);
            b = Mathf.Max(num4 + num6 + num + num5 + num * 2f, b);

            Rect rect = new Rect(offset.x, offset.y - b, 350f, b);
            Widgets.DrawWindowBackground(rect);

            Rect rect2 = rect.ContractedBy(num);
            Widgets.BeginGroup(rect2);
            float num7 = 0f;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, num7, rect2.width, num4), new GUIContent(" " + label, discordIcon));
            Text.Font = GameFont.Small;
            num7 += num4;
            GUI.color = Color.grey;
            Widgets.Label(new Rect(0f, num7, rect2.width, num6), text);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            num7 += num6 + num;
            Widgets.Label(new Rect(0f, num7, rect2.width, rect2.height - num7), description);
            Widgets.EndGroup();
        }
    }
}