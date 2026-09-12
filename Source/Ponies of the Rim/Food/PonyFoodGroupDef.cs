using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Food
{
    public class PonyFoodGroupDef : Def
    {
        public PonyFoodGroupDef extends;

        public List<ThingDef> thingDefs;

        public List<ThingCategoryDef> thingCategories;

        public List<ThingDef> excludedThingDefs;

        public HistoryEventDef ateEvent;

        public HistoryEventDef ateAsIngredientEvent;

        public PonyFoodGroupDef Root
        {
            get
            {
                PonyFoodGroupDef current = this;
                for (int i = 0; i < 16 && current.extends != null; i++)
                {
                    current = current.extends;
                }
                return current;
            }
        }
    }
}