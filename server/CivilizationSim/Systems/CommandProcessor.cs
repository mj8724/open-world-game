using System.Text.Json;
using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Ecs.Components;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>玩家指令类型</summary>
public enum CommandAction
{
    BUILD,
    RESEARCH,
    CREATE_ROUTE,
    CANCEL_ROUTE,
    UPDATE_ROUTE,
    SET_RALLY_POINT,
    CLEAR_RALLY_POINT,
    PRODUCE_TRANSPORT,
    ATTACK,
    RETREAT,
    SET_SPEED,
    UPGRADE_EDGE
}

/// <summary>玩家指令</summary>
public class GameCommand
{
    public CommandAction Action { get; set; }
    public string? NodeId { get; set; }
    public string? BuildingType { get; set; }
    public int TargetLevel { get; set; }
    public string? TechId { get; set; }
    public string? FromNodeId { get; set; }
    public string? TargetNodeId { get; set; }
    public string? CargoType { get; set; }
    public string? TransportType { get; set; }
    public int TransportCount { get; set; } = 1;
    public string? RouteMode { get; set; }
    public int Priority { get; set; } = 50;
    public int Quantity { get; set; } = 1;
    public int? TargetQuantity { get; set; }
    public bool Unlimited { get; set; }
    public Dictionary<string, RallyCargoPolicy>? RallyPolicies { get; set; }
    public int TroopCount { get; set; }
    public int Speed { get; set; } = 1;
    public int Seq { get; set; }
}

/// <summary>指令处理器 — 校验并执行玩家指令</summary>
public class CommandProcessor : IGameSystem
{
    public int Order => 10;
    private readonly Queue<GameCommand> _queue = new();
    private readonly object _lock = new();

    /// <summary>将指令入队（线程安全）</summary>
    public void Enqueue(GameCommand cmd)
    {
        lock (_lock)
        {
            _queue.Enqueue(cmd);
        }
    }

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        List<GameCommand> commands;
        lock (_lock)
        {
            commands = new List<GameCommand>(_queue);
            _queue.Clear();
        }

        foreach (var cmd in commands)
        {
            switch (cmd.Action)
            {
                case CommandAction.BUILD:
                    ProcessBuild(world, dict, cmd, logger);
                    break;
                case CommandAction.RESEARCH:
                    ProcessResearch(world, dict, cmd, logger);
                    break;
                case CommandAction.CREATE_ROUTE:
                    ProcessCreateRoute(world, dict, cmd, logger);
                    break;
                case CommandAction.CANCEL_ROUTE:
                    ProcessCancelRoute(world, dict, cmd, logger);
                    break;
                case CommandAction.UPDATE_ROUTE:
                    ProcessUpdateRoute(world, dict, cmd, logger);
                    break;
                case CommandAction.SET_RALLY_POINT:
                    ProcessSetRallyPoint(world, cmd, logger);
                    break;
                case CommandAction.CLEAR_RALLY_POINT:
                    ProcessClearRallyPoint(world, cmd, logger);
                    break;
                case CommandAction.PRODUCE_TRANSPORT:
                    ProcessProduceTransport(world, dict, cmd, logger);
                    break;
                default:
                    logger.Log($"[指令] 未实现的指令类型: {cmd.Action}");
                    break;
            }
        }
    }

    private void ProcessBuild(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        if (cmd.NodeId == null || cmd.BuildingType == null) return;
        if (!world.Nodes.TryGetValue(cmd.NodeId, out var node)) return;
        if (node.FactionId != "PLAYER") return;

        var buildingDef = dict.GetBuilding(cmd.BuildingType);
        int currentLevel = cmd.BuildingType switch
        {
            "FARM" => node.FarmLevel,
            "MINE" => node.MineLevel,
            "ARSENAL" => node.ArsenalLevel,
            "WALL" => node.WallLevel,
            "ORACLE_BEACON" => node.BeaconLevel,
            _ => 0
        };

        int targetLevel = currentLevel + 1;
        if (targetLevel > buildingDef.MaxLevel) return;

        // 检查资源消耗
        var costIron = (int)buildingDef.GetLevelValue(targetLevel, "build_cost_iron");
        if (node.InvIron < costIron) return;

        var buildTicks = (int)buildingDef.GetLevelValue(targetLevel, "build_ticks");

        // 检查是否已在建造队列中
        if (world.BuildQueue.Any(b => b.NodeId == cmd.NodeId && b.BuildingType == cmd.BuildingType))
            return;

        // 扣除资源
        node.InvIron -= costIron;
        world.MarkDirty(node);

        // 加入建造队列
        world.BuildQueue.Add(new BuildQueueItem
        {
            NodeId = cmd.NodeId,
            BuildingType = cmd.BuildingType,
            TargetLevel = targetLevel,
            RemainingTicks = buildTicks,
            FactionId = "PLAYER"
        });

        logger.Log($"[建造] {node.Name} 开始建造 {cmd.BuildingType} Lv.{targetLevel}，需要 {buildTicks} Tick");
    }

    private void ProcessResearch(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        if (cmd.TechId == null) return;
        if (!world.Factions.TryGetValue("PLAYER", out var faction)) return;
        if (!dict.HasTech(cmd.TechId)) return;

        var techDef = dict.GetTech(cmd.TechId);

        // 检查前置科技
        foreach (var prereq in techDef.Prerequisites)
        {
            if (!faction.UnlockedTechs.Contains(prereq)) return;
        }

        // 检查是否已解锁
        if (faction.UnlockedTechs.Contains(cmd.TechId)) return;

        // 检查研发资源消耗（从全局资源池扣除）
        // 简化：从首都扣除
        if (world.Nodes.TryGetValue(faction.CapitalNodeId, out var capital))
        {
            if (capital.InvIron < techDef.ResearchCostIron) return;
            capital.InvIron -= techDef.ResearchCostIron;
            world.MarkDirty(capital);
        }

        faction.ResearchingTechId = cmd.TechId;
        faction.ResearchProgress = 0;
        logger.Log($"[科技] 开始研发 {cmd.TechId}，需要 {techDef.ResearchTicks} Tick");
    }

    private void ProcessCreateRoute(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        if (cmd.FromNodeId == null || cmd.TargetNodeId == null) return;
        if (!world.Nodes.TryGetValue(cmd.FromNodeId, out var fromNode)) return;
        if (!world.Nodes.TryGetValue(cmd.TargetNodeId, out var toNode)) return;
        if (fromNode.FactionId != "PLAYER" || toNode.FactionId != "PLAYER") return;
        if (!world.Factions.TryGetValue("PLAYER", out var faction)) return;

        string cargoType = cmd.CargoType ?? cmd.BuildingType ?? "FOOD";
        if (!IsCargoType(cargoType)) return;

        string transportType = cmd.TransportType ?? "PORTER";
        if (!dict.HasTransport(transportType)) return;
        var transportDef = dict.GetTransport(transportType);

        if (!string.IsNullOrEmpty(transportDef.RequiredTech) && !faction.UnlockedTechs.Contains(transportDef.RequiredTech))
        {
            logger.Log($"[物流] 需要先研发 {transportDef.RequiredTech} 才能使用 {transportType}");
            return;
        }

        int transportCount = Math.Max(1, cmd.TransportCount);
        var stock = EnsureTransportStock(world, fromNode.Id, fromNode.FactionId);
        var stockEntry = EnsureTransportEntry(stock, transportType);
        if (stockEntry.Idle < transportCount)
        {
            logger.Log($"[物流] {fromNode.Name} 可用 {transportType} 不足，需要 {transportCount}，当前 {stockEntry.Idle}");
            return;
        }

        var pathNodes = FindCompatiblePath(world, transportType, cmd.FromNodeId, cmd.TargetNodeId);
        if (pathNodes.Count < 2)
        {
            logger.Log($"[物流] {cmd.FromNodeId} 到 {cmd.TargetNodeId} 没有适合 {transportType} 的路线");
            return;
        }

        var pathEdges = GetPathEdges(world, pathNodes);
        if (pathEdges.Count != pathNodes.Count - 1) return;

        stockEntry.Idle -= transportCount;
        stockEntry.Assigned += transportCount;
        world.MarkDirty(stock);

        int entityId = world.EntityManager.CreateEntityId();
        var logistics = new LogisticsComponent
        {
            EntityId = entityId,
            FactionId = "PLAYER",
            Mode = cmd.RouteMode == "AUTO" ? "AUTO" : "MANUAL",
            TransportType = transportType,
            AssignedTransportCount = transportCount,
            CargoType = cargoType,
            FromNodeId = cmd.FromNodeId,
            ToNodeId = cmd.TargetNodeId,
            CurrentEdgeId = pathEdges.FirstOrDefault(),
            PathNodeIds = pathNodes,
            PathEdgeIds = pathEdges,
            Priority = Math.Clamp(cmd.Priority, 0, 100),
            SourceKind = cmd.RouteMode == "AUTO" ? "PRODUCTION" : "MANUAL",
            DesiredTargetQuantity = cmd.TargetQuantity,
            UnlimitedTarget = cmd.Unlimited
        };

        for (int i = 0; i < transportCount; i++)
        {
            logistics.Trips.Add(new LogisticsTripState { TripId = i + 1 });
        }
        UpdateRouteEstimates(world, dict, logistics);

        world.Logistics[entityId] = logistics;
        world.MarkDirty(logistics);

        world.AddEvent(new GameEvent
        {
            Type = "LOGISTICS_CREATE",
            TextKey = "event.logistics_create",
            Params = new()
            {
                ["from"] = fromNode.Name,
                ["to"] = toNode.Name,
                ["transport"] = transportType,
                ["cargo"] = cargoType,
                ["amount"] = transportDef.Capacity * transportCount
            }
        });

        logger.Log($"[物流] 创建路线 {fromNode.Name} → {toNode.Name}，{transportType} × {transportCount} 运输 {cargoType}");
    }

    private void ProcessCancelRoute(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        int entityId = cmd.TroopCount;
        if (!world.Logistics.TryGetValue(entityId, out var logi)) return;

        int returningCargo = logi.CargoAmount + logi.Trips.Sum(t => t.CargoAmount);
        if (returningCargo > 0 && world.Nodes.TryGetValue(logi.FromNodeId, out var srcNode))
        {
            AddCargo(srcNode, logi.CargoType, returningCargo);
            world.MarkDirty(srcNode);
        }

        if (world.TransportStocks.TryGetValue(logi.FromNodeId, out var stock) &&
            stock.Stock.TryGetValue(logi.TransportType, out var entry))
        {
            entry.Assigned = Math.Max(0, entry.Assigned - logi.AssignedTransportCount);
            entry.Idle += logi.AssignedTransportCount;
            world.MarkDirty(stock);
        }

        world.Logistics.Remove(entityId);
        world.MarkRemoved(entityId);
        logger.Log($"[物流] 取消路线 #{entityId}");
    }

    private void ProcessUpdateRoute(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        int entityId = cmd.TroopCount;
        if (!world.Logistics.TryGetValue(entityId, out var logi)) return;
        logi.Mode = "MANUAL";
        logi.Priority = Math.Clamp(cmd.Priority, 0, 100);
        logi.Enabled = cmd.Speed != 0;
        world.MarkDirty(logi);
    }

    private void ProcessSetRallyPoint(World world, GameCommand cmd, GameLogger logger)
    {
        if (cmd.NodeId == null || !world.Nodes.TryGetValue(cmd.NodeId, out var node)) return;
        if (node.FactionId != "PLAYER") return;

        var rally = new RallyPointComponent
        {
            NodeId = node.Id,
            FactionId = node.FactionId,
            Enabled = true,
            CargoPolicies = cmd.RallyPolicies ?? new Dictionary<string, RallyCargoPolicy>()
        };

        foreach (var policy in rally.CargoPolicies.Values)
        {
            policy.Priority = Math.Clamp(policy.Priority, 0, 100);
            policy.Enabled = policy.Enabled && IsCargoType(policy.CargoType);
        }

        world.RallyPoints[node.Id] = rally;
        world.MarkDirty(rally);
        logger.Log($"[物流] 设置集结点 {node.Name}");
    }

    private void ProcessClearRallyPoint(World world, GameCommand cmd, GameLogger logger)
    {
        if (cmd.NodeId == null) return;
        if (world.RallyPoints.Remove(cmd.NodeId))
        {
            world.MarkRemovedRallyPoint(cmd.NodeId);
            logger.Log($"[物流] 清除集结点 {cmd.NodeId}");
        }
    }

    private void ProcessProduceTransport(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        if (cmd.NodeId == null || cmd.TransportType == null) return;
        if (!world.Nodes.TryGetValue(cmd.NodeId, out var node) || node.FactionId != "PLAYER") return;
        if (!world.Factions.TryGetValue("PLAYER", out var faction)) return;

        if (!dict.HasTransport(cmd.TransportType)) return;
        var transport = dict.GetTransport(cmd.TransportType);
        if (!string.IsNullOrEmpty(transport.RequiredTech) && !faction.UnlockedTechs.Contains(transport.RequiredTech)) return;

        int quantity = Math.Max(1, cmd.Quantity);
        int foodCost = transport.CostFood * quantity;
        int ironCost = transport.CostIron * quantity;
        if (node.InvFood < foodCost || node.InvIron < ironCost) return;

        node.InvFood -= foodCost;
        node.InvIron -= ironCost;
        world.MarkDirty(node);
        world.TransportProductionQueue.Add(new TransportProductionQueueItem
        {
            NodeId = node.Id,
            TransportType = transport.Id,
            Quantity = quantity,
            RemainingTicks = transport.BuildTicks * quantity,
            TotalTicks = transport.BuildTicks * quantity,
            FactionId = node.FactionId
        });
        logger.Log($"[物流] {node.Name} 开始生产 {transport.Id} × {quantity}");
    }

    private static bool IsCargoType(string cargoType) => cargoType is "FOOD" or "IRON" or "AMMO";

    private static int GetCargo(NodeComponent node, string cargoType) => cargoType switch
    {
        "FOOD" => node.InvFood,
        "IRON" => node.InvIron,
        "AMMO" => node.InvAmmo,
        _ => 0
    };

    private static void AddCargo(NodeComponent node, string cargoType, int amount)
    {
        switch (cargoType)
        {
            case "FOOD": node.InvFood += amount; break;
            case "IRON": node.InvIron += amount; break;
            case "AMMO": node.InvAmmo += amount; break;
        }
    }

    private static void RemoveCargo(NodeComponent node, string cargoType, int amount)
    {
        switch (cargoType)
        {
            case "FOOD": node.InvFood -= amount; break;
            case "IRON": node.InvIron -= amount; break;
            case "AMMO": node.InvAmmo -= amount; break;
        }
    }

    private static TransportStockComponent EnsureTransportStock(World world, string nodeId, string factionId)
    {
        if (!world.TransportStocks.TryGetValue(nodeId, out var stock))
        {
            stock = new TransportStockComponent { NodeId = nodeId, FactionId = factionId };
            world.TransportStocks[nodeId] = stock;
        }
        return stock;
    }

    private static TransportStockEntry EnsureTransportEntry(TransportStockComponent stock, string transportType)
    {
        if (!stock.Stock.TryGetValue(transportType, out var entry))
        {
            entry = new TransportStockEntry { TransportType = transportType };
            stock.Stock[transportType] = entry;
        }
        return entry;
    }

    private static List<string> FindCompatiblePath(World world, string transportType, string from, string to)
    {
        var edges = world.Edges.Values
            .Where(e => IsEdgeCompatible(transportType, e.EdgeType))
            .Select(e => (e.SourceNodeId, e.TargetNodeId, e.Length));
        return Pathfinding.FindShortestPath(edges, from, to);
    }

    private static bool IsEdgeCompatible(string transportType, string edgeType) => transportType switch
    {
        "TRAIN" => edgeType == "RAILWAY",
        "CARRIAGE" => edgeType is "ROAD" or "RAILWAY",
        _ => edgeType is "TRAIL" or "ROAD" or "RAILWAY"
    };

    private static List<string> GetPathEdges(World world, List<string> pathNodes)
    {
        var edges = new List<string>();
        for (int i = 0; i < pathNodes.Count - 1; i++)
        {
            var from = pathNodes[i];
            var to = pathNodes[i + 1];
            var edge = world.Edges.Values.FirstOrDefault(e =>
                (e.SourceNodeId == from && e.TargetNodeId == to) ||
                (e.SourceNodeId == to && e.TargetNodeId == from));
            if (edge == null) return new List<string>();
            edges.Add(edge.Id);
        }
        return edges;
    }

    private static void UpdateRouteEstimates(World world, DictRegistry dict, LogisticsComponent logistics)
    {
        var transport = dict.GetTransport(logistics.TransportType);
        float oneWay = logistics.PathEdgeIds
            .Select(id => world.Edges.TryGetValue(id, out var edge) ? edge.Length / Math.Max(1, transport.Speed) : 0)
            .Sum();
        logistics.EstimatedRoundTripTicks = oneWay * 2;
        logistics.EstimatedThroughputPerTick = logistics.EstimatedRoundTripTicks > 0
            ? logistics.AssignedTransportCount * transport.Capacity / logistics.EstimatedRoundTripTicks
            : 0;
        int target = logistics.DesiredTargetQuantity ?? transport.Capacity * logistics.AssignedTransportCount;
        logistics.EstimatedRequiredTransportCount = Math.Max(1, (int)Math.Ceiling(target / (float)Math.Max(1, transport.Capacity)));
    }
}
