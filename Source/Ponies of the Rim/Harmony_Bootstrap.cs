using System;
using System.Collections.Generic;
using System.Reflection;
using AlienRace;
using HarmonyLib;
using PoniesOfTheRim.Flying;
using PoniesOfTheRim.Food;
using PoniesOfTheRim.Genetics;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    public static class POTR_Bootstrap
    {
        public static readonly Harmony Harmony = new Harmony("Rimworld.PoniesOfTheRim.Core");

        private static int _patched;
        private static int _failed;

        static POTR_Bootstrap()
        {
            PonyLog.InstallMainThreadPump();

            RunStage(BuildCaches);
            RunStage(AddHiveMindStatPart);
            RunStage(RegisterCorePatches);
            RunStage(RegisterScenarioPatches);
            RunStage(RegisterBiotechPatches);
            RunStage(RegisterIdeologyPatches);
            RunStage(RegisterCompatibilityPatches);

            PonyLog.Trace($"Core: {_patched} patched, {_failed} failed");
        }

        private static void RunStage(Action stage)
        {
            try
            {
                stage();
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Core: {stage.Method.Name} aborted\n{ex}");
            }
        }

        private static void BuildCaches()
        {
            PonyHelper.BuildRaceCache();
            PonyFlightCache.BuildStaticCaches();
            PonyFoodCache.Build();
        }

        private static void AddHiveMindStatPart()
        {
            StatDef stat = StatDefOf.GlobalLearningFactor;
            stat.parts ??= new List<StatPart>();
            stat.parts.Add(new StatPart_HiveMind { parentStat = stat });
        }

        private static void RegisterCorePatches()
        {
            // cutiemarks
            PonyMarkVariantSelector.Initialize();
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp), nameof(AlienPartGenerator.AlienComp.CompRenderNodes)),
                postfix: new HarmonyMethod(typeof(PonyMarkVariantSelector), nameof(PonyMarkVariantSelector.CompRenderNodes_Postfix)));
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp), "RegenerateAddonsForced", Type.EmptyTypes),
                postfix: new HarmonyMethod(typeof(PonyMarkVariantSelector), nameof(PonyMarkVariantSelector.RegenerateAddonsForced_Postfix)));

            // names
            TryPatch(
                AccessTools.PropertyGetter(typeof(NameTriple), nameof(NameTriple.IsValid)),
                postfix: new HarmonyMethod(typeof(Patch_DialogNamePawn_SingleName), nameof(Patch_DialogNamePawn_SingleName.IsValid_Postfix)));

            // body
            TryPatch(
                AccessTools.Method(typeof(PawnFootprintMaker), "TryPlaceFootprint"),
                prefix: new HarmonyMethod(typeof(HoofprintPatch), nameof(HoofprintPatch.TryPlaceHoofprint)));
            TryPatch(
                AccessTools.Method(typeof(AlienRace.HarmonyPatches), "CheckBodyType"),
                postfix: new HarmonyMethod(typeof(PonyBodyTypePatch), nameof(PonyBodyTypePatch.BodyTypePatch)));
            TryPatch(
                AccessTools.Method(typeof(Pawn_StoryTracker), "ExposeData"),
                postfix: new HarmonyMethod(typeof(ChildPonyBodyTypeFixPatch), nameof(ChildPonyBodyTypeFixPatch.ChildBodyTypeFixPatch)));
            TryPatch(
                AccessTools.Method(typeof(AlienPawnRenderNodeWorker_BodyAddon), "OffsetFor"),
                postfix: new HarmonyMethod(typeof(OffsetForPonyEarsPatch), nameof(OffsetForPonyEarsPatch.OffsetForPonyEars)));
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "GetGraphic"),
                postfix: new HarmonyMethod(typeof(PonyHairExtensionPatch), nameof(PonyHairExtensionPatch.PonyTailPatch)));

            // styling station
            TryPatch(
                AccessTools.Method(typeof(StylingStation), "DoAddonList"),
                prefix: new HarmonyMethod(typeof(StylingStationPatch), nameof(StylingStationPatch.DoAddonList_PonyPatch)));
            TryPatch(
                AccessTools.Method(typeof(StylingStation), "DoAddonInfo"),
                prefix: new HarmonyMethod(typeof(StylingStationPatch), nameof(StylingStationPatch.DoAddonInfo_PonyPatch)));

            // belts
            TryPatch(
                AccessTools.Method(typeof(WornGraphicData), "BeltScaleAt"),
                prefix: new HarmonyMethod(typeof(ScalableBeltPatch), nameof(ScalableBeltPatch.BeltScaleAtPatch)));
            TryPatch(
                AccessTools.Method(typeof(WornGraphicData), "BeltOffsetAt"),
                prefix: new HarmonyMethod(typeof(ScalableBeltPatch), nameof(ScalableBeltPatch.BeltOffsetAtPatch)));

            // comms console and quests
            TryPatch(
                AccessTools.Method(typeof(FactionDialogMaker), "FactionDialogFor"),
                postfix: new HarmonyMethod(typeof(CommsConsolePatch), nameof(CommsConsolePatch.CommsConsolePostfix)));
            TryPatch(
                AccessTools.Method(typeof(QuestNode_GetFaction), "IsGoodFaction"),
                postfix: new HarmonyMethod(typeof(QuestFactionPatches), nameof(QuestFactionPatches.IsGoodFaction_Postfix)));
            TryPatch(
                AccessTools.Method(typeof(QuestNode_GetNearbySettlement), "RunInt"),
                prefix: new HarmonyMethod(typeof(QuestFactionPatches), nameof(QuestFactionPatches.GetNearbySettlement_RunInt_Prefix)));
            TryPatch(
                AccessTools.Method(typeof(QuestNode_GetNearbySettlement), "TestRunInt"),
                prefix: new HarmonyMethod(typeof(QuestFactionPatches), nameof(QuestFactionPatches.GetNearbySettlement_TestRunInt_Prefix)));
            TryPatch(
                AccessTools.Method(typeof(QuestNode_GeneratePawn), "RunInt"),
                prefix: new HarmonyMethod(typeof(Patch_QuestNode_GeneratePawn_RaceAware), nameof(Patch_QuestNode_GeneratePawn_RaceAware.Prefix)));

            // character card
            TryPatch(
                AccessTools.Method(typeof(CharacterCardUtility), "DrawCharacterCard"),
                postfix: new HarmonyMethod(typeof(DrawCharacterCardPatch), nameof(DrawCharacterCardPatch.CutiemarkIcon)));
            TryPatch(
                AccessTools.Method(typeof(CharacterCardUtility), "DoTopStack"),
                transpiler: new HarmonyMethod(typeof(DrawCharacterCardPatch), nameof(DrawCharacterCardPatch.DoTopStackTranspiler)));

            // main menu
            TryPatch(
                AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoExpansionIcons)),
                postfix: new HarmonyMethod(typeof(MainMenuDiscordIconPatch), nameof(MainMenuDiscordIconPatch.Postfix)));

            // crystal ponies
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp), nameof(AlienPartGenerator.AlienComp.CompRenderNodes)),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_CompRenderNodes),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_CompRenderNodes.Postfix)));
            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.AlienComp), "RegenerateAddonsForced", Type.EmptyTypes),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_RegenerateAddonsForced),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_AlienComp_RegenerateAddonsForced.Postfix)));
            TryPatch(
                AccessTools.Method(typeof(PawnRenderNode_Hair), nameof(PawnRenderNode_Hair.GraphicFor)),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_PawnRenderNodeHair_GraphicForPawn),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_PawnRenderNodeHair_GraphicForPawn.Postfix)));
            TryPatch(
                AccessTools.Method(typeof(Page_ConfigureStartingPawns), "PreOpen"),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_PreOpen),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_PreOpen.Postfix)));
            TryPatch(
                AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoWindowContents"),
                postfix: new HarmonyMethod(
                    typeof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_DoWindowContents),
                    nameof(Crystalpony_CrystalizeGraphics_Bootstrap.Patch_ConfigureStartingPawns_DoWindowContents.Postfix)));

            // foals; the hair postfix runs after the crystal one
            TryPatch(
                AccessTools.Method(typeof(PawnRenderNode_Hair), nameof(PawnRenderNode_Hair.GraphicFor)),
                postfix: new HarmonyMethod(typeof(PonyBabyHairPatch), nameof(PonyBabyHairPatch.PonyBabyHairPostfix)));
            TryPatch(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(PonyBabyHairPatch), nameof(PonyBabyHairPatch.GeneratePawn_Postfix)));
        }

        private static void RegisterScenarioPatches()
        {
            TryPatch(
                Patch_StartingPawns_ScenarioReset.ResolveTarget(),
                postfix: new HarmonyMethod(typeof(Patch_StartingPawns_ScenarioReset), nameof(Patch_StartingPawns_ScenarioReset.Postfix)));
        }

        private static void RegisterBiotechPatches()
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            // Egg laying
            if (Gene_Oviparous.OvipGeneDef != null)
            {
                TryPatch(
                    AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome"),
                    prefix: new HarmonyMethod(typeof(Patch_PregnancyUtility_ApplyBirthOutcome), nameof(Patch_PregnancyUtility_ApplyBirthOutcome.Prefix)));
            }

            PonyRacialGeneUtility.BuildCache();
            TryPatch(
                AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.GetInheritedGenes),
                    new[] { typeof(Pawn), typeof(Pawn), typeof(bool).MakeByRefType() }),
                postfix: new HarmonyMethod(typeof(Patch_PregnancyUtility_RacialGenes), nameof(Patch_PregnancyUtility_RacialGenes.Postfix)));

            TryPatch(
                AccessTools.Method(typeof(LifeStageWorker_HumanlikeChild), "Notify_LifeStageStarted"),
                postfix: new HarmonyMethod(typeof(Patch_LifeStageWorker_HumanlikeChild_Notify), nameof(Patch_LifeStageWorker_HumanlikeChild_Notify.Postfix)));
            TryPatch(
                AccessTools.Method(typeof(Thing), "Ingested", new[] { typeof(Pawn), typeof(float) }),
                postfix: new HarmonyMethod(typeof(PonyFoodGenesPatch), nameof(PonyFoodGenesPatch.IngestedPonyFoodGenesPatch)));
            TryPatch(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(PonyFoodGeneRemovalPatch), nameof(PonyFoodGeneRemovalPatch.PonyFoodGeneRemovalForGeneratePawn)));
        }

        private static void RegisterIdeologyPatches()
        {
            if (!ModsConfig.IdeologyActive || !Patch_FoodUtility_ThoughtsFromIngesting.IsReady
                || !PonyFoodCache.AnyIngredientEvents)
            {
                return;
            }

            TryPatch(
                AccessTools.Method(typeof(FoodUtility), "ThoughtsFromIngesting"),
                postfix: new HarmonyMethod(typeof(Patch_FoodUtility_ThoughtsFromIngesting), nameof(Patch_FoodUtility_ThoughtsFromIngesting.Postfix)));
        }

        private static void RegisterCompatibilityPatches()
        {
            FurCompatibilityPatch.EnsurePonyFurPaths();
        }

        private static void TryPatch(
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null)
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"Core: target of {Describe(prefix ?? postfix ?? transpiler)} not found, skipped");
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
                PonyLog.Error($"Core: {Describe(prefix ?? postfix ?? transpiler)} failed on {original.DeclaringType?.Name}.{original.Name}\n{ex}");
            }
        }

        private static string Describe(HarmonyMethod patch)
        {
            MethodInfo method = patch?.method;
            return method == null ? "?" : method.DeclaringType?.Name + "." + method.Name;
        }
    }
}