#if DEBUG
#endif
using BepInEx;
using Elin.Plugin.Generated;
using Elin.Plugin.Main.Models;
using Elin.Plugin.Main.Models.Settings;
using Elin.Plugin.Main.PluginHelpers;
using HarmonyLib;
using System.Linq;

namespace Elin.Plugin.Main
{
    partial class Plugin
    {
        #region property

        private Harmony? Harmony { get; set; }

        private PluginWatcher? PluginWatcher { get; set; }

        #endregion

        #region function

        /// <summary>
        /// 起動時のプラグイン独自処理。
        /// </summary>
        /// <param name="harmony"></param>
        private void AwakePlugin(Harmony harmony)
        {
            Setting.Instance = Setting.Bind(Config, new Setting());
            Harmony = harmony;
        }

        /// <summary>
        /// 終了時のプラグイン独自処理。
        /// </summary>
        private void OnDestroyPlugin()
        {
            PluginWatcher?.Dispose();
        }

        private BaseUnityPlugin? FindPlugin(string pluginId) => ModManager.ListPluginObject
            .OfType<BaseUnityPlugin>()
            .Where(a => a is not null)
            .FirstOrDefault(a => a.Info.Metadata.GUID == pluginId)
        ;

        public void Start()
        {
            Debug.Assert(Harmony is not null);

            var setting = Setting.Instance;
            if (!setting.IsEnabled)
            {
                // 無効なら無言でばいばい
                return;
            }

            if (string.IsNullOrWhiteSpace(setting.PluginId))
            {
                ModHelper.WriteDev(ModHelper.Lang.General.PluginIdNotSet);
                return;
            }

            if (setting.PluginId == Package.Id)
            {
                throw new System.InvalidOperationException(ModHelper.Lang.General.StopIt);
            }

            var plugin = FindPlugin(setting.PluginId);
            if (plugin is not null)
            {
                ModHelper.WriteDev(ModHelper.Lang.Formatter.FormatPluginFound(pluginId: setting.PluginId));
                PluginWatcher = new PluginWatcher(setting);
                PluginWatcher.Register(plugin);
            }
            else
            {
                ModHelper.WriteDev(ModHelper.Lang.Formatter.FormatPluginNotFound(pluginId: setting.PluginId));
            }
        }

        #endregion
    }
}
