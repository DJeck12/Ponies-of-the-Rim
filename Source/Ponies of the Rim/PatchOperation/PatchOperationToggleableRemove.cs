using System;
using System.Linq;
using System.Xml;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableRemove : PatchOperationToggleable
    {
        public string xpath;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!Enabled)
            {
                return true;
            }

            bool matched = false;
            try
            {
                foreach (XmlNode target in xml.SelectNodes(xpath).Cast<XmlNode>().ToArray())
                {
                    matched = true;
                    target.ParentNode.RemoveChild(target);
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