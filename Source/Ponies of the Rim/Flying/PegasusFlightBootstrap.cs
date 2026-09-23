using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Flying
{
    [StaticConstructorOnStartup]
    public static class PegasusFlightBootstrap
    {
        private static readonly Harmony Harmony = new("PoniesOfTheRim.Flying");

        private static int _patched;
        private static int _failed;

        static PegasusFlightBootstrap()
        {
            TryPatch(
                AccessTools.PropertyGetter(typeof(Pawn_DrawTracker), "DrawPos"),
                postfix: Postfix(typeof(Patch_PawnDrawTracker_DrawPos_BodyBob)),
                label: "Pawn_DrawTracker.DrawPos (покачивание в полёте)");

            TryPatch(
                AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Flying)),
                postfix: Postfix(typeof(Patch_Pawn_Flying)),
                label: "Pawn.Flying");

            TryPatch(
                AccessTools.Method(typeof(Pawn_FlightTracker), "FlightTick"),
                prefix: Prefix(typeof(Patch_FlightTracker_FlightTick)),
                label: "Pawn_FlightTracker.FlightTick");

            TryPatch(
                AccessTools.Method(typeof(Pawn_FlightTracker), "ForceLand"),
                prefix: Prefix(typeof(Patch_FlightTracker_ForceLand)),
                label: "Pawn_FlightTracker.ForceLand");

            TryPatch(
                AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new[] { typeof(Pawn), typeof(IntVec3) }),
                postfix: Postfix(typeof(Patch_CostToMoveIntoCell)),
                label: "Pawn_PathFollower.CostToMoveIntoCell");

            TryPatch(
                Patch_PathFinder_CreateRequest.FindTargetMethod(),
                prefix: Prefix(typeof(Patch_PathFinder_CreateRequest)),
                label: "PathFinder.CreateRequest");

            TryPatch(
                AccessTools.Method(typeof(GenGrid), "WalkableBy"),
                postfix: Postfix(typeof(Patch_GenGrid_WalkableBy)),
                label: "GenGrid.WalkableBy");

            TryPatch(
                AccessTools.Method(typeof(Pawn_PathFollower), "BuildingBlockingNextPathCell"),
                postfix: Postfix(typeof(Patch_BuildingBlockingNextPathCell)),
                label: "Pawn_PathFollower.BuildingBlockingNextPathCell");

            TryPatch(
                AccessTools.Method(typeof(Pawn_PathFollower), "NextCellDoorToWaitForOrManuallyOpen"),
                postfix: Postfix(typeof(Patch_NextCellDoor)),
                label: "Pawn_PathFollower.NextCellDoorToWaitForOrManuallyOpen");

            TryPatch(
                AccessTools.Method(typeof(Pawn_PathFollower), "TryEnterNextPathCell"),
                prefix: Prefix(typeof(Patch_TryEnterNextPathCell_BlockFog)),
                label: "Pawn_PathFollower.TryEnterNextPathCell (туман)");

            TryPatch(
                AccessTools.Method(typeof(TraverseParms), nameof(TraverseParms.For),
                    new[] { typeof(Pawn), typeof(Danger), typeof(TraverseMode),
                            typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                postfix: Postfix(typeof(Patch_TraverseParms_AvoidFog)),
                label: "TraverseParms.For (туман)");

            TryPatch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.GetPathContext)),
                postfix: Postfix(typeof(Patch_GetPathContext)),
                label: "Pawn.GetPathContext");

            TryPatch(
                AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "CanDrawAddon",
                    new[] { typeof(Pawn) }),
                postfix: Postfix(typeof(Patch_BodyAddon_CanDrawAddon)),
                label: "BodyAddon.CanDrawAddon");

            TryPatch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)),
                prefix: Prefix(typeof(Patch_Pawn_ExposeData_SavePositionFix)),
                postfix: Postfix(typeof(Patch_Pawn_ExposeData_SavePositionFix)),
                finalizer: Finalizer(typeof(Patch_Pawn_ExposeData_SavePositionFix)),
                label: "Pawn.ExposeData (позиция при сохранении)");

            TryPatch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
                postfix: Postfix(typeof(Patch_SpawnSetup_EnsureNaturalWings)),
                label: "Pawn.SpawnSetup (природные крылья)");

            TryPatch(
                AccessTools.Method(typeof(Game), nameof(Game.DeinitAndRemoveMap)),
                postfix: Postfix(typeof(Patch_Game_DeinitAndRemoveMap)),
                label: "Game.DeinitAndRemoveMap");

            PonyLog.Trace($"Pegasus flight: патчей установлено {_patched}, ошибок {_failed}.");
        }

        private static void TryPatch(
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            HarmonyMethod finalizer = null,
            string label = "")
        {
            if (original == null)
            {
                _failed++;
                PonyLog.Error($"Pegasus flight: метод не найден — '{label}'.");
                return;
            }
            if (prefix == null && postfix == null && transpiler == null && finalizer == null)
            {
                _failed++;
                PonyLog.Error($"Pegasus flight: не найден ни один метод патча для '{label}' — патч пропущен.");
                return;
            }
            try
            {
                Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
                _patched++;
            }
            catch (Exception ex)
            {
                _failed++;
                PonyLog.Error($"Pegasus flight: ошибка патча '{label}':\n{ex}");
            }
        }

        private static HarmonyMethod Prefix(Type type) => Make(type, "Prefix");
        private static HarmonyMethod Postfix(Type type) => Make(type, "Postfix");
        private static HarmonyMethod Finalizer(Type type) => Make(type, "Finalizer");

        private static HarmonyMethod Make(Type type, string name)
        {
            MethodInfo m = AccessTools.Method(type, name);
            return m == null ? null : new HarmonyMethod(m);
        }
    }
}