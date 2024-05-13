using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public class Gene_Diet : Gene
    {
        public override void PostMake()
        {
            base.PostMake();
            if (pawn.genes.HasGene(Pony_DefOf.Pony_Herbivore))
            {
                pawn.RaceProps.foodType = FoodTypeFlags.VegetarianAnimal;
            }
            if (pawn.genes.HasGene(Pony_DefOf.Pony_Carnivore))
            {
                pawn.RaceProps.foodType = FoodTypeFlags.CarnivoreAnimal;
            }
        }
    }
}
