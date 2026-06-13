using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    /// <summary>
    /// 游戏全局数据配置（ScriptableObject）
    /// 替代硬编码的 BuildableDef.Defaults() 等，支持运行时调整和可视化编辑。
    /// 创建方式：右键 Project → Create → OpenWorld → Game Data Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataConfig", menuName = "OpenWorld/Game Data Config")]
    public class GameDataConfig : ScriptableObject
    {
        [Header("建筑定义")]
        public List<BuildableDef> BuildingDefs = BuildableDef.Defaults();

        [Header("兵种属性")]
        public List<UnitKindDef> UnitDefs = UnitKindDef.Defaults();

        [Header("车辆定义")]
        public List<VehicleDef> VehicleDefs = new()
        {
            new VehicleDef { Kind = VehicleKind.HandCart, DisplayName = "Hand Cart", RequiredEra = TechEra.WoodStone, Cost = new[] { new ResourceAmount(ResourceKind.Wood, 10), new ResourceAmount(ResourceKind.IronIngot, 2) } },
            new VehicleDef { Kind = VehicleKind.Wagon, DisplayName = "Wagon", RequiredEra = TechEra.WoodStone, Cost = new[] { new ResourceAmount(ResourceKind.Wood, 18), new ResourceAmount(ResourceKind.IronIngot, 4) } },
            new VehicleDef { Kind = VehicleKind.Truck, DisplayName = "Truck", RequiredEra = TechEra.Industrial, Cost = new[] { new ResourceAmount(ResourceKind.Steel, 12), new ResourceAmount(ResourceKind.MachineParts, 5), new ResourceAmount(ResourceKind.Fuel, 10) } },
            new VehicleDef { Kind = VehicleKind.ArmoredCar, DisplayName = "Armored Car", RequiredEra = TechEra.Industrial, Cost = new[] { new ResourceAmount(ResourceKind.Steel, 20), new ResourceAmount(ResourceKind.MachineParts, 8), new ResourceAmount(ResourceKind.Weapons, 3), new ResourceAmount(ResourceKind.Fuel, 15) } },
            new VehicleDef { Kind = VehicleKind.Locomotive, DisplayName = "Locomotive", RequiredEra = TechEra.Industrial, Cost = new[] { new ResourceAmount(ResourceKind.Steel, 30), new ResourceAmount(ResourceKind.MachineParts, 12), new ResourceAmount(ResourceKind.Fuel, 20) } },
            new VehicleDef { Kind = VehicleKind.CargoWagon, DisplayName = "Cargo Wagon", RequiredEra = TechEra.Industrial, Cost = new[] { new ResourceAmount(ResourceKind.Steel, 14), new ResourceAmount(ResourceKind.MachineParts, 4) } },
        };

        [Header("生产配方")]
        public List<ProductionRecipeDef> RecipeDefs = new(OpenWorldDataCatalog.ProductionRecipes);

        [Header("科技定义")]
        public List<TechDef> TechDefs = new(OpenWorldDataCatalog.Techs);

        /// <summary>按 Kind 查找建筑定义</summary>
        public BuildableDef GetBuilding(BuildableKind kind) => BuildingDefs.Find(d => d.Kind == kind);

        /// <summary>按 Kind 查找兵种定义</summary>
        public UnitKindDef GetUnit(UnitKind kind) => UnitDefs.Find(d => d.Kind == kind);

        /// <summary>按 Kind 查找车辆定义</summary>
        public VehicleDef GetVehicle(VehicleKind kind) => VehicleDefs.Find(d => d.Kind == kind);

        /// <summary>按 Id 查找配方</summary>
        public ProductionRecipeDef GetRecipe(string id) => RecipeDefs.Find(r => r.Id == id);

        /// <summary>按 Id 查找科技</summary>
        public TechDef GetTech(string id) => TechDefs.Find(t => t.Id == id);

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 确保唯一性
            HashSet<BuildableKind> seenKinds = new();
            BuildingDefs.RemoveAll(d =>
            {
                if (seenKinds.Contains(d.Kind)) return true;
                seenKinds.Add(d.Kind);
                return false;
            });
        }
#endif
    }

    /// <summary>
    /// 兵种数据定义（可 ScriptableObject 编辑）
    /// 替代 OpenWorldState.ApplyUnitDefaults() 中的 switch 硬编码
    /// </summary>
    [System.Serializable]
    public class UnitKindDef
    {
        public UnitKind Kind;
        public string DisplayName = "";
        public int AttackPower = 8;
        public int Hp = 100;
        public int MaxHp = 100;
        public float Morale = 100f;
        public int VisionRange = 12;
        public float AttackRange = 1.5f;
        public float Accuracy = 0.65f;
        public float Armor;
        public float Speed = 4f;
        public float Ammo = 20f;
        public int MaxAmmo = 35;
        public bool IsRanged;

        public void ApplyTo(UnitEntity unit)
        {
            unit.AttackPower = AttackPower;
            unit.Hp = Hp;
            unit.MaxHp = MaxHp;
            unit.Morale = Morale;
            unit.VisionRange = VisionRange;
            unit.AttackRange = AttackRange;
            unit.Accuracy = Accuracy;
            unit.Armor = Armor;
            unit.Ammo = Ammo;
        }

        public static List<UnitKindDef> Defaults() => new()
        {
            new UnitKindDef { Kind = UnitKind.Worker, DisplayName = "Worker", AttackPower = 5, Hp = 80, MaxHp = 80, VisionRange = 12, AttackRange = 1.4f, Accuracy = 0.48f, Speed = 4f },
            new UnitKindDef { Kind = UnitKind.Engineer, DisplayName = "Engineer", AttackPower = 4, Hp = 90, MaxHp = 90, VisionRange = 15, AttackRange = 1.5f, Accuracy = 0.52f, Speed = 3.8f },
            new UnitKindDef { Kind = UnitKind.Scout, DisplayName = "Scout", AttackPower = 4, Hp = 70, MaxHp = 70, VisionRange = 26, AttackRange = 5f, Accuracy = 0.65f, Ammo = 5, IsRanged = true, Speed = 5f },
            new UnitKindDef { Kind = UnitKind.Militia, DisplayName = "Militia", AttackPower = 6, Hp = 80, MaxHp = 80, VisionRange = 10, AttackRange = 1.3f, Accuracy = 0.55f, Speed = 3.5f },
            new UnitKindDef { Kind = UnitKind.Melee, DisplayName = "Swordsman", AttackPower = 10, Hp = 100, MaxHp = 100, VisionRange = 12, AttackRange = 1.5f, Accuracy = 0.72f, Armor = 1f, Speed = 3.5f },
            new UnitKindDef { Kind = UnitKind.Spearman, DisplayName = "Spearman", AttackPower = 9, Hp = 100, MaxHp = 100, VisionRange = 12, AttackRange = 1.8f, Accuracy = 0.68f, Speed = 3.5f },
            new UnitKindDef { Kind = UnitKind.Ranged, DisplayName = "Archer", AttackPower = 8, Hp = 80, MaxHp = 80, VisionRange = 14, AttackRange = 6f, Accuracy = 0.64f, IsRanged = true, Ammo = 30, Speed = 3.5f },
            new UnitKindDef { Kind = UnitKind.Musketeer, DisplayName = "Musketeer", AttackPower = 14, Hp = 90, MaxHp = 90, VisionRange = 16, AttackRange = 7f, Accuracy = 0.70f, IsRanged = true, Ammo = 35, Speed = 3.2f },
            new UnitKindDef { Kind = UnitKind.Rifleman, DisplayName = "Rifleman", AttackPower = 14, Hp = 90, MaxHp = 90, VisionRange = 16, AttackRange = 7f, Accuracy = 0.72f, IsRanged = true, Ammo = 35, Speed = 3.2f },
            new UnitKindDef { Kind = UnitKind.MachineGunner, DisplayName = "Machine Gunner", AttackPower = 22, Hp = 100, MaxHp = 100, VisionRange = 18, AttackRange = 8f, Accuracy = 0.62f, Armor = 2f, IsRanged = true, Ammo = 70, Speed = 2.8f },
            new UnitKindDef { Kind = UnitKind.Artillery, DisplayName = "Artillery", AttackPower = 35, Hp = 70, MaxHp = 70, VisionRange = 20, AttackRange = 12f, Accuracy = 0.52f, Armor = 3f, IsRanged = true, Ammo = 24, Speed = 2f },
            new UnitKindDef { Kind = UnitKind.Medic, DisplayName = "Medic", AttackPower = 2, Hp = 60, MaxHp = 60, VisionRange = 14, AttackRange = 1.2f, Accuracy = 0.45f, Speed = 3.8f },
            new UnitKindDef { Kind = UnitKind.Hauler, DisplayName = "Hauler", AttackPower = 3, Hp = 70, MaxHp = 70, VisionRange = 10, AttackRange = 1.2f, Accuracy = 0.40f, Speed = 3.5f },
        };
    }
}
