using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using PoniesOfTheRim.Abilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim
{
    public class PoniesOfTheRimSettingsData : ModSettings
    {
        public bool classical           = true;
        public bool ideology            = true;
        public bool abilities           = true;
        public bool prosthetics         = true;
        public bool anthro              = true;
        public bool enableRegularHorses = true;
        public bool enableFoodGenes     = true;
        public bool enableFoodDebuffs   = true;

        public Dictionary<string, bool> patchToggles = new();

        public Dictionary<string, bool> abilityToggles = new();

        public bool IsAbilityEnabled(string abilityDefName)
        {
            if (!abilities)
                return false;
            return !abilityToggles.TryGetValue(abilityDefName, out bool val) || val;
        }

        public void ClassicalSettingsToggle(bool action)
        {
            ideology            = action;
            abilities           = action;
            prosthetics         = action;
            anthro              = action;
            enableRegularHorses = action;
        }

        public void ClassicalSettingsDefault() => ClassicalSettingsToggle(true);

        public override void ExposeData()
        {
            Scribe_Values.Look(ref classical,            "classical",            true, true);
            Scribe_Values.Look(ref ideology,             "ideology",             true, true);
            Scribe_Values.Look(ref abilities,            "abilities",            true, true);
            Scribe_Values.Look(ref prosthetics,          "prosthetics",          true, true);
            Scribe_Values.Look(ref anthro,               "anthro",               true, true);
            Scribe_Values.Look(ref enableRegularHorses,  "enableRegularHorses",  true, true);
            Scribe_Values.Look(ref enableFoodGenes,      "enableFoodGenes",      true, true);
            Scribe_Values.Look(ref enableFoodDebuffs,    "enableFoodDebuffs",    true, true);

            Scribe_Collections.Look(ref patchToggles,   "patchToggles",   LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref abilityToggles, "abilityToggles", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                patchToggles   ??= new();
                abilityToggles ??= new();
            }

            base.ExposeData();
        }
    }

    [StaticConstructorOnStartup]
    public static class AbilityToggleInitializer
    {
        static AbilityToggleInitializer()
        {
            int registered = 0;
            foreach (AbilityDef def in DefDatabase<AbilityDef>.AllDefs)
            {
                AbilityToggleExtension ext = def.GetModExtension<AbilityToggleExtension>();
                if (ext == null)
                    continue;

                PoniesOfTheRimSettings.RegisterAbilityToggle(def.defName, ext.defaultEnabled);
                registered++;
            }

            if (registered > 0)
                Log.Message($"[PoniesOfTheRim] Зарегистрировано {registered} переключаемых способностей в настройках.");
        }
    }

    public class PoniesOfTheRimSettings : Mod
    {
        public static PoniesOfTheRimSettingsData settings;

        public static Dictionary<string, bool>   defaultPatchToggles   = new();

        public static Dictionary<string, bool>   defaultAbilityToggles = new();

        public static Dictionary<string, string> patchLabels           = new();

        public static Dictionary<string, string> patchDescriptions     = new();

        private Dictionary<string, bool> _patchSnapshot = new();

        private int      currentTab        = 0;
        private Vector2  generalScroll     = Vector2.zero;
        private Vector2  patchScroll       = Vector2.zero;
        private bool     abilitiesExpanded = true;
        private bool     dietExpanded      = true;

        public PoniesOfTheRimSettings(ModContentPack content) : base(content)
        {
            settings = GetSettings<PoniesOfTheRimSettingsData>();
            InitializePatchToggles(content);
        }

        public static void RegisterAbilityToggle(string defName, bool defaultEnabled)
        {
            defaultAbilityToggles[defName] = defaultEnabled;
            if (!settings.abilityToggles.ContainsKey(defName))
                settings.abilityToggles[defName] = defaultEnabled;
        }

        private void InitializePatchToggles(ModContentPack content)
        {
            string patchesDir = Path.Combine(content.RootDir, "1.6", "Patches");
            if (!Directory.Exists(patchesDir))
            {
                Log.Warning($"[PoniesOfTheRim] Директория патчей не найдена: {patchesDir}");
                return;
            }

            foreach (string file in Directory.GetFiles(patchesDir, "*.xml", SearchOption.AllDirectories))
            {
                try
                {
                    XmlDocument doc = new();
                    doc.Load(file);

                    XmlNode opNode = doc.DocumentElement?.SelectSingleNode("Operation");
                    if (opNode == null)
                        continue;

                    string settingId = opNode.SelectSingleNode("settingId")?.InnerText?.Trim();
                    if (string.IsNullOrEmpty(settingId))
                        continue;

                    bool defaultState = true;
                    XmlNode defaultNode = opNode.SelectSingleNode("defaultState");
                    if (defaultNode != null)
                        bool.TryParse(defaultNode.InnerText.Trim(), out defaultState);

                    string label       = opNode.SelectSingleNode("label")?.InnerText?.Trim();
                    string description = opNode.SelectSingleNode("description")?.InnerText?.Trim();

                    patchLabels[settingId]       = !string.IsNullOrEmpty(label) ? label : settingId;
                    patchDescriptions[settingId] = description ?? string.Empty;

                    defaultPatchToggles[settingId] = defaultState;
                    if (!settings.patchToggles.ContainsKey(settingId))
                        settings.patchToggles[settingId] = defaultState;
                }
                catch (Exception ex)
                {
                    Log.Error($"[PoniesOfTheRim] Ошибка чтения патч-XML {file}: {ex.Message}");
                }
            }

            _patchSnapshot = new Dictionary<string, bool>(settings.patchToggles);
        }

        public override string SettingsCategory() => "SettingsPoniesOfTheRim".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            List<TabRecord> tabs = new()
            {
                new TabRecord("Pony_TabGeneral".Translate(), () => currentTab = 0, currentTab == 0),
                new TabRecord("Pony_TabPatches".Translate(), () => currentTab = 1, currentTab == 1),
            };

            Rect contentRect = inRect;
            contentRect.yMin += 32f;             
            TabDrawer.DrawTabs(contentRect, tabs);

            if (currentTab == 0)
                DrawGeneralTab(contentRect);
            else
                DrawPatchTab(contentRect);

        }

        private void DrawGeneralTab(Rect rect)
        {
            float lineHeight   = Text.LineHeight + 4f;
            float baseLines    = 8f;
            float abilityLines = settings.abilities && abilitiesExpanded
                ? settings.abilityToggles.Count + 1f : 1f;
            float dietLines    = settings.classical && settings.enableFoodGenes && dietExpanded
                ? 2f : 1f;
            float totalHeight  = (baseLines + abilityLines + dietLines) * lineHeight + 60f;

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(totalHeight, rect.height));
            Widgets.BeginScrollView(rect, ref generalScroll, viewRect);

            Listing_Standard ls = new();
            ls.Begin(viewRect);

            DrawHeader(ls);

            GUI.color = Color.yellow;
            ls.Label("\u26a0 " + "Pony_WarnReloadSave".Translate());
            GUI.color = Color.white;

            ls.GapLine(6f);
            ls.Label("SettingsCore".Translate());

            DrawClassicalSettings(ls);

            ls.End();
            Widgets.EndScrollView();
        }

        private void DrawHeader(Listing_Standard ls)
        {
            ls.Label("SettingsHeader".Translate());

            Rect row  = ls.GetRect(28f);
            float btnW = row.width * 0.25f;

            if (Widgets.ButtonText(new Rect(row.x, row.y, btnW, 28f), "SettingsDefulatAll".Translate()))
                DefaultAllData();

            ls.Gap(6f);
        }

        private void DrawCollapsibleHeader(
            Listing_Standard ls,
            string label,
            string tooltip,
            ref bool masterEnabled,
            ref bool expanded,
            float indent = 22f)
        {
            Rect row = ls.GetRect(Text.LineHeight);

            Rect arrowRect = new Rect(row.x + indent, row.y, 20f, row.height);
            if (masterEnabled)
            {
                if (Widgets.ButtonText(arrowRect, expanded ? "▼" : "►"))
                    expanded = !expanded;
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(arrowRect, "►");
                GUI.color = Color.white;
                expanded = false;
            }

            Rect labelRect = new Rect(
                arrowRect.xMax + 4f, row.y,
                row.xMax - arrowRect.xMax - 4f - 28f, row.height);
            Widgets.Label(labelRect, label);

            bool prev = masterEnabled;
            Widgets.Checkbox(row.xMax - 24f, row.y, ref masterEnabled);
            if (!prev && masterEnabled)
                expanded = true;    

            if (!tooltip.NullOrEmpty())
                TooltipHandler.TipRegion(row, tooltip);

            ls.Gap(2f);
        }

        private void DrawClassicalSettings(Listing_Standard ls)
        {
            ls.CheckboxLabeled(
                "└► " + "SettingsClassical".Translate(),
                ref settings.classical,
                "SettingsToolTipClassical".Translate(), 0, 1);

            if (!settings.classical)
            {
                settings.ClassicalSettingsToggle(false);
                settings.enableFoodGenes   = false;
                settings.enableFoodDebuffs = false;
                return;
            }

            DrawCollapsibleHeader(
                ls,
                "   " + "SettingsAbilities".Translate(),
                "SettingsToolTipAbilities".Translate(),
                ref settings.abilities,
                ref abilitiesExpanded,
                indent: 30f);

            if (settings.abilities && abilitiesExpanded && settings.abilityToggles.Count > 0)
            {
                List<string> keys = settings.abilityToggles.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    string defName = keys[i];
                    bool val = settings.abilityToggles[defName];

                    AbilityDef def = DefDatabase<AbilityDef>.GetNamed(defName, errorOnFail: false);
                    AbilityToggleExtension ext = def?.GetModExtension<AbilityToggleExtension>();

                    string label = !ext?.settingLabel.NullOrEmpty() == true
                        ? ext.settingLabel
                        : def?.label?.CapitalizeFirst() ?? defName;

                    bool isLast = i == keys.Count - 1;
                    ls.CheckboxLabeled(
                        (isLast ? "         └► " : "         ├► ") + label,
                        ref val, tooltip: null, 0, 1);

                    settings.abilityToggles[defName] = val;
                }
            }

            DrawCollapsibleHeader(
                ls,
                "   " + "Pony_SettingsDiet".Translate(),
                "Pony_SettingsToolTipDiet".Translate(),
                ref settings.enableFoodGenes,
                ref dietExpanded,
                indent: 30f);

            if (settings.enableFoodGenes && dietExpanded)
            {
                ls.CheckboxLabeled(
                    "         └► " + "Pony_SettingsFoodDebuffs".Translate(),
                    ref settings.enableFoodDebuffs,
                    "Pony_SettingsToolTipFoodDebuffs".Translate(), 0, 1);
            }
            else if (!settings.enableFoodGenes)
            {
                settings.enableFoodDebuffs = false;
            }
        }

        private void DrawPatchTab(Rect rect)
        {
            if (settings.patchToggles.Count == 0)
            {
                Listing_Standard ls = new();
                ls.Begin(rect);
                ls.Label("Pony_NoPatchesFound".Translate());
                ls.End();
                return;
            }

            bool restartNeeded = PatchSettingsChanged();

            float lineHeight  = Text.LineHeight + 4f;
            float extraHeight = restartNeeded ? 40f : 0f;
            float totalHeight = settings.patchToggles.Count * lineHeight + 60f + extraHeight;

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(totalHeight, rect.height));
            Widgets.BeginScrollView(rect, ref patchScroll, viewRect);

            Listing_Standard ls2 = new();
            ls2.Begin(viewRect);

            ls2.Label("Pony_PatchSettingsHeader".Translate());

            GUI.color = Color.yellow;
            ls2.Label("\u26a0 " + "Pony_WarnReloadGame".Translate());
            GUI.color = Color.white;

            if (restartNeeded)
            {
                ls2.Gap(4f);
                Rect btnRow = ls2.GetRect(28f);
                GUI.color = new Color(1f, 0.65f, 0.1f);   
                if (Widgets.ButtonText(btnRow, "\u26a0 " + "Pony_RestartNow".Translate()))
                {
                    GUI.color = Color.white;
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "Pony_RestartConfirm".Translate(),
                        () =>
                        {
                            WriteSettings();                  
                            GenCommandLine.Restart();       
                        },
                        destructive: false));
                }
                GUI.color = Color.white;
                ls2.Gap(4f);
            }

            ls2.GapLine(6f);

            foreach (string settingId in settings.patchToggles.Keys.ToList())
            {
                bool value = settings.patchToggles[settingId];

                string displayLabel = patchLabels.TryGetValue(settingId, out string lbl) && !lbl.NullOrEmpty()
                    ? lbl.Translate()
                    : settingId;
                string description = patchDescriptions.TryGetValue(settingId, out string desc) && !desc.NullOrEmpty()
                    ? desc.Translate()
                    : null;

                Rect rowRect = ls2.GetRect(Text.LineHeight + 2f);
                ls2.Gap(2f);

                if (description != null)
                {
                    if (Mouse.IsOver(rowRect))
                        Widgets.DrawHighlight(rowRect);
                    TooltipHandler.TipRegion(rowRect, description);
                }

                Widgets.CheckboxLabeled(rowRect, displayLabel, ref value);
                settings.patchToggles[settingId] = value;
            }

            ls2.End();
            Widgets.EndScrollView();
        }

        private bool PatchSettingsChanged()
        {
            foreach (KeyValuePair<string, bool> kv in settings.patchToggles)
            {
                if (_patchSnapshot.TryGetValue(kv.Key, out bool snap) && snap != kv.Value)
                    return true;
            }
            return false;
        }

        private void DefaultAllData()
        {
            settings.classical = true;
            settings.ClassicalSettingsDefault();
            settings.enableFoodGenes   = true;
            settings.enableFoodDebuffs = true;

            foreach (string key in defaultAbilityToggles.Keys)
                settings.abilityToggles[key] = defaultAbilityToggles[key];

            foreach (string key in defaultPatchToggles.Keys)
                settings.patchToggles[key] = defaultPatchToggles[key];
        }
    }
}