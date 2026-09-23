using AlienRace;
using HarmonyLib;
using PoniesOfTheRim.Flying;
using PoniesOfTheRim.Food;
using PoniesOfTheRim.Genetics;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class POTR_Bootstrap
    {
        public static readonly Harmony Harmony =
            new Harmony("Rimworld.PoniesOfTheRim.Core");

        private static int _patched;
        private static int _failed;

        static POTR_Bootstrap()
        {
            PonyLog.InstallMainThreadPump();
            PonyLog.Trace("Bootstrap: запуск.");
            RunStage(RegisterCoreSetup, "CoreSetup");
            RunStage(RegisterCorePatches, "CorePatches");
            RunStage(RegisterBiotechPatches, "BiotechPatches");
            RunStage(RegisterIdeologyPatches, "IdeologyPatches");
            RunStage(RegisterCompatibilityPatches, "CompatibilityPatches");
            PonyLog.Trace($"Bootstrap: завершён, патчей установлено {_patched}, ошибок {_failed}.");
        }

        private static void RunStage(Action stage, string label)
        {
            try
            {
                stage();
            }
            catch (Exception arg)
            {
                _failed++;
                PonyLog.Error($"Bootstrap: этап '{label}' прерван исключением — часть патчей этапа не зарегистрирована:\n{arg}");
            }
        }

        private static void RegisterIdeologyPatches()
        {
            if (!ModsConfig.IdeologyActive)
            {
                PonyLog.Trace("Ideology не активен — соответствующие патчи пропущены.");
                return;
            }
            if (!Patch_FoodUtility_ThoughtsFromIngesting.IsReady)
            {
                return;
            }
            TryPatch(
                AccessTools.Method(typeof(FoodUtility), "ThoughtsFromIngesting"),
                postfix: new HarmonyMethod(typeof(Patch_FoodUtility_ThoughtsFromIngesting),
                                           nameof(Patch_FoodUtility_ThoughtsFromIngesting.Postfix)),
                label: "FoodUtility.ThoughtsFromIngesting (фрукты в составе блюда)"
            );
        }

        private static void RegisterCoreSetup()
        {
            PonyHelper.BuildRaceCache();
            PonyFlightCache.BuildStaticCaches();
            PonyFoodCache.Build();
            try
            {
                StatDefOf.GlobalLearningFactor.parts ??= new List<StatPart>();
                StatPart_HiveMind part = new StatPart_HiveMind
                {
                    parentStat = StatDefOf.GlobalLearningFactor
                };
                StatDefOf.GlobalLearningFactor.parts.Add(part);
                PonyLog.Trace("Bootstrap: ✓ StatPart_HiveMind.");
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Bootstrap: ошибка StatPart_HiveMind:\n{ex}");
            }
        }

        private static void RegisterCorePatches()
        {
            PonyMarkVariantSelector.Initialize();
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp),
                                   nameof(AlienPartGenerator.AlienComp.CompRenderNodes)),
                postfix: new HarmonyMethod(typeof(PonyMarkVariantSelector),
                                           nameof(PonyMarkVariantSelector.CompRenderNodes_Postfix)),
                label: "AlienComp.CompRenderNodes (mark variants)"
            );

            TryPatch(
                AccessTools.PropertyGetter(typeof(NameTriple), nameof(NameTriple.IsValid)),
                postfix: new HarmonyMethod(typeof(Patch_DialogNamePawn_SingleName),
                                           nameof(Patch_DialogNamePawn_SingleName.IsValid_Postfix)),
                label: "NameTriple.IsValid (одиночные имена)"
            );

            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp),
                                   "RegenerateAddonsForced", new Type[0]),
                postfix: new HarmonyMethod(typeof(PonyMarkVariantSelector),
                                           nameof(PonyMarkVariantSelector.RegenerateAddonsForced_Postfix)),
                label: "AlienComp.RegenerateAddonsForced (mark variants)"
            );
            TryPatch(
                AccessTools.Method(typeof(PawnFootprintMaker), "TryPlaceFootprint"),
                prefix: new HarmonyMethod(typeof(HoofprintPatch), nameof(HoofprintPatch.TryPlaceHoofprint)),
                label: "PawnFootprintMaker.TryPlaceFootprint"
            );

            TryPatch(
                AccessTools.Method(typeof(AlienRace.HarmonyPatches), "CheckBodyType"),
                postfix: new HarmonyMethod(typeof(PonyBodyTypePatch), nameof(PonyBodyTypePatch.BodyTypePatch)),
                label: "HarmonyPatches.CheckBodyType"
            );

            TryPatch(
                AccessTools.Method(typeof(Pawn_StoryTracker), "ExposeData"),
                postfix: new HarmonyMethod(typeof(ChildPonyBodyTypeFixPatch), nameof(ChildPonyBodyTypeFixPatch.ChildBodyTypeFixPatch)),
                label: "Pawn_StoryTracker.ExposeData"
            );

            TryPatch(
                AccessTools.Method(typeof(AlienPawnRenderNodeWorker_BodyAddon), "OffsetFor"),
                postfix: new HarmonyMethod(typeof(OffsetForPonyEarsPatch), nameof(OffsetForPonyEarsPatch.OffsetForPonyEars)),
                label: "AlienPawnRenderNodeWorker_BodyAddon.OffsetFor"
            );

            TryPatch(
                AccessTools.Method(typeof(StylingStation), "DoAddonList"),
                prefix: new HarmonyMethod(typeof(StylingStationPatch), nameof(StylingStationPatch.DoAddonList_PonyPatch)),
                label: "StylingStation.DoAddonList"
            );

            TryPatch(
                AccessTools.Method(typeof(StylingStation), "DoAddonInfo"),
                prefix: new HarmonyMethod(typeof(StylingStationPatch), nameof(StylingStationPatch.DoAddonInfo_PonyPatch)),
                label: "StylingStation.DoAddonInfo"
            );

            TryPatch(
                AccessTools.Method(typeof(WornGraphicData), "BeltScaleAt"),
                prefix: new HarmonyMethod(typeof(ScalableBeltPatch), nameof(ScalableBeltPatch.BeltScaleAtPatch)),
                label: "WornGraphicData.BeltScaleAt"
            );
            TryPatch(
                AccessTools.Method(typeof(WornGraphicData), "BeltOffsetAt"),
                prefix: new HarmonyMethod(typeof(ScalableBeltPatch), nameof(ScalableBeltPatch.BeltOffsetAtPatch)),
                label: "WornGraphicData.BeltOffsetAt"
            );

            TryPatch(
                AccessTools.Method(typeof(FactionDialogMaker), "FactionDialogFor"),
                postfix: new HarmonyMethod(typeof(CommsConsolePatch), nameof(CommsConsolePatch.CommsConsolePostfix)),
                label: "FactionDialogMaker.FactionDialogFor"
            );
            QuestFactionPatches.Apply(Harmony);
            Patch_QuestNode_GeneratePawn_RaceAware.Register(Harmony);

            TryPatch(
                AccessTools.Method(typeof(CharacterCardUtility), "DrawCharacterCard"),
                postfix: new HarmonyMethod(typeof(DrawCharacterCardPatch), nameof(DrawCharacterCardPatch.CutiemarkIcon)),
                label: "CharacterCardUtility.DrawCharacterCard"
            );
            TryPatch(
                AccessTools.Method(typeof(CharacterCardUtility), "DoTopStack"),
                transpiler: new HarmonyMethod(typeof(DrawCharacterCardPatch), nameof(DrawCharacterCardPatch.DoTopStackTranspiler)),
                label: "CharacterCardUtility.DoTopStack"
            );

            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "GetGraphic"),
                postfix: new HarmonyMethod(typeof(PonyHairExtensionPatch), nameof(PonyHairExtensionPatch.PonyTailPatch)),
                label: "BodyAddon.GetGraphic"
            );

            TryPatch(
                AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoExpansionIcons)),
                postfix: new HarmonyMethod(typeof(MainMenuDiscordIconPatch), nameof(MainMenuDiscordIconPatch.Postfix)),
                label: "MainMenuDrawer.DoExpansionIcons"
            );

            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp), nameof(AlienPartGenerator.AlienComp.CompRenderNodes)),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_CompRenderNodes),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_CompRenderNodes.Postfix)),
                label: "AlienComp.CompRenderNodes"
            );
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp), "RegenerateAddonsForced", new Type[0]),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_RegenerateAddonsForced),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_RegenerateAddonsForced.Postfix)),
                label: "AlienComp.RegenerateAddonsForced"
            );
            TryPatch(
                AccessTools.Method(typeof(PawnRenderNode_Hair), nameof(PawnRenderNode_Hair.GraphicFor)),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_PawnRenderNodeHair_GraphicForPawn),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_PawnRenderNodeHair_GraphicForPawn.Postfix)),
                label: "PawnRenderNode_Hair.GraphicFor (crystal)"
            );
            TryPatch(
                AccessTools.Method(typeof(PawnRenderNode_Hair), nameof(PawnRenderNode_Hair.GraphicFor)),
                postfix: new HarmonyMethod(typeof(PonyBabyHairPatch), nameof(PonyBabyHairPatch.PonyBabyHairPostfix)),
                label: "PawnRenderNode_Hair.GraphicFor (baby hair)"
            );
            TryPatch(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(PonyBabyHairPatch), nameof(PonyBabyHairPatch.GeneratePawn_Postfix)),
                label: "PawnGenerator.GeneratePawn (baby hair)"
            );
            TryPatch(
                AccessTools.Method(typeof(Page_ConfigureStartingPawns), "PreOpen"),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_PreOpen),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_PreOpen.Postfix)),
                label: "Page_ConfigureStartingPawns.PreOpen"
            );
            TryPatch(
                AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoWindowContents"),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_DoWindowContents),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_DoWindowContents.Postfix)),
                label: "Page_ConfigureStartingPawns.DoWindowContents (crystal)"
            );
        }

        private static void RegisterBiotechPatches()
        {
            if (!ModsConfig.BiotechActive)
            {
                PonyLog.Trace("Biotech не активен — соответствующие патчи пропущены.");
                return;
            }

            TryPatch(
                AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome"),
                prefix: new HarmonyMethod(typeof(Patch_PregnancyUtility_ApplyBirthOutcome), nameof(Patch_PregnancyUtility_ApplyBirthOutcome.Prefix)),
                label: "PregnancyUtility.ApplyBirthOutcome (egg birth)"
            );

            PonyRacialGeneUtility.BuildCache();
            TryPatch(
                AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.GetInheritedGenes),
                    new[] { typeof(Pawn), typeof(Pawn), typeof(bool).MakeByRefType() }),
                postfix: new HarmonyMethod(typeof(Patch_PregnancyUtility_RacialGenes),
                                           nameof(Patch_PregnancyUtility_RacialGenes.Postfix)),
                label: "PregnancyUtility.GetInheritedGenes (расовый ген ребёнка)"
            );

            TryPatch(
                AccessTools.Method(typeof(LifeStageWorker_HumanlikeChild), "Notify_LifeStageStarted"),
                postfix: new HarmonyMethod(
                    typeof(Patch_LifeStageWorker_HumanlikeChild_Notify),
                    nameof(Patch_LifeStageWorker_HumanlikeChild_Notify.Postfix)),
                label: "LifeStageWorker_HumanlikeChild.Notify_LifeStageStarted"
            );

            TryPatch(
                AccessTools.Method(typeof(Thing), "Ingested", new[] { typeof(Pawn), typeof(float) }),
                postfix: new HarmonyMethod(typeof(PonyFoodGenesPatch), nameof(PonyFoodGenesPatch.IngestedPonyFoodGenesPatch)),
                label: "Thing.Ingested"
            );

            TryPatch(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(PonyFoodGeneRemovalPatch), nameof(PonyFoodGeneRemovalPatch.PonyFoodGeneRemovalForGeneratePawn)),
                label: "PawnGenerator.GeneratePawn"
            );
        }

        private static void RegisterCompatibilityPatches()
        {
            FurCompatibilityPatch.EnsurePonyFurPaths();
        }

        private static void TryPatch(
            MethodInfo original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            string label = "")
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"Bootstrap: метод не найден — '{label}'.");
                return;
            }
            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler);
                _patched++;
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Bootstrap: ошибка патча '{label}':\n{ex}");
            }
        }
    }
}