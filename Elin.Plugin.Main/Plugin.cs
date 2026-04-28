#if DEBUG
#endif
using BepInEx;
using Elin.Plugin.Generated;
using Elin.Plugin.Main.Models;
using Elin.Plugin.Main.Models.Settings;
using Elin.Plugin.Main.PluginHelpers;
using System.Linq;

namespace Elin.Plugin.Main
{
    partial class Plugin
    {
        #region property

        private PluginWatcher? PluginWatcher { get; set; }

        #endregion

        #region function

        /// <summary>
        /// 起動時のプラグイン独自処理。
        /// </summary>
        private void AwakePlugin()
        {
            Setting.Instance = Setting.Bind(Config, new Setting());
            CallPatchAll = false;
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
                ModHelper.LogNotify(BepInEx.Logging.LogLevel.Warning, ModHelper.Lang.General.PluginIdNotSet);
                return;
            }

            if (setting.PluginId == Package.Id)
            {
                throw new System.InvalidOperationException(ModHelper.Lang.General.StopIt);
            }

            var plugin = FindPlugin(setting.PluginId);
            if (plugin is not null)
            {
                ModHelper.LogNotify(BepInEx.Logging.LogLevel.Info, ModHelper.Lang.Formatter.FormatPluginFound(pluginId: setting.PluginId));
                PluginWatcher = new PluginWatcher(setting);
                PluginWatcher.Register(plugin);
            }
            else
            {
                ModHelper.LogNotify(BepInEx.Logging.LogLevel.Error, ModHelper.Lang.Formatter.FormatPluginNotFound(pluginId: setting.PluginId));
            }
        }

        #endregion
    }
}
