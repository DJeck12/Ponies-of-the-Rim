using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.Genetics
{
    public class PonyRacialGeneOption
    {
        public GeneDef gene;

        public float weight = 1f;

        public void LoadDataFromXmlCustom(XmlNode xmlNode)
        {
            if (xmlNode.ChildNodes.Count == 1 && xmlNode.FirstChild.NodeType == XmlNodeType.Text)
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "gene", xmlNode.InnerText);
                return;
            }
            XmlNode geneNode = xmlNode["gene"];
            if (geneNode != null)
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "gene", geneNode.InnerText);
            }
            XmlNode weightNode = xmlNode["weight"];
            if (weightNode != null)
            {
                weight = ParseHelper.FromString<float>(weightNode.InnerText);
            }
        }
    }

    public class PonyRacialGeneGroupDef : Def
    {
        public List<PonyRacialGeneOption> racialGenes = new List<PonyRacialGeneOption>();

        public List<GeneDef> markerGenes = new List<GeneDef>();

        public List<ThingDef> requiredRaces = new List<ThingDef>();

        public GeneDef chooserGene;

        public bool enforceOnInheritance = true;
        public bool inheritMultipleFromParent = true;

        public bool AppliesToRace(ThingDef race)
        {
            if (requiredRaces == null || requiredRaces.Count == 0)
            {
                return true;
            }
            if (race == null)
            {
                return false;
            }
            for (int i = 0; i < requiredRaces.Count; i++)
            {
                if (requiredRaces[i] == race)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsRacialGene(GeneDef gene)
        {
            if (gene == null)
            {
                return false;
            }
            for (int i = 0; i < racialGenes.Count; i++)
            {
                PonyRacialGeneOption option = racialGenes[i];
                if (option != null && option.gene == gene)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMarkerGene(GeneDef gene)
        {
            if (gene == null || markerGenes == null)
            {
                return false;
            }
            for (int i = 0; i < markerGenes.Count; i++)
            {
                if (markerGenes[i] == gene)
                {
                    return true;
                }
            }
            return false;
        }

        public GeneDef RandomGeneByWeight(Func<GeneDef, bool> allowed)
        {
            float total = 0f;
            for (int i = 0; i < racialGenes.Count; i++)
            {
                PonyRacialGeneOption option = racialGenes[i];
                if (option != null && option.gene != null && option.weight > 0f && (allowed == null || allowed(option.gene)))
                {
                    total += option.weight;
                }
            }
            if (total <= 0f)
            {
                return null;
            }
            float roll = Rand.Value * total;
            for (int i = 0; i < racialGenes.Count; i++)
            {
                PonyRacialGeneOption option = racialGenes[i];
                if (option == null || option.gene == null || option.weight <= 0f || (allowed != null && !allowed(option.gene)))
                {
                    continue;
                }
                roll -= option.weight;
                if (roll <= 0f)
                {
                    return option.gene;
                }
            }
            return null;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }
            if (racialGenes == null || racialGenes.Count == 0)
            {
                yield return "[PoniesOfTheRim] racialGenes пуст — группа ничего не делает.";
                yield break;
            }
            for (int i = 0; i < racialGenes.Count; i++)
            {
                if (racialGenes[i] == null || racialGenes[i].gene == null)
                {
                    yield return "[PoniesOfTheRim] racialGenes[" + i + "] без валидного gene.";
                }
            }
        }
    }
}