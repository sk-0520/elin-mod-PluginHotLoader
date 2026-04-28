namespace Elin.Plugin.Main.PluginHelpers
{
    /// <summary>
    /// Elin のゲーム内での便利関数をまとめるクラス。
    /// </summary>
    /// <remarks>Mod 用テンプレート組み込み想定。</remarks>
    public class ElinHelper
    {
        #region function

        /// <summary>
        /// 戦争依頼ゾーンか。
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public bool IsDefenseGame(Zone zone)
        {
            // [ELIN.Card.SpawnLoot]
            // -> if (EClass.rnd((EClass._zone.events.GetEvent<ZoneEventDefenseGame>() != null) ? 3 : 2) == 0)
            // 実際もっといい判定方法があると思う
            var defenseGame = zone.events.GetEvent<ZoneEventDefenseGame>();
            return defenseGame is not null;
        }

        /// <summary>
        /// 現在グローバルマップにいるか。
        /// </summary>
        /// <returns></returns>
        public bool IsGlobalMap(Scene scene, Zone zone)
        {
            //EMono.scene.mode != Scene.Mode.Zone || EMono._zone.IsRegion;
            // TODO: グローバルマップ判定分かってないんだなぁ
            return scene.mode != Scene.Mode.Zone || zone.IsRegion;
        }

        /// <summary>
        /// 自拠点か。
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public bool IsSelfZone(Zone zone)
        {
            return zone.IsPCFactionOrTent;
        }

        #endregion
    }

    public static class ElinHelperExtensions
    {
        #region function

        /// <inheritdoc cref="ElinHelper.IsDefenseGame(Zone)"/>
        /// <remarks>現在状態を使用する。</remarks>
        public static bool IsDefenseGameForCurrent(this ElinHelper helper)
        {
            return helper.IsDefenseGame(EMono._zone);
        }

        /// <inheritdoc cref="ElinHelper.IsGlobalMap(Scene, Zone)"/>
        /// <remarks>現在状態を使用する。</remarks>
        public static bool IsGlobalMapForCurrent(this ElinHelper helper)
        {
            return helper.IsGlobalMap(EMono.scene, EMono._zone);
        }

        /// <inheritdoc cref="ElinHelper.IsSelfZone(Zone)"/>
        /// <remarks>現在状態を使用する。</remarks>
        public static bool IsSelfZoneForCurrent(this ElinHelper helper)
        {
            return helper.IsSelfZone(EMono._zone);
        }

        #endregion
    }
}
