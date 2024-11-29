using Verse;

namespace PoniesOfTheRim
{
    public class HediffComp_PonyProsthetic : HediffComp
    {

		public string prefix;
		public string inBrackets;
		public string descriptionExtra;
		public string tipStringExtra;

	public override string CompLabelInBracketsExtra
	{
		get
		{
			if (base.Pawn.IsPony())
			{
				return base.CompLabelInBracketsExtra + Props.inBrackets;
			}
			return base.CompLabelInBracketsExtra;
		}
	}

	public override string CompLabelPrefix
	{
		get
		{
			if (base.Pawn.IsPony())
			{
				return base.CompLabelPrefix + Props.prefix;
			}
			return base.CompLabelPrefix;
		}
	}

	public override string CompDescriptionExtra
	{
		get
		{
			if (base.Pawn.IsPony())
			{
				return base.CompDescriptionExtra + Props.descriptionExtra;
			}
			return base.CompDescriptionExtra;
		}
	}

	public override string CompTipStringExtra
	{
		get
		{
			if (base.Pawn.IsPony())
			{
				return base.CompTipStringExtra + Props.tipStringExtra;
			}
			return base.CompTipStringExtra;
		}
	}

		public HediffCompProperties_PonyProsthetic Props
		{
			get
			{
				return (HediffCompProperties_PonyProsthetic)props;
			}
		}
    }
}
