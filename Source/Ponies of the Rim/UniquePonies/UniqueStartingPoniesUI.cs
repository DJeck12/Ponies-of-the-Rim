using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public static class UniqueStartingPawnUI
    {
        private static FieldInfo _curPawnIndexField;
        private static FieldInfo CurPawnIndexField =>
            _curPawnIndexField ??= AccessTools.Field(typeof(Page_ConfigureStartingPawns), "curPawnIndex");

        public static void DoWindowContents_Postfix(Page_ConfigureStartingPawns __instance, Rect rect)
        {
            if (CurPawnIndexField == null) return;

            int index = (int)CurPawnIndexField.GetValue(__instance);
            var pawns = Find.GameInitData?.startingAndOptionalPawns;
            if (pawns == null) return;

            bool isPony = index >= 0 && index < pawns.Count
                          && pawns[index] != null
                          && pawns[index].IsPony();

                        var btn = new Rect(
                rect.x + 657f,
                rect.yMax - Page.StandardSize.y + 134f,
                93f,
                Page.StandardSize.y - 744f
            );

            if (!isPony)
                btn.y += 47f;

                        bool isRandomPlusActive = ModLister.GetActiveModWithIdentifier("mastertea.RandomPlus") != null;
            bool isPersonalitiesActive = ModLister.GetActiveModWithIdentifier("hahkethomemah.simplepersonalities") != null;

            if (isRandomPlusActive && !isPersonalitiesActive)
                btn.x -= 50f;
            if (isPersonalitiesActive)
                btn.x += 100f;

            if (!Widgets.ButtonText(btn, "Unique pawns"))
                return;

            var options = new List<FloatMenuOption>();
            foreach (var config in UniquePawnConfig.Characters)
            {
                var kind = DefDatabase<PawnKindDef>.GetNamed(config.KindDefName, errorOnFail: false);
                if (kind == null) continue;

                var label = !kind.label.NullOrEmpty()
                    ? kind.label.CapitalizeFirst()
                    : kind.defName;

                var capturedKind = kind;
                options.Add(new FloatMenuOption(label, () => ReplaceSelectedWith(__instance, capturedKind)));
            }

            if (options.Count == 0)
                Messages.Message("No Unique PawnKindDef found.", MessageTypeDefOf.RejectInput, false);
            else
                Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void ReplaceSelectedWith(Page_ConfigureStartingPawns page, PawnKindDef kind)
        {
            if (CurPawnIndexField == null) return;

            int index = (int)CurPawnIndexField.GetValue(page);
            var list = Find.GameInitData?.startingAndOptionalPawns;
            if (list == null || index < 0 || index >= list.Count) return;

            var oldPawn = list[index];

            var req = StartingPawnUtility.GetGenerationRequest(index);
            req.PawnKindDefGetter = null;
            req.KindDef = kind;

            if (ModsConfig.BiotechActive)
            {
                req.ForcedCustomXenotype = null;
                req.AllowedXenotypes = null;
                req.ForceBaselinerChance = 0f;
                req.ForcedXenotype = kind.xenotypeSet != null ? null : XenotypeDefOf.Baseliner;
            }

            req.Context = PawnGenerationContext.PlayerStarter;
            StartingPawnUtility.SetGenerationRequest(index, req);

            var newPawn = PawnGenerator.GeneratePawn(req);
            newPawn.relations.everSeenByPlayer = true;
            PawnComponentsUtility.AddComponentsForSpawn(newPawn);
            StartingPawnUtility.GeneratePossessions(newPawn);

            list[index] = newPawn;

                        if (oldPawn != null && !oldPawn.Destroyed)
            {
                try
                {
                    oldPawn.Discard(silentlyRemoveReferences: true);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[PoniesOfTheRim] Failed to discard old pawn: {ex.Message}");
                }
            }

            PortraitsCache.Clear();
            Messages.Message(
                $"Replaced pawn at slot {index + 1} with {kind.LabelCap}.",
                MessageTypeDefOf.NeutralEvent,
                false
            );
        }
    }
}