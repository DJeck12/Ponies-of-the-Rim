using System;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableAdd : Verse.PatchOperation
    {
        public string xpath;
        public XmlContainer value;
        public string settingId;
        public bool defaultState;

        protected bool CanRun()
        {
            bool enabled = PoniesOfTheRimSettings.settings.patchToggles.GetWithFallback(settingId, true);
            return enabled;
        }

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!CanRun())
            {
                return true;
            }

            bool result = false;
            try
            {
                foreach (object item in xml.SelectNodes(xpath))
                {
                    if (item is not XmlNode node)
                    {
                        continue;
                    }
                    XmlNode xmlNode = value.node;
                    if (xmlNode.HasChildNodes)
                    {
                        foreach (XmlNode childNode in xmlNode.ChildNodes)
                        {
                            node.AppendChild(node.OwnerDocument.ImportNode(childNode, true));
                        }
                    }
                    else
                    {
                        node.AppendChild(node.OwnerDocument.ImportNode(xmlNode, true));
                    }
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error applying patch: {ex.Message}");
            }
            return result;
        }

        public override void Complete(string modIdentifier)
        {
            base.Complete(modIdentifier);
        }
    }
}