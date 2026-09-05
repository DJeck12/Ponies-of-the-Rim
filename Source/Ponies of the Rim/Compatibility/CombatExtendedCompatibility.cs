using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class CombatExtendedCompatability
    {
        public const string PackageId = "ceteam.combatextended";
        public const string PackageIdSteam = "ceteam.combatextended_steam";

        private const string TypeCeUtility = "CombatExtended.CE_Utility";
        private const string TypeRaceExtension = "CombatExtended.RacePropertiesExtensionCE";
        private const string TypeToolCe = "CombatExtended.ToolCE";

        private const string TypeCompInventory = "CombatExtended.CompInventory";
        private const string TypeCompSuppressable = "CombatExtended.CompSuppressable";
        private const string TypeCompTacticalManager = "CombatExtended.CompTacticalManager";
        private const string TypeCompPawnGizmo = "CombatExtended.CompPawnGizmo";

        private const string TypePropsInventory = "CombatExtended.CompProperties_Inventory";
        private const string TypePropsSuppressable = "CombatExtended.CompProperties_Suppressable";
        private const string TypePropsTacticalManager = "CombatExtended.CompProperties_TacticalManager";

        private static bool _resolved;
        private static bool _active;

        private static Type _tRaceExtension;
        private static Type _tToolCe;
        private static FieldInfo _fiBodyShape;
        private static MethodInfo _miTryUpdateInventory;

        private static Dictionary<ThingDef, CombatExtendedFlightExtension> _flightExtByRace;

        public static bool Active
        {
            get
            {
                Resolve();
                return _active;
            }
        }

        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            Type ceUtility = AccessTools.TypeByName(TypeCeUtility);
            if (ceUtility == null)
            {
                _active = false;
                return;
            }

            _tRaceExtension = AccessTools.TypeByName(TypeRaceExtension);
            _tToolCe = AccessTools.TypeByName(TypeToolCe);
            _fiBodyShape = _tRaceExtension != null ? AccessTools.Field(_tRaceExtension, "bodyShape") : null;
            _miTryUpdateInventory = AccessTools.Method(ceUtility, "TryUpdateInventory", new Type[1] { typeof(Pawn) });

            _active = _tRaceExtension != null && _tToolCe != null;

            if (!_active)
            {
                Log.Warning("[PoniesOfTheRim] Combat Extended обнаружен, но его публичные типы не разрешились. " +
                            "Патч совместимости отключён.");
            }
        }

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn == null || !Active || _miTryUpdateInventory == null)
            {
                return;
            }

            try
            {
                _miTryUpdateInventory.Invoke(null, new object[1] { pawn });
            }
            catch (Exception arg)
            {
                Log.Warning($"[PoniesOfTheRim] CE: не удалось обновить инвентарь для {pawn.LabelShortCap}:\n{arg}");
            }
        }

        public static void BuildFlightExtensionCache()
        {
            Dictionary<ThingDef, CombatExtendedFlightExtension> dictionary =
                new Dictionary<ThingDef, CombatExtendedFlightExtension>();

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                CombatExtendedFlightExtension ext = def.GetModExtension<CombatExtendedFlightExtension>();
                if (ext != null)
                {
                    dictionary[def] = ext;
                }
            }

            _flightExtByRace = dictionary;

            if (dictionary.Count > 0)
            {
                Log.Message($"[PoniesOfTheRim] CE: полётные множители коллизии загружены для {dictionary.Count} рас(ы).");
            }
        }

        public static bool TryGetFlightExtension(ThingDef def, out CombatExtendedFlightExtension ext)
        {
            ext = null;
            if (def == null || _flightExtByRace == null)
            {
                return false;
            }

            return _flightExtByRace.TryGetValue(def, out ext);
        }

        public static void EnsureRaceComps()
        {
            if (!Active)
            {
                return;
            }

            Type tInventory = AccessTools.TypeByName(TypeCompInventory);
            Type tSuppressable = AccessTools.TypeByName(TypeCompSuppressable);
            Type tTactical = AccessTools.TypeByName(TypeCompTacticalManager);
            Type tGizmo = AccessTools.TypeByName(TypeCompPawnGizmo);

            if (tInventory == null || tSuppressable == null || tTactical == null || tGizmo == null)
            {
                Log.Warning("[PoniesOfTheRim] CE: типы компонентов не разрешились — нормализация пропущена.");
                return;
            }

            int removed = 0;
            int added = 0;
            int racesTouched = 0;

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (!PonyHelper.IsPonyRace(def))
                {
                    continue;
                }

                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }

                int before = removed + added;

                removed += RemoveExtraComps(def, tInventory);
                removed += RemoveExtraComps(def, tSuppressable);
                removed += RemoveExtraComps(def, tTactical);
                removed += RemoveExtraComps(def, tGizmo);

                added += EnsureComp(def, tInventory, TypePropsInventory);
                added += EnsureComp(def, tSuppressable, TypePropsSuppressable);
                added += EnsureComp(def, tTactical, TypePropsTacticalManager);
                added += EnsureComp(def, tGizmo, null);

                if (removed + added != before)
                {
                    racesTouched++;
                }
            }

            if (racesTouched > 0)
            {
                Log.Message($"[PoniesOfTheRim] CE: компоненты нормализованы у {racesTouched} рас(ы) — " +
                            $"удалено дубликатов {removed}, добавлено недостающих {added}.");
            }
        }

        private static int RemoveExtraComps(ThingDef def, Type compType)
        {
            int seen = 0;
            int removed = 0;

            for (int i = 0; i < def.comps.Count; i++)
            {
                CompProperties props = def.comps[i];
                if (props?.compClass == null || !compType.IsAssignableFrom(props.compClass))
                {
                    continue;
                }

                seen++;
                if (seen > 1)
                {
                    def.comps.RemoveAt(i);
                    removed++;
                    i--;
                }
            }

            return removed;
        }

        private static int EnsureComp(ThingDef def, Type compType, string propsTypeName)
        {
            for (int i = 0; i < def.comps.Count; i++)
            {
                CompProperties props = def.comps[i];
                if (props?.compClass != null && compType.IsAssignableFrom(props.compClass))
                {
                    return 0;
                }
            }

            try
            {
                CompProperties created;
                if (propsTypeName != null)
                {
                    Type propsType = AccessTools.TypeByName(propsTypeName);
                    if (propsType == null)
                    {
                        return 0;
                    }

                    created = (CompProperties)Activator.CreateInstance(propsType);
                }
                else
                {
                    created = new CompProperties { compClass = compType };
                }

                def.comps.Add(created);
                return 1;
            }
            catch (Exception arg)
            {
                Log.Warning($"[PoniesOfTheRim] CE: не удалось добавить {compType.Name} к {def.defName}:\n{arg}");
                return 0;
            }
        }

        public static void AuditRaces()
        {
            if (!Active)
            {
                return;
            }

            List<string> unpatchedTools = new List<string>();
            List<string> duplicateExtensions = new List<string>();
            List<string> missingExtension = new List<string>();
            List<string> missingBodyShape = new List<string>();
            List<string> duplicateComps = new List<string>();
            List<string> missingComps = new List<string>();

            int racesChecked = 0;

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (!PonyHelper.IsPonyRace(def))
                {
                    continue;
                }

                racesChecked++;
                AuditTools(def, unpatchedTools);
                AuditExtensions(def, duplicateExtensions, missingExtension, missingBodyShape);
                AuditComps(def, duplicateComps, missingComps);
            }

            Report("не сконвертированы в ToolCE (автопатчер CE перехватит расу)", unpatchedTools);
            Report("получили несколько RacePropertiesExtensionCE", duplicateExtensions);
            Report("остались без RacePropertiesExtensionCE", missingExtension);
            Report("имеют RacePropertiesExtensionCE без bodyShape (NRE при крит-нокдауне)", missingBodyShape);
            Report("получили дублирующиеся компоненты CE", duplicateComps);
            Report("остались без компонентов CE", missingComps);

            if (unpatchedTools.Count == 0 && duplicateExtensions.Count == 0 && missingExtension.Count == 0
                && missingBodyShape.Count == 0 && duplicateComps.Count == 0 && missingComps.Count == 0)
            {
                Log.Message($"[PoniesOfTheRim] CE-аудит: проверено рас — {racesChecked}, замечаний нет.");
            }
        }

        private static void AuditTools(ThingDef def, List<string> unpatchedTools)
        {
            if (def.tools == null)
            {
                return;
            }

            for (int i = 0; i < def.tools.Count; i++)
            {
                Tool tool = def.tools[i];
                if (tool != null && !_tToolCe.IsInstanceOfType(tool))
                {
                    unpatchedTools.Add(def.defName + " → " + (tool.label ?? tool.id ?? "<без имени>"));
                }
            }
        }

        private static void AuditExtensions(ThingDef def, List<string> duplicates, List<string> missing, List<string> noShape)
        {
            int count = 0;
            object first = null;

            List<DefModExtension> extensions = def.modExtensions;
            if (extensions != null)
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    if (_tRaceExtension.IsInstanceOfType(extensions[i]))
                    {
                        count++;
                        if (first == null)
                        {
                            first = extensions[i];
                        }
                    }
                }
            }

            if (count == 0)
            {
                missing.Add(def.defName);
                return;
            }

            if (count > 1)
            {
                duplicates.Add(def.defName + " (x" + count + ")");
            }

            if (_fiBodyShape != null && _fiBodyShape.GetValue(first) == null)
            {
                noShape.Add(def.defName);
            }
        }

        private static void AuditComps(ThingDef def, List<string> duplicates, List<string> missing)
        {
            CountComp(def, TypeCompInventory, duplicates, missing);
            CountComp(def, TypeCompSuppressable, duplicates, missing);
            CountComp(def, TypeCompTacticalManager, duplicates, missing);
            CountComp(def, TypeCompPawnGizmo, duplicates, missing);
        }

        private static void CountComp(ThingDef def, string compTypeName, List<string> duplicates, List<string> missing)
        {
            Type compType = AccessTools.TypeByName(compTypeName);
            if (compType == null)
            {
                Log.Warning("[PoniesOfTheRim] CE-аудит: тип " + compTypeName + " не найден — проверка пропущена.");
                return;
            }

            int count = 0;
            if (def.comps != null)
            {
                for (int i = 0; i < def.comps.Count; i++)
                {
                    CompProperties props = def.comps[i];
                    if (props?.compClass != null && compType.IsAssignableFrom(props.compClass))
                    {
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                missing.Add(def.defName + " → " + compType.Name);
            }
            else if (count > 1)
            {
                duplicates.Add(def.defName + " → " + compType.Name + " (x" + count + ")");
            }
        }

        private static void Report(string what, List<string> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            Log.Warning("[PoniesOfTheRim] CE-аудит: " + entries.Count + " записей " + what + ":\n  " +
                        string.Join("\n  ", entries.ToArray()));
        }
    }
}