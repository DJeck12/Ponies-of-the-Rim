using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{
    public class StatPart_HiveMind : StatPart
    {
        private const float BONUS_SMALL_HIVE = 0.05f;
        private const float BONUS_LARGE_HIVE = 0.03f;
        private const int LARGE_HIVE_THRESHOLD = 12;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn)
            {
                float bonus = GetBonus(GetMemberCount(pawn));
                if (bonus > 0f)
                    val += bonus;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is Pawn pawn)
            {
                int count = GetMemberCount(pawn);
                float bonus = GetBonus(count);
                if (bonus > 0f)
                {
                    return "Pony_HiveMind_StatDesc"
                        .Translate(count) + ": +"
                        + bonus.ToStringPercent();
                }
            }
            return null;
        }

        private static float GetBonus(int count)
        {
            if (count <= 0) return 0f;

            int total = count + 1;
            float rate = total < LARGE_HIVE_THRESHOLD
                ? BONUS_SMALL_HIVE
                : BONUS_LARGE_HIVE;

            return count * rate;
        }

        private static int GetMemberCount(Pawn pawn)
        {
            if (ModsConfig.BiotechActive)
            {
                List<Gene> genes = pawn.genes?.GenesListForReading;
                if (genes == null)
                    return 0;

                for (int i = 0; i < genes.Count; i++)
                {
                    if (genes[i] is Gene_HiveMind hive && hive.Active)
                        return hive.CachedHiveMemberCount;
                }

                return 0;
            }

            if (pawn.Map != null
                && (pawn.IsChangeling() || pawn.IsChangedling()))
            {
                MapComponent_HiveMind comp = pawn.Map
                    .GetComponent<MapComponent_HiveMind>();
                if (comp != null)
                    return comp.GetHiveMemberCount(pawn);
            }

            return 0;
        }
    }
}