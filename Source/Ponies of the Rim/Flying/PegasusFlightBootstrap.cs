using System;
using System.Collections.Generic;
using System.Reflection;
using AlienRace;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    [StaticConstructorOnStartup]
    public static class PegasusFlightBootstrap
    {
        [Flags]
        private enum Hook
        {
            Prefix = 1,
            Postfix = 2,
            Finalizer = 4,
            Transpiler = 8
        }

        private static readonly Harmony _harmony = new Harmony("PoniesOfTheRim.Flying");

        private static int _patched;
        private static int _failed;

        static PegasusFlightBootstrap()
        {
            TryPatch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Flying)),
                typeof(Patch_Pawn_Flying), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(Pawn_FlightTracker), "FlightTick"),
                typeof(Patch_FlightTracker_FlightTick), Hook.Prefix);
            TryPatch(AccessTools.Method(typeof(Pawn_FlightTracker), "ForceLand"),
                typeof(Patch_FlightTracker_ForceLand), Hook.Prefix);

            TryPatch(AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new[] { typeof(Pawn), typeof(IntVec3) }),
                typeof(Patch_CostToMoveIntoCell), Hook.Postfix);
            TryPatch(Patch_PathFinder_CreateRequest.FindTargetMethod(),
                typeof(Patch_PathFinder_CreateRequest), Hook.Prefix);
            TryPatch(AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReach),
                    new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) }),
                typeof(Patch_Reachability_CanReach), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReachMapEdge),
                    new[] { typeof(IntVec3), typeof(TraverseParms) }),
                typeof(Patch_Reachability_CanReachMapEdge), Hook.Postfix);
            TryPatch(Patch_PathFollower_StartPath.FindTargetMethod(),
                typeof(Patch_PathFollower_StartPath), Hook.Prefix | Hook.Finalizer);
            TryPatch(AccessTools.Method(typeof(GenGrid), "WalkableBy"),
                typeof(Patch_GenGrid_WalkableBy), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(CastPositionFinder), "EvaluateCell"),
                typeof(Patch_CastPositionFinder_EvaluateCell), Hook.Prefix);
            TryPatch(AccessTools.Method(typeof(Pawn_PathFollower), "BuildingBlockingNextPathCell"),
                typeof(Patch_BuildingBlockingNextPathCell), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(Pawn_PathFollower), "NextCellDoorToWaitForOrManuallyOpen"),
                typeof(Patch_NextCellDoor), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetPathContext)),
                typeof(Patch_GetPathContext), Hook.Postfix);

            TryPatch(AccessTools.Method(typeof(Pawn_PathFollower), "TryEnterNextPathCell"),
                typeof(Patch_TryEnterNextPathCell_BlockFog), Hook.Prefix);
            TryPatch(AccessTools.Method(typeof(TraverseParms), nameof(TraverseParms.For),
                    new[] { typeof(Pawn), typeof(Danger), typeof(TraverseMode),
                            typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                typeof(Patch_TraverseParms_AvoidFog), Hook.Postfix);

            TryPatchAll(Patch_MindState_TerrainThoughtInFlight.ResolveTargets(),
                typeof(Patch_MindState_TerrainThoughtInFlight), Hook.Transpiler);
            TryPatch(AccessTools.Method(typeof(Pawn_FilthTracker), nameof(Pawn_FilthTracker.Notify_EnteredNewCell)),
                typeof(Patch_FilthTracker_FilthInFlight), Hook.Prefix);
            TryPatch(AccessTools.Method(typeof(Pawn_PathFollower), "TryEnterNextPathCell"),
                typeof(Patch_PathFollower_SnowInFlight), Hook.Transpiler);
            TryPatch(AccessTools.Method(typeof(ToxicUtility), nameof(ToxicUtility.PawnToxicTickInterval),
                    new[] { typeof(Pawn), typeof(int) }),
                typeof(Patch_ToxicUtility_ToxicWaterInFlight), Hook.Transpiler);
            TryPatch(AccessTools.Method(typeof(Pawn_MeleeVerbs_TerrainSource), "Verse.IVerbOwner.VerbsStillUsableBy"),
                typeof(Patch_TerrainVerbs_KickInFlight), Hook.Postfix);

            TryPatch(AccessTools.PropertyGetter(typeof(Pawn_DrawTracker), "DrawPos"),
                typeof(Patch_PawnDrawTracker_DrawPos_BodyBob), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "CanDrawAddon",
                    new[] { typeof(Pawn) }),
                typeof(Patch_BodyAddon_CanDrawAddon), Hook.Postfix);

            TryPatch(AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
                typeof(Patch_SpawnSetup_EnsureNaturalWings), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn",
                    new[] { typeof(PawnGenerationRequest) }),
                typeof(Patch_PawnGenerator_NaturalWings), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn",
                    new[] { typeof(PawnGenerationRequest) }),
                typeof(Patch_PawnGenerator_FlightInfoAbility), Hook.Postfix);

            TryPatch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)),
                typeof(Patch_Pawn_ExposeData_SavePositionFix), Hook.Prefix | Hook.Finalizer);
            TryPatch(AccessTools.Method(typeof(Game), nameof(Game.DeinitAndRemoveMap)),
                typeof(Patch_Game_DeinitAndRemoveMap), Hook.Postfix);
            TryPatch(AccessTools.Method(typeof(Game), nameof(Game.Dispose)),
                typeof(Patch_Game_Dispose), Hook.Postfix);

            PonyLog.Trace($"Flight: {_patched} patched, {_failed} failed");
        }

        private static void TryPatch(MethodBase original, Type patchType, Hook hooks)
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"Flight: target of {patchType.Name} not found, skipped");
                return;
            }

            try
            {
                _harmony.Patch(original,
                    prefix: Resolve(patchType, hooks, Hook.Prefix),
                    postfix: Resolve(patchType, hooks, Hook.Postfix),
                    transpiler: Resolve(patchType, hooks, Hook.Transpiler),
                    finalizer: Resolve(patchType, hooks, Hook.Finalizer));
                _patched++;
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Flight: {patchType.Name} failed on {original.DeclaringType?.Name}.{original.Name}\n{ex}");
            }
        }

        private static void TryPatchAll(List<MethodInfo> originals, Type patchType, Hook hooks)
        {
            if (originals == null || originals.Count == 0)
            {
                TryPatch(null, patchType, hooks);
                return;
            }
            foreach (MethodInfo original in originals)
            {
                TryPatch(original, patchType, hooks);
            }
        }

        private static HarmonyMethod Resolve(Type patchType, Hook requested, Hook hook)
        {
            if ((requested & hook) == 0)
            {
                return null;
            }
            MethodInfo method = AccessTools.Method(patchType, hook.ToString());
            if (method == null)
            {
                throw new MissingMethodException(patchType.FullName, hook.ToString());
            }
            return new HarmonyMethod(method);
        }
    }
}