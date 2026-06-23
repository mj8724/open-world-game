using System;
using UnityEngine;

namespace OpenWorld
{
    /// <summary>
    /// 游戏调参配置 - 将硬编码的魔数提取为可配置参数。
    /// TODO: 考虑转换为 ScriptableObject 以便在 Unity Editor 中可视化编辑。
    /// </summary>
    [Serializable]
    public class GameBalanceConfig
    {
        [Header("战斗数值")]
        [Tooltip("基础伤害系数（AttackPower * Accuracy * efficiency 的乘数）")]
        public float CombatDamageMultiplier = 0.32f;

        [Tooltip("护甲减伤系数")]
        public float CombatArmorReduction = 0.15f;

        [Tooltip("士气影响效率的分母（Morale / 此值）")]
        public float CombatMoraleBase = 100f;

        [Tooltip("士气效率下限")]
        public float CombatMoraleMin = 0.2f;

        [Tooltip("疲劳影响效率的分母（Fatigue / 此值）")]
        public float CombatFatigueBase = 120f;

        [Tooltip("疲劳效率下限")]
        public float CombatFatigueMin = 0.25f;

        [Tooltip("远程单位弹药不足时士气惩罚")]
        public float CombatAmmoMoralePenalty = 0.5f;

        [Tooltip("远程单位每次攻击消耗弹药")]
        public float CombatAmmoConsumption = 1f;

        [Tooltip("伤害转压制值的系数")]
        public float CombatSuppressionMultiplier = 1.5f;

        [Tooltip("伤害转士气损失的系数")]
        public float CombatMoraleDamageMultiplier = 0.35f;

        [Tooltip("低血量判定阈值（MaxHp 占比）")]
        public float CombatWoundedThreshold = 0.45f;

        [Header("人口与士气")]
        [Tooltip("基础人口容量（无房屋时）")]
        public int PopulationBaseCapacity = 16;

        [Tooltip("每个房屋提供的人口容量")]
        public int PopulationPerHouse = 6;

        [Tooltip("食物消耗比率（Residents / 此值）")]
        public int PopulationFoodDivisor = 8;

        [Tooltip("有食物时士气增长")]
        public float PopulationMoraleGainWithFood = 0.35f;

        [Tooltip("缺食物时士气损失")]
        public float PopulationMoraleLossWithoutFood = 2.0f;

        [Tooltip("医疗压力阈值（受伤比率）")]
        public float PopulationMedicalPressureThreshold = 0.45f;

        [Tooltip("伤口恢复血量阈值（MaxHp 占比）")]
        public float PopulationWoundedRecoveryThreshold = 0.75f;

        [Header("单位疲劳与恢复")]
        [Tooltip("活动时疲劳增长")]
        public float UnitFatigueGainActive = 0.5f;

        [Tooltip("休息时疲劳恢复")]
        public float UnitFatigueRecoveryIdle = 1.5f;

        [Tooltip("其他任务疲劳恢复")]
        public float UnitFatigueRecoveryOther = 0.8f;

        [Tooltip("休息时血量恢复")]
        public int UnitHpRecoveryIdle = 1;

        [Tooltip("低补给/疲劳/受伤时士气损失")]
        public float UnitMoraleLossBadCondition = 0.6f;

        [Tooltip("低补给阈值")]
        public float UnitLowSuppliesThreshold = 5f;

        [Tooltip("高疲劳阈值")]
        public float UnitHighFatigueThreshold = 80f;

        [Tooltip("良好状态士气恢复")]
        public float UnitMoraleGainGoodCondition = 0.2f;

        [Header("医疗与服务")]
        [Tooltip("诊所治疗距离（平方）")]
        public float ClinicRangeSquared = 64f;

        [Tooltip("诊所治疗恢复血量")]
        public int ClinicHealAmount = 18;

        [Tooltip("诊所治疗士气恢复")]
        public float ClinicMoraleBonus = 4f;

        [Tooltip("车辆服务距离（平方）")]
        public float VehicleServiceRangeSquared = 16f;

        [Tooltip("每单位燃料恢复量")]
        public float VehicleFuelRefillAmount = 20f;

        [Tooltip("每单位零件修理恢复量")]
        public float VehicleRepairAmount = 15f;

        [Tooltip("服务完成判定阈值")]
        public float VehicleServiceCompleteThreshold = 99f;

        [Header("战斗平衡")]
        [Tooltip("远程武器判定阈值（AttackRange）")]
        public float RangedWeaponThreshold = 2f;
    }

    /// <summary>
    /// 全局游戏平衡配置实例。
    /// 当前为代码内静态实例，后续可改为从 ScriptableObject 加载。
    /// </summary>
    public static class GameBalance
    {
        private static GameBalanceConfig _config;

        public static GameBalanceConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new GameBalanceConfig();
                    // TODO: 从 Resources 或 ScriptableObject 加载自定义配置
                    // var asset = Resources.Load<GameBalanceConfigAsset>("GameBalance");
                    // if (asset != null) _config = asset.Config;
                }
                return _config;
            }
        }

        // 便捷访问
        public static GameBalanceConfig C => Config;
    }
}
