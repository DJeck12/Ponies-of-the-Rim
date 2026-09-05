using System.Collections.Generic;
using AlienRace;
using Verse;

namespace PoniesOfTheRim
{
    public class Gene_Random : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            if (pawn == null || pawn.genes == null)
            {
                return;
            }
            GeneExtension extension = def.GetModExtension<GeneExtension>();
            if (extension == null || extension.genes == null || extension.genes.Count == 0)
            {
                Log.Error("[PoniesOfTheRim] Gene_Random: у гена '" + def.defName + "' нет GeneExtension со списком genes — случайный выбор невозможен.");
                return;
            }

            bool xenogene = pawn.genes.IsXenogene(this);
            GeneDef pick = PickAllowed(extension, xenogene);
            if (pick != null)
            {
                pawn.genes.AddGene(pick, xenogene);
            }
            else
            {
                Log.Warning("[PoniesOfTheRim] Gene_Random: для расы '" + pawn.def.defName + "' ни один вариант гена '" + def.defName + "' не разрешён — ничего не добавлено.");
            }
            RemoveSelfIfNeeded(extension);
        }

        private GeneDef PickAllowed(GeneExtension extension, bool xenogene)
        {
            List<GeneDef> allowed = new List<GeneDef>(extension.genes.Count);
            for (int i = 0; i < extension.genes.Count; i++)
            {
                GeneDef candidate = extension.genes[i];
                if (candidate == null)
                {
                    continue;
                }
                bool canHave;
                try
                {
                    canHave = RaceRestrictionSettings.CanHaveGene(candidate, pawn.def, xenogene);
                }
                catch
                {
                    canHave = true;
                }
                if (canHave)
                {
                    allowed.Add(candidate);
                }
            }
            if (allowed.Count == 0)
            {
                return null;
            }
            return allowed.RandomElement();
        }

        private void RemoveSelfIfNeeded(GeneExtension extension)
        {
            if (extension.removeAfter && pawn.genes.GetGene(def) == this)
            {
                pawn.genes.RemoveGene(this);
            }
        }
    }
}