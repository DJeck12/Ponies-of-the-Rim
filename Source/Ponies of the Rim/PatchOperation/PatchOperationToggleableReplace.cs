using System;
using System.Linq;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableReplace : PatchOperationToggleable
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
                XmlNode source = value.node;
                foreach (XmlNode target in xml.SelectNodes(xpath).Cast<XmlNode>().ToArray())
                {
                    matched = true;
                    XmlNode parent = target.ParentNode;
                    foreach (XmlNode child in source.ChildNodes)
                    {
                        parent.InsertBefore(parent.OwnerDocument.ImportNode(child, true), target);
                    }
                    parent.RemoveChild(target);
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