using System.Collections.Generic;
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

            float left = WingFactor(diffSet, pawn, PegasusFlightUtility.LeftWingPartDef, impactors);
            float right = WingFactor(diffSet, pawn, PegasusFlightUtility.RightWingPartDef, impactors);

            if (left <= 0f || right <= 0f)
            {
                impactors?.Add(new CapacityImpactorText("Крыло отсутствует (missing) → эффективность полёта = 0%"));
                return 0f;
            }

            float wings = left + right;

            float consciousness = CalculateCapacityAndRecord(diffSet, PawnCapacityDefOf.Consciousness, impactors);
            float result = wings * consciousness;

            impactors?.Add(new CapacityImpactorText($"Крылья суммарно: {wings.ToStringPercent()}"));
            impactors?.Add(new CapacityImpactorText($"× сознание: {consciousness.ToStringPercent()}"));
            impactors?.Add(new CapacityImpactorText($"Итоговая эффективность полёта: {result.ToStringPercent()}"));

            return Mathf.Clamp(result, 0f, 2f);
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            List<BodyPartRecord> allParts = body?.AllParts;
            if (allParts == null)
            {
                return false;
            }
            for (int i = 0; i < allParts.Count; i++)
            {
                string defName = allParts[i].def?.defName;
                if (defName == "Pony_LeftWing" || defName == "Pony_RightWing")
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsProstheticWing(Hediff wing)
        {
            return wing != null && (wing is Hediff_AddedPart || wing.def.countsAsAddedPartOrImplant);
        }

        internal static BodyPartRecord FindWingPart(Pawn pawn, string partDefName)
        {
            BodyDef body = pawn.RaceProps?.body;
            if (body == null) return null;

            if (PonyFlightCache.TryGetWingParts(body, out var wp))
            {
                return partDefName == PegasusFlightUtility.LeftWingPartDef ? wp.Left : wp.Right;
            }

            List<BodyPartRecord> parts = body.AllParts;
            if (parts == null) return null;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].def?.defName == partDefName)
                    return parts[i];
            }
            return null;
        }

        private static float WingFactor(HediffSet diffSet, Pawn pawn, string partDefName, List<PawnCapacityUtility.CapacityImpactor> impactors)
        {
            BodyPartRecord part = FindWingPart(pawn, partDefName);
            if (part == null || diffSet.PartIsMissing(part))
            {
                return 0f;
            }
            Hediff wing = PegasusFlightUtility.FindWingHediff(diffSet, part);

            if (IsProstheticWing(wing))
            {
                impactors?.Add(new PawnCapacityUtility.CapacityImpactorHediff { hediff = wing });
                return PegasusFlightUtility.GetWingEfficiency(wing.def);
            }

            impactors?.Add(new PawnCapacityUtility.CapacityImpactorBodyPartHealth { bodyPart = part });
            float partHealth = diffSet.GetPartHealth(part);
            float maxHealth = part.def.GetMaxHealth(pawn);
            float healthFrac = (maxHealth > 0f) ? Mathf.Clamp01(partHealth / maxHealth) : 1f;
            return 0.5f * healthFrac;
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
        private static StatDef _effStat;
        private static bool _effStatResolved;

        private static StatDef EffStat
        {
            get
            {
                if (!_effStatResolved)
                {
                    _effStatResolved = true;
                    _effStat = DefDatabase<StatDef>.GetNamed("Pegasus_FlightEfficiency", errorOnFail: false);
                }
                return _effStat;
            }
        }

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
            PawnCapacityDef cap = Pony_DefOf.Pegasus_Flight;
            if (cap == null) return 0f;

            float eff = p.health.capacities.GetLevel(cap);
            return Mathf.RoundToInt(Mathf.Max(0f, BaseSeconds * eff));
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            Pawn p = req.Thing as Pawn;
            if (p == null || !p.HasWings()) return base.GetExplanationUnfinalized(req, numberSense);

            PawnCapacityDef cap = Pony_DefOf.Pegasus_Flight;
            float eff = (cap == null) ? 0f : p.health.capacities.GetLevel(cap);
            int seconds = Mathf.RoundToInt(Mathf.Max(0f, BaseSeconds * eff));

            var sb = new StringBuilder();
            sb.AppendLine($"Формула: {BaseSeconds:0.#}с × эффективность полёта (Pegasus_Flight)");
            sb.AppendLine($"Эффективность (Pegasus_Flight): {eff.ToStringPercent()}");
            sb.AppendLine($"Итог: {seconds}с");
            sb.AppendLine();
            sb.AppendLine("Из чего складывается эффективность:");

            StatDef effStat = EffStat;
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
            var flightCapacityDef = Pony_DefOf.Pegasus_Flight;
            if (flightCapacityDef == null) return 0f;

            return pawn.health.capacities.GetLevel(flightCapacityDef);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || !pawn.HasWings())
            {
                return base.GetExplanationUnfinalized(req, numberSense);
            }

            HediffSet set = pawn.health.hediffSet;
            var sb = new StringBuilder();
            sb.AppendLine("Эффективность полёта = (левое крыло + правое крыло) × сознание");
            AppendWingLine(sb, set, pawn, PegasusFlightUtility.LeftWingPartDef, "Левое крыло");
            AppendWingLine(sb, set, pawn, PegasusFlightUtility.RightWingPartDef, "Правое крыло");

            float consciousness = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            sb.AppendLine("Сознание: " + consciousness.ToStringPercent());

            PawnCapacityDef flightCap = Pony_DefOf.Pegasus_Flight;
            float total = ((flightCap == null) ? 0f : pawn.health.capacities.GetLevel(flightCap));
            sb.AppendLine("Итог: " + total.ToStringPercent());

            return sb.ToString();
        }

        private static void AppendWingLine(StringBuilder sb, HediffSet set, Pawn pawn, string partDefName, string label)
        {
            BodyPartRecord part = PawnCapacityWorker_Pegasus_Flight.FindWingPart(pawn, partDefName);
            if (part == null || set.PartIsMissing(part))
            {
                sb.AppendLine("  " + label + ": отсутствует → 0%");
                return;
            }
            Hediff wing = PegasusFlightUtility.FindWingHediff(set, part);
            if (PawnCapacityWorker_Pegasus_Flight.IsProstheticWing(wing))
            {
                string type = PegasusFlightUtility.GetWingTypeLabel(wing.def);
                float eff = PegasusFlightUtility.GetWingEfficiency(wing.def);
                sb.AppendLine("  " + label + " (" + type + "): протез — фиксированные " + eff.ToStringPercent() + ", урон не влияет");
                return;
            }
            float partHealth = set.GetPartHealth(part);
            float maxHealth = part.def.GetMaxHealth(pawn);
            float frac = ((maxHealth > 0f) ? Mathf.Clamp01(partHealth / maxHealth) : 1f);
            sb.AppendLine("  " + label + " (Natural): 50% × " + frac.ToStringPercent() + " здоровья = " + (0.5f * frac).ToStringPercent());
        }
    }
}