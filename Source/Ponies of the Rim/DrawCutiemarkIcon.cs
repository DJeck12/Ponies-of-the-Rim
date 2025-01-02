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
			if (pawn.IsPony())
			{
				int value = alienComp.addonVariants[0];
				Texture2D image = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value, value, null) + "_east");
				GUI.DrawTexture(rect, image);
			}
		}
		public static void DrawInSelector(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, ref int sharedIndex, int variant)
		{
			Widgets.DrawBox(rect);
			if (pawn.IsPony())
			{
				Texture2D image = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref sharedIndex, variant, null) + "_east");
				GUI.DrawTexture(rect, image);

			}
		}
	}
}