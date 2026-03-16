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
        private static readonly Harmony Harmony = new("PoniesOfTheRim.Flying");

        static PegasusFlightBootstrap()
        {
            var h = Harmony;

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
                prefix:  Prefix(typeof(Patch_Pawn_ExposeData_SavePositionFix)),
                postfix: Postfix(typeof(Patch_Pawn_ExposeData_SavePositionFix)));

            h.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
                postfix: Postfix(typeof(Patch_SpawnSetup_EnsureNaturalWings)));

            h.Patch(
                AccessTools.Method(typeof(Game), nameof(Game.DeinitAndRemoveMap)),
                postfix: Postfix(typeof(Patch_Game_DeinitAndRemoveMap)));

            Log.Message("[PoniesOfTheRim] Pegasus flight patches registered: 16");
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
            if (!p.HasWings())
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

                    if (!Pawn.Dead && Pawn.IsColonistPlayerControlled)
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

                    if (Pawn.IsColonistPlayerControlled)
                    {
                        Messages.Message(
                            $"{Pawn.LabelShort} crashed - wing torn off!",
                            Pawn, MessageTypeDefOf.NegativeEvent, historical: false);
                    }
                    return;
                }

                if (Pawn.Position.Roofed(Pawn.Map))
                {
                    flightEnabled = false;
                    PegasusFlightUtil.SafeLand(Pawn);

                    if (Pawn.IsColonistPlayerControlled)
                    {
                        Messages.Message(
                            $"{Pawn.LabelShort} landed - cannot fly under roof!",
                            Pawn, MessageTypeDefOf.SilentInput, historical: false);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn == null || !Pawn.HasWings() || !Pawn.Spawned || Pawn.Dead)
                yield break;

            if (Pawn.Faction != Faction.OfPlayer)
                yield break;

            if (Pawn.drafter?.Drafted != true)
                yield break;

            if (!PegasusFlightUtil.HasUsableWings(Pawn))
                yield break;

            var timerComp = Pawn.TryGetComp<CompPegasusFlightTimer>();

            var gizmo = new Command_Action
            {
                defaultLabel = flightEnabled ? "Disable flight" : "Enable flight",
                defaultDesc  = $"Toggle pegasus constant flying.\n\nStamina: {timerComp?.CurrentStaminaPercent.ToStringPercent() ?? "N/A"}",
                icon         = ContentFinder<Texture2D>.Get("UI/Abilities/PegasusFlightToggle", false),
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

            if (timerComp != null)
                yield return new Gizmo_FlightStamina(Pawn, timerComp);
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
        private const int   BaseFlightDurationTicks = 600;
        private const float BaseRestFallPerInterval = 0.00575f;

        private float currentFlightStamina  = 1f;
        private bool  isFlying              = false;
        private bool  _wasFlying            = false;

        private int   _flightCooldownEndTick = -1;
        private const int FlightCooldownTicks = 300;

        private HediffDef _fatigueDef;
        private HediffDef FatigueDef =>
            _fatigueDef ??= DefDatabase<HediffDef>.GetNamedSilentFail("Pony_FlightFatigue");

        private float _cachedDrainMult      = 1f;
        private int   _drainMultCacheTick   = -999;

        public CompPropertiesPegasusFlightTimer Props => (CompPropertiesPegasusFlightTimer)props;

        public float CurrentStaminaPercent => currentFlightStamina;
        public bool  IsFlying              => isFlying;

        public bool CanFly
        {
            get
            {
                if (currentFlightStamina <= 0.05f) return false;
                if (_flightCooldownEndTick > 0 &&
                    Find.TickManager.TicksGame < _flightCooldownEndTick) return false;
                return true;
            }
        }

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

        private float GetWingDrainMultiplier()
        {
            int now = Find.TickManager.TicksGame;
            if (now - _drainMultCacheTick < 300) return _cachedDrainMult;
            _drainMultCacheTick = now;

            Pawn pawn = parent as Pawn;
            if (pawn?.health?.hediffSet == null) return _cachedDrainMult = 1f;

            int bionic = 0, archotech = 0;
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                string pName = h.Part?.def?.defName;
                if (pName != PegasusFlightUtil.LeftWingPartDef &&
                    pName != PegasusFlightUtil.RightWingPartDef) continue;
                switch (h.def?.defName)
                {
                    case "BionicWing":    bionic++;    break;
                    case "ArchotechWing": archotech++; break;
                }
            }

            _cachedDrainMult =
                archotech >= 2                ? 1f / 10f :
                archotech == 1 && bionic >= 1 ? 1f / 7f  :
                archotech == 1                ? 1f / 5f  :
                bionic >= 2                   ? 1f / 4f  :
                bionic == 1                   ? 1f / 2f  :
                                                1f;
            return _cachedDrainMult;
        }

        private float BaseStaminaDrainPerSecond
        {
            get
            {
                int d = MaxFlightDurationTicks;
                return d <= 0 ? 0f : 60f / d;
            }
        }

        private float StaminaDrainPerSecond => BaseStaminaDrainPerSecond * GetWingDrainMultiplier();

        private float StaminaRecoveryPerSecond
        {
            get
            {
                float r = BaseStaminaDrainPerSecond * 0.25f;
                Pawn pawn = parent as Pawn;
                if (pawn != null && !pawn.Awake()) r *= 2f;
                return r;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentFlightStamina,   "flightStamina",         1f);
            Scribe_Values.Look(ref isFlying,               "isFlying",              false);
            Scribe_Values.Look(ref _flightCooldownEndTick, "flightCooldownEndTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned) return;

            var flightComp = pawn.TryGetComp<CompPegasusFlightToggle>();
            isFlying = flightComp?.FlightEnabled ?? false;

            if (_wasFlying && !isFlying)
                _flightCooldownEndTick = Find.TickManager.TicksGame + FlightCooldownTicks;
            _wasFlying = isFlying;

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
                        if (pawn.IsColonistPlayerControlled)
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
                    currentFlightStamina  = Mathf.Min(currentFlightStamina, 1f);
                }
            }

            if (pawn.IsHashIntervalTick(60))
                UpdateFatigueHediff(pawn);

            if (pawn.IsHashIntervalTick(150))
                ApplyExtraRestFall(pawn);
        }

        private void UpdateFatigueHediff(Pawn pawn)
        {
            if (FatigueDef == null || pawn?.health == null) return;

            float spent = 1f - currentFlightStamina;

            if (spent < 0.25f)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(FatigueDef);
                if (h != null) pawn.health.RemoveHediff(h);
                return;
            }

            Hediff fatigue = pawn.health.hediffSet.GetFirstHediffOfDef(FatigueDef);
            if (fatigue == null)
            {
                fatigue = HediffMaker.MakeHediff(FatigueDef, pawn);
                pawn.health.AddHediff(fatigue);
            }
            fatigue.Severity = spent;     
        }

        private void ApplyExtraRestFall(Pawn pawn)
        {
            if (pawn.needs?.rest == null) return;
            float spent = 1f - currentFlightStamina;
            float bonus =
                spent >= 0.75f ? 0.50f :
                spent >= 0.50f ? 0.35f :
                spent >= 0.25f ? 0.20f : 0f;
            if (bonus <= 0f) return;
            pawn.needs.rest.CurLevel =
                Mathf.Max(0f, pawn.needs.rest.CurLevel - BaseRestFallPerInterval * bonus);
        }

        public override string CompInspectStringExtra()
        {
            if (parent is not Pawn pawn || !pawn.HasWings())
                return null;
            if (pawn.Faction != Faction.OfPlayer)
                return null;

            int maxDuration = MaxFlightDurationTicks;
            if (maxDuration <= 0)
                return "Can't fly now.";

            float maxSeconds     = maxDuration / 60f;
            float currentSeconds = maxSeconds * currentFlightStamina;

            string mult = GetWingDrainMultiplier() < 1f
                ? $" (drain ×{GetWingDrainMultiplier():F2})"
                : "";
            return $"Flight stamina: {currentFlightStamina.ToStringPercent()} ({currentSeconds:F1}s / {maxSeconds:F1}s){mult}";
        }
    }

    public class Gizmo_FlightStamina : Gizmo
    {
        private readonly Pawn                   _pawn;
        private readonly CompPegasusFlightTimer _timer;

        private const float GizmoWidth  = 200f;
        private const float GizmoHeight = 75f;

        public Gizmo_FlightStamina(Pawn pawn, CompPegasusFlightTimer timer)
        {
            _pawn  = pawn;
            _timer = timer;
            Order  = -98f;      
        }

        public override float GetWidth(float maxWidth) => Mathf.Min(GizmoWidth, maxWidth);

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            float width = Mathf.Min(GizmoWidth, maxWidth);
            Rect  outer = new Rect(topLeft.x, topLeft.y, width, GizmoHeight);

            Widgets.DrawWindowBackground(outer);

            const float pad = 6f;
            Rect inner = outer.ContractedBy(pad);

            float pct = _timer.CurrentStaminaPercent;

            Rect titleR = new Rect(inner.x, inner.y, inner.width, 16f);
            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color   = Color.white;
            Widgets.Label(titleR, "Flight Stamina");

            Rect barBG = new Rect(inner.x, titleR.yMax + 3f, inner.width, 18f);

            var prevColor = GUI.color;
            GUI.color = new Color(0.1f, 0.1f, 0.1f);
            GUI.DrawTexture(barBG, BaseContent.WhiteTex);

            float fillW = (barBG.width - 2f) * pct;
            if (fillW > 0f)
            {
                GUI.color = GetBarColor(pct);
                GUI.DrawTexture(
                    new Rect(barBG.x + 1f, barBG.y + 1f, fillW, barBG.height - 2f),
                    BaseContent.WhiteTex);
            }
            GUI.color = prevColor;

            DrawMarker(barBG, 0.25f);
            DrawMarker(barBG, 0.50f);
            DrawMarker(barBG, 0.75f);

            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color   = Color.white;
            Widgets.Label(barBG, pct.ToStringPercent("F0"));

            Rect descR = new Rect(inner.x, barBG.yMax + 3f,
                                  inner.width, inner.yMax - barBG.yMax - 3f);
            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color   = GetTierColor(pct);
            Widgets.Label(descR, GetTierLabel(pct));

            GUI.color   = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font   = GameFont.Small;

            if (Mouse.IsOver(outer))
                TooltipHandler.TipRegion(outer, BuildTooltip());

            return new GizmoResult(GizmoState.Clear);
        }

        private static void DrawMarker(Rect bar, float threshold)
        {
            float x      = bar.x + bar.width * threshold;
            Rect  marker = new Rect(x - 0.5f, bar.y, 1f, bar.height);
            var   prev   = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(marker, BaseContent.WhiteTex);
            GUI.color = prev;
        }

        private static Color GetBarColor(float pct)
        {
            if (pct >= 0.75f) return new Color(0.22f, 0.78f, 0.22f);  
            if (pct >= 0.50f) return new Color(0.90f, 0.82f, 0.10f);  
            if (pct >= 0.25f) return new Color(0.90f, 0.50f, 0.10f);  
            return new Color(0.80f, 0.12f, 0.12f);                     
        }

        private static Color GetTierColor(float pct)
        {
            if (pct >= 0.75f) return Color.green;
            if (pct >= 0.50f) return Color.yellow;
            if (pct >= 0.25f) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }

        private static string GetTierLabel(float pct)
        {
            if (pct >= 0.75f) return "Safe flight";
            if (pct >= 0.50f) return "+20% sleep need";
            if (pct >= 0.25f) return "+35% sleep, -5% consciousness";
            if (pct > 0f)     return "+50% sleep, -15% consciousness";
            return "EXHAUSTED — flight disabled";
        }

        private static string BuildTooltip()
        {
            return
                "Flight Stamina\n" +
                "Shows remaining flight endurance.\n\n" +
                "Stamina zones:\n" +
                "  \u2265 75%  (0–25% spent)    Safe flight, no penalties\n" +
                "  50–75%   +20% sleep need rate\n" +
                "  25–50%   +35% sleep need,  −5% consciousness\n" +
                "  < 25%  +50% sleep need, −15% consciousness\n" +
                "  0%     Flight disabled until rested\n\n";
        }
    }
}