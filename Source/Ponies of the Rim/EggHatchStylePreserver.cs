using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class EggHatchStylePreserver
    {
        private struct SavedStyle
        {
            public HairDef   hair;
            public BeardDef  beard;
            public TattooDef faceTattoo;
        }

        private static readonly Dictionary<Pawn, SavedStyle> _styles
            = new Dictionary<Pawn, SavedStyle>();

        public static void Save(Pawn pawn)
        {
            if (pawn?.story == null) return;

            _styles[pawn] = new SavedStyle
            {
                hair       = pawn.story.hairDef,
                beard      = pawn.style?.beardDef,
                faceTattoo = pawn.style?.FaceTattoo
            };
        }

        public static void TryRestore(Pawn pawn)
        {
            if (!_styles.TryGetValue(pawn, out SavedStyle saved))
                return;

            _styles.Remove(pawn);

            if (pawn.story != null && saved.hair != null)
                pawn.story.hairDef = saved.hair;

            if (pawn.style != null)
            {
                if (saved.beard != null)
                    pawn.style.beardDef = saved.beard;
                if (saved.faceTattoo != null)
                    pawn.style.FaceTattoo = saved.faceTattoo;
            }

            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}