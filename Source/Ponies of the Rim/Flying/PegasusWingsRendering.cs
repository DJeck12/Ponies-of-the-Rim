
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
            if (!pawn.IsPegasus())
                return false;

            var comp = pawn.TryGetComp<CompPegasusFlightToggle>();
            if (comp?.FlightEnabled != true)
                return false;

            if (parms.Portrait)
                return false;

            return pawn.GetPosture() == PawnPosture.Standing;
        }
    }


            
    public class CompProperties_PonyAlienComp : CompProperties
    {
        public CompProperties_PonyAlienComp()
        {
            compClass = typeof(PonyAlienComp);
        }
    }

    public class PonyAlienComp : AlienPartGenerator.AlienComp
    {
        public override List<PawnRenderNode> CompRenderNodes()
        {
            List<PawnRenderNode> list = base.CompRenderNodes();
            Pawn pawn = parent as Pawn;

            if (pawn == null || !pawn.IsPegasus())
                return list;

            var tree = pawn.Drawer?.renderer?.renderTree;
            if (tree == null)
                return list;

            list ??= new List<PawnRenderNode>();

            var props = new PonyRenderNodePropertiesPegasusWings
            {
                tagDef = DefDatabase<PawnRenderNodeTagDef>.GetNamed("PegasusWings"),
                parentTagDef = PawnRenderNodeTagDefOf.Body,
                pawnType = PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly,
                useGraphic = true,
                framePathPrefix = "Races/Bodies/Wings/PegasusWings",
                frameCount = 8,
                ticksPerFrame = 3,
                subworkerClasses = new List<Type>
                {
                    typeof(PonyRenderSubWorkerPegasusWings)
                }
            };

            list.Add(new PonyRenderNodePegasusWings(pawn, props, tree));
            return list;
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