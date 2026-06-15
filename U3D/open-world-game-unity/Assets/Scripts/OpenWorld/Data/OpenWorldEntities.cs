using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
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
        public int EscortVehicleId;
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
}
