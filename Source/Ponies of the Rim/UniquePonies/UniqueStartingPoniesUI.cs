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

        private static readonly bool _isRandomPlusActive = ModLister.GetActiveModWithIdentifier("mastertea.RandomPlus") != null;

        private static readonly bool _isPersonalitiesActive = ModLister.GetActiveModWithIdentifier("hahkethomemah.simplepersonalities") != null;

        public const float ElemWidth = 93f;

        public const float ElemSpacing = 0f;

        public static Rect LastUniquePawnsRect;

        public static Rect LastCutiemarkRect;

        public static Rect LastTailRect;

        private static FieldInfo CurPawnIndexField => _curPawnIndexField ?? (_curPawnIndexField = AccessTools.Field(typeof(Page_ConfigureStartingPawns), "curPawnIndex"));

        private static float BtnHeight => Page.StandardSize.y - 744f;

        public static void DoWindowContents_Prefix(Page_ConfigureStartingPawns __instance, Rect rect)
        {
            if (CurPawnIndexField == null)
            {
                return;
            }
            int index = (int)CurPawnIndexField.GetValue(__instance);
            List<Pawn> pawns = Find.GameInitData?.startingAndOptionalPawns;
            Pawn pawn = (pawns != null && index >= 0 && index < pawns.Count) ? pawns[index] : null;
            bool usePonyLayout = pawn != null && (pawn.IsPony() || CutiemarkAddonResolver.HasVisibleCutiemark(pawn));
            RebuildLayout(rect, usePonyLayout);
        }

        private static void RebuildLayout(Rect pageRect, bool usePonyLayout)
        {
            float modOffsetX = 0f;
            if (_isRandomPlusActive && !_isPersonalitiesActive)
            {
                modOffsetX = -50f;
            }
            else if (_isPersonalitiesActive)
            {
                modOffsetX = 100f;
            }
            float btnHeight = BtnHeight;
            float cutiemarkSize = 80f;
            float x = pageRect.x + 657f + modOffsetX;
            float y = pageRect.yMax - Page.StandardSize.y + 85f;
            float cutiemarkX = x + (ElemWidth - cutiemarkSize) / 2f;
            if (!usePonyLayout)
            {
                y += 47f;
            }
            LastUniquePawnsRect = new Rect(x, y, ElemWidth, btnHeight);
            LastCutiemarkRect = new Rect(cutiemarkX, y + btnHeight + ElemSpacing, cutiemarkSize, cutiemarkSize);
            LastTailRect = new Rect(x, y + btnHeight + cutiemarkSize + ElemSpacing, ElemWidth, btnHeight);
        }

        public static void DoWindowContents_Postfix(Page_ConfigureStartingPawns __instance, Rect rect)
        {
            if (CurPawnIndexField == null || !Widgets.ButtonText(LastUniquePawnsRect, "Unique pawns"))
            {
                return;
            }
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (UniqueCharacterConfig character in UniquePawnConfig.Characters)
            {
                PawnKindDef named = DefDatabase<PawnKindDef>.GetNamed(character.KindDefName, errorOnFail: false);
                if (named != null)
                {
                    string label = (named.label.NullOrEmpty() ? named.defName : named.label.CapitalizeFirst());
                    PawnKindDef capturedKind = named;
                    options.Add(new FloatMenuOption(label, delegate
                    {
                        ReplaceSelectedWith(__instance, capturedKind);
                    }));
                }
            }
            if (options.Count == 0)
            {
                Messages.Message("No Unique PawnKindDef found.", MessageTypeDefOf.RejectInput, historical: false);
            }
            else
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void ReplaceSelectedWith(Page_ConfigureStartingPawns page, PawnKindDef kind)
        {
            if (CurPawnIndexField == null)
            {
                return;
            }
            int index = (int)CurPawnIndexField.GetValue(page);
            List<Pawn> pawns = Find.GameInitData?.startingAndOptionalPawns;
            if (pawns == null || index < 0 || index >= pawns.Count)
            {
                return;
            }
            Pawn oldPawn = pawns[index];
            PawnGenerationRequest generationRequest = StartingPawnUtility.GetGenerationRequest(index);
            generationRequest.PawnKindDefGetter = null;
            generationRequest.KindDef = kind;
            if (ModsConfig.BiotechActive)
            {
                generationRequest.ForcedCustomXenotype = null;
                generationRequest.AllowedXenotypes = null;
                generationRequest.ForceBaselinerChance = 0f;
                generationRequest.ForcedXenotype = ((kind.xenotypeSet != null) ? null : XenotypeDefOf.Baseliner);
            }
            generationRequest.Context = PawnGenerationContext.PlayerStarter;
            StartingPawnUtility.SetGenerationRequest(index, generationRequest);
            Pawn newPawn = PawnGenerator.GeneratePawn(generationRequest);
            newPawn.relations.everSeenByPlayer = true;
            PawnComponentsUtility.AddComponentsForSpawn(newPawn);
            StartingPawnUtility.GeneratePossessions(newPawn);
            pawns[index] = newPawn;
            if (oldPawn != null && !oldPawn.Destroyed)
            {
                try
                {
                    oldPawn.Discard(silentlyRemoveReferences: true);
                }
                catch (Exception ex)
                {
                    PonyLog.WarnCaught("Не удалось убрать старую пешку с экрана выбора персонажей.", ex);
                }
            }
            PortraitsCache.Clear();
            Messages.Message($"Replaced pawn at slot {index + 1} with {kind.LabelCap}.", MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }
}