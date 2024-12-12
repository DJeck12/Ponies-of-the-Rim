using AlienRace;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim
{
	[StaticConstructorOnStartup]
	public static class PonyHairExtensionPatch
	{
		static PonyHairExtensionPatch()
		{
			new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "GetGraphic"), null, new HarmonyMethod(typeof(PonyHairExtensionPatch).GetMethod("PonyTailPatch")));
		}

		[HarmonyPostfix]
		public static void PonyTailPatch(Pawn pawn,AlienPartGenerator.BodyAddon __instance, ref int sharedIndex)
		{
			if (pawn.IsPony() && __instance.Name == "Tail")
			{
				PonyHairExtension ponyModExtension = pawn.story.hairDef.GetModExtension<PonyHairExtension>();
				if (pawn.story.hairDef.HasModExtension<PonyHairExtension>())
				{
					if (ponyModExtension.shortTail == true)
					{
						sharedIndex = Rand.Element(ponyModExtension.shortTailPool);
					}
					if (ponyModExtension.waveyTail == true)
					{
						sharedIndex = Rand.Element(ponyModExtension.waveyTailPool);
					}
					if (ponyModExtension.curlyTail == true)
					{
						sharedIndex = Rand.Element(ponyModExtension.curlyTailPool);
					}
					if (ponyModExtension.royalTail == true)
					{
						sharedIndex = Rand.Element(ponyModExtension.royalTailPool);
					}
					if (ponyModExtension.customPool != null)
					{
						sharedIndex = Rand.Element(ponyModExtension.customPool.ToArray());
					}
				}
			}
		}
	}
}
