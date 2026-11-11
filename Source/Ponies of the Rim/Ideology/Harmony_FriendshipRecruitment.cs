using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Ideology
{
    internal static class Patch_FriendshipRecruitment
    {
        internal static void Prefix(Pawn __0, Faction __1, out bool __state)
        {
            __state = ModsConfig.IdeologyActive && FriendshipPrecepts.HasRecruitmentRules
                && __0 != null && __0.IsPrisonerOfColony
                && __0.Faction != Faction.OfPlayer && __1 == Faction.OfPlayer;
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(Pawn __0, bool __state)
        {
            if (!__state || __0 == null || __0.Dead || __0.IsPrisoner
                || __0.IsSlaveOfColony || __0.Faction != Faction.OfPlayer)
            {
                return;
            }

            try
            {
                FriendshipPrecepts.PrisonerRecruited(__0);
            }
            catch (Exception ex)
            {
                PonyLog.ErrorCaught("Ideology: prisoner recruitment effects not applied", ex);
            }
        }
    }

    internal static class Patch_FriendshipViolence
    {
        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(Thing __instance, DamageInfo __0, DamageWorker.DamageResult __result)
        {
            if (!(__instance is Pawn victim) || __result == null)
            {
                return;
            }

            try
            {
                FriendshipPrecepts.ViolenceCommitted(victim, __0, __result.totalDamageDealt);
            }
            catch (Exception ex)
            {
                PonyLog.ErrorCaught("Ideology: violence effects not applied", ex);
            }
        }
    }
}
