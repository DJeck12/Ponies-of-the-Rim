using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class PegasusWingAnimation
    {
        public const int FrameCount    = 8;
        public const int TicksPerFrame = 3;

        public const int CycleTicks = FrameCount * TicksPerFrame;  

        public static float Phase()
            => (float)(Find.TickManager.TicksGame % CycleTicks) / CycleTicks;

        public static float BodyBobZ(float amplitude = 0.012f)
            => -Mathf.Sin(Phase() * Mathf.PI * 2f) * amplitude;
    }
}