using Elin.Plugin.Generated;
using System;

namespace Elin.Plugin.Main.Models.Settings
{
    public interface IWatchSetting
    {
        TimeSpan DelayTime { get; }
    }

    [GeneratePluginConfig]
    public partial class Setting : IWatchSetting
    {
        #region property

        internal static Setting Instance { get; set; } = new Setting();

        /// <summary>
        /// 有効か。
        /// </summary>
        public virtual bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 対象のプラグインID。
        /// </summary>
        public virtual string PluginId { get; set; } = string.Empty;

        /// <summary>
        /// 更新検知から再読み込みまでの遅延時間（1.5で1.5秒）
        /// </summary>
        [RangePluginConfig(0.5, 30)]
        public virtual double PrimitiveDelayTime { get; set; } = TimeSpan.FromSeconds(3).TotalSeconds;

        #endregion

        #region IWatchSetting

        /// <inheritdoc cref="PrimitiveDelayTime"/>
        [IgnorePluginConfig]
        public TimeSpan DelayTime
        {
            get => TimeSpan.FromSeconds(PrimitiveDelayTime);
            set => PrimitiveDelayTime = value.TotalSeconds;
        }

        #endregion
    }
}
