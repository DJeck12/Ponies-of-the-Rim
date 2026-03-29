using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            float bonus = GetBonus(pawn);
            if (bonus > 0f)
                val += bonus;
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (req.Thing is Pawn pawn)
        {
            float bonus = GetBonus(pawn);
            if (bonus > 0f)
            {
                int count = GetMemberCount(pawn);
                return "Pony_HiveMind_StatDesc"
                    .Translate(count) + ": +"
                    + bonus.ToStringPercent();
            }
        }
        return null;
    }

    private float GetBonus(Pawn pawn)
    {
        int count = GetMemberCount(pawn);
        if (count <= 0) return 0f;

        int total = count + 1;
        float rate = total < LARGE_HIVE_THRESHOLD
            ? BONUS_SMALL_HIVE
            : BONUS_LARGE_HIVE;

        return count * rate;
    }

    private int GetMemberCount(Pawn pawn)
    {
        if (ModsConfig.BiotechActive)
        {
            Gene_HiveMind gene = pawn.genes?.GenesListForReading
                .OfType<Gene_HiveMind>()
                .FirstOrDefault(g => g.Active);

            if (gene != null)
                return gene.CachedHiveMemberCount;
        }
        else
        {
            if (pawn.Map != null
                && (pawn.IsChangeling() || pawn.IsChangedling()))
            {
                var comp = pawn.Map
                    .GetComponent<MapComponent_HiveMind>();
                if (comp != null)
                    return comp.GetHiveMemberCount(pawn);
            }
        }

        return 0;
    }
}
}
