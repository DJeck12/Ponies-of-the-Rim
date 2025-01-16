using AlienRace;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
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
					List<int> sharedTailType = new List<int>();
					foreach (PonyTailType type in ponyModExtension.ponyTailType)
					{
						sharedTailType.AddRange(type.tailIndex);
					}
					sharedIndex = Rand.Element(sharedTailType.Distinct().ToArray());
				}
			}
		}
	}
}
