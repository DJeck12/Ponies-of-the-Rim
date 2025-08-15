using System;
using System.Linq;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableRemove : Verse.PatchOperation
    {
        public string xpath;
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
                XmlNode[] array = xml.SelectNodes(xpath).Cast<XmlNode>().ToArray();
                foreach (XmlNode xmlNode in array)
                {
                    result = true;
                    XmlNode parentNode = xmlNode.ParentNode;
                    parentNode.RemoveChild(xmlNode);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error applying remove patch: {ex.Message}");
            }
            return result;
        }

        public override void Complete(string modIdentifier)
        {
            base.Complete(modIdentifier);
        }
    }
}
