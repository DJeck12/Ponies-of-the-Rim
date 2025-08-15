using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;
using System.IO;

namespace PoniesOfTheRim
{
    public class PoniesOfTheRimSettingsData : ModSettings
    {
        //Keep Addisional Tabulators here to read hierarchy more cleary
        //Make Some Settings Apply per save

        public bool classical = true;
        public bool ideology = true;
        public bool abilities = true;
        public bool prosthetics = true;
        public bool anthro = true;
        public bool enableRegularHorses = true;
        public bool enableFoodGenes = true;
        public bool enableFoodDebuffs = true;

        public Dictionary<string, bool> patchToggles = [];

        public void ClassicalSettingsToggle(bool action)
        {
            ideology = action;
            abilities = action;
            prosthetics = action;
            anthro = action;
            enableRegularHorses = action;
        }



        public void ClassicalSettingsDefault() => ClassicalSettingsToggle(true);

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
            Scribe_Values.Look(ref enableFoodGenes, "enableFoodGenes", true, true);
            Scribe_Values.Look(ref enableFoodDebuffs, "enableFoodDebuffs", true, true);

            Scribe_Collections.Look(ref patchToggles, "patchToggles", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                patchToggles ??= [];
            }

            base.ExposeData();
        }
    }

    [StaticConstructorOnStartup]
    public class PoniesOfTheRimSettings : Mod
    {
        public static PoniesOfTheRimSettingsData settings;
        public static Dictionary<string, bool> defaultPatchToggles = [];

        public PoniesOfTheRimSettings(ModContentPack content) : base(content)
        {
            settings = GetSettings<PoniesOfTheRimSettingsData>();
            InitializePatchToggles(content);
        }
        //Copy paste symbols we use
        // └ 	┴ 	┬ 	├ 	─ 	┼  ►
        private void InitializePatchToggles(ModContentPack content)
        {
            string rootDir = content.RootDir;
            string versionDir = Path.Combine(rootDir, "1.6");
            if (!Directory.Exists(versionDir))
            {
                Log.Warning("Directory not found: " + versionDir);
                return;
            }

            var patchDirs = Directory.GetDirectories(versionDir, "Patches", SearchOption.AllDirectories);

            foreach (string patchesDir in patchDirs)
            {
                foreach (string file in Directory.GetFiles(patchesDir, "*.xml", SearchOption.AllDirectories))
                {
                    try
                    {
                        XmlDocument doc = new();
                        doc.Load(file);

                        XmlNode settingNode = doc.SelectSingleNode("//settingId");
                        string settingId = settingNode?.InnerText;
                        if (string.IsNullOrEmpty(settingId))
                        {
                            Log.Warning($"No valid <settingId> found in patch XML: {file}");
                            continue;
                        }

                        XmlNode defaultNode = doc.SelectSingleNode("//defaultState");
                        bool defaultState = true;
                        if (defaultNode != null && bool.TryParse(defaultNode.InnerText, out bool parsedDefault))
                        {
                            defaultState = parsedDefault;
                        }

                        defaultPatchToggles[settingId] = defaultState;

                        if (!settings.patchToggles.ContainsKey(settingId))
                        {
                            settings.patchToggles[settingId] = defaultState;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error loading patch XML {file}: {ex.Message}");
                    }
                }
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new();
            listingStandard.Begin(inRect);
            Settingheader(listingStandard);
            listingStandard.Label("SettingsCore".Translate());
            ClassicalSettings(listingStandard);


            listingStandard.GapLine(10);
            listingStandard.Label("Patch Settings".Translate());
            foreach (var kvp in settings.patchToggles.ToList())
            {
                string settingId = kvp.Key;
                bool value = kvp.Value;

                string label = "Enable " + settingId.Replace("enable", "").Replace("Patch", "").Trim().CapitalizeFirst() + " Patch";
                string tooltip = "Enables/disables the patch for " + settingId + " (from XML).";

                listingStandard.CheckboxLabeled(label.Translate(), ref value, tooltip.Translate(), 0, 1);
                settings.patchToggles[settingId] = value;

            }

            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
        }
        /// <summary>
        /// Overwriting this enables settings, leave blank to disable it
        /// </summary>
        public override string SettingsCategory()
        {
            return "SettingsPoniesOfTheRim".Translate();
        }

        #region Header (как было)

        private void Settingheader(Listing_Standard listingStandard)
        {
            //TODO: Make all button line horizontaly
            listingStandard.Label("SettingsHeader".Translate());
            if (listingStandard.ButtonText("SettingsEnableAll".Translate(), null, 0.2f)) SetAllData(true);
            if (listingStandard.ButtonText("SettingsDisableAll".Translate(), null, 0.2f)) SetAllData(false);
            if (listingStandard.ButtonText("SettingsDefulatAll".Translate(), null, 0.2f)) DefaultAllData();
            listingStandard.GapLine(10);
        }

        private void DefaultAllData()
        {
            settings.ClassicalSettingsDefault();

            foreach (var key in defaultPatchToggles.Keys.ToList())
            {
                settings.patchToggles[key] = defaultPatchToggles[key];
            }
        }

        private void SetAllData(bool action)
        {
            settings.classical = action;
            settings.ClassicalSettingsToggle(action);

            foreach (var key in settings.patchToggles.Keys.ToList())
            {
                settings.patchToggles[key] = action;
            }
        }
        #endregion

        #region Classical (как было)
        private void ClassicalSettings(Listing_Standard listingStandard)
        {
            listingStandard.CheckboxLabeled("└► " + "SettingsClassical".Translate(), ref settings.classical, "SettingsToolTipClassical".Translate(), 0, 1);
            if (settings.classical)
            {
                //! Uncomment ONLY HERE if is working

                // listingStandard.CheckboxLabeled("   ├► " + "SettingsIdeology".Translate(), ref settings.ideology, "SettingsToolTipIdeology".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   └► " + "SettingsAbilities".Translate(), ref settings.abilities, "SettingsToolTipAbilities".Translate(), 0, 1);
                // listingStandard.CheckboxLabeled("   ├► " + "SettingsProsthetics".Translate(), ref settings.prosthetics, "SettingsToolTipProsthetics".Translate(), 0, 1);
                // listingStandard.CheckboxLabeled("   ├► " + "SettingsAnthro".Translate(), ref settings.anthro, "SettingsToolTipAnthro".Translate(), 0, 1);
                // listingStandard.CheckboxLabeled("   └► " + "SettingsEnableRegularHorses".Translate(), ref settings.enableRegularHorses, "SettingsToolTipEnableRegularHorses".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   ├► " + "Enable Herbivore/Carnivore Genes".Translate(), ref settings.enableFoodGenes, "Включает гены 'Травоядный' и 'Плотоядный' у пони.".Translate(), 0, 1);
                listingStandard.CheckboxLabeled("   └► " + "Enable Food Debuffs for Genes".Translate(), ref settings.enableFoodDebuffs, "Включает дебаффы настроения от еды для генов 'Травоядный' и 'Плотоядный'/".Translate(), 0, 1);

            }
            else
            {
                settings.ClassicalSettingsToggle(false);
                settings.enableFoodGenes = false;
                settings.enableFoodDebuffs = false;
            }
        }
        #endregion

    }

}