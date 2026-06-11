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
