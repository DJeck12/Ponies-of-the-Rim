using System;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableAdd : PatchOperationToggleable
    {
        public string xpath;
        public XmlContainer value;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!Enabled)
            {
                return true;
            }

            bool matched = false;
            try
            {
                foreach (object item in xml.SelectNodes(xpath))
                {
                    if (item is not XmlNode target)
                    {
                        continue;
                    }
                    XmlNode source = value.node;
                    if (source.HasChildNodes)
                    {
                        foreach (XmlNode child in source.ChildNodes)
                        {
                            target.AppendChild(target.OwnerDocument.ImportNode(child, true));
                        }
                    }
                    else
                    {
                        target.AppendChild(target.OwnerDocument.ImportNode(source, true));
                    }
                    matched = true;
                }
            }
            catch (Exception ex)
            {
                PonyLog.Error($"Patches: toggle {settingId} failed on {xpath}: {ex.Message}");
            }
            return matched;
        }
    }
}