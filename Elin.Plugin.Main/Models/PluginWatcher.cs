using BepInEx;
using BepInEx.Logging;
using Elin.Plugin.Main.Models.Settings;
using Elin.Plugin.Main.PluginHelpers;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Elin.Plugin.Main.Models
{
    public class PluginWatcher : IDisposable
    {
        #region variable

        private readonly object _sync = new object();

        #endregion

        public PluginWatcher(IWatchSetting watchSetting)
        {
            WatchSetting = watchSetting;
        }

        ~PluginWatcher()
        {
            Dispose(disposing: false);
        }

        #region property

        private IWatchSetting WatchSetting { get; }

        private HashAlgorithm HashAlgorithm { get; } = MD5.Create(); // MD5 はなんか速そうってイメージだけで採用。確固たる理由もこだわりもなし

        private int ReloadCount { get; set; }

        private bool Waiting { get; set; }

        private string Hash { get; set; } = string.Empty;


        #endregion

        #region function

        private string ComputeFileHash(string path)
        {
            using var stream = File.OpenRead(path);
            var hash = HashAlgorithm.ComputeHash(stream);
            return System.BitConverter.ToString(hash);
        }

        private void RefreshPlugin(BaseUnityPlugin oldPlugin, string newPluginPath)
        {
            ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.Formatter.FormatRefreshStart(count: ++ReloadCount));
            var pluginManager = new PluginManager();
            try
            {
                pluginManager.Unload(oldPlugin);
                FileWatcherHelper.Unregister(oldPlugin.Info.Metadata.GUID);

                //TODO: 読み込み失敗時のあれこれ
                var newPlugin = pluginManager.Load(newPluginPath);
                if (newPlugin is not null)
                {
                    RegisterCore(newPlugin, newPluginPath);
                }
                ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.General.RefreshEnd);
            }
            catch (System.Exception ex)
            {
                ModHelper.LogNotExpected(ex);
            }
        }

        private void DelayRefreshPlugin(BaseUnityPlugin oldPlugin, string newPluginPath)
        {
            lock (this._sync)
            {
                if (Waiting)
                {
                    ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.General.AlreadyWaiting);
                    return;
                }

                Waiting = true;
            }

            // TODO: Update で動いてるっぽいから切り離した処理にした方がいいと思う(気がする)
            Timer.Start((float)WatchSetting.DelayTime.TotalSeconds, () =>
            {
                try
                {
                    ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.General.RefreshPreStart);
                    var hash = ComputeFileHash(newPluginPath);
                    if (hash == Hash)
                    {
                        ModHelper.LogNotify(LogLevel.Info, ModHelper.Lang.Formatter.FormatSkipRefresh(hash: hash));
                        return;
                    }
                    Hash = hash;
                    RefreshPlugin(oldPlugin, newPluginPath);
                }
                finally
                {
                    Waiting = false;
                }

            }, false);
        }

        private void RegisterCore(BaseUnityPlugin plugin, string pluginPath)
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

        public void Register(BaseUnityPlugin plugin)
        {
            var pluginType = plugin.GetType();
            Hash = ComputeFileHash(pluginType.Assembly.Location);
            RegisterCore(plugin, pluginType.Assembly.Location);
        }

        #endregion

        #region IDisposable

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    HashAlgorithm.Dispose();
                }

                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
