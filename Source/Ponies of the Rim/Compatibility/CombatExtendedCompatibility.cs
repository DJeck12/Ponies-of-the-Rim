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

        private static readonly string[] AuditedCompTypes =
        {
            TypeCompInventory,
            TypeCompSuppressable,
            TypeCompTacticalManager,
            TypeCompPawnGizmo
        };

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
            _miTryUpdateInventory = AccessTools.Method(ceUtility, "TryUpdateInventory", new[] { typeof(Pawn) });

            _active = _tRaceExtension != null && _tToolCe != null;

            if (!_active)
            {
                PonyLog.Warn("CE: CE is loaded but RacePropertiesExtensionCE or ToolCE was not found, compatibility disabled");
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
                _miTryUpdateInventory.Invoke(null, new object[] { pawn });
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught($"CE: inventory update failed for {pawn.LabelShortCap}", ex);
            }
        }

        public static void BuildFlightExtensionCache()
        {
            Dictionary<ThingDef, CombatExtendedFlightExtension> byRace =
                new Dictionary<ThingDef, CombatExtendedFlightExtension>();

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                CombatExtendedFlightExtension ext = def.GetModExtension<CombatExtendedFlightExtension>();
                if (ext != null)
                {
                    byRace[def] = ext;
                }
            }

            _flightExtByRace = byRace;

            if (byRace.Count > 0)
            {
                PonyLog.Trace($"CE: flight collision factors for {byRace.Count} races");
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
                PonyLog.Warn("CE: CE comp types not found, race comps not normalized");
                return;
            }

            int removed = 0;
            int added = 0;
            int racesTouched = 0;

            Dictionary<string, int> removedByType = new Dictionary<string, int>();
            Dictionary<string, int> addedByType = new Dictionary<string, int>();

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

                removed += Tally(removedByType, tInventory, RemoveExtraComps(def, tInventory));
                removed += Tally(removedByType, tSuppressable, RemoveExtraComps(def, tSuppressable));
                removed += Tally(removedByType, tTactical, RemoveExtraComps(def, tTactical));
                removed += Tally(removedByType, tGizmo, RemoveExtraComps(def, tGizmo));

                added += Tally(addedByType, tInventory, EnsureComp(def, tInventory, TypePropsInventory));
                added += Tally(addedByType, tSuppressable, EnsureComp(def, tSuppressable, TypePropsSuppressable));
                added += Tally(addedByType, tTactical, EnsureComp(def, tTactical, TypePropsTacticalManager));
                added += Tally(addedByType, tGizmo, EnsureComp(def, tGizmo, null));

                if (removed + added != before)
                {
                    racesTouched++;
                }
            }

            if (racesTouched > 0)
            {
                PonyLog.Trace($"CE: comps normalized on {racesTouched} races, " +
                              $"{removed} duplicates removed{FormatByType(removedByType)}, {added} added{FormatByType(addedByType)}");
            }
        }

        private static int Tally(Dictionary<string, int> byType, Type compType, int n)
        {
            if (n > 0)
            {
                byType.TryGetValue(compType.Name, out int total);
                byType[compType.Name] = total + n;
            }
            return n;
        }

        private static string FormatByType(Dictionary<string, int> byType)
        {
            if (byType.Count == 0)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>(byType.Count);
            foreach (KeyValuePair<string, int> pair in byType)
            {
                parts.Add(pair.Key + " " + pair.Value);
            }
            return " (" + string.Join(", ", parts) + ")";
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
            catch (Exception ex)
            {
                PonyLog.Warn($"CE: could not add {compType.Name} to {def.defName}\n{ex}");
                return 0;
            }
        }

        public static void AuditRaces()
        {
            if (!Active)
            {
                return;
            }

            List<Type> compTypes = ResolveAuditedCompTypes();

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
                AuditComps(def, compTypes, duplicateComps, missingComps);
            }

            Report("Tools not converted to ToolCE, the CE autopatcher will take over these races", unpatchedTools);
            Report("Races with more than one RacePropertiesExtensionCE", duplicateExtensions);
            Report("Races without RacePropertiesExtensionCE", missingExtension);
            Report("Races with RacePropertiesExtensionCE but no bodyShape (NRE on critical knockdown)", missingBodyShape);
            Report("Duplicate CE comps", duplicateComps);
            Report("Missing CE comps", missingComps);

            if (unpatchedTools.Count == 0 && duplicateExtensions.Count == 0 && missingExtension.Count == 0
                && missingBodyShape.Count == 0 && duplicateComps.Count == 0 && missingComps.Count == 0)
            {
                PonyLog.Trace($"CE audit: {racesChecked} races, no issues");
            }
        }

        private static List<Type> ResolveAuditedCompTypes()
        {
            List<Type> types = new List<Type>(AuditedCompTypes.Length);
            foreach (string name in AuditedCompTypes)
            {
                Type type = AccessTools.TypeByName(name);
                if (type == null)
                {
                    PonyLog.Warn($"CE audit: type {name} not found, check skipped");
                    continue;
                }
                types.Add(type);
            }
            return types;
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
                    unpatchedTools.Add(def.defName + ": " + (tool.label ?? tool.id ?? "?"));
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
                duplicates.Add(def.defName + " x" + count);
            }

            if (_fiBodyShape != null && _fiBodyShape.GetValue(first) == null)
            {
                noShape.Add(def.defName);
            }
        }

        private static void AuditComps(ThingDef def, List<Type> compTypes, List<string> duplicates, List<string> missing)
        {
            for (int i = 0; i < compTypes.Count; i++)
            {
                CountComp(def, compTypes[i], duplicates, missing);
            }
        }

        private static void CountComp(ThingDef def, Type compType, List<string> duplicates, List<string> missing)
        {
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
                missing.Add(def.defName + ": " + compType.Name);
            }
            else if (count > 1)
            {
                duplicates.Add(def.defName + ": " + compType.Name + " x" + count);
            }
        }

        private static void Report(string what, List<string> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            PonyLog.Warn($"CE audit: {entries.Count} {what}:\n  " + string.Join("\n  ", entries));
        }
    }
}