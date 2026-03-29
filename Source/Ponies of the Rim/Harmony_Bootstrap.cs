using AlienRace;
using HarmonyLib;
using PoniesOfTheRim.UniquePonies;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class POTR_Bootstrap
    {
        public static readonly Harmony Harmony =
            new Harmony("Rimworld.PoniesOfTheRim.Core");

        static POTR_Bootstrap()
        {
            Log.Message("[PoniesOfTheRim] Bootstrap: запуск.");
            RegisterCoreSetup();
            RegisterCorePatches();
            RegisterBiotechPatches();
            RegisterCompatibilityPatches();
            Log.Message("[PoniesOfTheRim] Bootstrap: завершён.");
        }

        private static void RegisterCoreSetup()
        {
            try
            {
                StatDefOf.GlobalLearningFactor.parts ??= new System.Collections.Generic.List<StatPart>();
                StatDefOf.GlobalLearningFactor.parts.Add(new StatPart_HiveMind());
                Log.Message("[PoniesOfTheRim] Bootstrap: ✓ StatPart_HiveMind.");
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Bootstrap: ошибка StatPart_HiveMind:\n{ex}");
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
                AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoWindowContents"),
                prefix:  new HarmonyMethod(typeof(UniqueStartingPawnUI), nameof(UniqueStartingPawnUI.DoWindowContents_Prefix)),
                postfix: new HarmonyMethod(typeof(UniqueStartingPawnUI), nameof(UniqueStartingPawnUI.DoWindowContents_Postfix))
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
                AccessTools.Method(typeof(JumpUtility), "CanHitTargetFrom"),
                prefix: new HarmonyMethod(typeof(JumpUtility_CanHitTargetFrom_Patch), nameof(JumpUtility_CanHitTargetFrom_Patch.CanHitTargetFrom_Patch)),
                label: "JumpUtility.CanHitTargetFrom"
            );

            TryPatch(
                AccessTools.Method(typeof(LoadedModManager), "ApplyPatches"),
                prefix: new HarmonyMethod(
                    typeof(PatchesForPonySettings.LoadedModManager_ApplyPatches_Patch),
                    nameof(PatchesForPonySettings.LoadedModManager_ApplyPatches_Patch.Prefix)),
                label: "LoadedModManager.ApplyPatches"
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
                Log.Message("[PoniesOfTheRim] Biotech не активен — соответствующие патчи пропущены.");
                return;
            }

            TryPatch(
                AccessTools.Method(typeof(Hediff), "Tick"),
                prefix: new HarmonyMethod(typeof(Patch_Hediff_Pregnant_Tick), nameof(Patch_Hediff_Pregnant_Tick.Prefix)),
                label: "Hediff.Tick (egg birth)"
            );

            TryPatch(
                AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome"),
                prefix: new HarmonyMethod(typeof(Patch_PregnancyUtility_ApplyBirthOutcome), nameof(Patch_PregnancyUtility_ApplyBirthOutcome.Prefix)),
                label: "PregnancyUtility.ApplyBirthOutcome (egg birth)"
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
            RegisterRooCompatibility();
        }

        private static void RegisterRooCompatibility()
        {
            bool anyActive =
                ModsConfig.IsActive("tug.Minotaur")          ||
                ModsConfig.IsActive("tug.Minotaur.Expanded") ||
                ModsConfig.IsActive("V.Rooboid.Faun")        ||
                ModsConfig.IsActive("tug.Satyr")             ||
                ModsConfig.IsActive("tug.SatyrFaun.Expanded");

            if (!anyActive)
            {
                Log.Message("[PoniesOfTheRim] Roo моды не обнаружены — патч пропущен.");
                return;
            }

            TryPatch(
                AccessTools.Method(typeof(PawnRenderNode_Fur), "GraphicFor"),
                prefix: new HarmonyMethod(typeof(PonyRooCompatPatch), nameof(PonyRooCompatPatch.DisableFurForPonyMinotaur))
                    { priority = Priority.HigherThanNormal },
                label: "PawnRenderNode_Fur.GraphicFor"
            );
        }

        private static void TryPatch(
            MethodInfo original,
            HarmonyMethod prefix      = null,
            HarmonyMethod postfix     = null,
            HarmonyMethod transpiler  = null,
            string label              = "")
        {
            if (original == null)
            {
                Log.Error($"[PoniesOfTheRim] Bootstrap: метод не найден — '{label}'.");
                return;
            }
            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler);
                Log.Message($"[PoniesOfTheRim] Bootstrap: ✓ {label}");
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Bootstrap: ошибка патча '{label}':\n{ex}");
            }
        }
    }
}