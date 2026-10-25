using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PoniesOfTheRim.Compatibility
{
	[StaticConstructorOnStartup]
	public static class CombatExtendedBootstrap
	{
		public static readonly Harmony Harmony = new Harmony("rimworld.poniesoftherim.combatextended");

		private static int _patched;
		private static int _failed;

		static CombatExtendedBootstrap()
		{
			if (!CombatExtendedCompatability.Active)
			{
				return;
			}

			CombatExtendedCompatability.BuildFlightExtensionCache();
			RegisterCollisionPatches();
			PonyLog.Trace($"CE: {_patched} patched, {_failed} failed");

			LongEventHandler.ExecuteWhenFinished(() =>
			{
				CombatExtendedCompatability.EnsureRaceComps();
				CombatExtendedCompatability.AuditRaces();
			});
		}

		private static void RegisterCollisionPatches()
		{
			TryPatch(AccessTools.Method("CombatExtended.CE_Utility:GetCollisionBodyFactors"),
				postfix: new HarmonyMethod(typeof(Patch_CE_GetCollisionBodyFactors), "Postfix"));

			var heightRange = Patch_CE_CollisionVerticalLift.TargetMethod();
			TryPatch(heightRange,
				prefix: new HarmonyMethod(typeof(Patch_CE_CollisionVerticalLift), "Prefix"),
				postfix: new HarmonyMethod(typeof(Patch_CE_CollisionVerticalLift), "Postfix"),
				finalizer: new HarmonyMethod(typeof(Patch_CE_CollisionVerticalLift), "Finalizer"));
			TryPatch(heightRange,
				postfix: new HarmonyMethod(typeof(Patch_CE_PonyCoverPeek), "Postfix"));

			TryPatch(Patch_CE_SuppressionOffMap.TargetMethod(),
				prefix: new HarmonyMethod(typeof(Patch_CE_SuppressionOffMap), "Prefix"));
		}

		private static void TryPatch(
			MethodBase original,
			HarmonyMethod prefix = null,
			HarmonyMethod postfix = null,
			HarmonyMethod finalizer = null)
		{
			if (original == null)
			{
				_failed++;
				PonyLog.Error($"CE: target of {Describe(prefix ?? postfix ?? finalizer)} not found, skipped");
				return;
			}
			try
			{
				Harmony.Patch(original, prefix, postfix, null, finalizer);
				_patched++;
			}
			catch (Exception ex)
			{
				_failed++;
				PonyLog.Error($"CE: {Describe(prefix ?? postfix ?? finalizer)} failed on {original.DeclaringType?.Name}.{original.Name}\n{ex}");
			}
		}

		private static string Describe(HarmonyMethod patch)
		{
			MethodInfo method = patch?.method;
			return method == null ? "?" : method.DeclaringType?.Name + "." + method.Name;
		}
	}
}