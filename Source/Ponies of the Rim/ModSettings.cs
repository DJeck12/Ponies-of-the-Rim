using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{

    public class PoniesOfTheRimSettingsData : ModSettings
    {
        public bool storryTeller; //! <-- This is only to test config funcionality do not include it
        public bool ideology;
        public bool abilities;
        public bool prosthetics;
        //TODO: Add Fractions

        /// <summary>
        /// This writes our settings to file.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref storryTeller, "storryTeller", true);
            Scribe_Values.Look(ref ideology, "ideology", true);
            Scribe_Values.Look(ref abilities, "abilities", true);
            Scribe_Values.Look(ref prosthetics, "prosthetics", true);
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
            // └ 	┴ 	┬ 	├ 	─ 	┼ 
            // ►

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            //TODO: Add enable and siable All button
            listingStandard.Label("PoniesOfRimSettings".Translate());
            listingStandard.Gap(15);
            listingStandard.Label("Core".Translate());
            listingStandard.CheckboxLabeled("├► " + "storryTeller".Translate(), ref settings.storryTeller, "addStorryTeller".Translate(), 0, 1);
            listingStandard.CheckboxLabeled("├► " + "ideology".Translate(), ref settings.ideology, "addIdeology".Translate(), 0, 1);
            listingStandard.CheckboxLabeled("├► " + "abilities".Translate(), ref settings.abilities, "addAbilities".Translate(), 0, 1);
            listingStandard.CheckboxLabeled("└► " + "prosthetics".Translate(), ref settings.prosthetics, "addProsthetics".Translate(), 0, 1);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Overwriting this enables confings
        /// </summary>
        public override string SettingsCategory()
        {
            return "PoniesOfTheRim".Translate();
        }
    }
}
