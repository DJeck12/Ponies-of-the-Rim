using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Thoughts
{
    public class SylvanistExtension : DefModExtension
    {
        public List<BiomeDef> forestBiomes = new List<BiomeDef>();
    }

    public class ThoughtWorker_Pony_Sylvanist : ThoughtWorker
    {
        private SylvanistExtension cachedExtension;
        private bool extensionCached = false;
        private static TraitDef _sylvanistTrait;
        private static bool _sylvanistTraitResolved;

        private static TraitDef SylvanistTrait
        {
            get
            {
                if (!_sylvanistTraitResolved)
                {
                    _sylvanistTraitResolved = true;
                    _sylvanistTrait = DefDatabase<TraitDef>.GetNamed("Pony_Sylvanist", errorOnFail: false);

                    if (_sylvanistTrait == null)
                    {
                        PonyLog.ErrorOnce("ThoughtWorker_Pony_Sylvanist.NoTrait",
                            "Мысли: TraitDef 'Pony_Sylvanist' не найден — мысль сильваниста не будет работать.");
                    }
                }
                return _sylvanistTrait;
            }
        }

        private SylvanistExtension Extension
        {
            get
            {
                if (!extensionCached)
                {
                    cachedExtension = def.GetModExtension<SylvanistExtension>();

                    if (cachedExtension == null)
                    {
                        PonyLog.ErrorOnce("ThoughtWorker_Pony_Sylvanist|" + def.defName,
                            "Мысли: у ThoughtDef '" + def.defName + "' нет SylvanistExtension — мысль не будет работать. " +
                            "Добавьте <modExtensions><li Class=\"PoniesOfTheRim.Thoughts.SylvanistExtension\"> в XML.");
                    }

                    extensionCached = true;
                }
                return cachedExtension;
            }
        }

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            TraitDef trait = SylvanistTrait;
            if (trait == null || p.story?.traits == null || !p.story.traits.HasTrait(trait))
                return ThoughtState.Inactive;

            if (p.Map == null)
                return ThoughtState.Inactive;

            SylvanistExtension ext = Extension;
            if (ext == null)
                return ThoughtState.Inactive;

            BiomeDef currentBiome = p.Map.Biome;

            foreach (BiomeDef forestBiome in ext.forestBiomes)
            {
                if (forestBiome != null && forestBiome == currentBiome)
                    return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.ActiveAtStage(1);
        }
    }
}