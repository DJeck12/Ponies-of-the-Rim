using System;
using System.Reflection;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Multiplayer
{
    public static class Patch_StylingDialogDummyPawn_Genes
    {
        private static bool _resolved;
        private static FieldInfo _origPawnField;

        public static void Prefix(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.genes != null)
                    return;

                Pawn orig = GetOriginalPawn(pawn);
                if (orig?.genes == null)
                    return;

                Pawn_GeneTracker clone = CloneGeneTracker(orig.genes, pawn);
                if (clone == null)
                    return;

                pawn.genes = clone;

                PonyLog.Trace($"MP: styling dummy of {pawn.LabelShortCap} got a copy of the gene tracker");
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("MP: could not give genes to the styling dummy", ex);
            }
        }

        private static Pawn GetOriginalPawn(Pawn dummy)
        {
            if (!_resolved)
            {
                _resolved = true;
                Type dummyType = AccessTools.TypeByName("Multiplayer.Client.Patches.StylingDialog_DummyPawn");
                _origPawnField = dummyType == null ? null : AccessTools.Field(dummyType, "origPawn");
                if (_origPawnField == null)
                    PonyLog.Warn("MP: StylingDialog_DummyPawn.origPawn not found, styling station may fail on ConditionGene addons");
            }
            if (_origPawnField == null)
                return null;
            if (!_origPawnField.DeclaringType.IsInstanceOfType(dummy))
                return null;
            return _origPawnField.GetValue(dummy) as Pawn;
        }

        private static Pawn_GeneTracker CloneGeneTracker(Pawn_GeneTracker source, Pawn owner)
        {
            try
            {
                Pawn_GeneTracker clone = new Pawn_GeneTracker(owner);
                foreach (FieldInfo field in AccessTools.GetDeclaredFields(typeof(Pawn_GeneTracker)))
                {
                    if (field.IsStatic)
                        continue;
                    field.SetValue(clone, field.GetValue(source));
                }
                FieldInfo pawnField = AccessTools.Field(typeof(Pawn_GeneTracker), "pawn");
                pawnField?.SetValue(clone, owner);
                return clone;
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("MP: gene tracker copy failed", ex);
                return null;
            }
        }
    }

    public static class Patch_ExtendedGraphicsPawnWrapper_HasGene
    {
        public static bool Prefix(ExtendedGraphicsPawnWrapper __instance, ref bool __result)
        {
            if (__instance?.WrappedPawn?.genes != null)
                return true;

            __result = false;

            PonyLog.WarnOnce("Patch_ExtendedGraphicsPawnWrapper_HasGene.NoGenes",
                "MP: HasGene on a pawn without genes, ConditionGene treated as false");
            return false;
        }
    }
}