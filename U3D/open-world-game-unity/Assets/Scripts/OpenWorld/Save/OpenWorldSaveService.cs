using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenWorld
{
    [Serializable]
    public class OpenWorldSaveData
    {
        public int Version = 2;
        public int MapSize;
        public int ChunkSize;
        public int Seed;
        public ResourceInventory Inventory = new();
        public List<SavedCell> ModifiedCells = new();
        public List<BuildingEntity> Buildings = new();
        public List<UnitEntity> Units = new();
        public List<VehicleEntity> Vehicles = new();
        public List<JobRecord> Jobs = new();
        public List<BlueprintJob> Blueprints = new();
        public List<FactionRecord> Factions = new();
        public List<RegionRecord> Regions = new();
        public List<StrategicSiteRecord> StrategicSites = new();
        public List<DiplomacyRecord> Diplomacy = new();
        public List<LogisticsRoute> LogisticsRoutes = new();
        public List<ProductionOrder> ProductionOrders = new();
        public List<ResearchOrder> ResearchOrders = new();
        public List<WorkerAssignment> WorkerAssignments = new();
        public List<TradeContract> TradeContracts = new();
        public List<IntelSnapshot> IntelSnapshots = new();
        public List<RailSchedule> RailSchedules = new();
        public List<RepairRefuelOrder> ServiceOrders = new();
        public List<SurveyRecord> Surveys = new();
        public List<DrillReport> DrillReports = new();
        public List<MiningZoneRecord> MiningZones = new();
        public PopulationState Population = new();
        public TechState Tech = new();
        public KnowledgeState[] KnowledgeCells;
        public List<SavedKnowledgeCell> Knowledge = new();
    }

    [Serializable]
    public class SavedCell
    {
        public int X;
        public int Z;
        public SurfaceCell Cell;
    }

    [Serializable]
    public class SavedKnowledgeCell
    {
        public int X;
        public int Z;
        public KnowledgeState State;
    }

    public static class OpenWorldSaveService
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "open_world_surface_save.json");

        public static void Save(OpenWorldState world)
        {
            var data = new OpenWorldSaveData
            {
                MapSize = world.MapSize,
                ChunkSize = world.ChunkSize,
                Seed = world.Seed,
                Inventory = world.Inventory,
                Buildings = new List<BuildingEntity>(world.Buildings.Values),
                Units = new List<UnitEntity>(world.Units.Values),
                Vehicles = new List<VehicleEntity>(world.Vehicles.Values),
                Jobs = new List<JobRecord>(world.Jobs),
                Blueprints = new List<BlueprintJob>(world.Blueprints),
                Factions = new List<FactionRecord>(world.Factions),
                Regions = new List<RegionRecord>(world.Regions),
                StrategicSites = new List<StrategicSiteRecord>(world.StrategicSites),
                Diplomacy = new List<DiplomacyRecord>(world.Diplomacy),
                LogisticsRoutes = new List<LogisticsRoute>(world.LogisticsRoutes),
                ProductionOrders = new List<ProductionOrder>(world.ProductionOrders),
                ResearchOrders = new List<ResearchOrder>(world.ResearchOrders),
                WorkerAssignments = new List<WorkerAssignment>(world.WorkerAssignments),
                TradeContracts = new List<TradeContract>(world.TradeContracts),
                IntelSnapshots = new List<IntelSnapshot>(world.IntelSnapshots),
                RailSchedules = new List<RailSchedule>(world.RailSchedules),
                ServiceOrders = new List<RepairRefuelOrder>(world.ServiceOrders),
                Surveys = new List<SurveyRecord>(world.Surveys),
                DrillReports = new List<DrillReport>(world.DrillReports),
                MiningZones = new List<MiningZoneRecord>(world.MiningZones),
                Population = world.Population,
                Tech = world.Tech
            };

            foreach (var cell in world.ModifiedCells)
            {
                if (!world.InBounds(cell)) continue;
                data.ModifiedCells.Add(new SavedCell { X = cell.x, Z = cell.y, Cell = world.GetCell(cell) });
            }

            if (world.KnowledgeCells != null)
            {
                for (int i = 0; i < world.KnowledgeCells.Length; i++)
                {
                    var state = world.KnowledgeCells[i];
                    if (state == KnowledgeState.Unknown) continue;
                    data.Knowledge.Add(new SavedKnowledgeCell
                    {
                        X = i % world.MapSize,
                        Z = i / world.MapSize,
                        State = state
                    });
                }
            }

            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[OpenWorld] Saved to {SavePath}");
        }

        public static bool TryLoad(out OpenWorldSaveData data)
        {
            data = null;
            if (!File.Exists(SavePath)) return false;

            try
            {
                data = JsonUtility.FromJson<OpenWorldSaveData>(File.ReadAllText(SavePath));
                Migrate(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenWorld] Failed to read save: {ex.Message}");
                return false;
            }

            if (data == null || data.MapSize <= 0 || data.ChunkSize <= 0)
            {
                Debug.LogWarning("[OpenWorld] Save file is missing required world metadata.");
                data = null;
                return false;
            }

            return true;
        }

        public static void Migrate(OpenWorldSaveData data)
        {
            if (data == null) return;
            data.Inventory ??= new ResourceInventory();
            data.ModifiedCells ??= new List<SavedCell>();
            data.Buildings ??= new List<BuildingEntity>();
            data.Units ??= new List<UnitEntity>();
            data.Vehicles ??= new List<VehicleEntity>();
            data.Jobs ??= new List<JobRecord>();
            data.Blueprints ??= new List<BlueprintJob>();
            data.Factions ??= new List<FactionRecord>();
            data.Regions ??= new List<RegionRecord>();
            data.StrategicSites ??= new List<StrategicSiteRecord>();
            data.Diplomacy ??= new List<DiplomacyRecord>();
            data.LogisticsRoutes ??= new List<LogisticsRoute>();
            data.ProductionOrders ??= new List<ProductionOrder>();
            data.ResearchOrders ??= new List<ResearchOrder>();
            data.WorkerAssignments ??= new List<WorkerAssignment>();
            data.TradeContracts ??= new List<TradeContract>();
            data.IntelSnapshots ??= new List<IntelSnapshot>();
            data.RailSchedules ??= new List<RailSchedule>();
            data.ServiceOrders ??= new List<RepairRefuelOrder>();
            data.Surveys ??= new List<SurveyRecord>();
            data.DrillReports ??= new List<DrillReport>();
            data.MiningZones ??= new List<MiningZoneRecord>();
            data.Population ??= new PopulationState();
            data.Tech ??= new TechState();
            data.Knowledge ??= new List<SavedKnowledgeCell>();

            foreach (var saved in data.ModifiedCells)
            {
                var cell = saved.Cell;
                OpenWorldState.NormalizeLayers(ref cell);
                saved.Cell = cell;
            }

            foreach (var unit in data.Units) unit.CurrentOrder ??= new UnitOrder();
            foreach (var building in data.Buildings)
            {
                building.Storage ??= new ResourceInventory();
                building.StorageCapacity = Mathf.Max(building.StorageCapacity, OpenWorldDataCatalog.StorageCapacityFor(building.Kind));
            }
            data.Tech.CompletedResearch ??= new List<string>();
            data.Version = 2;
        }
    }
}
