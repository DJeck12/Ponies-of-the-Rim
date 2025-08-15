using System.Linq;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableReplace : Verse.PatchOperation
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
            XmlNode node = value.node;
            bool result = false;
            XmlNode[] array = xml.SelectNodes(xpath).Cast<XmlNode>().ToArray();
            foreach (XmlNode xmlNode in array)
            {
                result = true;
                XmlNode parentNode = xmlNode.ParentNode;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    parentNode.InsertBefore(parentNode.OwnerDocument.ImportNode(childNode, true), xmlNode);
                }
                parentNode.RemoveChild(xmlNode);
            }
            return result;
        }

        public override void Complete(string modIdentifier)
        {
            base.Complete(modIdentifier);
        }
    }
}
