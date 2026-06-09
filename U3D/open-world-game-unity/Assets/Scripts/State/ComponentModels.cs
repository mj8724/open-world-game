using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 数据模型 — 与服务端 Ecs/Components/Components.cs 一一对应
/// 用于 Newtonsoft.Json 反序列化 WebSocket 消息
/// </summary>
namespace GameState
{
    // ─── 节点 ───

    [System.Serializable]
    public class PlacedBuilding
    {
        [JsonProperty("buildingType")] public string BuildingType { get; set; } = "";
        [JsonProperty("level")] public int Level { get; set; }
        [JsonProperty("localX")] public float LocalX { get; set; }
        [JsonProperty("localZ")] public float LocalZ { get; set; }
        [JsonProperty("rotation")] public float Rotation { get; set; }
    }

    [System.Serializable]
    public class WallSegment
    {
        [JsonProperty("fromX")] public float FromX { get; set; }
        [JsonProperty("fromZ")] public float FromZ { get; set; }
        [JsonProperty("toX")] public float ToX { get; set; }
        [JsonProperty("toZ")] public float ToZ { get; set; }
        [JsonProperty("level")] public int Level { get; set; } = 1;
        [JsonProperty("hp")] public int Hp { get; set; }
        [JsonProperty("maxHp")] public int MaxHp { get; set; }
    }

    [System.Serializable]
    public class NodeComponent
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
        [JsonProperty("isCapital")] public bool IsCapital { get; set; }
        [JsonProperty("popCount")] public int PopCount { get; set; }
        [JsonProperty("invFood")] public int InvFood { get; set; }
        [JsonProperty("invIron")] public int InvIron { get; set; }
        [JsonProperty("invAmmo")] public int InvAmmo { get; set; }
        [JsonProperty("farmLevel")] public int FarmLevel { get; set; }
        [JsonProperty("mineLevel")] public int MineLevel { get; set; }
        [JsonProperty("arsenalLevel")] public int ArsenalLevel { get; set; }
        [JsonProperty("wallLevel")] public int WallLevel { get; set; }
        [JsonProperty("wallHpCurrent")] public int WallHpCurrent { get; set; }
        [JsonProperty("beaconLevel")] public int BeaconLevel { get; set; }
        [JsonProperty("garrisonCount")] public int GarrisonCount { get; set; }
        [JsonProperty("loyalty")] public float Loyalty { get; set; } = 1.0f;
        [JsonProperty("tags")] public List<string> Tags { get; set; } = new();
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("terrain")] public string Terrain { get; set; } = "PLAINS";
        [JsonProperty("placedBuildings")] public List<PlacedBuilding> PlacedBuildings { get; set; } = new();
        [JsonProperty("wallSegments")] public List<WallSegment> WallSegments { get; set; } = new();
    }

    // ─── 边 ───

    [System.Serializable]
    public class EdgeComponent
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("sourceNodeId")] public string SourceNodeId { get; set; } = "";
        [JsonProperty("targetNodeId")] public string TargetNodeId { get; set; } = "";
        [JsonProperty("edgeType")] public string EdgeType { get; set; } = "ROAD";
        [JsonProperty("length")] public float Length { get; set; }
    }

    // ─── 军队 ───

    [System.Serializable]
    public class ArmyComponent
    {
        [JsonProperty("entityId")] public int EntityId { get; set; }
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
        [JsonProperty("unitKind")] public string UnitKind { get; set; } = "COMPANY";
        [JsonProperty("unitDefId")] public string UnitDefId { get; set; } = "MILITIA";
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("strength")] public int Strength { get; set; }
        [JsonProperty("maxStrength")] public int MaxStrength { get; set; }
        [JsonProperty("troopCount")] public int TroopCount { get; set; }
        [JsonProperty("meleeTroops")] public int MeleeTroops { get; set; }
        [JsonProperty("rangedTroops")] public int RangedTroops { get; set; }
        [JsonProperty("morale")] public float Morale { get; set; } = 1.0f;
        [JsonProperty("supplyFood")] public int SupplyFood { get; set; }
        [JsonProperty("supplyAmmo")] public int SupplyAmmo { get; set; }
        [JsonProperty("maxSupplyFood")] public int MaxSupplyFood { get; set; }
        [JsonProperty("maxSupplyAmmo")] public int MaxSupplyAmmo { get; set; }
        [JsonProperty("carryFood")] public int CarryFood { get; set; }
        [JsonProperty("carryAmmo")] public int CarryAmmo { get; set; }
        [JsonProperty("regimentId")] public int RegimentId { get; set; }
        [JsonProperty("divisionId")] public int DivisionId { get; set; }
        [JsonProperty("stance")] public string Stance { get; set; } = "DEFEND";
        [JsonProperty("currentNodeId")] public string? CurrentNodeId { get; set; }
        [JsonProperty("currentEdgeId")] public string? CurrentEdgeId { get; set; }
        [JsonProperty("edgeProgress")] public float EdgeProgress { get; set; }
        [JsonProperty("targetNodeId")] public string? TargetNodeId { get; set; }
        [JsonProperty("state")] public string State { get; set; } = "IDLE";
    }

    // ─── 编组 ───

    [System.Serializable]
    public class FormationComponent
    {
        [JsonProperty("entityId")] public int EntityId { get; set; }
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
        [JsonProperty("formationType")] public string FormationType { get; set; } = "REGIMENT";
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("parentFormationId")] public int? ParentFormationId { get; set; }
        [JsonProperty("childUnitIds")] public List<int> ChildUnitIds { get; set; } = new();
        [JsonProperty("currentNodeId")] public string? CurrentNodeId { get; set; }
    }

    // ─── 物流 ───

    [System.Serializable]
    public class LogisticsTripState
    {
        [JsonProperty("tripId")] public int TripId { get; set; }
        [JsonProperty("returning")] public bool Returning { get; set; }
        [JsonProperty("cargoAmount")] public int CargoAmount { get; set; }
        [JsonProperty("currentPathIndex")] public int CurrentPathIndex { get; set; }
        [JsonProperty("edgeProgress")] public float EdgeProgress { get; set; }
    }

    [System.Serializable]
    public class LogisticsComponent
    {
        [JsonProperty("entityId")] public int EntityId { get; set; }
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
        [JsonProperty("mode")] public string Mode { get; set; } = "MANUAL";
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;
        [JsonProperty("transportType")] public string TransportType { get; set; } = "PORTER";
        [JsonProperty("assignedTransportCount")] public int AssignedTransportCount { get; set; } = 1;
        [JsonProperty("cargoType")] public string CargoType { get; set; } = "FOOD";
        [JsonProperty("cargoAmount")] public int CargoAmount { get; set; }
        [JsonProperty("fromNodeId")] public string FromNodeId { get; set; } = "";
        [JsonProperty("toNodeId")] public string ToNodeId { get; set; } = "";
        [JsonProperty("currentEdgeId")] public string? CurrentEdgeId { get; set; }
        [JsonProperty("edgeProgress")] public float EdgeProgress { get; set; }
        [JsonProperty("returning")] public bool Returning { get; set; }
        [JsonProperty("pathNodeIds")] public List<string> PathNodeIds { get; set; } = new();
        [JsonProperty("pathEdgeIds")] public List<string> PathEdgeIds { get; set; } = new();
        [JsonProperty("priority")] public int Priority { get; set; } = 50;
        [JsonProperty("desiredTargetQuantity")] public int? DesiredTargetQuantity { get; set; }
        [JsonProperty("unlimitedTarget")] public bool UnlimitedTarget { get; set; }
        [JsonProperty("sourceKind")] public string SourceKind { get; set; } = "MANUAL";
        [JsonProperty("autoManagedKey")] public string? AutoManagedKey { get; set; }
        [JsonProperty("retireWhenIdle")] public bool RetireWhenIdle { get; set; }
        [JsonProperty("trips")] public List<LogisticsTripState> Trips { get; set; } = new();
        [JsonProperty("estimatedRequiredTransportCount")] public int EstimatedRequiredTransportCount { get; set; }
        [JsonProperty("estimatedRoundTripTicks")] public float EstimatedRoundTripTicks { get; set; }
        [JsonProperty("estimatedThroughputPerTick")] public float EstimatedThroughputPerTick { get; set; }
        [JsonProperty("deliveredLastTick")] public int DeliveredLastTick { get; set; }
        [JsonProperty("deliveredTotal")] public int DeliveredTotal { get; set; }
    }

    // ─── 集结点 ───

    [System.Serializable]
    public class RallyCargoPolicy
    {
        [JsonProperty("cargoType")] public string CargoType { get; set; } = "";
        [JsonProperty("enabled")] public bool Enabled { get; set; }
        [JsonProperty("unlimited")] public bool Unlimited { get; set; }
        [JsonProperty("targetQuantity")] public int? TargetQuantity { get; set; }
        [JsonProperty("priority")] public int Priority { get; set; } = 50;
    }

    [System.Serializable]
    public class RallyPointComponent
    {
        [JsonProperty("nodeId")] public string NodeId { get; set; } = "";
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;
        [JsonProperty("cargoPolicies")] public Dictionary<string, RallyCargoPolicy> CargoPolicies { get; set; } = new();
    }

    // ─── 运输工具库存 ───

    [System.Serializable]
    public class TransportStockEntry
    {
        [JsonProperty("transportType")] public string TransportType { get; set; } = "";
        [JsonProperty("total")] public int Total { get; set; }
        [JsonProperty("idle")] public int Idle { get; set; }
        [JsonProperty("assigned")] public int Assigned { get; set; }
        [JsonProperty("maintenanceBlocked")] public int MaintenanceBlocked { get; set; }
    }

    [System.Serializable]
    public class TransportStockComponent
    {
        [JsonProperty("nodeId")] public string NodeId { get; set; } = "";
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
        [JsonProperty("stock")] public Dictionary<string, TransportStockEntry> Stock { get; set; } = new();
    }

    // ─── 运输工具生产队列 ───

    [System.Serializable]
    public class TransportProductionQueueItem
    {
        [JsonProperty("nodeId")] public string NodeId { get; set; } = "";
        [JsonProperty("transportType")] public string TransportType { get; set; } = "";
        [JsonProperty("quantity")] public int Quantity { get; set; }
        [JsonProperty("remainingTicks")] public int RemainingTicks { get; set; }
        [JsonProperty("totalTicks")] public int TotalTicks { get; set; }
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
    }

    // ─── 势力 ───

    [System.Serializable]
    public class FactionComponent
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("ownedNodeIds")] public List<string> OwnedNodeIds { get; set; } = new();
        [JsonProperty("unlockedTechs")] public List<string> UnlockedTechs { get; set; } = new();
        [JsonProperty("researchingTechId")] public string? ResearchingTechId { get; set; }
        [JsonProperty("researchProgress")] public int ResearchProgress { get; set; }
        [JsonProperty("isPlayer")] public bool IsPlayer { get; set; }
        [JsonProperty("capitalNodeId")] public string CapitalNodeId { get; set; } = "";
    }

    // ─── 建造队列 ───

    [System.Serializable]
    public class BuildQueueItem
    {
        [JsonProperty("nodeId")] public string NodeId { get; set; } = "";
        [JsonProperty("buildingType")] public string BuildingType { get; set; } = "";
        [JsonProperty("targetLevel")] public int TargetLevel { get; set; }
        [JsonProperty("remainingTicks")] public int RemainingTicks { get; set; }
        [JsonProperty("factionId")] public string FactionId { get; set; } = "";
    }

    // ─── 野外实体 ───

    [System.Serializable]
    public class WildResource
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("z")] public float Z { get; set; }
        [JsonProperty("resourceType")] public string ResourceType { get; set; } = "";
        [JsonProperty("yield")] public int Yield { get; set; }
        [JsonProperty("ownerFactionId")] public string OwnerFactionId { get; set; } = "";
    }

    [System.Serializable]
    public class NeutralStructure
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("z")] public float Z { get; set; }
        [JsonProperty("structureType")] public string StructureType { get; set; } = "";
        [JsonProperty("ownerFactionId")] public string OwnerFactionId { get; set; } = "";
    }

    // ─── 游戏事件 ───

    [System.Serializable]
    public class GameEvent
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("textKey")] public string TextKey { get; set; } = "";
        [JsonProperty("params")] public Dictionary<string, object> Params { get; set; } = new();
    }

    // ─── 快照/Delta ───

    [System.Serializable]
    public class FullStateSnapshot
    {
        [JsonProperty("tick")] public int Tick { get; set; }
        [JsonProperty("nodes")] public Dictionary<string, NodeComponent> Nodes { get; set; } = new();
        [JsonProperty("edges")] public Dictionary<string, EdgeComponent> Edges { get; set; } = new();
        [JsonProperty("armies")] public Dictionary<int, ArmyComponent> Armies { get; set; } = new();
        [JsonProperty("formations")] public Dictionary<int, FormationComponent> Formations { get; set; } = new();
        [JsonProperty("logisticsEntities")] public Dictionary<int, LogisticsComponent> LogisticsEntities { get; set; } = new();
        [JsonProperty("rallyPoints")] public Dictionary<string, RallyPointComponent> RallyPoints { get; set; } = new();
        [JsonProperty("transportStocks")] public Dictionary<string, TransportStockComponent> TransportStocks { get; set; } = new();
        [JsonProperty("factions")] public Dictionary<string, FactionComponent> Factions { get; set; } = new();
        [JsonProperty("wildResources")] public Dictionary<string, WildResource> WildResources { get; set; } = new();
        [JsonProperty("neutralStructures")] public Dictionary<string, NeutralStructure> NeutralStructures { get; set; } = new();
        [JsonProperty("buildQueue")] public List<BuildQueueItem> BuildQueue { get; set; } = new();
        [JsonProperty("transportProductionQueue")] public List<TransportProductionQueueItem> TransportProductionQueue { get; set; } = new();
    }

    [System.Serializable]
    public class TickDelta
    {
        [JsonProperty("tick")] public int Tick { get; set; }
        [JsonProperty("nodes")] public Dictionary<string, NodeComponent>? Nodes { get; set; }
        [JsonProperty("edges")] public Dictionary<string, EdgeComponent>? Edges { get; set; }
        [JsonProperty("armies")] public Dictionary<int, ArmyComponent>? Armies { get; set; }
        [JsonProperty("formations")] public Dictionary<int, FormationComponent>? Formations { get; set; }
        [JsonProperty("logisticsEntities")] public Dictionary<int, LogisticsComponent>? LogisticsEntities { get; set; }
        [JsonProperty("rallyPoints")] public Dictionary<string, RallyPointComponent>? RallyPoints { get; set; }
        [JsonProperty("transportStocks")] public Dictionary<string, TransportStockComponent>? TransportStocks { get; set; }
        [JsonProperty("factions")] public Dictionary<string, FactionComponent>? Factions { get; set; }
        [JsonProperty("wildResources")] public Dictionary<string, WildResource>? WildResources { get; set; }
        [JsonProperty("neutralStructures")] public Dictionary<string, NeutralStructure>? NeutralStructures { get; set; }
        [JsonProperty("removedEntityIds")] public List<int>? RemovedEntityIds { get; set; }
        [JsonProperty("removedRallyPointIds")] public List<string>? RemovedRallyPointIds { get; set; }
        [JsonProperty("removedWildResourceIds")] public List<string>? RemovedWildResourceIds { get; set; }
        [JsonProperty("removedNeutralStructureIds")] public List<string>? RemovedNeutralStructureIds { get; set; }
        [JsonProperty("events")] public List<GameEvent>? Events { get; set; }
        [JsonProperty("buildQueue")] public List<BuildQueueItem>? BuildQueue { get; set; }
        [JsonProperty("transportProductionQueue")] public List<TransportProductionQueueItem>? TransportProductionQueue { get; set; }
    }

    // ─── WebSocket 消息 ───

    [System.Serializable]
    public class ServerMessage
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("tick")] public int Tick { get; set; }
        [JsonProperty("ack")] public int Ack { get; set; }
        [JsonProperty("data")] public object? Data { get; set; }
    }

    [System.Serializable]
    public class ClientCommand
    {
        [JsonProperty("type")] public string Type { get; set; } = "COMMAND";
        [JsonProperty("action")] public string Action { get; set; } = "";
        [JsonProperty("payload")] public object Payload { get; set; } = new();
        [JsonProperty("seq")] public int Seq { get; set; }
    }
}
