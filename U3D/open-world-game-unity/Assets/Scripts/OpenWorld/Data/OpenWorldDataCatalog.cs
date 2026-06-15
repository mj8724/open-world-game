using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    [Serializable]
    public readonly struct ResourceAmount
    {
        public readonly ResourceKind Kind;
        public readonly int Amount;

        public ResourceAmount(ResourceKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    public sealed class ProductionRecipeDef
    {
        public string Id = "";
        public BuildableKind Building;
        public TechEra RequiredEra = TechEra.WoodStone;
        public ResourceAmount[] Inputs = Array.Empty<ResourceAmount>();
        public ResourceAmount[] Outputs = Array.Empty<ResourceAmount>();
        public int Workers = 1;
        public string BlockedLabel = "missing input";
    }

    public sealed class VehicleDef
    {
        public VehicleKind Kind;
        public string DisplayName = "";
        public TechEra RequiredEra = TechEra.WoodStone;
        public ResourceAmount[] Cost = Array.Empty<ResourceAmount>();
    }

    public sealed class TechDef
    {
        public string Id = "";
        public string DisplayName = "";
        public TechEra RequiredEra = TechEra.WoodStone;
        public TechEra UnlockEra = TechEra.WoodStone;
        public int ResearchTicks = 10;
        public int Engineers = 1;
        public ResourceAmount[] CostPerTick = Array.Empty<ResourceAmount>();
        public string NextResearch = "";
    }

    /// <summary>
    /// 兵种数据定义。<see cref="OpenWorldDataCatalog.GetUnit"/> 提供按 Kind 的查找，
    /// 由 <see cref="OpenWorldState.AddUnit"/> 应用到新生成的单位。
    /// </summary>
    [Serializable]
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

    public static class OpenWorldDataCatalog
    {
        public static IReadOnlyList<ProductionRecipeDef> ProductionRecipes => _productionRecipes;
        public static IReadOnlyList<VehicleDef> Vehicles => _vehicles;
        public static IReadOnlyList<TechDef> Techs => _techs;

        private static readonly List<ProductionRecipeDef> _productionRecipes = new()
        {
            Recipe("vehicle:HandCart", BuildableKind.VehicleFactory, TechEra.WoodStone, Amounts((ResourceKind.Wood, 10), (ResourceKind.IronIngot, 1)), Array.Empty<ResourceAmount>(), 1, "needs materials"),
            Recipe("vehicle:Wagon", BuildableKind.VehicleFactory, TechEra.WoodStone, Amounts((ResourceKind.Wood, 18), (ResourceKind.IronIngot, 3)), Array.Empty<ResourceAmount>(), 1, "needs materials"),
            Recipe("vehicle:Truck", BuildableKind.VehicleFactory, TechEra.Industrial, Amounts((ResourceKind.Steel, 12), (ResourceKind.MachineParts, 5), (ResourceKind.Fuel, 10)), Array.Empty<ResourceAmount>(), 2, "needs steel/parts/fuel"),
            Recipe("vehicle:ArmoredCar", BuildableKind.VehicleFactory, TechEra.Industrial, Amounts((ResourceKind.Steel, 20), (ResourceKind.MachineParts, 8), (ResourceKind.Weapons, 3), (ResourceKind.Fuel, 15)), Array.Empty<ResourceAmount>(), 2, "needs steel/parts/weapons/fuel"),
            Recipe("vehicle:Locomotive", BuildableKind.TrainFactory, TechEra.Industrial, Amounts((ResourceKind.Steel, 30), (ResourceKind.MachineParts, 12), (ResourceKind.Fuel, 20)), Array.Empty<ResourceAmount>(), 3, "needs steel/parts/fuel"),
            Recipe("vehicle:CargoWagon", BuildableKind.TrainFactory, TechEra.Industrial, Amounts((ResourceKind.Steel, 14), (ResourceKind.MachineParts, 4)), Array.Empty<ResourceAmount>(), 1, "needs steel/parts"),
            Recipe("farm-food", BuildableKind.Farm, TechEra.WoodStone, Array.Empty<ResourceAmount>(), Amounts((ResourceKind.Food, 3)), 1, "needs workers"),
            Recipe("lumber-wood", BuildableKind.LumberCamp, TechEra.WoodStone, Array.Empty<ResourceAmount>(), Amounts((ResourceKind.Wood, 2), (ResourceKind.Lumber, 1)), 1, "needs workers"),
            Recipe("quarry-stone", BuildableKind.Quarry, TechEra.WoodStone, Array.Empty<ResourceAmount>(), Amounts((ResourceKind.Stone, 3)), 1, "needs workers"),
            Recipe("market-medicine", BuildableKind.Market, TechEra.WoodStone, Amounts((ResourceKind.Food, 2)), Amounts((ResourceKind.Medicine, 1)), 1, "needs food"),
            Recipe("smelt-iron", BuildableKind.Smelter, TechEra.Iron, Amounts((ResourceKind.IronOre, 2), (ResourceKind.Coal, 1)), Amounts((ResourceKind.IronIngot, 1)), 2, "needs ore/coal"),
            Recipe("make-steel", BuildableKind.Steelworks, TechEra.Iron, Amounts((ResourceKind.IronIngot, 2), (ResourceKind.Coal, 2)), Amounts((ResourceKind.Steel, 1)), 2, "needs iron/coal"),
            Recipe("machine-parts", BuildableKind.MachineShop, TechEra.Industrial, Amounts((ResourceKind.Steel, 1)), Amounts((ResourceKind.MachineParts, 1), (ResourceKind.RailParts, 1)), 2, "needs steel"),
            Recipe("gunpowder", BuildableKind.Armory, TechEra.Gunpowder, Amounts((ResourceKind.Sulfur, 1), (ResourceKind.Nitrate, 1), (ResourceKind.Coal, 1)), Amounts((ResourceKind.Gunpowder, 2)), 1, "needs sulfur/nitrate/coal"),
            Recipe("ammo", BuildableKind.Armory, TechEra.Gunpowder, Amounts((ResourceKind.Gunpowder, 1), (ResourceKind.IronIngot, 1)), Amounts((ResourceKind.Ammo, 8)), 1, "needs powder/iron"),
            Recipe("weapons", BuildableKind.Armory, TechEra.Iron, Amounts((ResourceKind.IronIngot, 1)), Amounts((ResourceKind.Weapons, 1)), 1, "needs iron"),
            Recipe("oil-fuel", BuildableKind.Refinery, TechEra.Industrial, Amounts((ResourceKind.Oil, 1)), Amounts((ResourceKind.Fuel, 3)), 2, "needs oil"),
            Recipe("coal-power", BuildableKind.PowerPlant, TechEra.Industrial, Amounts((ResourceKind.Coal, 2)), Amounts((ResourceKind.Power, 4)), 2, "needs coal"),
            Recipe("rail-parts", BuildableKind.TrainFactory, TechEra.Industrial, Amounts((ResourceKind.Steel, 4), (ResourceKind.MachineParts, 2)), Amounts((ResourceKind.RailParts, 4)), 3, "needs steel/parts")
        };

        private static readonly List<VehicleDef> _vehicles = new()
        {
            Vehicle(VehicleKind.HandCart, "Hand Cart", TechEra.WoodStone, Amounts((ResourceKind.Wood, 10), (ResourceKind.IronIngot, 1))),
            Vehicle(VehicleKind.Wagon, "Wagon", TechEra.WoodStone, Amounts((ResourceKind.Wood, 18), (ResourceKind.IronIngot, 3))),
            Vehicle(VehicleKind.Truck, "Truck", TechEra.Industrial, Amounts((ResourceKind.Steel, 12), (ResourceKind.MachineParts, 5), (ResourceKind.Fuel, 10))),
            Vehicle(VehicleKind.ArmoredCar, "Armored Car", TechEra.Industrial, Amounts((ResourceKind.Steel, 20), (ResourceKind.MachineParts, 8), (ResourceKind.Weapons, 3), (ResourceKind.Fuel, 15))),
            Vehicle(VehicleKind.Locomotive, "Locomotive", TechEra.Industrial, Amounts((ResourceKind.Steel, 30), (ResourceKind.MachineParts, 12), (ResourceKind.Fuel, 20))),
            Vehicle(VehicleKind.CargoWagon, "Cargo Wagon", TechEra.Industrial, Amounts((ResourceKind.Steel, 14), (ResourceKind.MachineParts, 4))),
            Vehicle(VehicleKind.Tank, "Tank", TechEra.Aviation, Amounts((ResourceKind.Steel, 45), (ResourceKind.MachineParts, 18), (ResourceKind.Weapons, 8), (ResourceKind.Fuel, 25))),
            Vehicle(VehicleKind.Aircraft, "Aircraft", TechEra.Aviation, Amounts((ResourceKind.Steel, 30), (ResourceKind.MachineParts, 20), (ResourceKind.Fuel, 30))),
            Vehicle(VehicleKind.TransportPlane, "Transport Plane", TechEra.Aviation, Amounts((ResourceKind.Steel, 40), (ResourceKind.MachineParts, 26), (ResourceKind.Fuel, 45)))
        };

        private static readonly List<TechDef> _techs = new()
        {
            Tech("Iron Working", "Iron Working", TechEra.WoodStone, TechEra.Iron, 8, 1, Amounts((ResourceKind.Wood, 1), (ResourceKind.Stone, 1)), "Gunpowder"),
            Tech("Gunpowder", "Gunpowder", TechEra.Iron, TechEra.Gunpowder, 10, 1, Amounts((ResourceKind.IronIngot, 1), (ResourceKind.Coal, 1)), "Industrialization"),
            Tech("Industrialization", "Industrialization", TechEra.Gunpowder, TechEra.Industrial, 14, 2, Amounts((ResourceKind.Steel, 1)), "Aviation"),
            Tech("Aviation", "Aviation", TechEra.Industrial, TechEra.Aviation, 18, 3, Amounts((ResourceKind.Fuel, 1), (ResourceKind.MachineParts, 2)), "")
        };

        private static readonly List<UnitKindDef> _unitKinds = UnitKindDef.Defaults();
        private static readonly Dictionary<UnitKind, UnitKindDef> _defCache = new();

        static OpenWorldDataCatalog()
        {
            foreach (var def in _unitKinds)
                _defCache[def.Kind] = def;
        }

        public static IReadOnlyList<UnitKindDef> UnitKinds => _unitKinds;

        public static bool TryGetDef(UnitKind kind, out UnitKindDef def) => _defCache.TryGetValue(kind, out def);

        public static UnitKindDef GetUnit(UnitKind kind) => TryGetDef(kind, out var def) ? def : null;

        public static VehicleDef GetVehicle(VehicleKind kind) => _vehicles.Find(v => v.Kind == kind);

        public static ProductionRecipeDef GetRecipe(string id) => _productionRecipes.Find(r => r.Id == id);

        public static TechDef GetTech(string id) => _techs.Find(t => t.Id == id);

        public static int StorageCapacityFor(BuildableKind kind) => kind switch
        {
            BuildableKind.TownCenter => 500,
            BuildableKind.Warehouse => 900,
            BuildableKind.Station => 650,
            BuildableKind.Market => 400,
            BuildableKind.Dock => 500,
            BuildableKind.Garage => 300,
            BuildableKind.VehicleFactory => 320,
            BuildableKind.TrainFactory => 420,
            BuildableKind.Farm => 180,
            BuildableKind.MinePost => 220,
            BuildableKind.LumberCamp => 180,
            BuildableKind.Quarry => 220,
            BuildableKind.Smelter => 180,
            BuildableKind.Steelworks => 200,
            BuildableKind.MachineShop => 200,
            BuildableKind.Armory => 220,
            BuildableKind.OilDerrick => 220,
            BuildableKind.PowerPlant => 240,
            BuildableKind.Clinic => 120,
            BuildableKind.DrillRig => 80,
            BuildableKind.Refinery => 260,
            _ => 0
        };

        public static TechEra RequiredEraFor(BuildableKind kind) => kind switch
        {
            BuildableKind.Smelter or BuildableKind.Steelworks or BuildableKind.DrillRig => TechEra.Iron,
            BuildableKind.Armory => TechEra.Gunpowder,
            BuildableKind.MachineShop or BuildableKind.VehicleFactory or BuildableKind.TrainFactory or BuildableKind.Station or BuildableKind.OilDerrick or BuildableKind.PowerPlant or BuildableKind.Refinery => TechEra.Industrial,
            _ => TechEra.WoodStone
        };

        public static bool IsStorageNode(BuildableKind kind) => StorageCapacityFor(kind) > 0;

        public static bool EraUnlocked(TechEra current, TechEra required) => current >= required;

        public static bool CanSpend(ResourceInventory inventory, IReadOnlyList<ResourceAmount> amounts, out string missing)
        {
            missing = "";
            for (int i = 0; i < amounts.Count; i++)
            {
                var amount = amounts[i];
                if (inventory.Get(amount.Kind) >= amount.Amount) continue;
                missing = $"{amount.Kind} {inventory.Get(amount.Kind)}/{amount.Amount}";
                return false;
            }
            return true;
        }

        public static void Spend(ResourceInventory inventory, IReadOnlyList<ResourceAmount> amounts)
        {
            for (int i = 0; i < amounts.Count; i++)
                inventory.Add(amounts[i].Kind, -amounts[i].Amount);
        }

        public static void Add(ResourceInventory inventory, IReadOnlyList<ResourceAmount> amounts)
        {
            for (int i = 0; i < amounts.Count; i++)
                inventory.Add(amounts[i].Kind, amounts[i].Amount);
        }

        private static ProductionRecipeDef Recipe(string id, BuildableKind building, TechEra era, ResourceAmount[] inputs, ResourceAmount[] outputs, int workers, string blockedLabel)
        {
            return new ProductionRecipeDef
            {
                Id = id,
                Building = building,
                RequiredEra = era,
                Inputs = inputs,
                Outputs = outputs,
                Workers = workers,
                BlockedLabel = blockedLabel
            };
        }

        private static VehicleDef Vehicle(VehicleKind kind, string displayName, TechEra era, ResourceAmount[] cost)
        {
            return new VehicleDef { Kind = kind, DisplayName = displayName, RequiredEra = era, Cost = cost };
        }

        private static TechDef Tech(string id, string displayName, TechEra requiredEra, TechEra unlockEra, int ticks, int engineers, ResourceAmount[] costPerTick, string next)
        {
            return new TechDef
            {
                Id = id,
                DisplayName = displayName,
                RequiredEra = requiredEra,
                UnlockEra = unlockEra,
                ResearchTicks = ticks,
                Engineers = engineers,
                CostPerTick = costPerTick,
                NextResearch = next
            };
        }

        private static ResourceAmount[] Amounts(params (ResourceKind kind, int amount)[] values)
        {
            var amounts = new ResourceAmount[values.Length];
            for (int i = 0; i < values.Length; i++)
                amounts[i] = new ResourceAmount(values[i].kind, values[i].amount);
            return amounts;
        }
    }
}
