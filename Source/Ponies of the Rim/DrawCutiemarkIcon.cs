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
				if (ModsConfig.BiotechActive)
				{
					int value = alienComp.addonVariants[4];
					Texture2D image = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value, value, null) + "_east");
					GUI.DrawTexture(rect, image);
				}
				else
				{
					int value2 = alienComp.addonVariants[3];
					Texture2D image2 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value2, value2, null) + "_east");
					GUI.DrawTexture(rect, image2);
				}
			}
			if (pawn.IsUnicorn())
			{
				if (ModsConfig.BiotechActive)
				{
					int value3 = alienComp.addonVariants[5];
					Texture2D image3 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value3, value3, null) + "_east");
					GUI.DrawTexture(rect, image3);
				}
				else
				{
					int value4 = alienComp.addonVariants[4];
					Texture2D image4 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value4, value4, null) + "_east");
					GUI.DrawTexture(rect, image4);
				}
			}
			if (pawn.IsPegasus())
			{
				if (ModsConfig.BiotechActive)
				{
					int value5 = alienComp.addonVariants[6];
					Texture2D image5 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value5, value5, null) + "_east");
					GUI.DrawTexture(rect, image5);
				}
				else
				{
					int value6 = alienComp.addonVariants[5];
					Texture2D image6 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref value6, value6, null) + "_east");
					GUI.DrawTexture(rect, image6);
				}
			}
		}

		public static void DrawInSelector(Pawn pawn, Rect rect, AlienPartGenerator.BodyAddon cutiemarkBodyAddon, ref int sharedIndex, int variant)
		{
			Widgets.DrawBox(rect);
			if (pawn.IsEarthpony())
			{
				Texture2D image = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref sharedIndex, variant, null) + "_east");
				GUI.DrawTexture(rect, image);
			}
			if (pawn.IsUnicorn())
			{
				Texture2D image2 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref sharedIndex, variant, null) + "_east");
				GUI.DrawTexture(rect, image2);
			}
			if (pawn.IsPegasus())
			{
				Texture2D image3 = ContentFinder<Texture2D>.Get((cutiemarkBodyAddon).GetPath(pawn, ref sharedIndex, variant, null) + "_east");
				GUI.DrawTexture(rect, image3);
			}
		}
	}
}
