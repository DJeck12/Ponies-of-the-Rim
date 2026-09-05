using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Food
{
    public static class PonyFoodCache
    {
        private static readonly Dictionary<PonyFoodGroupDef, HashSet<ThingDef>> GroupMembers = new Dictionary<PonyFoodGroupDef, HashSet<ThingDef>>();

        private static readonly Dictionary<ThingDef, HistoryEventDef> IngredientEvents = new Dictionary<ThingDef, HistoryEventDef>();

        private static readonly HashSet<ThingDef> EmptySet = new HashSet<ThingDef>();

        private static HashSet<ThingDef> fruits = EmptySet;

        public static bool AnyIngredientEvents;

        public static void Build()
        {
            GroupMembers.Clear();
            IngredientEvents.Clear();
            AnyIngredientEvents = false;
            fruits = EmptySet;
            List<PonyFoodGroupDef> groups = DefDatabase<PonyFoodGroupDef>.AllDefsListForReading;
            if (groups.Count == 0)
            {
                Log.Warning("[PoniesOfTheRim] PonyFoodCache: не найдено ни одной PonyFoodGroupDef.");
                return;
            }
            for (int i = 0; i < groups.Count; i++)
            {
                AddDirectMembers(groups[i]);
            }
            AddMembersFromModExtensions();
            for (int i = 0; i < groups.Count; i++)
            {
                RemoveExcluded(groups[i]);
            }
            ApplyFoodEvents();
            fruits = MembersOf(Pony_DefOf.Pony_FoodGroup_Fruits);
            Log.Message($"[PoniesOfTheRim] PonyFoodCache: групп {GroupMembers.Count}, фруктов и ягод {fruits.Count}.");
        }

        public static bool IsPonyFruit(this ThingDef def)
        {
            return def != null && fruits.Contains(def);
        }

        public static bool IsInFoodGroup(this ThingDef def, PonyFoodGroupDef group)
        {
            return def != null && group != null && MembersOf(group).Contains(def);
        }

        public static bool ContainsPonyFruit(this Thing thing)
        {
            if (thing == null)
            {
                return false;
            }
            if (fruits.Contains(thing.def))
            {
                return true;
            }
            List<ThingDef> ingredients = thing.TryGetComp<CompIngredients>()?.ingredients;
            if (ingredients == null)
            {
                return false;
            }
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (fruits.Contains(ingredients[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetIngredientEvent(ThingDef def, out HistoryEventDef ateEvent)
        {
            if (def == null)
            {
                ateEvent = null;
                return false;
            }
            return IngredientEvents.TryGetValue(def, out ateEvent);
        }

        private static HashSet<ThingDef> MembersOf(PonyFoodGroupDef group)
        {
            if (group == null)
            {
                return EmptySet;
            }
            HashSet<ThingDef> members;
            return GroupMembers.TryGetValue(group.Root, out members) ? members : EmptySet;
        }

        private static HashSet<ThingDef> MembersForWrite(PonyFoodGroupDef root)
        {
            HashSet<ThingDef> members;
            if (!GroupMembers.TryGetValue(root, out members))
            {
                members = new HashSet<ThingDef>();
                GroupMembers[root] = members;
            }
            return members;
        }

        private static void AddDirectMembers(PonyFoodGroupDef group)
        {
            HashSet<ThingDef> members = MembersForWrite(group.Root);
            List<ThingDef> thingDefs = group.thingDefs;
            if (thingDefs != null)
            {
                for (int i = 0; i < thingDefs.Count; i++)
                {
                    if (thingDefs[i] != null)
                    {
                        members.Add(thingDefs[i]);
                    }
                }
            }
            List<ThingCategoryDef> categories = group.thingCategories;
            if (categories == null)
            {
                return;
            }
            for (int i = 0; i < categories.Count; i++)
            {
                ThingCategoryDef category = categories[i];
                if (category == null)
                {
                    continue;
                }
                foreach (ThingDef descendant in category.DescendantThingDefs)
                {
                    members.Add(descendant);
                }
            }
        }

        private static void AddMembersFromModExtensions()
        {
            List<ThingDef> allThings = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allThings.Count; i++)
            {
                ThingDef thingDef = allThings[i];
                if (thingDef.modExtensions == null)
                {
                    continue;
                }
                List<PonyFoodGroupDef> groups = thingDef.GetModExtension<PonyFoodExtension>()?.foodGroups;
                if (groups == null)
                {
                    continue;
                }
                for (int j = 0; j < groups.Count; j++)
                {
                    if (groups[j] != null)
                    {
                        MembersForWrite(groups[j].Root).Add(thingDef);
                    }
                }
            }
        }

        private static void RemoveExcluded(PonyFoodGroupDef group)
        {
            List<ThingDef> excluded = group.excludedThingDefs;
            if (excluded == null)
            {
                return;
            }
            HashSet<ThingDef> members = MembersForWrite(group.Root);
            for (int i = 0; i < excluded.Count; i++)
            {
                if (excluded[i] != null)
                {
                    members.Remove(excluded[i]);
                }
            }
        }

        private static void ApplyFoodEvents()
        {
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }
            int applied = 0;
            int occupied = 0;
            foreach (KeyValuePair<PonyFoodGroupDef, HashSet<ThingDef>> pair in GroupMembers)
            {
                HistoryEventDef directEvent = pair.Key.ateEvent;
                HistoryEventDef ingredientEvent = pair.Key.ateAsIngredientEvent ?? pair.Key.ateEvent;
                if (directEvent == null && ingredientEvent == null)
                {
                    continue;
                }
                foreach (ThingDef member in pair.Value)
                {
                    IngestibleProperties ingestible = member.ingestible;
                    if (ingestible == null)
                    {
                        continue;
                    }
                    if (directEvent != null)
                    {
                        if (ingestible.ateEvent == null)
                        {
                            ingestible.ateEvent = directEvent;
                            applied++;
                        }
                        else if (ingestible.ateEvent != directEvent)
                        {
                            occupied++;
                        }
                    }
                    if (ingredientEvent != null && !IngredientEvents.ContainsKey(member))
                    {
                        IngredientEvents[member] = ingredientEvent;
                    }
                }
            }
            AnyIngredientEvents = IngredientEvents.Count > 0;
            if (occupied > 0)
            {
                Log.Message($"[PoniesOfTheRim] PonyFoodCache: ateEvent проставлен для {applied} дефов, пропущено {occupied} (поле занято другим модом).");
            }
        }
    }
}