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

/// <summary>物流实体组件</summary>
public class LogisticsComponent
{
    public int EntityId { get; set; }
    public string FactionId { get; set; } = "";
    public string TransportType { get; set; } = "PORTER";
    public string CargoType { get; set; } = "FOOD";
    public int CargoAmount { get; set; }
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public string? CurrentEdgeId { get; set; }
    public float EdgeProgress { get; set; }
    public bool Returning { get; set; }
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
