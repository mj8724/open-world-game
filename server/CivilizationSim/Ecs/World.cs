using System.Text.Json;
using System.Text.Json.Serialization;
using CivilizationSim.Dict;
using CivilizationSim.Ecs.Components;

namespace CivilizationSim.Ecs;

/// <summary>
/// World 容器 — 持有所有游戏状态的 Component 集合。
/// 提供 Delta 追踪能力，用于增量状态推送。
/// </summary>
public class World
{
    // ─── Component 集合 ───
    public Dictionary<string, NodeComponent> Nodes { get; } = new();
    public Dictionary<string, EdgeComponent> Edges { get; } = new();
    public Dictionary<int, ArmyComponent> Armies { get; } = new();
    public Dictionary<int, LogisticsComponent> Logistics { get; } = new();
    public Dictionary<string, FactionComponent> Factions { get; } = new();
    public List<BuildQueueItem> BuildQueue { get; } = new();

    public EntityManager EntityManager { get; } = new();

    // ─── Delta 追踪 ───
    private readonly HashSet<string> _dirtyNodes = new();
    private readonly HashSet<string> _dirtyEdges = new();
    private readonly HashSet<int> _dirtyArmies = new();
    private readonly HashSet<int> _dirtyLogistics = new();
    private readonly HashSet<string> _dirtyFactions = new();
    private readonly List<int> _removedEntities = new();
    private readonly List<GameEvent> _events = new();

    /// <summary>标记节点已变化</summary>
    public void MarkDirty(NodeComponent node) => _dirtyNodes.Add(node.Id);
    /// <summary>标记边已变化</summary>
    public void MarkDirty(EdgeComponent edge) => _dirtyEdges.Add(edge.Id);
    /// <summary>标记军队已变化</summary>
    public void MarkDirty(ArmyComponent army) => _dirtyArmies.Add(army.EntityId);
    /// <summary>标记物流已变化</summary>
    public void MarkDirty(LogisticsComponent logi) => _dirtyLogistics.Add(logi.EntityId);
    /// <summary>标记势力已变化</summary>
    public void MarkDirty(FactionComponent faction) => _dirtyFactions.Add(faction.Id);
    /// <summary>记录实体已移除</summary>
    public void MarkRemoved(int entityId) => _removedEntities.Add(entityId);
    /// <summary>添加游戏事件</summary>
    public void AddEvent(GameEvent evt) => _events.Add(evt);

    /// <summary>开始新一轮 Delta 追踪</summary>
    public void BeginDeltaTracking()
    {
        _dirtyNodes.Clear();
        _dirtyEdges.Clear();
        _dirtyArmies.Clear();
        _dirtyLogistics.Clear();
        _dirtyFactions.Clear();
        _removedEntities.Clear();
        _events.Clear();
    }

    /// <summary>收集当前 Tick 的 Delta 状态</summary>
    public TickDelta CollectDelta(int tick)
    {
        var delta = new TickDelta
        {
            Tick = tick,
            Nodes = new Dictionary<string, NodeComponent>(),
            Edges = new Dictionary<string, EdgeComponent>(),
            Armies = new Dictionary<int, ArmyComponent>(),
            LogisticsEntities = new Dictionary<int, LogisticsComponent>(),
            Factions = new Dictionary<string, FactionComponent>(),
            RemovedEntityIds = new List<int>(_removedEntities),
            Events = new List<GameEvent>(_events),
            BuildQueue = new List<BuildQueueItem>(BuildQueue)
        };

        foreach (var id in _dirtyNodes)
            if (Nodes.TryGetValue(id, out var node))
                delta.Nodes[id] = node;

        foreach (var id in _dirtyEdges)
            if (Edges.TryGetValue(id, out var edge))
                delta.Edges[id] = edge;

        foreach (var id in _dirtyArmies)
            if (Armies.TryGetValue(id, out var army))
                delta.Armies[id] = army;

        foreach (var id in _dirtyLogistics)
            if (Logistics.TryGetValue(id, out var logi))
                delta.LogisticsEntities[id] = logi;

        foreach (var id in _dirtyFactions)
            if (Factions.TryGetValue(id, out var faction))
                delta.Factions[id] = faction;

        return delta;
    }

    /// <summary>从地图定义初始化世界</summary>
    public void InitializeFromMap(MapDef map)
    {
        // 创建所有节点
        foreach (var nodeDef in map.Nodes)
        {
            var node = new NodeComponent
            {
                Id = nodeDef.Id,
                Name = nodeDef.Name,
                X = nodeDef.X,
                Y = nodeDef.Y,
                Tags = new List<string>(nodeDef.Tags),
                FactionId = "NEUTRAL",
                Loyalty = 0.5f
            };
            Nodes[node.Id] = node;
        }

        // 创建所有边
        foreach (var edgeDef in map.Edges)
        {
            var edge = new EdgeComponent
            {
                Id = edgeDef.Id,
                SourceNodeId = edgeDef.Source,
                TargetNodeId = edgeDef.Target,
                EdgeType = edgeDef.Type,
                Length = edgeDef.Length
            };
            Edges[edge.Id] = edge;
        }

        // 初始化势力
        foreach (var factionStart in map.FactionStarts)
        {
            var faction = new FactionComponent
            {
                Id = factionStart.FactionId,
                Name = factionStart.FactionId,
                CapitalNodeId = factionStart.CapitalNode,
                OwnedNodeIds = new List<string>(factionStart.OwnedNodes),
                IsPlayer = factionStart.FactionId == "PLAYER",
                UnlockedTechs = new List<string> { "SCAVENGING", "PORTERS" }
            };
            Factions[faction.Id] = faction;

            // 设置节点归属
            foreach (var nodeId in factionStart.OwnedNodes)
            {
                if (Nodes.TryGetValue(nodeId, out var node))
                {
                    node.FactionId = factionStart.FactionId;
                    node.Loyalty = 1.0f;

                    if (nodeId == factionStart.CapitalNode)
                    {
                        node.IsCapital = true;
                        node.BeaconLevel = 1;
                    }
                }
            }

            // 应用初始建筑
            foreach (var (nodeId, bld) in factionStart.StartingBuildings)
            {
                if (Nodes.TryGetValue(nodeId, out var node))
                {
                    node.FarmLevel = bld.FarmLevel;
                    node.MineLevel = bld.MineLevel;
                    node.ArsenalLevel = bld.ArsenalLevel;
                    node.WallLevel = bld.WallLevel;
                    node.BeaconLevel = Math.Max(node.BeaconLevel, bld.BeaconLevel);
                }
            }

            // 应用初始人口
            foreach (var (nodeId, pop) in factionStart.StartingPop)
            {
                if (Nodes.TryGetValue(nodeId, out var node))
                    node.PopCount = pop;
            }

            // 应用初始驻军
            foreach (var (nodeId, garrison) in factionStart.StartingGarrison)
            {
                if (Nodes.TryGetValue(nodeId, out var node))
                    node.GarrisonCount = garrison;
            }

            // 应用初始资源
            foreach (var nodeId in factionStart.OwnedNodes)
            {
                if (Nodes.TryGetValue(nodeId, out var node))
                {
                    node.InvFood = 500;
                    node.InvIron = 200;
                    node.InvAmmo = 0;
                }
            }
        }
    }
}

/// <summary>单 Tick 的增量状态</summary>
public class TickDelta
{
    public int Tick { get; set; }
    public Dictionary<string, NodeComponent> Nodes { get; set; } = new();
    public Dictionary<string, EdgeComponent> Edges { get; set; } = new();
    public Dictionary<int, ArmyComponent> Armies { get; set; } = new();
    public Dictionary<int, LogisticsComponent> LogisticsEntities { get; set; } = new();
    public Dictionary<string, FactionComponent> Factions { get; set; } = new();
    public List<int> RemovedEntityIds { get; set; } = new();
    public List<GameEvent> Events { get; set; } = new();
    public List<BuildQueueItem> BuildQueue { get; set; } = new();
}

/// <summary>游戏事件</summary>
public class GameEvent
{
    public string Type { get; set; } = "";
    public string TextKey { get; set; } = "";
    public Dictionary<string, object> Params { get; set; } = new();
}
