using System;
using System.Collections.Generic;
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

                PonyLog.Trace($"Multiplayer: копии пешки '{pawn.LabelShortCap}' выдан трекер генов для стайлинг-станции.");
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Multiplayer: не удалось выдать гены копии пешки в стайлинг-станции.", ex);
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
                    PonyLog.Warn("Multiplayer: StylingDialog_DummyPawn.origPawn не найден — " +
                        "версия Multiplayer изменилась, стайлинг-станция может падать на аддонах с ConditionGene.");
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
                PonyLog.WarnCaught("Multiplayer: не удалось скопировать гены пешки для стайлинг-станции.", ex);
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
                "Multiplayer: HasGene вызван для пешки без трекера генов — условие ConditionGene " +
                "считается невыполненным. Копия пешки стайлинг-станции не получила гены; " +
                "проверьте совместимость с текущей версией Multiplayer.");
            return false;
        }
    }
}