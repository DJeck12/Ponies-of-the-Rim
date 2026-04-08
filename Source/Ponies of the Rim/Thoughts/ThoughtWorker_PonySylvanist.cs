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

        private SylvanistExtension Extension
        {
            get
            {
                if (!extensionCached)
                {
                    cachedExtension = def.GetModExtension<SylvanistExtension>();

                    if (cachedExtension == null)
                    {
                        Log.ErrorOnce(
                            "[PoniesOfTheRim] ThoughtWorker_Pony_Sylvanist: SylvanistExtension не найден на ThoughtDef '"
                            + def.defName + "'. Добавьте <modExtensions><li Class=\"PoniesOfTheRim.SylvanistExtension\"> в XML.",
                            def.shortHash);
                    }

                    extensionCached = true;
                }
                return cachedExtension;
            }
        }

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.story?.traits == null || !p.story.traits.HasTrait(TraitDef.Named("Pony_Sylvanist")))
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