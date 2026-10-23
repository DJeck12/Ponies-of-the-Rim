using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PoniesOfTheRim.Compatibility
{
    public static class CombatExtendedLoadoutDiagnostics
    {
        private static bool _resolved;

        private static Type _tLoadoutExt;
        private static Type _tCompInventory;
        private static Type _tCompAmmoUser;
        private static Type _tAmmoThing;

        private static MethodInfo _miAvailableWeight;
        private static MethodInfo _miAvailableBulk;
        private static MethodInfo _miUseAmmo;

        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            _tLoadoutExt = AccessTools.TypeByName("CombatExtended.LoadoutPropertiesExtension");
            _tCompInventory = AccessTools.TypeByName("CombatExtended.CompInventory");
            _tCompAmmoUser = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
            _tAmmoThing = AccessTools.TypeByName("CombatExtended.AmmoThing");

            if (_tCompInventory != null)
            {
                _miAvailableWeight = AccessTools.Method(_tCompInventory, "GetAvailableWeight");
                _miAvailableBulk = AccessTools.Method(_tCompInventory, "GetAvailableBulk");
            }

            if (_tCompAmmoUser != null)
            {
                _miUseAmmo = AccessTools.PropertyGetter(_tCompAmmoUser, "UseAmmo");
            }
        }

        public static void Report(Pawn pawn, PawnKindDef kindToInspect, string context)
        {
            if (!PonyLog.Verbose || pawn == null || !CombatExtendedCompatability.Active)
            {
                return;
            }

            try
            {
                Resolve();
                PonyLog.Trace(Build(pawn, kindToInspect, context));
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("CE loadout: report failed", ex);
            }
        }

        private static string Build(Pawn pawn, PawnKindDef kindToInspect, string context)
        {
            bool hasExt = _tLoadoutExt != null && kindToInspect?.modExtensions != null
                && kindToInspect.modExtensions.Any(e => _tLoadoutExt.IsInstanceOfType(e));
            bool manipulation = pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false;
            bool violenceDisabled = pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool toolUser = pawn.RaceProps?.ToolUser ?? false;
            ThingComp inventoryComp = FindComp(pawn, _tCompInventory);
            ThingWithComps primary = pawn.equipment?.Primary;

            StringBuilder sb = new StringBuilder();
            sb.Append("CE loadout: ").Append(pawn.LabelShortCap)
              .Append(" (").Append(context).Append("), kind ").Append(kindToInspect?.defName ?? "none");

            sb.Append("\n  Loadout extension ").Append(YesNo(hasExt))
              .Append(", manipulation ").Append(YesNo(manipulation))
              .Append(", violence ").Append(violenceDisabled ? "disabled" : "allowed")
              .Append(", tool user ").Append(YesNo(toolUser))
              .Append(", CompInventory ").Append(YesNo(inventoryComp != null));

            sb.Append("\n  Weapon ").Append(primary?.def?.defName ?? "none");
            if (primary != null)
            {
                ThingComp ammoUser = FindComp(primary, _tCompAmmoUser);
                sb.Append(", CompAmmoUser ")
                  .Append(ammoUser == null ? "no" : "yes, UseAmmo " + Describe(SafeInvoke(_miUseAmmo, ammoUser)));
            }
            if (inventoryComp != null)
            {
                sb.Append(", free weight ").Append(Describe(SafeInvoke(_miAvailableWeight, inventoryComp)))
                  .Append(", bulk ").Append(Describe(SafeInvoke(_miAvailableBulk, inventoryComp)));
            }

            sb.Append("\n  Inventory: ").Append(DescribeInventory(pawn));
            return sb.ToString();
        }

        private static string YesNo(bool value)
        {
            return value ? "yes" : "no";
        }

        private static object SafeInvoke(MethodInfo method, object instance)
        {
            if (method == null)
            {
                return null;
            }

            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.HasDefaultValue)
                {
                    args[i] = p.DefaultValue;
                }
                else if (p.ParameterType.IsValueType)
                {
                    args[i] = Activator.CreateInstance(p.ParameterType);
                }
                else
                {
                    args[i] = null;
                }
            }

            return method.Invoke(instance, args);
        }

        private static string Describe(object value)
        {
            return value?.ToString() ?? "?";
        }

        private static ThingComp FindComp(ThingWithComps thing, Type compType)
        {
            if (thing?.AllComps == null || compType == null)
            {
                return null;
            }

            for (int i = 0; i < thing.AllComps.Count; i++)
            {
                if (compType.IsInstanceOfType(thing.AllComps[i]))
                {
                    return thing.AllComps[i];
                }
            }

            return null;
        }

        private static string DescribeInventory(Pawn pawn)
        {
            ThingOwner<Thing> container = pawn.inventory?.innerContainer;
            if (container == null || container.Count == 0)
            {
                return "empty";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < container.Count; i++)
            {
                Thing t = container[i];
                bool isAmmo = _tAmmoThing != null && _tAmmoThing.IsInstanceOfType(t);
                parts.Add((isAmmo ? "[ammo] " : "") + t.def.defName + " x" + t.stackCount);
            }

            return string.Join(", ", parts);
        }
    }

    public static class CombatExtendedAbilityDiagnostics
    {
        private const int WatchTicks = 180;
        private const int CallerFrames = 8;

        private static MethodInfo _miCanHitTargetFrom;
        private static FieldInfo _fiJobAbility;
        private static Pawn _watched;
        private static int _watchUntil = -1;

        public static void Register(Harmony harmony)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Type verbType = AccessTools.TypeByName("CombatExtended.Verb_AbilityShootCE");
            if (verbType == null)
            {
                PonyLog.Trace("CE ability: Verb_AbilityShootCE not found, diagnostics off");
                return;
            }

            PonyLog.Trace("CE ability: " + DescribeVerbType(verbType));

            _miCanHitTargetFrom = AccessTools.Method(verbType, "CanHitTargetFrom",
                new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(string).MakeByRefType() });
            _fiJobAbility = AccessTools.Field(typeof(Job), "ability");

            Patch(harmony, FindMethod(verbType, "OrderForceTarget", 1), nameof(OrderForceTarget_Postfix), false);
            Patch(harmony, FindMethod(verbType, "ValidateTarget", 2), nameof(ValidateTarget_Postfix), false);
            Patch(harmony, FindMethod(verbType, "TryStartCastOn", 2), nameof(TryStartCastOn_Postfix), false);
            Patch(harmony, AccessTools.Method(verbType, "TryCastShot", Type.EmptyTypes), nameof(TryCastShot_Postfix), false);
            Patch(harmony, AccessTools.Method(typeof(Ability), "QueueCastingJob", new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                nameof(QueueCastingJob_Prefix), true);
            Patch(harmony, AccessTools.GetDeclaredMethods(typeof(Pawn_JobTracker)).FirstOrDefault(m =>
                    m.Name == "StartJob" && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(Job)),
                nameof(StartJob_Prefix), true);
            Patch(harmony, AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob),
                    new[] { typeof(JobCondition), typeof(bool), typeof(bool) }),
                nameof(EndCurrentJob_Prefix), true);
        }

        private static string DescribeVerbType(Type type)
        {
            List<string> chain = new List<string>();
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                chain.Add(t.Name);
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            string[] watched = { "OrderForceTarget", "ValidateTarget", "TryStartCastOn", "TryCastShot", "Available", "CanHitTargetFrom" };
            List<string> declared = watched.Where(name => type.GetMethods(flags).Any(m => m.Name == name)).ToList();

            return $"verb type {type.FullName} in {type.Assembly.GetName().Name}: {string.Join(" : ", chain)}; " +
                   $"declares {(declared.Count == 0 ? "none of the traced methods" : string.Join(", ", declared))}";
        }

        private static MethodInfo FindMethod(Type type, string name, int minParams)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type t = type; t != null; t = t.BaseType)
            {
                foreach (MethodInfo method in t.GetMethods(flags))
                {
                    if (method.Name != name)
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < minParams || parameters[0].ParameterType != typeof(LocalTargetInfo))
                    {
                        continue;
                    }

                    if (name == "TryStartCastOn" && parameters[1].ParameterType != typeof(LocalTargetInfo))
                    {
                        continue;
                    }

                    if (name == "ValidateTarget" && parameters[1].ParameterType != typeof(bool))
                    {
                        continue;
                    }

                    if (name == "OrderForceTarget" && parameters.Length != 1)
                    {
                        continue;
                    }

                    return method;
                }
            }
            return null;
        }

        private static void Patch(Harmony harmony, MethodInfo target, string patchName, bool prefix)
        {
            if (target == null)
            {
                PonyLog.Trace($"CE ability: target for {patchName} not found, skipped");
                return;
            }

            try
            {
                HarmonyMethod patch = new HarmonyMethod(typeof(CombatExtendedAbilityDiagnostics), patchName);
                harmony.Patch(target, prefix: prefix ? patch : null, postfix: prefix ? null : patch);
            }
            catch (Exception ex)
            {
                PonyLog.Trace($"CE ability: {patchName} failed on {target.DeclaringType?.Name}.{target.Name} ({ex.Message})");
            }
        }

        public static void OrderForceTarget_Postfix(Verb __instance, LocalTargetInfo __0)
        {
            Ability ability = PonyAbility(__instance);
            if (ability == null)
            {
                return;
            }

            Watch(ability.pawn);
            Job job = ability.pawn.CurJob;
            PonyLog.Trace($"CE ability: {ability.pawn.LabelShort} ordered {ability.def.defName} at {Describe(__0)} " +
                          $"via {__instance.GetType().Name}.OrderForceTarget, current job {DescribeJob(job)}");
        }

        public static void QueueCastingJob_Prefix(Ability __instance, LocalTargetInfo __0)
        {
            if (PonyAbility(__instance?.verb) == null)
            {
                return;
            }

            Watch(__instance.pawn);
            PonyLog.Trace($"CE ability: {__instance.def.defName}.QueueCastingJob at {Describe(__0)}, " +
                          $"can queue {AskBool(__instance, "CanQueueCast")}, can apply {AskCanApplyOn(__instance, __0)}");
        }

        public static void ValidateTarget_Postfix(Verb __instance, LocalTargetInfo __0, bool __1, bool __result)
        {
            if (__result || !__1)
            {
                return;
            }

            Ability ability = PonyAbility(__instance);
            if (ability != null)
            {
                PonyLog.Trace($"CE ability: {ability.def.defName} rejected {Describe(__0)}: {Reasons(__instance, ability, __0)}");
            }
        }

        public static void TryStartCastOn_Postfix(Verb __instance, LocalTargetInfo __0, bool __result)
        {
            Ability ability = PonyAbility(__instance);
            if (ability == null)
            {
                return;
            }

            if (__result)
            {
                PonyLog.Trace($"CE ability: {ability.def.defName} started on {Describe(__0)}");
            }
            else
            {
                PonyLog.Trace($"CE ability: {ability.def.defName} did not start on {Describe(__0)}: {Reasons(__instance, ability, __0)}");
            }
        }

        public static void TryCastShot_Postfix(Verb __instance, bool __result)
        {
            Ability ability = PonyAbility(__instance);
            if (ability != null)
            {
                PonyLog.Trace($"CE ability: {ability.def.defName} at {Describe(__instance.CurrentTarget)} {(__result ? "fired" : "did not fire")}");
            }
        }

        public static void StartJob_Prefix(Job __0, Pawn ___pawn)
        {
            if (Watching(___pawn))
            {
                PonyLog.Trace($"CE ability: {___pawn.LabelShort} starts {DescribeJob(__0)}, called from {Callers()}");
            }
        }

        public static void EndCurrentJob_Prefix(Pawn_JobTracker __instance, JobCondition __0, Pawn ___pawn)
        {
            Job job = __instance.curJob;
            if (!Watching(___pawn) && PonyAbility(job?.verbToUse) == null)
            {
                return;
            }

            string callers = __0 == JobCondition.InterruptForced || __0 == JobCondition.Incompletable ? ", called from " + Callers() : "";
            PonyLog.Trace($"CE ability: {___pawn?.LabelShort}'s {DescribeJob(job)} ended: {__0}{callers}");
        }

        private static void Watch(Pawn pawn)
        {
            _watched = pawn;
            _watchUntil = Find.TickManager.TicksGame + WatchTicks;
        }

        private static bool Watching(Pawn pawn)
        {
            return pawn != null && pawn == _watched && Find.TickManager != null && Find.TickManager.TicksGame <= _watchUntil;
        }

        private static Ability PonyAbility(Verb verb)
        {
            if (!(verb?.verbTracker?.directOwner is Ability ability) || ability.pawn == null || !ability.pawn.IsPony())
            {
                return null;
            }
            string ns = verb.GetType().Namespace;
            return ns != null && ns.StartsWith("CombatExtended", StringComparison.Ordinal) ? ability : null;
        }

        private static string DescribeJob(Job job)
        {
            if (job == null)
            {
                return "no job";
            }

            List<string> parts = new List<string> { job.def?.defName ?? "?" };
            if (job.verbToUse != null)
            {
                Ability owner = job.verbToUse.verbTracker?.directOwner as Ability;
                parts.Add("verb " + job.verbToUse.GetType().Name + (owner != null ? " of " + owner.def.defName : ""));
            }
            if (_fiJobAbility?.GetValue(job) is Ability ability)
            {
                parts.Add("ability " + ability.def.defName);
            }
            return string.Join(", ", parts);
        }

        private static string Callers()
        {
            System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace(2, false).GetFrames();
            if (frames == null)
            {
                return "?";
            }

            List<string> names = new List<string>();
            foreach (System.Diagnostics.StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();
                if (method == null || method.DeclaringType == typeof(CombatExtendedAbilityDiagnostics))
                {
                    continue;
                }

                names.Add((method.DeclaringType?.Name ?? "?") + "." + method.Name);
                if (names.Count >= CallerFrames)
                {
                    break;
                }
            }
            return string.Join(" < ", names);
        }

        private static string Reasons(Verb verb, Ability ability, LocalTargetInfo target)
        {
            List<string> parts = new List<string>();
            Thing caster = verb.caster;

            parts.Add(caster == null ? "no caster" : caster.Spawned ? "spawned" : "not spawned");
            parts.Add("state " + verb.state);
            parts.Add("available " + (verb.Available() ? "yes" : "no"));
            parts.Add("can cast " + AskBool(ability, "CanCast"));

            if (caster != null && caster.Spawned && target.IsValid)
            {
                parts.Add($"distance {caster.Position.DistanceTo(target.Cell):0.0} of {verb.EffectiveRange:0.0}");
                parts.Add("can hit " + (verb.CanHitTarget(target) ? "yes" : "no"));

                string report = CeReport(verb, caster.Position, target);
                if (!string.IsNullOrEmpty(report))
                {
                    parts.Add("CE says: " + report);
                }
            }

            Stance stance = verb.CasterPawn?.stances?.curStance;
            if (stance != null)
            {
                parts.Add("stance " + stance.GetType().Name);
            }

            return string.Join(", ", parts);
        }

        private static string AskBool(Ability ability, string property)
        {
            try
            {
                return Format(AccessTools.Property(typeof(Ability), property)?.GetValue(ability, null));
            }
            catch (Exception ex)
            {
                return "check failed (" + ex.GetBaseException().Message + ")";
            }
        }

        private static string AskCanApplyOn(Ability ability, LocalTargetInfo target)
        {
            try
            {
                MethodInfo method = AccessTools.Method(typeof(Ability), "CanApplyOn", new[] { typeof(LocalTargetInfo) });
                return method == null ? "?" : Format(method.Invoke(ability, new object[] { target }));
            }
            catch (Exception ex)
            {
                return "check failed (" + ex.GetBaseException().Message + ")";
            }
        }

        private static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return "?";
                case bool flag:
                    return flag ? "yes" : "no";
                case AcceptanceReport report:
                    return report.Accepted ? "yes" : "no (" + report.Reason + ")";
                default:
                    return value.ToString();
            }
        }

        private static string CeReport(Verb verb, IntVec3 root, LocalTargetInfo target)
        {
            if (_miCanHitTargetFrom == null)
            {
                return null;
            }

            try
            {
                object[] args = { root, target, null };
                bool canHit = (bool)_miCanHitTargetFrom.Invoke(verb, args);
                return canHit ? null : (args[2] as string ?? "no reason given");
            }
            catch (Exception ex)
            {
                return "check failed (" + ex.GetBaseException().Message + ")";
            }
        }

        private static string Describe(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "no target";
            }
            return target.HasThing ? target.Thing.LabelShort : target.Cell.ToString();
        }
    }
}