using BepInEx;
using Elin.Plugin.Generated;
using Elin.Plugin.Main.PluginHelpers;
using HarmonyLib;

namespace Elin.Plugin.Main
{
    [BepInPlugin(Package.Id, Mod.Name, Mod.Version)]
    public partial class Plugin : BaseUnityPlugin
    {
        #region function

        /// <summary>
        /// 起動。
        /// </summary>
        /// <remarks>本メソッドではインフラ面の構築も行っているため、プラグインとしての起動処理は <see cref="AwakePlugin(Harmony)"/> で実施する。</remarks>
        public void Awake()
        {
            ModHelper.Initialize(this, Logger);

            var harmony = new Harmony(Package.Id);

            AwakePlugin(harmony);

            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            OnDestroyPlugin();
            ModHelper.Destroy();
        }

        #endregion
    }
}
