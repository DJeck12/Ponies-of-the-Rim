using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{

    public class PoniesOfTheRimSettingsData : ModSettings
    {
        //Keep Addisional Tabulators here to read hierarchy cleary
        #region Classical
        public bool classical;
            public bool ideology;
            public bool abilities;
            public bool prosthetics;
            public bool anthro;
            public bool enableRegularHorses;

        public void ClassicalSettingsToggle(bool action)
        {
            ideology = action;
            abilities = action;
            prosthetics = action;
            anthro = action;
            enableRegularHorses = action;
        }

        public void ClassicalSettingsDefault() => ClassicalSettingsToggle(true);
        #endregion

        /// <summary>
        /// This makes our settings file and sets them to default values.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref classical, "classical", true, true);
            Scribe_Values.Look(ref ideology, "ideology", true, true);
            Scribe_Values.Look(ref abilities, "abilities", true, true);
            Scribe_Values.Look(ref prosthetics, "prosthetics", true, true);
            Scribe_Values.Look(ref anthro, "anthro", true, true);
            Scribe_Values.Look(ref enableRegularHorses, "enableRegularHorses", true, true);
            base.ExposeData();
        }
    }

    public class PoniesOfTheRimSettings : Mod
    {
        PoniesOfTheRimSettingsData settings;
        public PoniesOfTheRimSettings(ModContentPack content) : base(content)
        {
            settings = GetSettings<PoniesOfTheRimSettingsData>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            //Copy paste symbols we use
            // └ 	┴ 	┬ 	├ 	─ 	┼  ►

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            Settingheader(listingStandard);
            listingStandard.Label("SettingsCore".Translate());
            ClassicalSettings(listingStandard);
            listingStandard.End();

            settings.Write();
            base.DoSettingsWindowContents(inRect);
        }

        private void Settingheader(Listing_Standard listingStandard)
        {
            listingStandard.Label("SettingsHeader".Translate());
            //TODO: Make all button line horizontaly
            //var buttons = listingStandard.BeginSection(20,1,1);
            if (listingStandard.ButtonText("SettingsEnableAll".Translate(), null, 0.2f)) SetAllData(true);
            if (listingStandard.ButtonText("SettingsDisableAll".Translate(), null, 0.2f)) SetAllData(false);
            if (listingStandard.ButtonText("SettingsDefulatAll".Translate(), null, 0.2f)) DefaultAllData();
            listingStandard.GapLine(10);
        }

        #region Classical
        private void ClassicalSettings(Listing_Standard listingStandard)
        {
            listingStandard.CheckboxLabeled("└► " + "SettingsClassical".Translate(), ref settings.classical, "SettingsToolTipClassical".Translate(), 0, 1);
            if (settings.classical)
            {
                //! Uncomment ONLY HERE if is working

                listingStandard.CheckboxLabeled("   ├► " + "SettingsIdeology".Translate(), ref settings.ideology, "SettingsToolTipIdeology".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "SettingsAbilities".Translate(), ref settings.abilities, "SettingsToolTipAbilities".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "SettingsProsthetics".Translate(), ref settings.prosthetics, "SettingsToolTipProsthetics".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "SettingsAnthro".Translate(), ref settings.anthro, "SettingsToolTipAnthro".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   └► " + "SettingsEnableRegularHorses".Translate(), ref settings.enableRegularHorses, "SettingsToolTipEnableRegularHorses".Translate(), 0, 1);
            }
            else
            {
                settings.ClassicalSettingsToggle(false);
            }
        }
        #endregion

        #region Buttons

        private void DefaultAllData()
        {
            settings.ClassicalSettingsDefault();
        }

        private void SetAllData(bool action)
        {
            settings.ClassicalSettingsToggle(action);
        }
        #endregion

        /// <summary>
        /// Overwriting this enables configs
        /// </summary>
        public override string SettingsCategory()
        {
            return "SettingsPoniesOfTheRim".Translate();
        }
    }
}
