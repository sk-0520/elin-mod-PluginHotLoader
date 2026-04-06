#if DEBUG
#endif
using BepInEx;
using Elin.Plugin.Generated;
using Elin.Plugin.Main.Models.Settings;
using Elin.Plugin.Main.PluginHelpers;
using HarmonyLib;
using System.Linq;

// Mod 用テンプレート組み込み想定

namespace Elin.Plugin.Main
{
    partial class Plugin
    {
        #region property

        private Harmony? Harmony { get; set; }

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
            //NOP
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
            if (string.IsNullOrWhiteSpace(setting.PluginId))
            {
                ModHelper.WriteDebug("skip reloader");
                return;
            }

            if (setting.PluginId == Package.Id)
            {
                throw new System.InvalidOperationException("やめろ");
            }

            var plugin = FindPlugin(setting.PluginId);
            if (plugin is not null)
            {
                ModHelper.WriteDebug($"plugin found: {setting.PluginId}");
                RegisterRefreshPlugin(plugin, Harmony!);
            }
            else
            {
                ModHelper.WriteDebug($"plugin not found: {setting.PluginId}");
            }

        }

        private void RegisterRefreshPlugin(BaseUnityPlugin plugin, Harmony harmony)
        {
            var pluginType = plugin.GetType();
            ModHelper.WriteDebug($"{plugin.Info.Metadata.Name}, {plugin.Info.Metadata.GUID}, {pluginType}");
            ModHelper.WriteDebug($"plugin: {pluginType.Assembly.Location}");

            FileWatcherHelper.Register(plugin.Info.Metadata.GUID, pluginType.Assembly.Location, "*", e =>
            {
                ModHelper.WriteDebug("refresh start");

                // 適用パッチ解除
                Harmony.UnpatchID(plugin.Info.Metadata.GUID);


                ModHelper.WriteDebug("refresh end");
            });
        }

        #endregion
    }
}
