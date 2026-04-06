using Elin.Plugin.Generated;

namespace Elin.Plugin.Main.Models.Settings
{
    [GeneratePluginConfig]
    public partial class Setting
    {
        #region property

        internal static Setting Instance { get; set; } = new Setting();

        public virtual string PluginId { get; set; } = string.Empty;

        #endregion
    }
}
