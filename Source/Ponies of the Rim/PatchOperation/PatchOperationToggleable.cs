using Verse;

namespace PoniesOfTheRim.PatchOperation
{
    public abstract class PatchOperationToggleable : Verse.PatchOperation
    {
        public string settingId;
        public bool defaultState = true;
        public string label;
        public string description;

        protected bool Enabled
        {
            get
            {
                if (string.IsNullOrEmpty(settingId))
                {
                    PonyLog.Warn($"Patches: {GetType().Name} has no settingId, applied as defaultState={defaultState}");
                    return defaultState;
                }
                return PoniesOfTheRimSettings.settings.patchToggles.GetWithFallback(settingId, defaultState);
            }
        }
    }
}