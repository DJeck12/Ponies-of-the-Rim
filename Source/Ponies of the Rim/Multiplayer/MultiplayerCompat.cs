using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace PoniesOfTheRim.Multiplayer
{
    public static class MultiplayerCompat
    {
        private const string BridgeTypeName = "Multiplayer.Common.MultiplayerAPIBridge";

        private static bool _resolveAttempted;
        private static object _api;

        private static Func<bool> _getIsInMultiplayer;
        private static Func<bool> _getInInterface;
        private static Func<bool> _getIsExecutingSyncCommand;
        private static Func<bool> _getIsHosting;

        private static MethodInfo _registerSyncMethodByName;
        private static MethodInfo _registerSyncMethodByInfo;

        private static readonly List<string> RegisteredMembers = new List<string>();

        public static bool Loaded
        {
            get
            {
                EnsureResolved();
                return _api != null;
            }
        }

        public static bool InMultiplayer => Read(_getIsInMultiplayer);

        public static bool InInterface => Read(_getInInterface);

        public static bool ExecutingSyncCommand => Read(_getIsExecutingSyncCommand);

        public static bool IsHosting => Read(_getIsHosting);

        public static bool TickCacheWritable => !InInterface;

        public static IReadOnlyList<string> Registered => RegisteredMembers;

        public static bool RegisterSyncMethod(Type type, string methodName)
        {
            EnsureResolved();
            if (_api == null)
            {
                return false;
            }
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                PonyLog.Error("MP: RegisterSyncMethod called without type or method name");
                return false;
            }
            if (_registerSyncMethodByName == null)
            {
                PonyLog.Warn($"MP: no RegisterSyncMethod(Type, string) in this API version, {type.Name}.{methodName} is not synced");
                return false;
            }
            return Invoke(_registerSyncMethodByName, new object[] { type, methodName, null }, type.Name + "." + methodName);
        }

        public static bool RegisterSyncMethod(MethodInfo method)
        {
            EnsureResolved();
            if (_api == null)
            {
                return false;
            }
            if (method == null)
            {
                PonyLog.Error("MP: RegisterSyncMethod called with null MethodInfo");
                return false;
            }
            string label = (method.DeclaringType?.Name ?? "?") + "." + method.Name;
            if (_registerSyncMethodByInfo == null)
            {
                PonyLog.Warn($"MP: no RegisterSyncMethod(MethodInfo) in this API version, {label} is not synced");
                return false;
            }
            return Invoke(_registerSyncMethodByInfo, new object[] { method, null }, label);
        }

        private static bool Invoke(MethodInfo target, object[] args, string label)
        {
            try
            {
                target.Invoke(_api, args);
                RegisteredMembers.Add(label);
                return true;
            }
            catch (Exception ex)
            {
                Exception inner = (ex as TargetInvocationException)?.InnerException ?? ex;
                PonyLog.Error($"MP: sync method {label} not registered\n{inner}");
                return false;
            }
        }

        private static bool Read(Func<bool> getter)
        {
            EnsureResolved();
            if (getter == null)
            {
                return false;
            }
            try
            {
                return getter();
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("MP: bridge property read failed", ex);
                return false;
            }
        }

        private static void EnsureResolved()
        {
            if (_resolveAttempted)
            {
                return;
            }
            _resolveAttempted = true;
            try
            {
                Assembly mp = LoadedModManager.RunningMods
                    .SelectMany(m => m.assemblies.loadedAssemblies)
                    .FirstOrDefault(a => a.GetName().Name == "Multiplayer");
                if (mp == null)
                {
                    return;
                }

                Type bridgeType = mp.GetType(BridgeTypeName);
                if (bridgeType == null)
                {
                    PonyLog.Warn($"MP: {BridgeTypeName} not found in the Multiplayer assembly, compatibility disabled");
                    return;
                }

                FieldInfo instanceField = bridgeType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                object api = instanceField?.GetValue(null);
                if (api == null)
                {
                    PonyLog.Warn("MP: MultiplayerAPIBridge.Instance is null, compatibility disabled");
                    return;
                }

                _getIsInMultiplayer = BindBoolGetter(bridgeType, api, "IsInMultiplayer");
                _getInInterface = BindBoolGetter(bridgeType, api, "InInterface");
                _getIsExecutingSyncCommand = BindBoolGetter(bridgeType, api, "IsExecutingSyncCommand");
                _getIsHosting = BindBoolGetter(bridgeType, api, "IsHosting");

                MethodInfo[] methods = bridgeType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (MethodInfo m in methods)
                {
                    if (m.Name != "RegisterSyncMethod")
                    {
                        continue;
                    }
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length == 3 && ps[0].ParameterType == typeof(Type) && ps[1].ParameterType == typeof(string))
                    {
                        _registerSyncMethodByName = m;
                    }
                    else if (ps.Length == 2 && ps[0].ParameterType == typeof(MethodInfo))
                    {
                        _registerSyncMethodByInfo = m;
                    }
                }

                if (_registerSyncMethodByName == null && _registerSyncMethodByInfo == null)
                {
                    PonyLog.Warn("MP: no usable RegisterSyncMethod overload, compatibility disabled");
                    return;
                }

                _api = api;
            }
            catch (Exception ex)
            {
                _api = null;
                PonyLog.ErrorCaught("MP: bridge init failed, compatibility disabled", ex);
            }
        }

        private static Func<bool> BindBoolGetter(Type bridgeType, object api, string propertyName)
        {
            try
            {
                MethodInfo getter = bridgeType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod();
                if (getter == null)
                {
                    return null;
                }
                return (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), api, getter);
            }
            catch (Exception ex)
            {
                PonyLog.Warn($"MP: could not bind {propertyName}: {ex.Message}");
                return null;
            }
        }
    }
}