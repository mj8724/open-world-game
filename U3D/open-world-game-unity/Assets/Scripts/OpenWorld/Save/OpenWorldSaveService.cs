using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenWorld
{
    [Serializable]
    public class OpenWorldSaveData
    {
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
    }
}
