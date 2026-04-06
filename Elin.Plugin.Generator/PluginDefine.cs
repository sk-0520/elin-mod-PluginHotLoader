using System;

namespace Elin.Plugin.Generator
{
    public record class PluginMacro
    {
        public string ElinModulePath { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
    }

    internal class PluginPackage
    {
        #region property

        public string Title { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int LoadPriority { get; set; }
        public string ElinVersion { get; set; } = string.Empty;
        public string[] Description { get; set; } = Array.Empty<string>();
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string Visibility { get; set; } = string.Empty;

        #endregion
    }

    internal class PluginMod
    {
        #region property

        public string Version { get; set; } = string.Empty;
        public bool UseDebugId { get; set; }
        public string Log { get; set; } = string.Empty;

        #endregion
    }

    internal class PluginDefine
    {
        #region property

        public PluginPackage Package { get; set; } = new PluginPackage();
        public PluginMod Mod { get; set; } = new PluginMod();

        #endregion
    }
}
