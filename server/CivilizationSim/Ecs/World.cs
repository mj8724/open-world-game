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
    public Dictionary<int, FormationComponent> Formations { get; } = new();
    public Dictionary<int, LogisticsComponent> Logistics { get; } = new();
    public Dictionary<string, RallyPointComponent> RallyPoints { get; } = new();
    public Dictionary<string, TransportStockComponent> TransportStocks { get; } = new();
    public Dictionary<string, FactionComponent> Factions { get; } = new();
    public List<BuildQueueItem> BuildQueue { get; } = new();
    public List<TransportProductionQueueItem> TransportProductionQueue { get; } = new();

    public EntityManager EntityManager { get; } = new();

    // ─── Delta 追踪 ───
    private readonly HashSet<string> _dirtyNodes = new();
    private readonly HashSet<string> _dirtyEdges = new();
    private readonly HashSet<int> _dirtyArmies = new();
    private readonly HashSet<int> _dirtyFormations = new();
    private readonly HashSet<int> _dirtyLogistics = new();
    private readonly HashSet<string> _dirtyRallyPoints = new();
    private readonly HashSet<string> _dirtyTransportStocks = new();
    private readonly HashSet<string> _dirtyFactions = new();
    private readonly List<int> _removedEntities = new();
    private readonly List<string> _removedRallyPoints = new();
    private readonly List<GameEvent> _events = new();

    /// <summary>标记节点已变化</summary>
    public void MarkDirty(NodeComponent node) => _dirtyNodes.Add(node.Id);
    /// <summary>标记边已变化</summary>
    public void MarkDirty(EdgeComponent edge) => _dirtyEdges.Add(edge.Id);
    /// <summary>标记军队已变化</summary>
    public void MarkDirty(ArmyComponent army) => _dirtyArmies.Add(army.EntityId);
    /// <summary>标记编组已变化</summary>
    public void MarkDirty(FormationComponent formation) => _dirtyFormations.Add(formation.EntityId);
    /// <summary>标记物流已变化</summary>
    public void MarkDirty(LogisticsComponent logi) => _dirtyLogistics.Add(logi.EntityId);
    /// <summary>标记集结点已变化</summary>
    public void MarkDirty(RallyPointComponent rally) => _dirtyRallyPoints.Add(rally.NodeId);
    /// <summary>标记运输工具库存已变化</summary>
    public void MarkDirty(TransportStockComponent stock) => _dirtyTransportStocks.Add(stock.NodeId);
    /// <summary>标记势力已变化</summary>
    public void MarkDirty(FactionComponent faction) => _dirtyFactions.Add(faction.Id);
    /// <summary>记录实体已移除</summary>
    public void MarkRemoved(int entityId) => _removedEntities.Add(entityId);
    /// <summary>记录集结点已移除</summary>
    public void MarkRemovedRallyPoint(string nodeId) => _removedRallyPoints.Add(nodeId);
    /// <summary>添加游戏事件</summary>
    public void AddEvent(GameEvent evt) => _events.Add(evt);

    /// <summary>开始新一轮 Delta 追踪</summary>
    public void BeginDeltaTracking()
    {
        _dirtyNodes.Clear();
        _dirtyEdges.Clear();
        _dirtyArmies.Clear();
        _dirtyFormations.Clear();
        _dirtyLogistics.Clear();
        _dirtyRallyPoints.Clear();
        _dirtyTransportStocks.Clear();
        _dirtyFactions.Clear();
        _removedEntities.Clear();
        _removedRallyPoints.Clear();
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
            Formations = new Dictionary<int, FormationComponent>(),
            LogisticsEntities = new Dictionary<int, LogisticsComponent>(),
            RallyPoints = new Dictionary<string, RallyPointComponent>(),
            TransportStocks = new Dictionary<string, TransportStockComponent>(),
            Factions = new Dictionary<string, FactionComponent>(),
            RemovedEntityIds = new List<int>(_removedEntities),
            RemovedRallyPointIds = new List<string>(_removedRallyPoints),
            Events = new List<GameEvent>(_events),
            BuildQueue = new List<BuildQueueItem>(BuildQueue),
            TransportProductionQueue = new List<TransportProductionQueueItem>(TransportProductionQueue)
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

        foreach (var id in _dirtyFormations)
            if (Formations.TryGetValue(id, out var formation))
                delta.Formations[id] = formation;

        foreach (var id in _dirtyLogistics)
            if (Logistics.TryGetValue(id, out var logi))
                delta.LogisticsEntities[id] = logi;

        foreach (var id in _dirtyRallyPoints)
            if (RallyPoints.TryGetValue(id, out var rally))
                delta.RallyPoints[id] = rally;

        foreach (var id in _dirtyTransportStocks)
            if (TransportStocks.TryGetValue(id, out var stock))
                delta.TransportStocks[id] = stock;

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
                UnlockedTechs = new List<string> { "SCAVENGING", "TRANSIT_ROADS" }
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
                if (Nodes.TryGetValue(nodeId, out var node) && garrison > 0)
                {
                    CreateStartingCompany(node, garrison);
                    node.GarrisonCount = garrison;
                }
            }

            // 应用初始资源
            foreach (var nodeId in factionStart.OwnedNodes)
            {
                if (Nodes.TryGetValue(nodeId, out var node))
                {
                    node.InvFood = 500;
                    node.InvIron = 200;
                    node.InvAmmo = 0;

                    TransportStocks[nodeId] = new TransportStockComponent
                    {
                        NodeId = nodeId,
                        FactionId = factionStart.FactionId,
                        Stock = new Dictionary<string, TransportStockEntry>
                        {
                            ["PORTER"] = new TransportStockEntry
                            {
                                TransportType = "PORTER",
                                Total = nodeId == factionStart.CapitalNode ? 6 : 2,
                                Idle = nodeId == factionStart.CapitalNode ? 6 : 2
                            },
                            ["CARRIAGE"] = new TransportStockEntry { TransportType = "CARRIAGE" },
                            ["TRAIN"] = new TransportStockEntry { TransportType = "TRAIN" }
                        }
                    };
                }
            }
        }
    }

    private void CreateStartingCompany(NodeComponent node, int strength)
    {
        var entityId = EntityManager.CreateEntityId();
        var army = new ArmyComponent
        {
            EntityId = entityId,
            FactionId = node.FactionId,
            UnitKind = "COMPANY",
            UnitDefId = "MILITIA",
            Name = $"{node.Name} 民兵连",
            Strength = strength,
            MaxStrength = Math.Max(strength, 10),
            TroopCount = strength,
            MeleeTroops = strength,
            Morale = 1.0f,
            SupplyFood = Math.Max(10, strength * 2),
            MaxSupplyFood = Math.Max(10, strength * 2),
            SupplyAmmo = 0,
            MaxSupplyAmmo = 0,
            CarryFood = Math.Max(10, strength * 2),
            CarryAmmo = 0,
            CurrentNodeId = node.Id,
            State = "IDLE"
        };
        Armies[entityId] = army;
    }
}

/// <summary>单 Tick 的增量状态</summary>
public class TickDelta
{
    public int Tick { get; set; }
    public Dictionary<string, NodeComponent> Nodes { get; set; } = new();
    public Dictionary<string, EdgeComponent> Edges { get; set; } = new();
    public Dictionary<int, ArmyComponent> Armies { get; set; } = new();
    public Dictionary<int, FormationComponent> Formations { get; set; } = new();
    public Dictionary<int, LogisticsComponent> LogisticsEntities { get; set; } = new();
    public Dictionary<string, RallyPointComponent> RallyPoints { get; set; } = new();
    public Dictionary<string, TransportStockComponent> TransportStocks { get; set; } = new();
    public Dictionary<string, FactionComponent> Factions { get; set; } = new();
    public List<int> RemovedEntityIds { get; set; } = new();
    public List<string> RemovedRallyPointIds { get; set; } = new();
    public List<GameEvent> Events { get; set; } = new();
    public List<BuildQueueItem> BuildQueue { get; set; } = new();
    public List<TransportProductionQueueItem> TransportProductionQueue { get; set; } = new();
}

/// <summary>游戏事件</summary>
public class GameEvent
{
    public string Type { get; set; } = "";
    public string TextKey { get; set; } = "";
    public Dictionary<string, object> Params { get; set; } = new();
}
