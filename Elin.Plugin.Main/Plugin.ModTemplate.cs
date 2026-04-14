using BepInEx;
using Elin.Plugin.Generated;
using Elin.Plugin.Main.PluginHelpers;
using HarmonyLib;
using System.Threading;

namespace Elin.Plugin.Main
{
    [BepInPlugin(Package.Id, Mod.Name, Mod.Version)]
    public partial class Plugin : BaseUnityPlugin
    {
        #region property

        /// <summary>
        /// <see cref="Harmony.PatchAll()"/>を呼び出すか。
        /// </summary>
        /// <remarks>
        /// <para>デフォルトの<see langword="true"/>で問題ない。</para>
        /// </remarks>
        private bool CallPatchAll { get; set; } = true;

        #endregion

        #region function

        /// <summary>
        /// 起動。
        /// </summary>
        /// <remarks>本メソッドではインフラ面の構築も行っているため、プラグインとしての起動処理は <see cref="AwakePlugin(Harmony)"/> で実施する。</remarks>
        public void Awake()
        {
            ModHelper.Initialize(this, Logger, SynchronizationContext.Current);

            var harmony = new Harmony(Package.Id);

            AwakePlugin(harmony);

            if (CallPatchAll)
            {
                harmony.PatchAll();
            }
        }

        public void OnDestroy()
        {
            OnDestroyPlugin();
            ModHelper.Destroy();
        }

        #endregion
    }
}
