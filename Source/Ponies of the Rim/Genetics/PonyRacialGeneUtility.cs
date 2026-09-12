using System;
using System.Collections.Generic;
using AlienRace;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Genetics
{
    public static class PonyRacialGeneUtility
    {
        private static PonyRacialGeneGroupDef[] _groups = new PonyRacialGeneGroupDef[0];

        public static void BuildCache()
        {
            if (!ModsConfig.BiotechActive)
            {
                _groups = new PonyRacialGeneGroupDef[0];
                return;
            }
            List<PonyRacialGeneGroupDef> list = new List<PonyRacialGeneGroupDef>();
            List<PonyRacialGeneGroupDef> all = DefDatabase<PonyRacialGeneGroupDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                PonyRacialGeneGroupDef def = all[i];
                if (def.racialGenes != null && def.racialGenes.Count > 0)
                {
                    list.Add(def);
                }
            }
            _groups = list.ToArray();
            Log.Message("[PoniesOfTheRim] Genetics: групп расовых генов загружено — " + _groups.Length + ".");
        }

        public static void Normalize(List<GeneDef> genes, Pawn mother, Pawn father)
        {
            if (genes == null || _groups.Length == 0)
            {
                return;
            }
            ThingDef childRace = ((mother != null) ? mother.def : null) ?? ((father != null) ? father.def : null);
            for (int i = 0; i < _groups.Length; i++)
            {
                PonyRacialGeneGroupDef group = _groups[i];
                if (!group.enforceOnInheritance || !group.AppliesToRace(childRace))
                {
                    continue;
                }
                try
                {
                    NormalizeGroup(group, genes, childRace, mother, father);
                }
                catch (Exception e)
                {
                    Log.Error("[PoniesOfTheRim] Genetics: сбой нормализации группы '" + group.defName + "':\n" + e);
                }
            }
        }

        private static void NormalizeGroup(PonyRacialGeneGroupDef group, List<GeneDef> genes, ThingDef childRace, Pawn mother, Pawn father)
        {
            int totalRacial = 0;
            List<GeneDef> distinct = null;
            bool hasMarker = false;
            bool hasChooser = false;

            for (int i = 0; i < genes.Count; i++)
            {
                GeneDef g = genes[i];
                if (group.IsRacialGene(g))
                {
                    totalRacial++;
                    if (distinct == null)
                    {
                        distinct = new List<GeneDef>(3);
                    }
                    if (!distinct.Contains(g))
                    {
                        distinct.Add(g);
                    }
                }
                else if (group.chooserGene != null && g == group.chooserGene)
                {
                    hasChooser = true;
                }
                else if (!hasMarker && group.IsMarkerGene(g))
                {
                    hasMarker = true;
                }
            }

            if (totalRacial == 0)
            {
                if (hasChooser || !hasMarker)
                {
                    return;
                }
                GeneDef pick = PickForChild(group, childRace, mother, father);
                if (pick == null)
                {
                    return;
                }
                genes.Add(pick);
                if (Prefs.DevMode)
                {
                    Log.Message("[PoniesOfTheRim] Genetics: ребёнку добавлен расовый ген '" + pick.defName + "' (группа '" + group.defName + "').");
                }
                return;
            }

            if (hasChooser)
            {
                RemoveAllOf(genes, group.chooserGene);
            }

            int maxAllowed = 1;
            if (group.inheritMultipleFromParent)
            {
                int m = CountInheritableRacialGenes(group, mother);
                int f = CountInheritableRacialGenes(group, father);
                int parentMax = ((m > f) ? m : f);
                if (parentMax > maxAllowed)
                {
                    maxAllowed = parentMax;
                }
            }

            if (distinct.Count <= maxAllowed && totalRacial == distinct.Count)
            {
                return;
            }

            EnforceLimit(group, genes, distinct, maxAllowed);
            if (Prefs.DevMode)
            {
                Log.Message("[PoniesOfTheRim] Genetics: расовых генов у ребёнка было " + totalRacial + ", лимит " + maxAllowed + " (группа '" + group.defName + "').");
            }
        }

        private static void EnforceLimit(PonyRacialGeneGroupDef group, List<GeneDef> genes, List<GeneDef> distinct, int maxAllowed)
        {
            List<GeneDef> pool = new List<GeneDef>(distinct);
            List<GeneDef> keep = new List<GeneDef>(maxAllowed);
            while (keep.Count < maxAllowed && pool.Count > 0)
            {
                GeneDef picked = pool.RandomElement();
                pool.Remove(picked);
                keep.Add(picked);
            }

            List<GeneDef> alreadyKept = new List<GeneDef>(keep.Count);
            for (int i = genes.Count - 1; i >= 0; i--)
            {
                GeneDef g = genes[i];
                if (!group.IsRacialGene(g))
                {
                    continue;
                }
                if (keep.Contains(g) && !alreadyKept.Contains(g))
                {
                    alreadyKept.Add(g);
                    continue;
                }
                genes.RemoveAt(i);
            }
        }

        private static GeneDef PickForChild(PonyRacialGeneGroupDef group, ThingDef childRace, Pawn mother, Pawn father)
        {
            List<GeneDef> candidates = new List<GeneDef>(4);
            CollectFromParent(group, mother, childRace, candidates);
            CollectFromParent(group, father, childRace, candidates);
            if (candidates.Count > 0)
            {
                return candidates.RandomElement();
            }
            return group.RandomGeneByWeight((GeneDef g) => CanHaveGene(g, childRace));
        }

        private static void CollectFromParent(PonyRacialGeneGroupDef group, Pawn parent, ThingDef childRace, List<GeneDef> outList)
        {
            if (parent == null || parent.genes == null)
            {
                return;
            }
            List<Gene> list = parent.genes.GenesListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                Gene gene = list[i];
                if (gene == null || !gene.Active || parent.genes.IsXenogene(gene))
                {
                    continue;
                }
                if (group.IsRacialGene(gene.def) && CanHaveGene(gene.def, childRace))
                {
                    outList.Add(gene.def);
                }
            }
        }

        private static int CountInheritableRacialGenes(PonyRacialGeneGroupDef group, Pawn parent)
        {
            if (parent == null || parent.genes == null)
            {
                return 0;
            }
            List<GeneDef> seen = new List<GeneDef>(3);
            List<Gene> list = parent.genes.GenesListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                Gene gene = list[i];
                if (gene == null || !gene.Active || parent.genes.IsXenogene(gene))
                {
                    continue;
                }
                if (group.IsRacialGene(gene.def) && !seen.Contains(gene.def))
                {
                    seen.Add(gene.def);
                }
            }
            return seen.Count;
        }

        private static bool CanHaveGene(GeneDef gene, ThingDef race)
        {
            if (gene == null)
            {
                return false;
            }
            if (race == null)
            {
                return true;
            }
            try
            {
                return RaceRestrictionSettings.CanHaveGene(gene, race, xeno: false);
            }
            catch
            {
                return true;
            }
        }

        private static void RemoveAllOf(List<GeneDef> genes, GeneDef gene)
        {
            for (int i = genes.Count - 1; i >= 0; i--)
            {
                if (genes[i] == gene)
                {
                    genes.RemoveAt(i);
                }
            }
        }
    }
}