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

        public PonyRenderNodePegasusWings(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            wingsProps = (PonyRenderNodePropertiesPegasusWings)props;
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            yield return GetCurrentFrameGraphic(pawn);
        }

        public override Graphic GraphicFor(Pawn pawn) => GetCurrentFrameGraphic(pawn);

        private Graphic GetCurrentFrameGraphic(Pawn pawn)
        {
            if (wingsProps?.frameCount <= 0 || wingsProps.ticksPerFrame <= 0 || pawn == null)
                return BaseContent.BadGraphic;

            int frameIndex = (Find.TickManager.TicksGame / wingsProps.ticksPerFrame) % wingsProps.frameCount;

            return GraphicDatabase.Get<GraphicPegasusWingsColored>(
                $"{wingsProps.framePathPrefix}{frameIndex}",
                ShaderDatabase.CutoutSkin,
                Vector2.one,
                Color.white);
        }

        protected override bool EnsureInitializationWithoutRecache => true;
    }

    public class PonyRenderSubWorkerPegasusWings : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            Pawn pawn = parms.pawn;
            if (!pawn.HasWings())
                return false;

            var comp = pawn.TryGetComp<CompPegasusFlightToggle>();
            if (comp?.FlightEnabled != true)
                return false;

            if (parms.Portrait)
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
        public override List<PawnRenderNode> CompRenderNodes()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.HasWings())
                return null;

            var tree = pawn.Drawer?.renderer?.renderTree;
            if (tree == null)
                return null;

            var wingTagDef = DefDatabase<PawnRenderNodeTagDef>.GetNamed("PegasusWings", errorOnFail: false);
            if (wingTagDef == null)
            {
                Log.Warning("[PoniesOfTheRim] PawnRenderNodeTagDef 'PegasusWings' не найден — " +
                            "анимация крыльев отключена. Убедитесь, что def объявлен в XML.");
                return null;
            }

            var props = new PonyRenderNodePropertiesPegasusWings
            {
                tagDef         = wingTagDef,
                parentTagDef   = PawnRenderNodeTagDefOf.Body,
                pawnType       = PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly,
                useGraphic     = true,
                framePathPrefix = "Races/Bodies/Wings/PegasusWings",
                frameCount     = PegasusWingAnimation.FrameCount,
                ticksPerFrame  = PegasusWingAnimation.TicksPerFrame,
                subworkerClasses = new List<Type>
                {
                    typeof(PonyRenderSubWorkerPegasusWings)
                }
            };

            return new List<PawnRenderNode>
            {
                new PonyRenderNodePegasusWings(pawn, props, tree)
            };
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