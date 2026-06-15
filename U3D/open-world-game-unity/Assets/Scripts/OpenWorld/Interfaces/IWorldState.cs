using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public interface IWorldState : IResourceProvider
    {
        int MapSize { get; }
        int ChunkSize { get; }
        int Seed { get; }
        ResourceInventory Inventory { get; }
        Dictionary<Vector2Int, WorldChunk> Chunks { get; }
        Dictionary<int, BuildingEntity> Buildings { get; }
        Dictionary<int, UnitEntity> Units { get; }
        Dictionary<int, VehicleEntity> Vehicles { get; }
        List<JobRecord> Jobs { get; }
        List<BlueprintJob> Blueprints { get; }
        List<FactionRecord> Factions { get; }
        List<DiplomacyRecord> Diplomacy { get; }
        List<RegionRecord> Regions { get; }
        List<StrategicSiteRecord> StrategicSites { get; }
        List<LogisticsRoute> LogisticsRoutes { get; }
        List<ProductionOrder> ProductionOrders { get; }
        List<ResearchOrder> ResearchOrders { get; }
        List<WorkerAssignment> WorkerAssignments { get; }
        List<TradeContract> TradeContracts { get; }
        List<IntelSnapshot> IntelSnapshots { get; }
        List<RailSchedule> RailSchedules { get; }
        List<RepairRefuelOrder> ServiceOrders { get; }
        List<SurveyRecord> Surveys { get; }
        List<DrillReport> DrillReports { get; }
        List<MiningZoneRecord> MiningZones { get; }
        Dictionary<Vector2Int, SurveyRecord> SurveyByCell { get; }
        Queue<OpenWorldCommand> Commands { get; }
        HashSet<Vector2Int> ModifiedCells { get; }
        PopulationState Population { get; }
        TechState Tech { get; }
        KnowledgeState[] KnowledgeCells { get; set; }

        bool InBounds(Vector2Int cell);
        Vector2Int ToChunkCoord(Vector2Int cell);
        Vector2Int ToLocalCell(Vector2Int cell);
        WorldChunk GetOrCreateChunk(Vector2Int chunkCoord);
        SurfaceCell GetCell(Vector2Int cell);
        void SetCell(Vector2Int cell, SurfaceCell value);
        void SetCell(Vector2Int cell, SurfaceCell value, bool fireEvent);
        void MarkDirty(Vector2Int cell);
        void MarkChunkDirty(Vector2Int chunkCoord);
        float GetHeight(Vector2Int cell);
        Vector3 CellToWorld(Vector2Int cell);
        Vector2Int WorldToCell(Vector3 world);
        BuildingEntity AddBuilding(BuildableDef def, Vector2Int origin, int rotation, int factionId);
        UnitEntity AddUnit(UnitKind kind, Vector2Int cell, int factionId);
        VehicleEntity AddVehicle(VehicleKind kind, Vector2Int cell, int factionId);
        JobRecord AddJob(UnitTask task, Vector2Int target, BuildableKind buildKind = BuildableKind.Warehouse);
        BlueprintJob AddBlueprint(BlueprintKind kind, Vector2Int cell, int radius, int factionId);
        LogisticsRoute AddRoute(Vector2Int source, Vector2Int target, ResourceKind cargo, VehicleKind vehicleKind, int priority, LogisticsMode mode);
        LogisticsRoute AddRoute(int sourceBuildingId, int targetBuildingId, Vector2Int source, Vector2Int target, ResourceKind cargo, VehicleKind vehicleKind, int priority, LogisticsMode mode);
        void EnsureBuildingStorage(BuildingEntity building);
        BuildingEntity FindNearestStorage(Vector2Int cell, int factionId, int maxDistance = 16);
        void InvalidateResourceCache();
        int AddToStorage(BuildingEntity building, ResourceKind kind, int amount);
        void BindRouteBuildings(LogisticsRoute route);
        Vector2Int FindBuildingAccessCell(BuildingEntity building, Vector2Int toward);
        OpenWorldCommand EnqueueCommand(CommandKind kind, int factionId);
        SurveyRecord GetSurvey(Vector2Int cell);
        SurveyRecord UpsertSurvey(Vector2Int cell);
        DrillReport AddDrillReport(Vector2Int cell, int drillRigBuildingId);
        MiningZoneRecord AddMiningZone(Vector2Int center, int radius, GroundMaterial target, int mineBuildingId, int priority);
        ProductionOrder AddProductionOrder(int buildingId, string recipeId, int cycles, int priority);
        ResearchOrder AddResearchOrder(string techId, int priority);
        void RestoreFrom(OpenWorldSaveData data);
        void TrimExcess();
    }
}
