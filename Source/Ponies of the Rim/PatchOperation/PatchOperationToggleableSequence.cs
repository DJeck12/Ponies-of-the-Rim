using System.Collections.Generic;
using System.Xml;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableSequence : PatchOperationToggleable
    {
        public List<Verse.PatchOperation> operations;
        public Verse.PatchOperation lastFailedOperation;

        private readonly List<Verse.PatchOperation> failedOperations = new List<Verse.PatchOperation>();

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!Enabled)
            {
                return true;
            }

            if (operations == null)
            {
                PonyLog.Warn($"Patches: toggle {settingId} has no operations");
                return false;
            }

            failedOperations.Clear();
            foreach (Verse.PatchOperation operation in operations)
            {
                if (!operation.Apply(xml))
                {
                    lastFailedOperation = operation;
                    failedOperations.Add(operation);
                }
            }
            return failedOperations.Count == 0;
        }

        public override string ToString()
        {
            int count = operations?.Count ?? 0;
            string text = $"{base.ToString()}(settingId={settingId}, count={count}";
            if (failedOperations.Count > 0)
            {
                text += $", failed={string.Join("; ", failedOperations)}";
            }
            return text + ")";
        }
    }
}