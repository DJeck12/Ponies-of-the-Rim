using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableSequence : PatchOperationSequence
    {
        public string settingId;
        public List<Verse.PatchOperation> operations;
        public Verse.PatchOperation lastFailedOperation;
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

            bool result = true;
            foreach (Verse.PatchOperation operation in operations)
            {
                result &= operation.Apply(xml);
            }
            return result;
        }

        public override string ToString()
	    {
		    int num = ((operations != null) ? operations.Count : 0);
		    string text = string.Format("{0}(count={1}", base.ToString(), num);
		    if (lastFailedOperation != null)
		    {
			    string text2 = text;
			    Verse.PatchOperation patchOperation = lastFailedOperation;
			    text = text2 + ", lastFailedOperation=" + ((patchOperation != null) ? patchOperation.ToString() : null);
		    }
		    return text + ")";
	    }

        public override void Complete(string modIdentifier)
        {
            base.Complete(modIdentifier);
        }
    }
}
