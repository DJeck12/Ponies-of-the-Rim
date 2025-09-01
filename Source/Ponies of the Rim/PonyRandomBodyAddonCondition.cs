using Verse;
using System.Xml;
using AlienRace.ExtendedGraphics;

namespace PoniesOfTheRim
{
    public class PonyRandomBodyAddonCondition : Condition
    {
        public float chance = 1f;

        public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
        {
            return Rand.Value < chance;
        }

        public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot["chance"] != null)
            {
                chance = ParseHelper.ParseFloat(xmlRoot["chance"].InnerText);
            }
        }
    }
}