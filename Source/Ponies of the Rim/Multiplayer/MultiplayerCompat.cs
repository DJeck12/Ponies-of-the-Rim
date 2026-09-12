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
            if (_api == null || _registerSyncMethodByName == null)
            {
                return false;
            }
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                Log.Error("[PoniesOfTheRim] Multiplayer: RegisterSyncMethod вызван с пустым типом или именем метода.");
                return false;
            }
            return Invoke(_registerSyncMethodByName, new object[3] { type, methodName, null }, type.Name + "." + methodName);
        }

        public static bool RegisterSyncMethod(MethodInfo method)
        {
            EnsureResolved();
            if (_api == null || _registerSyncMethodByInfo == null)
            {
                return false;
            }
            if (method == null)
            {
                Log.Error("[PoniesOfTheRim] Multiplayer: RegisterSyncMethod вызван с null MethodInfo.");
                return false;
            }
            return Invoke(_registerSyncMethodByInfo, new object[2] { method, null },
                (method.DeclaringType?.Name ?? "?") + "." + method.Name);
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
                Log.Error($"[PoniesOfTheRim] Multiplayer: не удалось зарегистрировать sync-метод '{label}':\n{inner}");
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
            catch
            {
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
                    .SelectMany((ModContentPack m) => m.assemblies.loadedAssemblies)
                    .FirstOrDefault((Assembly a) => a.GetName().Name == "Multiplayer");
                if (mp == null)
                {
                    return;
                }

                Type bridgeType = mp.GetType(BridgeTypeName);
                if (bridgeType == null)
                {
                    Log.Warning("[PoniesOfTheRim] Multiplayer: сборка найдена, но тип " + BridgeTypeName + " отсутствует — патч совместимости отключён.");
                    return;
                }

                FieldInfo instanceField = bridgeType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                object api = instanceField?.GetValue(null);
                if (api == null)
                {
                    Log.Warning("[PoniesOfTheRim] Multiplayer: MultiplayerAPIBridge.Instance недоступен — патч совместимости отключён.");
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
                    if (ps.Length != 2 && ps.Length != 3)
                    {
                        continue;
                    }
                    if (ps[0].ParameterType == typeof(Type) && ps.Length == 3 && ps[1].ParameterType == typeof(string))
                    {
                        _registerSyncMethodByName = m;
                    }
                    else if (ps[0].ParameterType == typeof(MethodInfo) && ps.Length == 2)
                    {
                        _registerSyncMethodByInfo = m;
                    }
                }

                if (_registerSyncMethodByName == null && _registerSyncMethodByInfo == null)
                {
                    Log.Warning("[PoniesOfTheRim] Multiplayer: не найдено ни одной перегрузки RegisterSyncMethod — версия API несовместима, патч совместимости отключён.");
                    return;
                }

                _api = api;
            }
            catch (Exception ex)
            {
                _api = null;
                Log.Error($"[PoniesOfTheRim] Multiplayer: ошибка инициализации моста совместимости:\n{ex}");
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
                Log.Warning($"[PoniesOfTheRim] Multiplayer: не удалось связать свойство '{propertyName}': {ex.Message}");
                return null;
            }
        }
    }
}