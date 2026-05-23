namespace CivilizationSim.Ecs.Components;

/// <summary>节点状态组件 — 城市/矿区/据点的完整状态</summary>
public class NodeComponent
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string FactionId { get; set; } = "";
    public bool IsCapital { get; set; }
    public int PopCount { get; set; }
    public int InvFood { get; set; }
    public int InvIron { get; set; }
    public int InvAmmo { get; set; }
    public int FarmLevel { get; set; }
    public int MineLevel { get; set; }
    public int ArsenalLevel { get; set; }
    public int WallLevel { get; set; }
    public int WallHpCurrent { get; set; }
    public int BeaconLevel { get; set; }
    public int GarrisonCount { get; set; }
    public float Loyalty { get; set; } = 1.0f;
    public List<string> Tags { get; set; } = new();
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>边组件 — 节点间连接通路</summary>
public class EdgeComponent
{
    public string Id { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public string EdgeType { get; set; } = "ROAD"; // "ROAD" | "RAILWAY"
    public float Length { get; set; }
}

/// <summary>军队实体组件</summary>
public class ArmyComponent
{
    public int EntityId { get; set; }
    public string FactionId { get; set; } = "";
    public int TroopCount { get; set; }
    public int MeleeTroops { get; set; }
    public int RangedTroops { get; set; }
    public float Morale { get; set; } = 1.0f;
    public int CarryFood { get; set; }
    public int CarryAmmo { get; set; }
    public string? CurrentNodeId { get; set; }
    public string? CurrentEdgeId { get; set; }
    public float EdgeProgress { get; set; }
    public string? TargetNodeId { get; set; }
    public string State { get; set; } = "IDLE"; // IDLE | MOVING | FIGHTING | FLEEING
}

/// <summary>物流路线组件</summary>
public class LogisticsComponent
{
    public int EntityId { get; set; }
    public string FactionId { get; set; } = "";
    public string Mode { get; set; } = "MANUAL";
    public bool Enabled { get; set; } = true;
    public string TransportType { get; set; } = "PORTER";
    public int AssignedTransportCount { get; set; } = 1;
    public string CargoType { get; set; } = "FOOD";
    public int CargoAmount { get; set; }
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public string? CurrentEdgeId { get; set; }
    public float EdgeProgress { get; set; }
    public bool Returning { get; set; }
    public List<string> PathNodeIds { get; set; } = new();
    public List<string> PathEdgeIds { get; set; } = new();
    public int Priority { get; set; } = 50;
    public int? DesiredTargetQuantity { get; set; }
    public bool UnlimitedTarget { get; set; }
    public string SourceKind { get; set; } = "MANUAL";
    public string? AutoManagedKey { get; set; }
    public List<LogisticsTripState> Trips { get; set; } = new();
    public int EstimatedRequiredTransportCount { get; set; }
    public float EstimatedRoundTripTicks { get; set; }
    public float EstimatedThroughputPerTick { get; set; }
    public int DeliveredLastTick { get; set; }
    public int DeliveredTotal { get; set; }
}

/// <summary>单个运输工具在路线上的状态</summary>
public class LogisticsTripState
{
    public int TripId { get; set; }
    public bool Returning { get; set; }
    public int CargoAmount { get; set; }
    public int CurrentPathIndex { get; set; }
    public float EdgeProgress { get; set; }
}

/// <summary>集结点组件</summary>
public class RallyPointComponent
{
    public string NodeId { get; set; } = "";
    public string FactionId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public Dictionary<string, RallyCargoPolicy> CargoPolicies { get; set; } = new();
}

/// <summary>集结点单项货物策略</summary>
public class RallyCargoPolicy
{
    public string CargoType { get; set; } = "";
    public bool Enabled { get; set; }
    public bool Unlimited { get; set; }
    public int? TargetQuantity { get; set; }
    public int Priority { get; set; } = 50;
}

/// <summary>节点运输工具库存</summary>
public class TransportStockComponent
{
    public string NodeId { get; set; } = "";
    public string FactionId { get; set; } = "";
    public Dictionary<string, TransportStockEntry> Stock { get; set; } = new();
}

/// <summary>单种运输工具库存</summary>
public class TransportStockEntry
{
    public string TransportType { get; set; } = "";
    public int Total { get; set; }
    public int Idle { get; set; }
    public int Assigned { get; set; }
    public int MaintenanceBlocked { get; set; }
}

/// <summary>运输工具生产队列条目</summary>
public class TransportProductionQueueItem
{
    public string NodeId { get; set; } = "";
    public string TransportType { get; set; } = "";
    public int Quantity { get; set; }
    public int RemainingTicks { get; set; }
    public int TotalTicks { get; set; }
    public string FactionId { get; set; } = "";
}

/// <summary>势力全局状态组件</summary>
public class FactionComponent
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> OwnedNodeIds { get; set; } = new();
    public List<string> UnlockedTechs { get; set; } = new();
    public string? ResearchingTechId { get; set; }
    public int ResearchProgress { get; set; }
    public bool IsPlayer { get; set; }
    public string CapitalNodeId { get; set; } = "";
}

/// <summary>建造队列条目</summary>
public class BuildQueueItem
{
    public string NodeId { get; set; } = "";
    public string BuildingType { get; set; } = "";
    public int TargetLevel { get; set; }
    public int RemainingTicks { get; set; }
    public string FactionId { get; set; } = "";
}
