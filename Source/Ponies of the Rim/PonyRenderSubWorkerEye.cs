using System.Collections.Generic;
using Verse;
using RimWorld;

namespace PoniesOfTheRim
{
    public class PonyRenderSubWorkerEye : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            if (PonyHelper.IsPony(parms.pawn))
            {
                return false;
            }
            return true;
        }
    }

    public class NoPawnRenderSubWorkerEye : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!PonyHelper.IsPony(parms.pawn))
            {
                return false;
            }
            return true;
        }
    }

    public class PonyRenderNodeProperties_Eye : PawnRenderNodeProperties
    {
        public PonyRenderNodeProperties_Eye()
        {
            visibleFacing = new List<Rot4>
        {
            Rot4.East,
            Rot4.South,
            Rot4.West
        };
            workerClass = typeof(PawnRenderNodeWorker_Eye);
            nodeClass = typeof(PawnRenderNode_AttachmentHead);
        }

        public override void ResolveReferences()
        {
            skipFlag = RenderSkipFlagDefOf.Eyes;
        }
    }
}
