using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public enum SurfaceTerrain
    {
        Plains,
        Forest,
        Hills,
        Mountain,
        Water,
        Road,
        Rail,
        Bridge,
        Shallows
    }

    public enum GroundMaterial
    {
        Dirt,
        Stone,
        IronOre,
        Coal,
        Clay,
        Wood,
        Food,
        Sulfur,
        Nitrate,
        Oil
    }

    public enum TerrainTool
    {
        None,
        Dig,
        Fill,
        Flatten,
        Ramp,
        Road,
        Trench,
        Rail,
        Bridge,
        Mine
    }

    public enum BuildableKind
    {
        TownCenter,
        Warehouse,
        House,
        Farm,
        MinePost,
        LumberCamp,
        Quarry,
        Smelter,
        Steelworks,
        MachineShop,
        Armory,
        Clinic,
        Market,
        Garage,
        VehicleFactory,
        TrainFactory,
        Station,
        ScoutTower,
        ControlPoint,
        Barracks,
        Wall,
        Gate,
        Tower,
        Roadblock,
        Bunker,
        Bridge,
        Dock,
        OilDerrick,
        PowerPlant,
        DrillRig,
        Refinery
    }

    public enum UnitKind
    {
        Worker,
        Hauler,
        Militia,
        Melee,
        Spearman,
        Ranged,
        Musketeer,
        Rifleman,
        MachineGunner,
        Artillery,
        Engineer,
        Scout,
        Medic
    }

    public enum UnitTask
    {
        Idle,
        Moving,
        Digging,
        Building,
        Hauling,
        Attacking,
        Defending,
        Scouting,
        Patrolling,
        Healing,
        Transporting,
        Refueling,
        Repairing,
        Surveying,
        Drilling
    }

    public enum FactionKind
    {
        Player,
        Enemy,
        Neutral,
        Ally
    }

    public enum DiplomacyStance
    {
        Hostile,
        Neutral,
        Trade,
        Allied
    }

    public enum KnowledgeState
    {
        Unknown,
        Explored,
        Visible
    }

    public enum StrategicOverlay
    {
        Exploration,
        Geology,
        Territory,
        Resources,
        Supply,
        RoadsRails,
        Logistics,
        EnemyIntel,
        Blueprints,
        MoraleMedical
    }

    public enum BlueprintKind
    {
        Building,
        Terrain,
        Road,
        Rail,
        Bridge,
        MiningZone,
        DefenseZone
    }

    public enum BlueprintStatus
    {
        Active,
        Paused,
        Complete,
        Cancelled,
        Blocked
    }

    public enum VehicleKind
    {
        HandCart,
        Wagon,
        Truck,
        ArmoredCar,
        Locomotive,
        CargoWagon,
        Tank,
        Aircraft,
        TransportPlane
    }

    public enum VehicleTask
    {
        Idle,
        Moving,
        Loading,
        Unloading,
        AutoTransport,
        Escort,
        Patrol,
        Refuel,
        Repair,
        Disabled
    }

    public enum ResourceKind
    {
        Dirt,
        Stone,
        IronOre,
        Coal,
        Clay,
        Wood,
        Food,
        Sulfur,
        Nitrate,
        Oil,
        Lumber,
        Brick,
        IronIngot,
        Steel,
        MachineParts,
        Medicine,
        Ammo,
        Gunpowder,
        Fuel,
        Power,
        Weapons,
        RailParts
    }

    public enum TechEra
    {
        WoodStone,
        Iron,
        Gunpowder,
        Industrial,
        Aviation
    }

    public enum LogisticsMode
    {
        Automatic,
        Manual
    }

    public enum SurveyState
    {
        Unknown,
        Suspected,
        Surveyed,
        Drilled,
        Exhausted
    }

    public enum UnitOrderKind
    {
        Move,
        Attack,
        Patrol,
        Defend,
        Escort,
        Survey,
        Drill
    }

    public enum SimulationTier
    {
        HighFrequency,
        LowFrequency,
        Dormant
    }

    public enum CommandKind
    {
        BuildBlueprint,
        CancelBlueprint,
        CancelAllBlueprints,
        PauseBlueprint,
        ResumeBlueprint,
        SetBlueprintPriority,
        TerrainBrush,
        Produce,
        Research,
        Move,
        Attack,
        Patrol,
        SetDefenseArea,
        SetTransportPolicy,
        ToggleRouteMode,
        AdjustRoutePriority,
        AdjustRouteTargetStock,
        CycleRouteCargo,
        ProduceVehicle,
        LoadCargo,
        UnloadCargo,
        Diplomacy,
        Scout,
        AssignWorkers,
        GeologicalSurvey,
        CoreDrill,
        AssignMiningZone,
        TrainUnit,
        CreateRoute,
        RepairVehicle,
        RefuelVehicle,
        SetRailSchedule,
        Trade
    }

    [Serializable]
    public struct MaterialLayer
    {
        public GroundMaterial Material;
        public int Thickness;
        public float Grade;
        public float Hardness;
        public float WaterRisk;
        public int RemainingAmount;

        public MaterialLayer(GroundMaterial material, int thickness)
            : this(material, thickness, DefaultGrade(material), DefaultHardness(material), DefaultWaterRisk(material), DefaultReserve(material, thickness))
        {
        }

        public MaterialLayer(GroundMaterial material, int thickness, float grade, float hardness, float waterRisk, int remainingAmount)
        {
            Material = material;
            Thickness = thickness;
            Grade = grade;
            Hardness = hardness;
            WaterRisk = waterRisk;
            RemainingAmount = remainingAmount;
        }

        public static float DefaultGrade(GroundMaterial material) => material switch
        {
            GroundMaterial.IronOre => 0.65f,
            GroundMaterial.Coal => 0.72f,
            GroundMaterial.Sulfur => 0.55f,
            GroundMaterial.Nitrate => 0.50f,
            GroundMaterial.Oil => 0.78f,
            GroundMaterial.Stone => 0.85f,
            GroundMaterial.Clay => 0.80f,
            _ => 1f
        };

        public static float DefaultHardness(GroundMaterial material) => material switch
        {
            GroundMaterial.Stone => 1.35f,
            GroundMaterial.IronOre => 1.55f,
            GroundMaterial.Coal => 1.15f,
            GroundMaterial.Sulfur => 1.25f,
            GroundMaterial.Nitrate => 1.10f,
            _ => 0.75f
        };

        public static float DefaultWaterRisk(GroundMaterial material) => material is GroundMaterial.Clay or GroundMaterial.Oil ? 0.35f : 0.08f;

        public static int DefaultReserve(GroundMaterial material, int thickness)
        {
            int multiplier = material is GroundMaterial.IronOre or GroundMaterial.Coal or GroundMaterial.Sulfur or GroundMaterial.Nitrate or GroundMaterial.Oil ? 12 : 6;
            return Mathf.Max(1, thickness * multiplier);
        }
    }

    [Serializable]
    public struct SurfaceCell
    {
        public float Height;
        public SurfaceTerrain Terrain;
        public MaterialLayer[] Layers;
        public int CurrentLayer;
        public bool HasRoad;
        public bool HasRail;
        public bool HasBridge;
        public bool HasTrench;
        public bool Occupied;
        public int BuildingId;
        public int RegionId;
        public int ResourceRichness;

        public float MoveCost
        {
            get
            {
                if (Occupied && !HasRoad) return 9999f;
                float cost = Terrain switch
                {
                    SurfaceTerrain.Forest => 1.5f,
                    SurfaceTerrain.Hills => 1.35f,
                    SurfaceTerrain.Mountain => 1.8f,
                    SurfaceTerrain.Water => 9999f,
                    _ => 1f
                };
                if (HasRoad) cost *= 0.55f;
                if (HasRail) cost *= 0.35f;
                if (HasBridge) cost *= 0.65f;
                if (HasTrench) cost *= 2.5f;
                return cost;
            }
        }

        public GroundMaterial TopMaterial
        {
            get
            {
                if (Layers == null || Layers.Length == 0) return GroundMaterial.Dirt;
                int idx = Mathf.Clamp(CurrentLayer, 0, Layers.Length - 1);
                return Layers[idx].Material;
            }
        }
    }

    [Serializable]
    public class ResourceInventory
    {
        public int Dirt;
        public int Stone;
        public int IronOre;
        public int Coal;
        public int Clay;
        public int Wood;
        public int Food;
        public int Sulfur;
        public int Nitrate;
        public int Oil;
        public int Lumber;
        public int Brick;
        public int IronIngot;
        public int Steel;
        public int MachineParts;
        public int Medicine;
        public int Ammo;
        public int Gunpowder;
        public int Fuel;
        public int Power;
        public int Weapons;
        public int RailParts;

        public int Total => Dirt + Stone + IronOre + Coal + Clay + Wood + Food + Sulfur + Nitrate + Oil
            + Lumber + Brick + IronIngot + Steel + MachineParts + Medicine + Ammo + Gunpowder + Fuel
            + Power + Weapons + RailParts;

        public void Add(GroundMaterial material, int amount)
        {
            switch (material)
            {
                case GroundMaterial.Dirt: Dirt += amount; break;
                case GroundMaterial.Stone: Stone += amount; break;
                case GroundMaterial.IronOre: IronOre += amount; break;
                case GroundMaterial.Coal: Coal += amount; break;
                case GroundMaterial.Clay: Clay += amount; break;
                case GroundMaterial.Wood: Wood += amount; break;
                case GroundMaterial.Food: Food += amount; break;
                case GroundMaterial.Sulfur: Sulfur += amount; break;
                case GroundMaterial.Nitrate: Nitrate += amount; break;
                case GroundMaterial.Oil: Oil += amount; break;
            }
        }

        public int Get(ResourceKind kind) => kind switch
        {
            ResourceKind.Dirt => Dirt,
            ResourceKind.Stone => Stone,
            ResourceKind.IronOre => IronOre,
            ResourceKind.Coal => Coal,
            ResourceKind.Clay => Clay,
            ResourceKind.Wood => Wood,
            ResourceKind.Food => Food,
            ResourceKind.Sulfur => Sulfur,
            ResourceKind.Nitrate => Nitrate,
            ResourceKind.Oil => Oil,
            ResourceKind.Lumber => Lumber,
            ResourceKind.Brick => Brick,
            ResourceKind.IronIngot => IronIngot,
            ResourceKind.Steel => Steel,
            ResourceKind.MachineParts => MachineParts,
            ResourceKind.Medicine => Medicine,
            ResourceKind.Ammo => Ammo,
            ResourceKind.Gunpowder => Gunpowder,
            ResourceKind.Fuel => Fuel,
            ResourceKind.Power => Power,
            ResourceKind.Weapons => Weapons,
            ResourceKind.RailParts => RailParts,
            _ => 0
        };

        public void Add(ResourceKind kind, int amount)
        {
            switch (kind)
            {
                case ResourceKind.Dirt: Dirt += amount; break;
                case ResourceKind.Stone: Stone += amount; break;
                case ResourceKind.IronOre: IronOre += amount; break;
                case ResourceKind.Coal: Coal += amount; break;
                case ResourceKind.Clay: Clay += amount; break;
                case ResourceKind.Wood: Wood += amount; break;
                case ResourceKind.Food: Food += amount; break;
                case ResourceKind.Sulfur: Sulfur += amount; break;
                case ResourceKind.Nitrate: Nitrate += amount; break;
                case ResourceKind.Oil: Oil += amount; break;
                case ResourceKind.Lumber: Lumber += amount; break;
                case ResourceKind.Brick: Brick += amount; break;
                case ResourceKind.IronIngot: IronIngot += amount; break;
                case ResourceKind.Steel: Steel += amount; break;
                case ResourceKind.MachineParts: MachineParts += amount; break;
                case ResourceKind.Medicine: Medicine += amount; break;
                case ResourceKind.Ammo: Ammo += amount; break;
                case ResourceKind.Gunpowder: Gunpowder += amount; break;
                case ResourceKind.Fuel: Fuel += amount; break;
                case ResourceKind.Power: Power += amount; break;
                case ResourceKind.Weapons: Weapons += amount; break;
                case ResourceKind.RailParts: RailParts += amount; break;
            }
        }

        public bool Spend(ResourceKind kind, int amount)
        {
            if (Get(kind) < amount) return false;
            Add(kind, -amount);
            return true;
        }

        public int AddLimited(ResourceKind kind, int amount, int capacity)
        {
            if (amount <= 0 || capacity <= Total) return 0;
            int accepted = Mathf.Min(amount, capacity - Total);
            Add(kind, accepted);
            return accepted;
        }

        public bool Spend(BuildCost cost)
        {
            if (Dirt < cost.Dirt || Stone < cost.Stone || IronOre < cost.IronOre || Wood < cost.Wood || Food < cost.Food)
                return false;
            Dirt -= cost.Dirt;
            Stone -= cost.Stone;
            IronOre -= cost.IronOre;
            Wood -= cost.Wood;
            Food -= cost.Food;
            return true;
        }
    }

    [Serializable]
    public struct BuildCost
    {
        public int Dirt;
        public int Stone;
        public int IronOre;
        public int Wood;
        public int Food;

        public BuildCost(int dirt, int stone, int ironOre, int wood, int food)
        {
            Dirt = dirt;
            Stone = stone;
            IronOre = ironOre;
            Wood = wood;
            Food = food;
        }
    }

    [Serializable]
    public class BuildableDef
    {
        public BuildableKind Kind;
        public string DisplayName = "";
        public Vector2Int Size = Vector2Int.one;
        public BuildCost Cost;
        public int MaxHp = 100;
        public bool BlocksMovement = true;
        public bool RequiresFlatGround = true;
        public bool IsDefense;
        public bool IsIndustrial;
        public bool IsTransport;
        public bool IsMedical;
        public bool ProvidesVision;
        public int VisionRange = 10;
        public Color Color = Color.white;

        public static List<BuildableDef> Defaults() => new()
        {
            new BuildableDef { Kind = BuildableKind.TownCenter, DisplayName = "Town Center", Size = new Vector2Int(4, 4), Cost = new BuildCost(0, 40, 0, 30, 0), MaxHp = 900, Color = new Color(0.72f, 0.58f, 0.38f) },
            new BuildableDef { Kind = BuildableKind.Warehouse, DisplayName = "Warehouse", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 20, 0, 20, 0), MaxHp = 400, Color = new Color(0.55f, 0.42f, 0.28f) },
            new BuildableDef { Kind = BuildableKind.House, DisplayName = "House", Size = new Vector2Int(2, 2), Cost = new BuildCost(0, 8, 0, 15, 0), MaxHp = 180, Color = new Color(0.78f, 0.68f, 0.50f) },
            new BuildableDef { Kind = BuildableKind.Farm, DisplayName = "Farm", Size = new Vector2Int(4, 4), Cost = new BuildCost(8, 0, 0, 5, 0), MaxHp = 80, BlocksMovement = false, Color = new Color(0.38f, 0.72f, 0.30f) },
            new BuildableDef { Kind = BuildableKind.MinePost, DisplayName = "Mine Post", Size = new Vector2Int(2, 2), Cost = new BuildCost(0, 18, 0, 12, 0), MaxHp = 220, Color = new Color(0.42f, 0.42f, 0.42f) },
            new BuildableDef { Kind = BuildableKind.LumberCamp, DisplayName = "Lumber Camp", Size = new Vector2Int(2, 2), Cost = new BuildCost(0, 10, 0, 10, 0), MaxHp = 180, Color = new Color(0.27f, 0.42f, 0.22f) },
            new BuildableDef { Kind = BuildableKind.Quarry, DisplayName = "Quarry", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 12, 0, 10, 0), MaxHp = 240, IsIndustrial = true, Color = new Color(0.47f, 0.48f, 0.44f) },
            new BuildableDef { Kind = BuildableKind.Smelter, DisplayName = "Smelter", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 28, 20, 18, 0), MaxHp = 360, IsIndustrial = true, Color = new Color(0.58f, 0.30f, 0.20f) },
            new BuildableDef { Kind = BuildableKind.Steelworks, DisplayName = "Steelworks", Size = new Vector2Int(4, 4), Cost = new BuildCost(0, 40, 35, 20, 0), MaxHp = 520, IsIndustrial = true, Color = new Color(0.32f, 0.34f, 0.36f) },
            new BuildableDef { Kind = BuildableKind.MachineShop, DisplayName = "Machine Shop", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 25, 30, 18, 0), MaxHp = 420, IsIndustrial = true, Color = new Color(0.42f, 0.47f, 0.52f) },
            new BuildableDef { Kind = BuildableKind.Armory, DisplayName = "Armory", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 25, 30, 16, 0), MaxHp = 420, IsIndustrial = true, Color = new Color(0.44f, 0.18f, 0.18f) },
            new BuildableDef { Kind = BuildableKind.Clinic, DisplayName = "Clinic", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 18, 6, 18, 0), MaxHp = 260, IsMedical = true, Color = new Color(0.72f, 0.86f, 0.84f) },
            new BuildableDef { Kind = BuildableKind.Market, DisplayName = "Market", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 12, 4, 22, 0), MaxHp = 220, Color = new Color(0.74f, 0.62f, 0.30f) },
            new BuildableDef { Kind = BuildableKind.Garage, DisplayName = "Garage", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 24, 18, 12, 0), MaxHp = 380, IsTransport = true, Color = new Color(0.34f, 0.40f, 0.48f) },
            new BuildableDef { Kind = BuildableKind.VehicleFactory, DisplayName = "Vehicle Factory", Size = new Vector2Int(4, 4), Cost = new BuildCost(0, 38, 35, 20, 0), MaxHp = 560, IsIndustrial = true, IsTransport = true, Color = new Color(0.24f, 0.30f, 0.36f) },
            new BuildableDef { Kind = BuildableKind.TrainFactory, DisplayName = "Train Factory", Size = new Vector2Int(5, 4), Cost = new BuildCost(0, 50, 55, 24, 0), MaxHp = 680, IsIndustrial = true, IsTransport = true, Color = new Color(0.20f, 0.24f, 0.28f) },
            new BuildableDef { Kind = BuildableKind.Station, DisplayName = "Station", Size = new Vector2Int(4, 2), Cost = new BuildCost(0, 30, 28, 18, 0), MaxHp = 420, IsTransport = true, Color = new Color(0.46f, 0.36f, 0.25f) },
            new BuildableDef { Kind = BuildableKind.ScoutTower, DisplayName = "Scout Tower", Size = new Vector2Int(2, 2), Cost = new BuildCost(0, 12, 4, 18, 0), MaxHp = 240, IsDefense = true, ProvidesVision = true, VisionRange = 28, Color = new Color(0.50f, 0.48f, 0.32f) },
            new BuildableDef { Kind = BuildableKind.ControlPoint, DisplayName = "Control Point", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 10, 0, 12, 0), MaxHp = 500, ProvidesVision = true, VisionRange = 22, Color = new Color(0.25f, 0.55f, 0.70f) },
            new BuildableDef { Kind = BuildableKind.Barracks, DisplayName = "Barracks", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 30, 15, 20, 0), MaxHp = 500, Color = new Color(0.50f, 0.25f, 0.20f) },
            new BuildableDef { Kind = BuildableKind.Wall, DisplayName = "Wall", Size = new Vector2Int(1, 1), Cost = new BuildCost(0, 5, 0, 0, 0), MaxHp = 350, IsDefense = true, Color = new Color(0.62f, 0.62f, 0.58f) },
            new BuildableDef { Kind = BuildableKind.Gate, DisplayName = "Gate", Size = new Vector2Int(2, 1), Cost = new BuildCost(0, 8, 4, 4, 0), MaxHp = 300, IsDefense = true, Color = new Color(0.45f, 0.32f, 0.20f) },
            new BuildableDef { Kind = BuildableKind.Tower, DisplayName = "Tower", Size = new Vector2Int(2, 2), Cost = new BuildCost(0, 18, 8, 0, 0), MaxHp = 450, IsDefense = true, Color = new Color(0.55f, 0.55f, 0.62f) },
            new BuildableDef { Kind = BuildableKind.Roadblock, DisplayName = "Roadblock", Size = new Vector2Int(1, 1), Cost = new BuildCost(2, 3, 0, 2, 0), MaxHp = 120, IsDefense = true, Color = new Color(0.35f, 0.25f, 0.17f) },
            new BuildableDef { Kind = BuildableKind.Bunker, DisplayName = "Bunker", Size = new Vector2Int(2, 2), Cost = new BuildCost(4, 28, 18, 2, 0), MaxHp = 620, IsDefense = true, Color = new Color(0.38f, 0.38f, 0.34f) },
            new BuildableDef { Kind = BuildableKind.Bridge, DisplayName = "Bridge", Size = new Vector2Int(3, 1), Cost = new BuildCost(0, 10, 8, 16, 0), MaxHp = 260, BlocksMovement = false, IsTransport = true, RequiresFlatGround = false, Color = new Color(0.50f, 0.36f, 0.22f) },
            new BuildableDef { Kind = BuildableKind.Dock, DisplayName = "Dock", Size = new Vector2Int(3, 2), Cost = new BuildCost(0, 10, 5, 20, 0), MaxHp = 320, IsTransport = true, RequiresFlatGround = false, Color = new Color(0.42f, 0.32f, 0.20f) },
            new BuildableDef { Kind = BuildableKind.OilDerrick, DisplayName = "Oil Derrick", Size = new Vector2Int(3, 3), Cost = new BuildCost(0, 30, 35, 18, 0), MaxHp = 400, IsIndustrial = true, Color = new Color(0.12f, 0.12f, 0.10f) },
            new BuildableDef { Kind = BuildableKind.PowerPlant, DisplayName = "Power Plant", Size = new Vector2Int(4, 4), Cost = new BuildCost(0, 40, 35, 20, 0), MaxHp = 560, IsIndustrial = true, Color = new Color(0.18f, 0.26f, 0.30f) },
            new BuildableDef { Kind = BuildableKind.DrillRig, DisplayName = "Core Drill Rig", Size = new Vector2Int(2, 2), Cost = new BuildCost(0, 12, 8, 8, 0), MaxHp = 260, IsIndustrial = true, Color = new Color(0.82f, 0.56f, 0.18f) },
            new BuildableDef { Kind = BuildableKind.Refinery, DisplayName = "Refinery", Size = new Vector2Int(4, 4), Cost = new BuildCost(0, 42, 40, 20, 0), MaxHp = 540, IsIndustrial = true, Color = new Color(0.28f, 0.24f, 0.18f) }
        };
    }

    [Serializable]
    public class BuildingEntity
    {
        public int Id;
        public BuildableKind Kind;
        public Vector2Int Origin;
        public Vector2Int Size;
        public int Rotation;
        public int Hp;
        public int FactionId;
        public bool UnderConstruction;
        public float WorkProgress;
        public int RegionId;
        public ResourceInventory Storage = new();
        public int StorageCapacity;
        public string LastStorageStatus = "Empty";
        public string ActiveRecipeId = "";
        public int AssignedWorkers;
        public float ProductionProgress;
        public string ProductionStatus = "Idle";
    }

    [Serializable]
    public class UnitEntity
    {
        public int Id;
        public UnitKind Kind;
        public int FactionId;
        public Vector2Int Cell;
        public Vector3 WorldPosition;
        public UnitTask Task;
        public int CarryAmount;
        public GroundMaterial CarryMaterial;
        public int AttackPower = 8;
        public int Hp = 100;
        public int MaxHp = 100;
        public float Morale = 100f;
        public float Fatigue;
        public float Supplies = 100f;
        public float Ammo = 20f;
        public bool Wounded;
        public int VisionRange = 12;
        public float Armor;
        public float AttackRange = 1.5f;
        public float Accuracy = 0.65f;
        public float Suppression;
        public SimulationTier SimulationTier = SimulationTier.HighFrequency;
        public UnitOrder CurrentOrder = new();
    }

    [Serializable]
    public class JobRecord
    {
        public int Id;
        public UnitTask Task;
        public Vector2Int TargetCell;
        public BuildableKind BuildKind;
        public int AssignedUnitId;
        public float WorkRemaining = 1f;
        public int Priority = 3;
        public BlueprintStatus Status = BlueprintStatus.Active;
        public int Radius;
        public GroundMaterial TargetMaterial;
        public int RelatedEntityId;
        public string BlockedReason = "";
    }

    [Serializable]
    public class FactionRecord
    {
        public int Id;
        public string Name = "";
        public FactionKind Kind;
        public Color Color = Color.white;
        public int Reputation = 50;
    }

    [Serializable]
    public class DiplomacyRecord
    {
        public int FactionA;
        public int FactionB;
        public DiplomacyStance Stance;
        public int Trust;
    }

    [Serializable]
    public class RegionRecord
    {
        public int Id;
        public string Name = "";
        public Vector2Int Center;
        public int Radius;
        public int OwnerFactionId;
        public int Control;
        public ResourceKind StrategicResource = ResourceKind.Food;
        public bool HasRailHub;
        public bool HasNeutralTown;
    }

    [Serializable]
    public class StrategicSiteRecord
    {
        public int Id;
        public string Name = "";
        public Vector2Int Cell;
        public int FactionId;
        public BuildableKind SiteKind;
        public ResourceKind Resource;
        public int RegionId;
        public bool Revealed;
    }

    [Serializable]
    public class BlueprintJob
    {
        public int Id;
        public BlueprintKind Kind;
        public BlueprintStatus Status = BlueprintStatus.Active;
        public Vector2Int Cell;
        public int Radius;
        public TerrainTool Tool;
        public BuildableKind BuildKind;
        public int Priority = 3;
        public float WorkRemaining = 2f;
        public int FactionId;
        public string BlockedReason = "";
        public int AssignedUnitId;
        public bool MaterialsReserved;
    }

    [Serializable]
    public class VehicleEntity
    {
        public int Id;
        public VehicleKind Kind;
        public int FactionId;
        public Vector2Int Cell;
        public Vector3 WorldPosition;
        public VehicleTask Task;
        public int Hp = 120;
        public int MaxHp = 120;
        public float Fuel = 100f;
        public float Condition = 100f;
        public ResourceKind CargoKind = ResourceKind.Food;
        public int CargoAmount;
        public int CargoCapacity = 40;
        public int CrewRequired = 1;
        public int VisionRange = 10;
        public int AssignedRouteId;
        public string StatusText = "Idle";
        public SimulationTier SimulationTier = SimulationTier.HighFrequency;
        public int EscortUnitId;
    }

    [Serializable]
    public class LogisticsRoute
    {
        public int Id;
        public string Name = "";
        public LogisticsMode Mode = LogisticsMode.Automatic;
        public Vector2Int Source;
        public Vector2Int Target;
        public int SourceBuildingId;
        public int TargetBuildingId;
        public ResourceKind CargoKind = ResourceKind.Food;
        public int TargetStock = 50;
        public int Priority = 3;
        public VehicleKind PreferredVehicle = VehicleKind.HandCart;
        public string Status = "Waiting";
        public float Risk;
        public int AssignedVehicleId;
    }

    [Serializable]
    public class PopulationState
    {
        public int Residents = 16;
        public int Workers = 8;
        public int Soldiers = 1;
        public int Drivers = 1;
        public int Engineers = 2;
        public int Doctors = 1;
        public int Homeless;
        public int Wounded;
        public float CityMorale = 82f;
        public float MedicalPressure;
    }

    [Serializable]
    public class TechState
    {
        public TechEra Era = TechEra.WoodStone;
        public int ResearchProgress;
        public string CurrentResearch = "Iron Working";
        public List<string> CompletedResearch = new();
    }

    [Serializable]
    public class UnitOrder
    {
        public UnitOrderKind Kind = UnitOrderKind.Move;
        public Vector2Int TargetCell;
        public Vector2Int SecondaryCell;
        public int TargetEntityId;
        public int Priority = 3;
        public bool Queued;
    }

    [Serializable]
    public class ProductionOrder
    {
        public int Id;
        public int BuildingId;
        public string RecipeId = "";
        public int RemainingCycles = 1;
        public int Priority = 3;
        public bool Paused;
        public string Status = "Waiting";
    }

    [Serializable]
    public class ResearchOrder
    {
        public int Id;
        public string TechId = "";
        public int Priority = 3;
        public float Progress;
        public bool Paused;
        public string Status = "Waiting";
    }

    [Serializable]
    public class WorkerAssignment
    {
        public int BuildingId;
        public int Workers;
        public int Engineers;
        public int Doctors;
        public int Drivers;
    }

    [Serializable]
    public class TradeContract
    {
        public int Id;
        public int PartnerFactionId;
        public ResourceKind ExportKind;
        public ResourceKind ImportKind;
        public int Amount = 10;
        public int Priority = 3;
        public bool Active = true;
        public string Status = "Waiting";
    }

    [Serializable]
    public class IntelSnapshot
    {
        public int EntityId;
        public int FactionId;
        public Vector2Int Cell;
        public float SeenAt;
        public string EntityType = "";
    }

    [Serializable]
    public class RailSchedule
    {
        public int Id;
        public int LocomotiveId;
        public List<int> WagonIds = new();
        public List<int> StationBuildingIds = new();
        public ResourceKind CargoKind;
        public int CurrentStop;
        public bool Active = true;
        public string Status = "Waiting";
    }

    [Serializable]
    public class RepairRefuelOrder
    {
        public int Id;
        public int VehicleId;
        public bool Refuel;
        public bool Repair;
        public int ServiceBuildingId;
        public int Priority = 3;
        public string Status = "Waiting";
    }

    [Serializable]
    public class SurveyRecord
    {
        public Vector2Int Cell;
        public SurveyState State;
        public float Confidence;
        public GroundMaterial EstimatedMaterial;
        public int MinDepth;
        public int MaxDepth;
        public float MinGrade;
        public float MaxGrade;
        public int MinReserve;
        public int MaxReserve;
        public float SurveyedAt;
    }

    [Serializable]
    public class DrillLayerReport
    {
        public GroundMaterial Material;
        public int StartDepth;
        public int EndDepth;
        public float Grade;
        public float Hardness;
        public float WaterRisk;
        public int RemainingAmount;
    }

    [Serializable]
    public class DrillReport
    {
        public int Id;
        public Vector2Int Cell;
        public int DrillRigBuildingId;
        public float CompletedAt;
        public List<DrillLayerReport> Layers = new();
    }

    [Serializable]
    public class MiningZoneRecord
    {
        public int Id;
        public Vector2Int Center;
        public int Radius = 2;
        public GroundMaterial TargetMaterial = GroundMaterial.IronOre;
        public int MineBuildingId;
        public int Priority = 3;
        public bool Active = true;
        public int ExtractedAmount;
        public string Status = "Waiting";
    }

    [Serializable]
    public class OpenWorldCommand
    {
        public int Id;
        public CommandKind Kind;
        public int FactionId;
        public Vector2Int Cell;
        public Vector2Int TargetCell;
        public TerrainTool TerrainTool;
        public BuildableKind BuildKind;
        public VehicleKind VehicleKind;
        public UnitKind UnitKind;
        public ResourceKind ResourceKind;
        public LogisticsMode LogisticsMode;
        public int Priority = 3;
        public int Amount;
        public int EntityId;
        public int SecondaryEntityId;
        public string Text = "";
    }

    public static class OpenWorldConstants
    {
        public const int PlayerFactionId = 1;
        public const int EnemyFactionId = 2;
        public const int NeutralFactionId = 3;
        public const int AllyFactionId = 4;
    }
}
