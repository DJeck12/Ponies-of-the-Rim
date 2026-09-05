using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class ScenarioStuffReconciler
    {
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
            catch (Exception arg)
            {
                Log.Warning($"[PoniesOfTheRim] ScenPart_StartingThing_Defined.{field} не найдено:\n{arg}");
                return null;
            }
        }

        private static void Reconcile()
        {
            if (ThingDefRef == null || StuffRef == null)
            {
                return;
            }

            ModContentPack owner = OwnModContentPack();
            if (owner == null)
            {
                Log.Warning("[PoniesOfTheRim] Не удалось определить собственный мод — сценарии не проверены.");
                return;
            }

            List<string> assigned = new List<string>();
            List<string> cleared = new List<string>();

            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefsListForReading)
            {
                if (def.modContentPack != owner || def.scenario == null)
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
                            assigned.Add(def.defName + " → " + thing.defName + " = " + chosen.defName);
                        }
                    }
                    else if (!thing.MadeFromStuff && stuff != null)
                    {
                        StuffRef(defined) = null;
                        cleared.Add(def.defName + " → " + thing.defName + " (был " + stuff.defName + ")");
                    }
                }
            }

            if (assigned.Count > 0)
            {
                Log.Message("[PoniesOfTheRim] Сценарии: материал назначен для " + assigned.Count + " предмет(ов):\n  " +
                            string.Join("\n  ", assigned.ToArray()));
            }

            if (cleared.Count > 0)
            {
                Log.Message("[PoniesOfTheRim] Сценарии: материал снят с " + cleared.Count + " предмет(ов):\n  " +
                            string.Join("\n  ", cleared.ToArray()));
            }
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

        private static ModContentPack OwnModContentPack()
        {
            Assembly self = typeof(ScenarioStuffReconciler).Assembly;

            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            for (int i = 0; i < mods.Count; i++)
            {
                List<Assembly> loaded = mods[i].assemblies?.loadedAssemblies;
                if (loaded != null && loaded.Contains(self))
                {
                    return mods[i];
                }
            }

            return null;
        }
    }
}