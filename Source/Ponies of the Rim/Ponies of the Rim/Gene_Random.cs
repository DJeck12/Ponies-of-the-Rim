using Verse;

namespace PoniesOfTheRim
{
    public class Gene_Random : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.genes.AddGene(def.GetModExtension<RandomGeneExtension>().genes.RandomElement(), pawn.genes.IsXenogene(this));
            if (def.GetModExtension<RandomGeneExtension>().removeAfter == true)
            {
                pawn.genes.RemoveGene(this);
            }
        }
    }
}
