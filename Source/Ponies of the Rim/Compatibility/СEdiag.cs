using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
    public static class CombatExtendedLoadoutDiagnostics
    {
        private static bool _resolved;

        private static Type _tLoadoutExt;
        private static Type _tCompInventory;
        private static Type _tCompAmmoUser;
        private static Type _tAmmoThing;

        private static MethodInfo _miAvailableWeight;
        private static MethodInfo _miAvailableBulk;
        private static MethodInfo _miUseAmmo;

        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            _tLoadoutExt = AccessTools.TypeByName("CombatExtended.LoadoutPropertiesExtension");
            _tCompInventory = AccessTools.TypeByName("CombatExtended.CompInventory");
            _tCompAmmoUser = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
            _tAmmoThing = AccessTools.TypeByName("CombatExtended.AmmoThing");

            if (_tCompInventory != null)
            {
                _miAvailableWeight = AccessTools.Method(_tCompInventory, "GetAvailableWeight");
                _miAvailableBulk = AccessTools.Method(_tCompInventory, "GetAvailableBulk");
            }

            if (_tCompAmmoUser != null)
            {
                _miUseAmmo = AccessTools.PropertyGetter(_tCompAmmoUser, "UseAmmo");
            }
        }

        public static void Report(Pawn pawn, PawnKindDef kindToInspect, string context)
        {
            if (!Prefs.DevMode || pawn == null || !CombatExtendedCompatability.Active)
            {
                return;
            }

            try
            {
                Resolve();
                Log.Message(Build(pawn, kindToInspect, context));
            }
            catch (Exception arg)
            {
                Log.Warning($"[PoniesOfTheRim] Разбор боезапаса не удался (на генерацию не влияет):\n{arg}");
            }
        }

        private static string Build(Pawn pawn, PawnKindDef kindToInspect, string context)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[PoniesOfTheRim] Разбор боезапаса CE: " + pawn.LabelShortCap + " (" + context + ")");
            sb.AppendLine("  боевой kindDef: " + (kindToInspect?.defName ?? "—"));

            bool hasExt = false;
            if (_tLoadoutExt != null && kindToInspect?.modExtensions != null)
            {
                hasExt = kindToInspect.modExtensions.Any(e => _tLoadoutExt.IsInstanceOfType(e));
            }

            sb.AppendLine("  1. LoadoutPropertiesExtension: " + (hasExt ? "есть" : "НЕТ — патроны не будут созданы"));

            bool manipulation = pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false;
            bool violentDisabled = pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool toolUser = pawn.RaceProps?.ToolUser ?? false;

            sb.AppendLine("  2. Манипуляция: " + (manipulation ? "есть" : "НЕТ — выход"));
            sb.AppendLine("  3. Насилие: " + (violentDisabled ? "ЗАПРЕЩЕНО — выход" : "разрешено"));
            sb.AppendLine("  4. ToolUser: " + (toolUser ? "да" : "НЕТ — выход"));

            ThingComp inventoryComp = FindComp(pawn, _tCompInventory);
            sb.AppendLine("  5. CompInventory: " + (inventoryComp != null
                ? "есть"
                : "НЕТ — CE напишет ошибку и выйдет"));

            ThingWithComps primary = pawn.equipment?.Primary;
            sb.AppendLine("  6. Основное оружие: " + (primary?.def?.defName ?? "НЕТ — патроны не создаются"));

            if (primary != null)
            {
                ThingComp ammoUser = FindComp(primary, _tCompAmmoUser);
                if (ammoUser == null)
                {
                    sb.AppendLine("  7. CompAmmoUser: НЕТ — оружие ближнего боя или не переведено на CE");
                }
                else
                {
                    sb.AppendLine("  7. CompAmmoUser: есть, UseAmmo = " + Describe(SafeInvoke(_miUseAmmo, ammoUser)));
                }
            }

            if (inventoryComp != null)
            {
                sb.AppendLine("  8. Свободно: вес " + Describe(SafeInvoke(_miAvailableWeight, inventoryComp))
                    + ", объём " + Describe(SafeInvoke(_miAvailableBulk, inventoryComp)));
            }

            sb.AppendLine("  Инвентарь: " + DescribeInventory(pawn));

            return sb.ToString();
        }

        private static object SafeInvoke(MethodInfo method, object instance)
        {
            if (method == null)
            {
                return null;
            }

            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.HasDefaultValue)
                {
                    args[i] = p.DefaultValue;
                }
                else if (p.ParameterType.IsValueType)
                {
                    args[i] = Activator.CreateInstance(p.ParameterType);
                }
                else
                {
                    args[i] = null;
                }
            }

            return method.Invoke(instance, args);
        }

        private static string Describe(object value)
        {
            return value?.ToString() ?? "?";
        }

        private static ThingComp FindComp(ThingWithComps thing, Type compType)
        {
            if (thing?.AllComps == null || compType == null)
            {
                return null;
            }

            for (int i = 0; i < thing.AllComps.Count; i++)
            {
                if (compType.IsInstanceOfType(thing.AllComps[i]))
                {
                    return thing.AllComps[i];
                }
            }

            return null;
        }

        private static string DescribeInventory(Pawn pawn)
        {
            ThingOwner<Thing> container = pawn.inventory?.innerContainer;
            if (container == null || container.Count == 0)
            {
                return "пусто";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < container.Count; i++)
            {
                Thing t = container[i];
                bool isAmmo = _tAmmoThing != null && _tAmmoThing.IsInstanceOfType(t);
                parts.Add((isAmmo ? "[патроны] " : "") + t.def.defName + " x" + t.stackCount);
            }

            return string.Join(", ", parts.ToArray());
        }
    }
}