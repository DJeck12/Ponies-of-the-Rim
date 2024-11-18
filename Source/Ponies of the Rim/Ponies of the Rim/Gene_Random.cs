using Verse;

namespace PoniesOfTheRim
{
    public class Gene_Random : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.genes.AddGene(def.GetModExtension<GeneExtension>().genes.RandomElement(), pawn.genes.IsXenogene(this));
            if (def.GetModExtension<GeneExtension>().removeAfter == true)
            {
                pawn.genes.RemoveGene(this);
            }
        }
    }
}
