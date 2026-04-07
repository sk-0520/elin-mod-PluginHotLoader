using BepInEx;
using BepInEx.Logging;
using Elin.Plugin.Main.PluginHelpers;
using System.IO;
using System.Security.Cryptography;

namespace Elin.Plugin.Main.Models
{
    public class PluginWatcher
    {
        #region property
        private HashAlgorithm HashAlgorithm { get; } = MD5.Create();

        private int ReloadCount { get; set; }

        private System.TimeSpan DelayTime { get; } = System.TimeSpan.FromSeconds(3);
        private bool Waiting { get; set; }
        private string Hash { get; set; } = string.Empty;

        #endregion

        #region function

        private string ComputeHash(string path)
        {
            using var stream = File.OpenRead(path);
            var hash = HashAlgorithm.ComputeHash(stream);
            return System.BitConverter.ToString(hash).ToLowerInvariant();
        }

        private void RegisterRefreshPluginCore(BaseUnityPlugin plugin, string pluginPath)
        {
            string pluginDirectoryPath = Path.GetDirectoryName(pluginPath);
            string pluginFileName = Path.GetFileName(pluginPath);
            FileWatcherHelper.Register(plugin.Info.Metadata.GUID, pluginDirectoryPath, pluginFileName, e =>
            {
                var ignoreChangeTypes = WatcherChangeTypes.Renamed | WatcherChangeTypes.Deleted;
                if ((e.ChangeType & ignoreChangeTypes) != 0)
                {
                    ModHelper.WriteDev(LogLevel.Debug, $"ignore: {e.ChangeType}");
                    return;
                }

                ModHelper.LogNotify(LogLevel.Debug, $"{e.ChangeType}, {e.FullPath}");
                DelayRefreshPlugin(plugin, e.FullPath);
            });

        }

        private void DelayRefreshPlugin(BaseUnityPlugin oldPlugin, string newPluginPath)
        {
            if (Waiting)
            {
                ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.General.AlreadyWaiting);
                return;
            }

            Waiting = true;
            Timer.Start((float)DelayTime.TotalSeconds, () =>
            {
                ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.General.RefreshPreStart);
                var hash = ComputeHash(newPluginPath);
                if (hash == Hash)
                {
                    ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.Formatter.FormatSkipRefresh(hash: hash));
                    Waiting = false;
                    return;
                }
                Hash = hash;
                RefreshPlugin(oldPlugin, newPluginPath);
            }, false);
        }

        private void RefreshPlugin(BaseUnityPlugin oldPlugin, string newPluginPath)
        {
            ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.Formatter.FormatRefreshStart(count: ++ReloadCount));
            var pluginManager = new PluginManager();
            try
            {
                pluginManager.Unload(oldPlugin);
                FileWatcherHelper.Unregister(oldPlugin.Info.Metadata.GUID);

                var newPlugin = pluginManager.Load(newPluginPath);
                if (newPlugin is not null)
                {
                    RegisterRefreshPluginCore(newPlugin, newPluginPath);
                }
                ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.General.RefreshEnd);
            }
            catch (System.Exception ex)
            {
                ModHelper.LogNotExpected(ex);
            }
            finally
            {
                Waiting = false;
            }
        }

        public void Register(BaseUnityPlugin plugin)
        {
            var pluginType = plugin.GetType();
            Hash = ComputeHash(pluginType.Assembly.Location);
            RegisterRefreshPluginCore(plugin, pluginType.Assembly.Location);
        }

        #endregion
    }
}
