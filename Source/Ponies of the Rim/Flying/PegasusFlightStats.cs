using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public class PawnCapacityWorker_Pegasus_Flight : PawnCapacityWorker
    {
        public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            Pawn pawn = diffSet?.pawn;
            if (pawn == null || !pawn.HasWings()) return 0f;

            float left  = WingFactor(diffSet, pawn, PegasusFlightUtil.LeftWingPartDef,  impactors);
            float right = WingFactor(diffSet, pawn, PegasusFlightUtil.RightWingPartDef, impactors);

            if (left <= 0f || right <= 0f)
            {
                impactors?.Add(new CapacityImpactorText("Крыло отсутствует (missing) → эффективность полёта = 0%"));
                return 0f;
            }

            float wings = left + right;

            float conc = CalculateCapacityAndRecord(diffSet, PawnCapacityDefOf.Consciousness, impactors);
            float mult = 0.7f + 0.3f * conc;
            impactors?.Add(new CapacityImpactorText(
                $"Множитель: 0.7 + 0.3×сознание = {mult.ToStringPercent()}"));

            float eff = wings * mult;
            impactors?.Add(new CapacityImpactorText($"Крылья суммарно: {wings.ToStringPercent()}"));
            impactors?.Add(new CapacityImpactorText($"Итоговая эффективность полёта: {eff.ToStringPercent()}"));

            return Mathf.Clamp(eff, 0f, 2f);
        }

        private static float WingFactor(
            HediffSet diffSet, Pawn pawn, string partDefName,
            List<PawnCapacityUtility.CapacityImpactor> impactors)
        {
            BodyPartRecord part = pawn.RaceProps?.body?.AllParts?
                .FirstOrDefault(p => p.def?.defName == partDefName);

            if (part == null || diffSet.PartIsMissing(part))
            {
                impactors?.Add(new CapacityImpactorText($"Крыло не найдено: {partDefName} => 0%"));
                return 0f;
            }

            impactors?.Add(new PawnCapacityUtility.CapacityImpactorBodyPartHealth { bodyPart = part });

            Hediff wing = diffSet.hediffs.FirstOrDefault(h =>
                h.Part == part && h.def != null && PegasusFlightUtil.WingHediffDefs.Contains(h.def.defName));

            if (wing != null)
                impactors?.Add(new PawnCapacityUtility.CapacityImpactorHediff { hediff = wing });

            float baseVal = 0.50f;
            if (wing != null)
            {
                switch (wing.def.defName)
                {
                    case "Pony_NaturalWing":          baseVal = 0.50f;  break;
                    case "Pony_SimpleProstheticWing":  baseVal = 0.35f;  break;
                    case "Pony_BionicWing":            baseVal = 0.625f; break;
                    case "Pony_ArchotechWing":         baseVal = 0.75f;  break;
                }
            }

            float hp  = diffSet.GetPartHealth(part);
            float max = part.def.GetMaxHealth(pawn);
            float healthFrac = (max > 0f) ? Mathf.Clamp01(hp / max) : 1f;

            return baseVal * healthFrac;
        }

        private class CapacityImpactorText : PawnCapacityUtility.CapacityImpactor
        {
            private readonly string text;
            public CapacityImpactorText(string text) => this.text = text;
            public override string Readable(Pawn pawn) => text;
        }
    }


    public class StatWorker_FlightDurationSeconds : StatWorker
    {
        private const float BaseSeconds = 10f;

        public override bool ShouldShowFor(StatRequest req)
        {
            if (!req.HasThing) return false;
            Pawn p = req.Thing as Pawn;
            return p != null && p.HasWings();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            Pawn p = req.Thing as Pawn;
            if (p == null || !p.HasWings()) return 0f;

            PawnCapacityDef cap = DefDatabase<PawnCapacityDef>.GetNamed("Pegasus_Flight", errorOnFail: false);
            if (cap == null) return 0f;

            float eff = p.health.capacities.GetLevel(cap);
            return Mathf.RoundToInt(Mathf.Max(0f, BaseSeconds * eff));
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            Pawn p = req.Thing as Pawn;
            if (p == null || !p.HasWings()) return base.GetExplanationUnfinalized(req, numberSense);

            PawnCapacityDef cap = DefDatabase<PawnCapacityDef>.GetNamed("Pegasus_Flight", errorOnFail: false);
            float eff = (cap == null) ? 0f : p.health.capacities.GetLevel(cap);
            int seconds = Mathf.RoundToInt(Mathf.Max(0f, BaseSeconds * eff));

            var sb = new StringBuilder();
            sb.AppendLine($"Формула: {BaseSeconds:0.#}с × эффективность полёта (Pegasus_Flight)");
            sb.AppendLine($"Эффективность (Pegasus_Flight): {eff.ToStringPercent()}");
            sb.AppendLine($"Итог: {seconds}с");
            sb.AppendLine();
            sb.AppendLine("Из чего складывается эффективность:");

            StatDef effStat = DefDatabase<StatDef>.GetNamed("Pegasus_FlightEfficiency", errorOnFail: false);
            if (effStat?.Worker != null)
                sb.AppendLine(effStat.Worker.GetExplanationUnfinalized(req, numberSense));

            return sb.ToString();
        }
    }


    public class StatWorker_FlightEfficiency : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            if (!req.HasThing) return false;
            Pawn pawn = req.Thing as Pawn;
            return pawn != null && pawn.HasWings();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (!req.HasThing) return 0f;

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !pawn.HasWings()) return 0f;

            var flightCapacityDef = DefDatabase<PawnCapacityDef>.GetNamed("Pegasus_Flight", errorOnFail: false);
            if (flightCapacityDef == null) return 0f;

            return pawn.health.capacities.GetLevel(flightCapacityDef);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            if (!req.HasThing) return base.GetExplanationUnfinalized(req, numberSense);

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !pawn.HasWings())
                return base.GetExplanationUnfinalized(req, numberSense);

            HediffSet hediffSet = pawn.health.hediffSet;
            var allParts = pawn.RaceProps?.body?.AllParts;

            BodyPartRecord leftPart  = allParts?.FirstOrDefault(p => p.def?.defName == PegasusFlightUtil.LeftWingPartDef);
            BodyPartRecord rightPart = allParts?.FirstOrDefault(p => p.def?.defName == PegasusFlightUtil.RightWingPartDef);

            (float leftBase, string leftType)   = GetWingBase(hediffSet, leftPart);
            (float rightBase, string rightType) = GetWingBase(hediffSet, rightPart);

            float leftHealth  = GetPartHealthFrac(hediffSet, leftPart,  pawn);
            float rightHealth = GetPartHealthFrac(hediffSet, rightPart, pawn);

            float leftFinal  = leftBase  * leftHealth;
            float rightFinal = rightBase * rightHealth;
            float totalWings = leftFinal + rightFinal;

            var sb = new StringBuilder();
            sb.AppendLine("Flight efficiency is calculated from multiple factors:");
            sb.AppendLine();
            sb.AppendLine("Pony_Wings:");
            sb.AppendLine($"  Pony_Left_Wing ({leftType}): {(leftBase * 2f).ToStringPercent()} × {leftHealth.ToStringPercent()} health = {(leftFinal * 2f).ToStringPercent()}");
            sb.AppendLine($"  Pony_Right_Wing ({rightType}): {(rightBase * 2f).ToStringPercent()} × {rightHealth.ToStringPercent()} health = {(rightFinal * 2f).ToStringPercent()}");
            sb.AppendLine($"  Total wing capacity: {totalWings.ToStringPercent()}");
            sb.AppendLine();

            float conc = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            sb.AppendLine($"Consciousness: {conc.ToStringPercent()} (contributes 30% to final)");

            float multiplier = 0.7f + conc * 0.3f;
            float finalEff = totalWings * multiplier;
            sb.AppendLine($"Flight efficiency = {totalWings.ToStringPercent()} × {multiplier.ToStringPercent()} = {finalEff.ToStringPercent()}");

            return sb.ToString();
        }

        private static (float baseVal, string typeName) GetWingBase(HediffSet diffSet, BodyPartRecord part)
        {
            if (part == null || diffSet.PartIsMissing(part))
                return (0f, "Missing");

            foreach (var h in diffSet.hediffs)
            {
                if (h.Part != part || h.def == null) continue;
                switch (h.def.defName)
                {
                    case "Pony_NaturalWing":          return (0.50f,  "Natural");
                    case "Pony_SimpleProstheticWing":  return (0.35f,  "Prosthetic");
                    case "Pony_BionicWing":            return (0.625f, "Bionic");
                    case "Pony_ArchotechWing":         return (0.75f,  "Archotech");
                }
            }

            return (0.50f, "Natural");
        }

        private static float GetPartHealthFrac(HediffSet diffSet, BodyPartRecord part, Pawn pawn)
        {
            if (part == null) return 0f;
            float max = part.def.GetMaxHealth(pawn);
            if (max <= 0f) return 1f;
            return Mathf.Clamp01(diffSet.GetPartHealth(part) / max);
        }
    }
}