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

        private static readonly bool _isRandomPlusActive =
            ModLister.GetActiveModWithIdentifier("mastertea.RandomPlus") != null;
        private static readonly bool _isPersonalitiesActive =
            ModLister.GetActiveModWithIdentifier("hahkethomemah.simplepersonalities") != null;

        public const  float ElemWidth   = 93f;
        public const  float ElemSpacing = 0f;
        private static float BtnHeight => Page.StandardSize.y - 744f;    

        public static Rect LastUniquePawnsRect;
        public static Rect LastCutiemarkRect;
        public static Rect LastTailRect;

        public static void DoWindowContents_Prefix(Page_ConfigureStartingPawns __instance, Rect rect)
        {
            if (CurPawnIndexField == null) return;

            int index = (int)CurPawnIndexField.GetValue(__instance);
            var pawns = Find.GameInitData?.startingAndOptionalPawns;
            bool isPony = pawns != null
                          && index >= 0 && index < pawns.Count
                          && pawns[index] != null
                          && pawns[index].IsPony();

            RebuildLayout(rect, isPony);
        }

        private static void RebuildLayout(Rect pageRect, bool isPony)
        {
            float modX = 0f;
            if (_isRandomPlusActive && !_isPersonalitiesActive) modX = -50f;
            else if (_isPersonalitiesActive)                    modX =  100f;

            float h  = BtnHeight;                   
            float iw = ElemWidth - 13f;                    
            float x  = pageRect.x + 657f + modX;
            float y  = pageRect.yMax - Page.StandardSize.y + 85;
            float cutieX = x + (ElemWidth - iw) / 2f;

            if (!isPony) y += 47f;

            LastUniquePawnsRect = new Rect(x, y, ElemWidth, h);
            LastCutiemarkRect = new Rect(cutieX, y + h + ElemSpacing, iw, iw);
            LastTailRect        = new Rect(x, y + h + iw + ElemSpacing * 2f, ElemWidth, h);
        }

        public static void DoWindowContents_Postfix(Page_ConfigureStartingPawns __instance, Rect rect)
        {
            if (CurPawnIndexField == null) return;

            if (!Widgets.ButtonText(LastUniquePawnsRect, "Unique pawns"))
                return;

            var options = new List<FloatMenuOption>();
            foreach (var config in UniquePawnConfig.Characters)
            {
                var kind = DefDatabase<PawnKindDef>.GetNamed(config.KindDefName, errorOnFail: false);
                if (kind == null) continue;

                string label = kind.label.NullOrEmpty()
                    ? kind.defName
                    : kind.label.CapitalizeFirst();

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
                req.ForcedCustomXenotype  = null;
                req.AllowedXenotypes      = null;
                req.ForceBaselinerChance  = 0f;
                req.ForcedXenotype        = kind.xenotypeSet != null ? null : XenotypeDefOf.Baseliner;
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
                try   { oldPawn.Discard(silentlyRemoveReferences: true); }
                catch (Exception ex)
                { Log.Warning($"[PoniesOfTheRim] Failed to discard old pawn: {ex.Message}"); }
            }

            PortraitsCache.Clear();
            Messages.Message(
                $"Replaced pawn at slot {index + 1} with {kind.LabelCap}.",
                MessageTypeDefOf.NeutralEvent, false);
        }
    }
}