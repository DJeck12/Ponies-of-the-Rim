using Verse;

namespace PoniesOfTheRim
{
	public class HediffCompProperties_PonyProsthetic : HediffCompProperties
	{
		public string prefix;
		public string inBrackets;
		public string descriptionExtra;
		public string tipStringExtra;

	public HediffCompProperties_PonyProsthetic()
		{
			compClass = typeof(HediffComp_PonyProsthetic);
		}
	}
}
