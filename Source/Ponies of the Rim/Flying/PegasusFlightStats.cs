
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
            if (pawn == null || !pawn.IsPegasus()) return 0f;

            float left  = WingFactor(diffSet, pawn, PegasusFlightUtil.LeftWingPartDef, impactors);
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
                    case "NaturalWing":           baseVal = 0.50f;  break;
                    case "SimpleProstheticWing":   baseVal = 0.35f;  break;
                    case "BionicWing":             baseVal = 0.625f; break;
                    case "ArchotechWing":          baseVal = 0.75f;  break;
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
            return p != null && p.IsPegasus();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            Pawn p = req.Thing as Pawn;
            if (p == null || !p.IsPegasus()) return 0f;

            PawnCapacityDef cap = DefDatabase<PawnCapacityDef>.GetNamedSilentFail("Pegasus_Flight");
            if (cap == null) return 0f;

            float eff = p.health.capacities.GetLevel(cap);
            return Mathf.RoundToInt(Mathf.Max(0f, BaseSeconds * eff));
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            Pawn p = req.Thing as Pawn;
            if (p == null || !p.IsPegasus()) return base.GetExplanationUnfinalized(req, numberSense);

            PawnCapacityDef cap = DefDatabase<PawnCapacityDef>.GetNamedSilentFail("Pegasus_Flight");
            float eff = (cap == null) ? 0f : p.health.capacities.GetLevel(cap);
            int seconds = Mathf.RoundToInt(Mathf.Max(0f, BaseSeconds * eff));

            var sb = new StringBuilder();
            sb.AppendLine($"Формула: {BaseSeconds:0.#}с × эффективность полёта (Pegasus_Flight)");
            sb.AppendLine($"Эффективность (Pegasus_Flight): {eff.ToStringPercent()}");
            sb.AppendLine($"Итог: {seconds}с");
            sb.AppendLine();
            sb.AppendLine("Из чего складывается эффективность:");
            sb.AppendLine(new StatWorker_FlightEfficiency().GetExplanationUnfinalized(req, numberSense));
            return sb.ToString();
        }
    }


            
    public class StatWorker_FlightEfficiency : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            if (!req.HasThing) return false;
            Pawn pawn = req.Thing as Pawn;
            return pawn != null && pawn.IsPegasus();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (!req.HasThing) return 0f;

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !pawn.IsPegasus()) return 0f;

            var flightCapacityDef = DefDatabase<PawnCapacityDef>.GetNamedSilentFail("Pegasus_Flight");
            if (flightCapacityDef == null) return 0f;

            return pawn.health.capacities.GetLevel(flightCapacityDef);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            if (!req.HasThing) return base.GetExplanationUnfinalized(req, numberSense);

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !pawn.IsPegasus())
                return base.GetExplanationUnfinalized(req, numberSense);

            var sb = new StringBuilder();
            sb.AppendLine("Flight efficiency is calculated from multiple factors:");
            sb.AppendLine();
            sb.AppendLine("Wings:");

            float leftWingBase = 0f, rightWingBase = 0f;
            string leftWingType = "Missing", rightWingType = "Missing";
            float leftWingHealth = 1f, rightWingHealth = 1f;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.Part == null) continue;
                string partDef = hediff.Part.def?.defName;

                if (partDef == PegasusFlightUtil.LeftWingPartDef)
                    ClassifyWing(hediff, pawn, ref leftWingBase, ref leftWingType, ref leftWingHealth);
                else if (partDef == PegasusFlightUtil.RightWingPartDef)
                    ClassifyWing(hediff, pawn, ref rightWingBase, ref rightWingType, ref rightWingHealth);
            }

            float leftFinal = leftWingBase * leftWingHealth;
            sb.AppendLine($"  Left wing ({leftWingType}): {(leftWingBase * 2f).ToStringPercent()} × {leftWingHealth.ToStringPercent()} health = {(leftFinal * 2f).ToStringPercent()}");

            float rightFinal = rightWingBase * rightWingHealth;
            sb.AppendLine($"  Right wing ({rightWingType}): {(rightWingBase * 2f).ToStringPercent()} × {rightWingHealth.ToStringPercent()} health = {(rightFinal * 2f).ToStringPercent()}");

            float totalWings = leftFinal + rightFinal;
            sb.AppendLine($"  Total wing capacity: {totalWings.ToStringPercent()}");
            sb.AppendLine();

            float conc = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            sb.AppendLine($"Consciousness: {conc.ToStringPercent()} (contributes 30% to final)");

            float multiplier = 0.7f + conc * 0.3f;
            float finalEff = totalWings * multiplier;
            sb.AppendLine($"Flight efficiency = {totalWings.ToStringPercent()} × {multiplier.ToStringPercent()} = {finalEff.ToStringPercent()}");

            return sb.ToString();
        }

        private static void ClassifyWing(Hediff hediff, Pawn pawn,
            ref float wingBase, ref string wingType, ref float wingHealth)
        {
            string defName = hediff.def?.defName;

            if (defName != null && PegasusFlightUtil.WingHediffDefs.Contains(defName))
            {
                switch (defName)
                {
                    case "NaturalWing":           wingBase = 0.50f;  wingType = "Natural";    break;
                    case "SimpleProstheticWing":   wingBase = 0.35f;  wingType = "Prosthetic"; break;
                    case "BionicWing":             wingBase = 0.625f; wingType = "Bionic";     break;
                    case "ArchotechWing":          wingBase = 0.75f;  wingType = "Archotech";  break;
                }
            }

            if (hediff is Hediff_Injury injury && hediff.Part != null)
            {
                float maxHealth = hediff.Part.def.GetMaxHealth(pawn);
                if (maxHealth > 0f)
                    wingHealth = Mathf.Max(0f, 1f - (injury.Severity / maxHealth));
            }
        }
    }
}