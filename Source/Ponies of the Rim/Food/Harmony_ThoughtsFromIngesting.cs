using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Food
{
    public static class Patch_FoodUtility_ThoughtsFromIngesting
    {
        private delegate void AddThoughtsFromIdeoDelegate(HistoryEventDef eventDef, Pawn ingester, ThingDef foodDef, MeatSourceCategory meatSourceCategory);

        private static readonly AddThoughtsFromIdeoDelegate AddThoughtsFromIdeo;

        private static readonly AccessTools.FieldRef<List<FoodUtility.ThoughtFromIngesting>> VanillaBuffer;

        private static readonly List<HistoryEventDef> FiredEvents = new List<HistoryEventDef>();

        static Patch_FoodUtility_ThoughtsFromIngesting()
        {
            try
            {
                AddThoughtsFromIdeo = AccessTools.MethodDelegate<AddThoughtsFromIdeoDelegate>(AccessTools.Method(typeof(FoodUtility), "AddThoughtsFromIdeo"));
                VanillaBuffer = AccessTools.StaticFieldRefAccess<List<FoodUtility.ThoughtFromIngesting>>(AccessTools.Field(typeof(FoodUtility), "ingestThoughts"));
            }
            catch (Exception arg)
            {
                AddThoughtsFromIdeo = null;
                VanillaBuffer = null;
                Log.Error($"[PoniesOfTheRim] ThoughtsFromIngesting: нет доступа к FoodUtility.AddThoughtsFromIdeo:\n{arg}");
            }
        }

        public static bool IsReady
        {
            get
            {
                return AddThoughtsFromIdeo != null && VanillaBuffer != null;
            }
        }

        public static void Postfix(Pawn ingester, Thing foodSource, ThingDef foodDef, List<FoodUtility.ThoughtFromIngesting> __result)
        {
            if (!PonyFoodCache.AnyIngredientEvents || __result == null || foodSource == null || ingester?.Ideo == null)
            {
                return;
            }
            List<ThingDef> ingredients = foodSource.TryGetComp<CompIngredients>()?.ingredients;
            if (ingredients == null || ingredients.Count == 0)
            {
                return;
            }
            HistoryEventDef directEvent = foodDef?.ingestible?.ateEvent;
            FiredEvents.Clear();
            for (int i = 0; i < ingredients.Count; i++)
            {
                ThingDef ingredient = ingredients[i];
                HistoryEventDef ingredientEvent;
                if (ingredient == null || !PonyFoodCache.TryGetIngredientEvent(ingredient, out ingredientEvent))
                {
                    continue;
                }
                if (ingredientEvent == directEvent || FiredEvents.Contains(ingredientEvent))
                {
                    continue;
                }
                FiredEvents.Add(ingredientEvent);
                AddIdeoThoughts(ingredientEvent, ingester, ingredient, __result);
            }
            FiredEvents.Clear();
        }

        private static void AddIdeoThoughts(HistoryEventDef eventDef, Pawn ingester, ThingDef ingredient, List<FoodUtility.ThoughtFromIngesting> result)
        {
            List<FoodUtility.ThoughtFromIngesting> buffer = VanillaBuffer();
            int countBefore = buffer.Count;
            AddThoughtsFromIdeo(eventDef, ingester, ingredient, FoodUtility.GetMeatSourceCategory(ingredient));
            if (ReferenceEquals(buffer, result))
            {
                return;
            }
            for (int i = countBefore; i < buffer.Count; i++)
            {
                result.Add(buffer[i]);
            }
        }
    }
}