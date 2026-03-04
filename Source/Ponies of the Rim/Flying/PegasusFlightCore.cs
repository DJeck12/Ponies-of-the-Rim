
using AlienRace;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace PoniesOfTheRim.Flying
{
            
    [StaticConstructorOnStartup]
    public static class PegasusFlightBootstrap
    {
        public static readonly Harmony HarmonyInstance = new("PoniesOfTheRim.Flying");

        static PegasusFlightBootstrap()
        {
            var h = HarmonyInstance;

                        h.Patch(
                AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Flying)),
                postfix: Postfix(typeof(Patch_Pawn_Flying)));

            h.Patch(
                AccessTools.Method(typeof(Pawn_FlightTracker), "FlightTick"),
                prefix: Prefix(typeof(Patch_FlightTracker_FlightTick)));

            h.Patch(
                AccessTools.Method(typeof(Pawn_FlightTracker), "ForceLand"),
                prefix: Prefix(typeof(Patch_FlightTracker_ForceLand)));

                        h.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new[] { typeof(Pawn), typeof(IntVec3) }),
                postfix: Postfix(typeof(Patch_CostToMoveIntoCell)));

            h.Patch(
                Patch_PathFinder_CreateRequest.FindTargetMethod(),
                prefix: Prefix(typeof(Patch_PathFinder_CreateRequest)));

            h.Patch(
                AccessTools.Method(typeof(GenGrid), nameof(GenGrid.WalkableBy)),
                prefix: Prefix(typeof(Patch_GenGrid_WalkableBy)));

            h.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "BuildingBlockingNextPathCell"),
                prefix: Prefix(typeof(Patch_BuildingBlockingNextPathCell)));

            h.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "NextCellDoorToWaitForOrManuallyOpen"),
                prefix: Prefix(typeof(Patch_NextCellDoor)));

                        h.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "TryEnterNextPathCell"),
                prefix: Prefix(typeof(Patch_TryEnterNextPathCell_BlockFog)));

            h.Patch(
                AccessTools.Method(typeof(TraverseParms), nameof(TraverseParms.For),
                    new[] { typeof(Pawn), typeof(Danger), typeof(TraverseMode),
                            typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                postfix: Postfix(typeof(Patch_TraverseParms_AvoidFog)));

            h.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.GetPathContext)),
                postfix: Postfix(typeof(Patch_GetPathContext)));

                        h.Patch(
                AccessTools.Method(typeof(PawnRenderNode), "AppendRequests"),
                prefix: Prefix(typeof(Patch_RenderNode_ForceWingsRefresh)));

            h.Patch(
                AccessTools.Method(typeof(AlienPartGenerator.BodyAddon), "CanDrawAddon",
                    new[] { typeof(Pawn) }),
                postfix: Postfix(typeof(Patch_BodyAddon_CanDrawAddon)));

                        h.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)),
                prefix: Prefix(typeof(Patch_Pawn_ExposeData_SavePositionFix)),
                postfix: Postfix(typeof(Patch_Pawn_ExposeData_SavePositionFix)));

            h.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
                postfix: Postfix(typeof(Patch_SpawnSetup_EnsureNaturalWings)));
                    }
        private static HarmonyMethod Prefix(Type type)  => new(AccessTools.Method(type, "Prefix"));
        private static HarmonyMethod Postfix(Type type) => new(AccessTools.Method(type, "Postfix"));
    }

            
    public static class PegasusFlightUtil
    {
        public const string LeftWingPartDef  = "Pony_LeftWing";
        public const string RightWingPartDef = "Pony_RightWing";

        public static readonly HashSet<string> WingHediffDefs = new()
        {
            "NaturalWing",
            "SimpleProstheticWing",
            "BionicWing",
            "ArchotechWing"
        };

                public static bool HasUsableWings(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || pawn.RaceProps?.body?.AllParts == null)
                return false;

            BodyPartRecord leftWing  = null;
            BodyPartRecord rightWing = null;

            var allParts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < allParts.Count; i++)
            {
                var defName = allParts[i].def?.defName;
                if (defName == LeftWingPartDef)       leftWing  = allParts[i];
                else if (defName == RightWingPartDef) rightWing = allParts[i];

                if (leftWing != null && rightWing != null) break;
            }

            if (leftWing == null || rightWing == null)
                return false;

            return !pawn.health.hediffSet.PartIsMissing(leftWing)
                && !pawn.health.hediffSet.PartIsMissing(rightWing);
        }

                public static bool IsPegasusConstantFlight(Pawn p)
        {
            if (p == null || !p.Spawned || p.Dead || p.Downed)
                return false;
            if (!p.IsPegasus())
                return false;

            var comp = p.TryGetComp<CompPegasusFlightToggle>();
            if (comp == null || !comp.FlightEnabled)
                return false;

            if (!HasUsableWings(p))
                return false;

            if (p.Position.Roofed(p.Map))
                return false;

            var timerComp = p.TryGetComp<CompPegasusFlightTimer>();
            if (timerComp != null && !timerComp.CanFly)
                return false;

            return true;
        }

                public static bool CanFlyToCell(Pawn pawn, IntVec3 c, Map map)
        {
            if (!IsPegasusConstantFlight(pawn))
                return false;
            if (c.Roofed(map))
                return false;
            if (c.Fogged(map))
                return false;
            if (!c.Walkable(map) && IsImpassableMountain(c, map))
                return false;
            return true;
        }

                public static bool IsImpassableMountain(IntVec3 c, Map map)
        {
            var edifice = c.GetEdifice(map);
            if (edifice != null && edifice.def?.building != null && edifice.def.building.isNaturalRock)
                return true;
            var terrain = c.GetTerrain(map);
            if (terrain != null && terrain.passability == Traversability.Impassable)
                return true;
            return false;
        }

                                public static void SafeLand(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return;

            if (pawn.Position.Walkable(pawn.Map))
                return;

            Map map = pawn.Map;
            IntVec3 bestCell = IntVec3.Invalid;
            int bestScore = -1;

                        for (int radius = 1; radius <= 15; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, radius == 1))
                {
                    if (!cell.InBounds(map) || !cell.Walkable(map))
                        continue;

                    int score = 0;
                    if (!cell.Fogged(map)) score += 2;
                    if (!cell.Roofed(map)) score += 1;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCell = cell;
                    }
                }

                                if (bestCell.IsValid)
                    break;
            }

            if (bestCell.IsValid)
            {
                pawn.Position = bestCell;
                pawn.Notify_Teleported(true, false);
            }
            else
            {
                                IntVec3 fallback;
                if (CellFinder.TryFindRandomCellNear(pawn.Position, map, 30,
                    c => c.Walkable(map) && !c.Fogged(map), out fallback))
                {
                    pawn.Position = fallback;
                    pawn.Notify_Teleported(true, false);
                }
            }
        }
    }


            
    public class CompPropertiesPegasusFlightToggle : CompProperties
    {
        public CompPropertiesPegasusFlightToggle()
        {
            compClass = typeof(CompPegasusFlightToggle);
        }
    }

    public class CompPegasusFlightToggle : ThingComp
    {
        private bool flightEnabled;

        public CompPropertiesPegasusFlightToggle Props => (CompPropertiesPegasusFlightToggle)props;

        public bool FlightEnabled
        {
            get => flightEnabled;
            set => flightEnabled = value;
        }

        public Pawn Pawn => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref flightEnabled, "PegasusFlightEnabled", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!flightEnabled || Pawn == null || !Pawn.Spawned)
                return;

            if (Pawn.IsHashIntervalTick(30))
            {
                                if (Pawn.Dead || Pawn.Downed)
                {
                    flightEnabled = false;
                    PegasusFlightUtil.SafeLand(Pawn);
                    if (!Pawn.Dead)
                    {
                        Messages.Message(
                            $"{Pawn.LabelShort} crashed - incapacitated!",
                            Pawn, MessageTypeDefOf.NegativeEvent, historical: false);
                    }
                    return;
                }

                if (!PegasusFlightUtil.HasUsableWings(Pawn))
                {
                    flightEnabled = false;
                    PegasusFlightUtil.SafeLand(Pawn);
                    Messages.Message(
                        $"{Pawn.LabelShort} crashed - wing torn off!",
                        Pawn, MessageTypeDefOf.NegativeEvent, historical: false);
                    return;
                }

                if (Pawn.Position.Roofed(Pawn.Map))
                {
                    flightEnabled = false;
                    PegasusFlightUtil.SafeLand(Pawn);
                    Messages.Message(
                        $"{Pawn.LabelShort} landed - cannot fly under roof!",
                        Pawn, MessageTypeDefOf.SilentInput, historical: false);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn == null || !Pawn.IsPegasus() || !Pawn.Spawned || Pawn.Dead)
                yield break;

            if (!PegasusFlightUtil.HasUsableWings(Pawn))
                yield break;

            var timerComp = Pawn.TryGetComp<CompPegasusFlightTimer>();

            var gizmo = new Command_Action
            {
                defaultLabel = flightEnabled ? "Disable flight" : "Enable flight",
                defaultDesc = $"Toggle pegasus constant flying.\n\nStamina: {timerComp?.CurrentStaminaPercent.ToStringPercent() ?? "N/A"}",
                icon = ContentFinder<Texture2D>.Get("UI/Abilities/PegasusFlightToggle", false),
                action = delegate
                {
                    if (!flightEnabled && Pawn.Position.Roofed(Pawn.Map))
                    {
                        Messages.Message(
                            $"{Pawn.LabelShort} cannot take flight under a roof!",
                            Pawn, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    if (!flightEnabled && timerComp != null && !timerComp.CanFly)
                    {
                        Messages.Message(
                            $"{Pawn.LabelShort} is too exhausted to fly!",
                            Pawn, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    flightEnabled = !flightEnabled;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
            };

            if (!flightEnabled)
            {
                if (Pawn.Position.Roofed(Pawn.Map))
                    gizmo.Disable("Cannot fly under roof");
                else if (timerComp != null && !timerComp.CanFly)
                    gizmo.Disable("Too exhausted");
            }

            yield return gizmo;
        }

        private void UpdateAnimation()
        {
            if (Pawn?.Drawer?.renderer?.renderTree != null)
                Pawn.Drawer.renderer.renderTree.SetDirty();
        }

        public void RefreshAnimation() => UpdateAnimation();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            UpdateAnimation();
        }
    }


            
    public class CompPropertiesPegasusFlightTimer : CompProperties
    {
        public CompPropertiesPegasusFlightTimer()
        {
            compClass = typeof(CompPegasusFlightTimer);
        }
    }

    public class CompPegasusFlightTimer : ThingComp
    {
        private const int BaseFlightDurationTicks = 600;

        private float currentFlightStamina = 1f;
        private bool isFlying = false;

        public CompPropertiesPegasusFlightTimer Props => (CompPropertiesPegasusFlightTimer)props;

        public float CurrentStaminaPercent => currentFlightStamina;
        public bool CanFly => currentFlightStamina > 0.05f;
        public bool IsFlying => isFlying;

        public int MaxFlightDurationTicks
        {
            get
            {
                Pawn pawn = parent as Pawn;
                if (pawn == null) return BaseFlightDurationTicks;

                PawnCapacityDef cap = DefDatabase<PawnCapacityDef>.GetNamedSilentFail("Pegasus_Flight");
                if (cap == null) return BaseFlightDurationTicks;

                float eff = Mathf.Clamp(pawn.health.capacities.GetLevel(cap), 0f, 2f);
                return Mathf.RoundToInt(BaseFlightDurationTicks * eff);
            }
        }

        private float StaminaDrainPerSecond
        {
            get
            {
                int maxDuration = MaxFlightDurationTicks;
                return (maxDuration <= 0) ? 0f : 60f / maxDuration;
            }
        }

        private float StaminaRecoveryPerSecond => StaminaDrainPerSecond * 0.5f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentFlightStamina, "flightStamina", 1f);
            Scribe_Values.Look(ref isFlying, "isFlying", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned) return;

            var flightComp = pawn.TryGetComp<CompPegasusFlightToggle>();
            isFlying = flightComp?.FlightEnabled ?? false;

            if (isFlying)
            {
                currentFlightStamina -= StaminaDrainPerSecond / 60f;

                if (currentFlightStamina <= 0f)
                {
                    currentFlightStamina = 0f;

                    if (flightComp != null)
                    {
                        flightComp.FlightEnabled = false;
                        PegasusFlightUtil.SafeLand(pawn);
                        Messages.Message(
                            $"{pawn.LabelShort} exhausted - flight ended!",
                            pawn, MessageTypeDefOf.NegativeEvent, historical: false);
                    }
                }
            }
            else
            {
                if (currentFlightStamina < 1f)
                {
                    currentFlightStamina += StaminaRecoveryPerSecond / 60f;
                    currentFlightStamina = Mathf.Min(currentFlightStamina, 1f);
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (parent is not Pawn pawn || !pawn.IsPegasus())
                return null;

            int maxDuration = MaxFlightDurationTicks;
            if (maxDuration <= 0)
                return "Can't fly now.";

            float maxSeconds = maxDuration / 60f;
            float currentSeconds = maxSeconds * currentFlightStamina;

            return $"Flight stamina: {currentFlightStamina.ToStringPercent()} ({currentSeconds:F1}s / {maxSeconds:F1}s)";
        }
    }
}