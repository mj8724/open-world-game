using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 地图节点定义
/// </summary>
public record MapNodeDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("terrain")]
    public string Terrain { get; init; } = "PLAINS";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// 地图边定义
/// </summary>
public record MapEdgeDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("source")]
    public string Source { get; init; } = "";

    [JsonPropertyName("target")]
    public string Target { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "ROAD";

    [JsonPropertyName("length")]
    public float Length { get; init; }
}

/// <summary>
/// 势力初始建筑配置
/// </summary>
public record FactionBuildingStart
{
    [JsonPropertyName("farm_level")]
    public int FarmLevel { get; init; }

    [JsonPropertyName("mine_level")]
    public int MineLevel { get; init; }

    [JsonPropertyName("arsenal_level")]
    public int ArsenalLevel { get; init; }

    [JsonPropertyName("wall_level")]
    public int WallLevel { get; init; }

    [JsonPropertyName("beacon_level")]
    public int BeaconLevel { get; init; }
}

/// <summary>
/// 势力初始配置
/// </summary>
public record FactionStartDef
{
    [JsonPropertyName("faction_id")]
    public string FactionId { get; init; } = "";

    [JsonPropertyName("owned_nodes")]
    public List<string> OwnedNodes { get; init; } = new();

    [JsonPropertyName("capital_node")]
    public string CapitalNode { get; init; } = "";

    [JsonPropertyName("starting_buildings")]
    public Dictionary<string, FactionBuildingStart> StartingBuildings { get; init; } = new();

    [JsonPropertyName("starting_pop")]
    public Dictionary<string, int> StartingPop { get; init; } = new();

    [JsonPropertyName("starting_garrison")]
    public Dictionary<string, int> StartingGarrison { get; init; } = new();
}

/// <summary>
/// 完整地图定义
/// </summary>
public record MapDef
{
    [JsonPropertyName("nodes")]
    public List<MapNodeDef> Nodes { get; init; } = new();

    [JsonPropertyName("edges")]
    public List<MapEdgeDef> Edges { get; init; } = new();

    [JsonPropertyName("wild_resources")]
    public List<MapWildResourceDef> WildResources { get; init; } = new();

    [JsonPropertyName("neutral_structures")]
    public List<MapNeutralStructureDef> NeutralStructures { get; init; } = new();

    [JsonPropertyName("faction_starts")]
    public List<FactionStartDef> FactionStarts { get; init; } = new();
}

/// <summary>
/// 野外资源点定义
/// </summary>
public record MapWildResourceDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("z")]
    public float Z { get; init; }

    [JsonPropertyName("resource_type")]
    public string ResourceType { get; init; } = "FOOD";

    [JsonPropertyName("yield")]
    public int Yield { get; init; }
}

/// <summary>
/// 中立建筑/遗迹定义
/// </summary>
public record MapNeutralStructureDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("z")]
    public float Z { get; init; }

    [JsonPropertyName("structure_type")]
    public string StructureType { get; init; } = "RUINS";
}
