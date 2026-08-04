using System;
using System.Collections.Generic;
using System.Xml;
using AlienRace;
using AlienRace.ExtendedGraphics;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public class GraphicPegasusWingsColored : Graphic_Multi
    {
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            var baseMat = base.MatAt(rot, thing);
            if (thing is not Pawn pawn)
                return baseMat;

            var alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp == null)
                return baseMat;

            var skinChannel = alienComp.GetChannel("skin");
            Color skinColor = skinChannel.first;

            if (skinColor == Color.clear || skinColor.a <= 0f)
                skinColor = pawn.story?.SkinColor ?? Color.white;

            Graphic coloredGraphic = this.GetColoredVersion(this.Shader, skinColor, this.ColorTwo);
            return coloredGraphic.MatAt(rot);
        }
    }

    public class PonyRenderNodePropertiesPegasusWings : PawnRenderNodeProperties
    {
        public string framePathPrefix;
        public int frameCount;
        public int ticksPerFrame;

        public PonyRenderNodePropertiesPegasusWings()
        {
            parentTagDef = PawnRenderNodeTagDefOf.Body;
            pawnType = RenderNodePawnType.HumanlikeOnly;
            useGraphic = true;
        }
    }

    public class PonyRenderNodePegasusWings : PawnRenderNode
    {
        private readonly PonyRenderNodePropertiesPegasusWings wingsProps;
        private Graphic[] _frames;
        private Color _framesColor;
        private int _lastRequestedFrame = -1;
        private AlienPartGenerator.AlienComp _alienComp;
        private bool _alienCompResolved;

        public PonyRenderNodePegasusWings(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            wingsProps = (PonyRenderNodePropertiesPegasusWings)props;
        }

        private int CurrentFrameIndex
        {
            get
            {
                int frameCount = wingsProps?.frameCount ?? 0;
                int ticksPerFrame = wingsProps?.ticksPerFrame ?? 0;
                if (frameCount <= 0 || ticksPerFrame <= 0)
                    return 0;
                return Find.TickManager.TicksGame / ticksPerFrame % frameCount;
            }
        }

        public void RequestRecacheIfFrameChanged()
        {
            int idx = CurrentFrameIndex;
            if (idx != _lastRequestedFrame)
            {
                _lastRequestedFrame = idx;
                requestRecache = true;
            }
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            yield return GraphicFor(pawn);
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (pawn == null || wingsProps == null || wingsProps.frameCount <= 0 || wingsProps.ticksPerFrame <= 0)
                return BaseContent.BadGraphic;
            EnsureFrames(pawn);
            return _frames[CurrentFrameIndex];
        }

        private void EnsureFrames(Pawn pawn)
        {
            Color color = ResolveSkinColor(pawn);
            if (_frames != null && _framesColor == color)
                return;

            int frameCount = wingsProps.frameCount;
            Graphic[] frames = (_frames != null && _frames.Length == frameCount) ? _frames : new Graphic[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                frames[i] = GraphicDatabase.Get<Graphic_Multi>(
                    wingsProps.framePathPrefix + i,
                    ShaderDatabase.CutoutSkin,
                    Vector2.one,
                    color);
            }
            _frames = frames;
            _framesColor = color;
        }

        private Color ResolveSkinColor(Pawn pawn)
        {
            if (!_alienCompResolved)
            {
                _alienCompResolved = true;
                _alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            }
            Color color = Color.clear;
            if (_alienComp != null)
            {
                var skinChannel = _alienComp.GetChannel("skin");
                if (skinChannel != null)
                    color = skinChannel.first;
            }
            if (color == Color.clear || color.a <= 0f)
                color = pawn.story?.SkinColor ?? Color.white;
            return color;
        }

        protected override bool EnsureInitializationWithoutRecache => true;
    }

    public class PonyRenderSubWorkerPegasusWings : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            if (parms.Portrait)
                return false;
            Pawn pawn = parms.pawn;
            if (pawn == null || !pawn.HasWings())
                return false;
            CompPegasusFlightToggle comp = PonyFlightCache.GetToggle(pawn);
            if (comp == null || !comp.FlightEnabled)
                return false;
            return pawn.GetPosture() == PawnPosture.Standing;
        }
    }


    public class CompProperties_PegasusWingsRenderer : CompProperties
    {
        public CompProperties_PegasusWingsRenderer()
        {
            compClass = typeof(CompPegasusWingsRenderer);
        }
    }

    public class CompPegasusWingsRenderer : ThingComp
    {
        private static PawnRenderNodeTagDef _wingTagDef;
        private static bool _wingTagDefResolved;
        private static bool _missingTagWarned;
        private PonyRenderNodePegasusWings _node;
        private bool _lastFlightEnabled;

        public override void CompTick()
        {
            base.CompTick();
            PonyRenderNodePegasusWings node = _node;
            if (node == null)
                return;
            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned)
                return;
            CompPegasusFlightToggle toggle = PonyFlightCache.GetToggle(pawn);
            bool flying = toggle != null && toggle.FlightEnabled;
            if (flying != _lastFlightEnabled)
            {
                _lastFlightEnabled = flying;
                node.requestRecache = true;
            }
            if (!flying)
                return;
            node.RequestRecacheIfFrameChanged();
        }

        public override List<PawnRenderNode> CompRenderNodes()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.HasWings())
                return null;

            var tree = pawn.Drawer?.renderer?.renderTree;
            if (tree == null)
                return null;

            if (!_wingTagDefResolved)
            {
                _wingTagDefResolved = true;
                _wingTagDef = DefDatabase<PawnRenderNodeTagDef>.GetNamed("PegasusWings", errorOnFail: false);
            }
            if (_wingTagDef == null)
            {
                if (!_missingTagWarned)
                {
                    _missingTagWarned = true;
                    Log.Warning("[PoniesOfTheRim] PawnRenderNodeTagDef 'PegasusWings' не найден — " +
                                "анимация крыльев отключена. Убедитесь, что def объявлен в XML.");
                }
                return null;
            }

            var props = new PonyRenderNodePropertiesPegasusWings
            {
                tagDef = _wingTagDef,
                parentTagDef = PawnRenderNodeTagDefOf.Body,
                pawnType = PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly,
                useGraphic = true,
                framePathPrefix = "Races/Bodies/Wings/PegasusWings",
                frameCount = PegasusWingAnimation.FrameCount,
                ticksPerFrame = PegasusWingAnimation.TicksPerFrame,
                subworkerClasses = new List<Type>
                {
                    typeof(PonyRenderSubWorkerPegasusWings)
                }
            };

            _node = new PonyRenderNodePegasusWings(pawn, props, tree);
            return new List<PawnRenderNode> { _node };
        }
    }


    public class ConditionPegasusFlightEnabled : Condition
    {
        public new const string XmlNameParseKey = "PegasusFlightEnabled";
        public bool enabled = true;

        public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot?.Attributes?["enabled"] != null)
                enabled = ParseHelper.ParseBool(xmlRoot.Attributes["enabled"].Value);
        }

        public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
        {
            var comp = pawn?.WrappedPawn?.TryGetComp<CompPegasusFlightToggle>();
            return (comp?.FlightEnabled ?? false) == enabled;
        }
    }
}