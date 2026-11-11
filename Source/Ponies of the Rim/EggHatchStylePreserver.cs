using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class EggHatchStylePreserver
    {
        private sealed class SavedStyle
        {
            public HairDef hair;
            public BeardDef beard;
            public TattooDef faceTattoo;
        }

        private static readonly ConditionalWeakTable<Pawn, SavedStyle> _styles
            = new ConditionalWeakTable<Pawn, SavedStyle>();

        public static void Save(Pawn pawn)
        {
            if (pawn == null) return;

            var saved = new SavedStyle
            {
                hair = pawn.story?.hairDef,
                beard = pawn.style?.beardDef,
                faceTattoo = pawn.style?.FaceTattoo
            };
            _styles.Remove(pawn);
            _styles.Add(pawn, saved);
        }

        public static void TryRestore(Pawn pawn)
        {
            if (pawn == null || !_styles.TryGetValue(pawn, out SavedStyle saved))
                return;

            _styles.Remove(pawn);

            bool changed = false;
            if (pawn.story != null && saved.hair != null && pawn.story.hairDef != saved.hair)
            {
                pawn.story.hairDef = saved.hair;
                changed = true;
            }

            if (pawn.style != null)
            {
                if (saved.beard != null && pawn.style.beardDef != saved.beard)
                {
                    pawn.style.beardDef = saved.beard;
                    changed = true;
                }
                if (saved.faceTattoo != null && pawn.style.FaceTattoo != saved.faceTattoo)
                {
                    pawn.style.FaceTattoo = saved.faceTattoo;
                    changed = true;
                }
            }

            if (changed)
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}
