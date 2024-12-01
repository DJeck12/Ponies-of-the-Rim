using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{

    public class PoniesOfTheRimSettingsData : ModSettings
    {
        //Keep Addisional Tabulators here to read hierarchy cleary
        public bool classical; 
            public bool ideology;
            public bool abilities;
            public bool prosthetics;
            public bool anthro;
            public bool enableRegularHorses;

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
            listingStandard.Label("SettingsHeader".Translate());
            //TODO: Make all button line horizontaly
            if (listingStandard.ButtonText("SettingsEnableAll".Translate(), null, 0.2f))  SetAllData(true);
            if (listingStandard.ButtonText("SettingsDisableAll".Translate(), null, 0.2f)) SetAllData(false);
            if (listingStandard.ButtonText("SettingsDefulatAll".Translate(), null, 0.2f)) DefaultAllData();
            listingStandard.GapLine(10);
            listingStandard.Label("SettingsCore".Translate());
            ClassicalSettings(listingStandard);
            listingStandard.End();

            settings.Write();
            base.DoSettingsWindowContents(inRect);
        }

        #region Classical
        private void ClassicalSettings(Listing_Standard listingStandard)
        {
            listingStandard.CheckboxLabeled("└► " + "SettingsClassical".Translate(), ref settings.classical, "classicalToolTip".Translate(), 0, 1);
            if (settings.classical)
            {
             //! Uncomment ONLY HERE if is working

                listingStandard.CheckboxLabeled("   ├► " + "SettingsIdeology".Translate(), ref settings.ideology, "addIdeologyToolTip".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "SettingsAbilities".Translate(), ref settings.abilities, "addAbilitiesToolTip".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "SettingsProsthetics".Translate(), ref settings.prosthetics, "addProstheticsToolTip".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "SettingsAnthro".Translate(), ref settings.anthro, "addAnthroToolTip".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   └► " + "SettingsEnableRegularHorses".Translate(), ref settings.enableRegularHorses, "addEnableRegularHorsesToolTip".Translate(), 0, 1);
            }
            else
            {
                ClassicalSettingsDisabled();
            }
        }

        private void ClassicalSettingsDefault()
        {
            settings.classical = true;
            settings.ideology = true;
            settings.abilities = true;
            settings.prosthetics = true;
            settings.anthro = true;
            settings.enableRegularHorses = true;
        }

        private void ClassicalSettingsDisabled()
        {
            settings.ideology = false;
            settings.abilities = false;
            settings.prosthetics = false;
            settings.anthro = false;
            settings.enableRegularHorses = false;
        }
        #endregion

        #region Main Buttons

        private void DefaultAllData()
        {
            ClassicalSettingsDefault();
        }

        private void SetAllData(bool action)
        {
            settings.classical = action;
            settings.ideology = action;
            settings.abilities = action;
            settings.prosthetics = action;
            settings.anthro = action;
            settings.enableRegularHorses = action;
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
