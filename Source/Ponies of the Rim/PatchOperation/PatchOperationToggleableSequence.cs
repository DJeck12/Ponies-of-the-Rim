using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public class PatchOperationToggleableSequence : Verse.PatchOperation
    {
        public List<Verse.PatchOperation> operations;
        public Verse.PatchOperation lastFailedOperation;

        public string settingId;
        public bool defaultState;
        public string label;
        public string description;

        protected bool CanRun()
        {
            return PoniesOfTheRimSettings.settings.patchToggles.GetWithFallback(settingId, defaultState);
        }

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (!CanRun())
            {
                return true;
            }

            if (operations == null)
            {
                Log.Warning($"[PoniesOfTheRim] PatchOperationToggleableSequence '{settingId}': список operations равен null. Пропуск.");
                return false;
            }

            bool result = true;
            foreach (Verse.PatchOperation operation in operations)
            {
                if (!operation.Apply(xml))
                {
                    lastFailedOperation = operation;
                    result = false;
                }
            }
            return result;
        }

        public override string ToString()
        {
            int count = (operations != null) ? operations.Count : 0;
            string text = $"{base.ToString()}(settingId={settingId}, count={count}";
            if (lastFailedOperation != null)
            {
                text += $", lastFailedOperation={lastFailedOperation}";
            }
            return text + ")";
        }
    }
}