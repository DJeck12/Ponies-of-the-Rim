using Verse;

namespace PoniesOfTheRim
{
    public class HediffComp_PonyProsthetic : HediffComp
    {
        public HediffCompProperties_PonyProsthetic Props => (HediffCompProperties_PonyProsthetic)props;

        public override string CompLabelPrefix
        {
            get
            {
                if (!base.Pawn.IsPony())
                {
                    return base.CompLabelPrefix;
                }
                return Props.ResolvedPrefix ?? base.CompLabelPrefix;
            }
        }

        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (!base.Pawn.IsPony())
                {
                    return base.CompLabelInBracketsExtra;
                }
                return Props.ResolvedInBrackets ?? base.CompLabelInBracketsExtra;
            }
        }

        public override string CompDescriptionExtra
        {
            get
            {
                if (!base.Pawn.IsPony())
                {
                    return base.CompDescriptionExtra;
                }
                return Props.ResolvedDescriptionExtra ?? base.CompDescriptionExtra;
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                if (!base.Pawn.IsPony())
                {
                    return base.CompTipStringExtra;
                }
                return Props.ResolvedTipStringExtra ?? base.CompTipStringExtra;
            }
        }
    }
}