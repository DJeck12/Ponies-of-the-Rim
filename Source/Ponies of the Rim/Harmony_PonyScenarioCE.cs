using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class ScenarioStuffReconciler
    {
        private const string PackagePrefix = "pony.poniesoftherim";

        private static readonly AccessTools.FieldRef<ScenPart_StartingThing_Defined, ThingDef> ThingDefRef =
            MakeRef("thingDef");

        private static readonly AccessTools.FieldRef<ScenPart_StartingThing_Defined, ThingDef> StuffRef =
            MakeRef("stuff");

        static ScenarioStuffReconciler()
        {
            LongEventHandler.ExecuteWhenFinished(Reconcile);
        }

        private static AccessTools.FieldRef<ScenPart_StartingThing_Defined, ThingDef> MakeRef(string field)
        {
            try
            {
                return AccessTools.FieldRefAccess<ScenPart_StartingThing_Defined, ThingDef>(field);
            }
            catch (Exception ex)
            {
                PonyLog.Warn($"Scenarios: ScenPart_StartingThing_Defined.{field} not found, starting items not checked ({ex.Message})");
                return null;
            }
        }

        private static void Reconcile()
        {
            if (ThingDefRef == null || StuffRef == null)
            {
                return;
            }

            List<string> assigned = new List<string>();
            List<string> cleared = new List<string>();

            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefsListForReading)
            {
                if (!IsOurs(def.modContentPack) || def.scenario == null)
                {
                    continue;
                }

                foreach (ScenPart part in def.scenario.AllParts)
                {
                    if (!(part is ScenPart_StartingThing_Defined defined))
                    {
                        continue;
                    }

                    ThingDef thing = ThingDefRef(defined);
                    if (thing == null)
                    {
                        continue;
                    }

                    ThingDef stuff = StuffRef(defined);

                    if (thing.MadeFromStuff && stuff == null)
                    {
                        ThingDef chosen = PickStuff(thing);
                        if (chosen != null)
                        {
                            StuffRef(defined) = chosen;
                            assigned.Add($"{def.defName}: {thing.defName} = {chosen.defName}");
                        }
                    }
                    else if (!thing.MadeFromStuff && stuff != null)
                    {
                        StuffRef(defined) = null;
                        cleared.Add($"{def.defName}: {thing.defName} (was {stuff.defName})");
                    }
                }
            }

            if (assigned.Count > 0)
            {
                PonyLog.Trace($"Scenarios: stuff set on {assigned.Count} starting items\n  " + string.Join("\n  ", assigned));
            }

            if (cleared.Count > 0)
            {
                PonyLog.Trace($"Scenarios: stuff removed from {cleared.Count} starting items\n  " + string.Join("\n  ", cleared));
            }
        }

        private static bool IsOurs(ModContentPack pack)
        {
            string id = pack?.PackageId;
            return id != null && id.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static ThingDef PickStuff(ThingDef thing)
        {
            if (thing.stuffCategories != null
                && thing.stuffCategories.Contains(StuffCategoryDefOf.Metallic)
                && ThingDefOf.Steel != null)
            {
                return ThingDefOf.Steel;
            }

            return GenStuff.DefaultStuffFor(thing);
        }
    }
}