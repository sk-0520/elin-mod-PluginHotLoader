using BepInEx;
using BepInEx.Logging;
using Elin.Plugin.Main.PluginHelpers;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Elin.Plugin.Main.Models
{
    public class PluginManager
    {
        #region define

        private static class MethodName
        {
            public const string OnDestroy = "OnDestroy";
            public const string Awake = "Awake";
            public const string Start = "Start";
            public const string Debug = "PHL";
        }

        #endregion

        #region property

        #endregion

        #region function

        private void CallMethodIfExists(Type type, BaseUnityPlugin plugin, string methodName)
        {
            ModHelper.WriteDev(methodName);
            var method = AccessTools.Method(type, methodName);
            if (method is not null)
            {
                ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.Formatter.FormatMethodStart(methodName: methodName));
                method.Invoke(plugin, null);
                ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.Formatter.FormatMethodEnd(methodName: methodName));
            }
            else
            {
                ModHelper.LogNotify(LogLevel.Debug, ModHelper.Lang.Formatter.FormatMethodNotFound(methodName: methodName));
            }
        }

        public void Unload(BaseUnityPlugin plugin)
        {
            // 適用パッチ解除
            ModHelper.WriteDev("Unpatch");
            Harmony.UnpatchID(plugin.Info.Metadata.GUID);

            // 破棄
            //ModHelper.WriteDebug("Object.DestroyImmediate");
            //Object.DestroyImmediate(plugin);
            CallMethodIfExists(plugin.GetType(), plugin, MethodName.OnDestroy);

            ModHelper.WriteDev("Remove");
            if (!ModManager.ListPluginObject.Remove(plugin))
            {
                throw new System.InvalidOperationException(ModHelper.Lang.General.FailedRemove);
            }
        }

        public BaseUnityPlugin? Load(string pluginPath)
        {
            var binaryAassembly = File.ReadAllBytes(pluginPath);
            var assembly = Assembly.Load(binaryAassembly);
            var newPluginType = assembly.GetTypes().FirstOrDefault(x => typeof(BaseUnityPlugin).IsAssignableFrom(x));
            if (newPluginType is null)
            {
                return null;
            }

            var newPlugin = Activator.CreateInstance(newPluginType) as BaseUnityPlugin;
            if (newPlugin is null)
            {
                return null;
            }
            ModManager.ListPluginObject.Add(newPlugin);

            CallMethodIfExists(newPluginType, newPlugin, MethodName.Awake);
            CallMethodIfExists(newPluginType, newPlugin, MethodName.Start);
            CallMethodIfExists(newPluginType, newPlugin, MethodName.Debug);

            return newPlugin;
        }

        #endregion
    }
}
