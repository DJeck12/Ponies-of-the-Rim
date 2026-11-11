using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Ideology
{
    internal static class FriendshipPrecepts
    {
        private sealed class Rule
        {
            public PreceptDef precept;
            public FriendshipPreceptExtension settings;
        }

        private static readonly List<Rule> recruitmentRules = new List<Rule>();
        private static readonly List<Rule> violenceRules = new List<Rule>();

        internal static bool HasRecruitmentRules => recruitmentRules.Count != 0;
        internal static bool HasViolenceRules => violenceRules.Count != 0;

        internal static void BuildCaches()
        {
            recruitmentRules.Clear();
            violenceRules.Clear();

            List<PreceptDef> precepts = DefDatabase<PreceptDef>.AllDefsListForReading;
            for (int i = 0; i < precepts.Count; i++)
            {
                PreceptDef precept = precepts[i];
                FriendshipPreceptExtension settings = precept.GetModExtension<FriendshipPreceptExtension>();
                if (settings == null)
                {
                    continue;
                }

                Rule rule = new Rule { precept = precept, settings = settings };
                if (settings.recruitedPrisonerSocialThought != null)
                {
                    recruitmentRules.Add(rule);
                }

                if (settings.violenceMoodThought != null || settings.violenceSocialThought != null)
                {
                    violenceRules.Add(rule);
                }
            }

            PonyLog.Trace($"Ideology: {recruitmentRules.Count} recruitment rules, {violenceRules.Count} violence rules");
        }

        internal static bool IsPerson(Pawn pawn)
        {
            return pawn != null && pawn.RaceProps?.Humanlike == true
                && !pawn.RaceProps.IsMechanoid && !pawn.IsSubhuman && !pawn.IsGhoul;
        }

        private static bool IsPlayerPawn(Pawn pawn)
        {
            return IsPerson(pawn) && !pawn.Dead && !pawn.IsPrisoner
                && (pawn.IsColonist || pawn.IsSlaveOfColony);
        }

        internal static void PrisonerRecruited(Pawn recruited)
        {
            if (!ModsConfig.IdeologyActive || !IsPlayerPawn(recruited))
            {
                return;
            }

            Ideo colonyIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (colonyIdeo == null)
            {
                return;
            }

            for (int i = 0; i < recruitmentRules.Count; i++)
            {
                Rule rule = recruitmentRules[i];
                if (!colonyIdeo.HasPrecept(rule.precept))
                {
                    continue;
                }

                GiveSocialMemoryToPlayerPawns(rule.settings.recruitedPrisonerSocialThought, recruited);
            }
        }

        internal static void ViolenceCommitted(Pawn victim, DamageInfo damage, float damageDealt)
        {
            if (!ModsConfig.IdeologyActive || !HasViolenceRules || damageDealt <= 0f
                || damage.Def == null || !damage.Def.ExternalViolenceFor(victim)
                || !damage.Def.harmsHealth || !IsPerson(victim))
            {
                return;
            }

            Pawn attacker = damage.Instigator as Pawn;
            if (attacker == null)
            {
                attacker = damage.Instigator?.TryGetComp<CompMannable>()?.ManningPawn;
            }

            if (!IsPerson(attacker) || attacker == victim || attacker.Dead || attacker.Ideo == null)
            {
                return;
            }

            for (int i = 0; i < violenceRules.Count; i++)
            {
                Rule rule = violenceRules[i];
                if (!attacker.Ideo.HasPrecept(rule.precept))
                {
                    continue;
                }

                GainOrRenewMemory(attacker, rule.settings.violenceMoodThought, null);
                GiveSocialMemoryToPlayerPawns(rule.settings.violenceSocialThought, attacker);
            }
        }

        private static void GiveSocialMemoryToPlayerPawns(ThoughtDef thought, Pawn subject)
        {
            if (thought == null)
            {
                return;
            }
            List<Pawn> observers = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
            for (int i = 0; i < observers.Count; i++)
            {
                Pawn observer = observers[i];
                if (observer != subject && IsPlayerPawn(observer))
                {
                    GainOrRenewMemory(observer, thought, subject);
                }
            }
        }

        private static void GainOrRenewMemory(Pawn pawn, ThoughtDef thought, Pawn otherPawn)
        {
            if (thought == null)
            {
                return;
            }

            try
            {
                MemoryThoughtHandler memories = pawn.needs?.mood?.thoughts?.memories;
                if (memories == null || !ThoughtUtility.CanGetThought(pawn, thought))
                {
                    return;
                }
                List<Thought_Memory> existing = memories.Memories;
                for (int i = 0; i < existing.Count; i++)
                {
                    Thought_Memory memory = existing[i];
                    if (memory.def == thought && memory.otherPawn == otherPawn)
                    {
                        memory.Renew();
                        return;
                    }
                }
                memories.TryGainMemory(thought, otherPawn);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught($"Ideology: memory {thought.defName} not applied to {pawn?.LabelShortCap ?? "?"}", ex);
            }
        }
    }
}
