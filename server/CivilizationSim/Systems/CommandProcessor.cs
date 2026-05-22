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
        if (fromNode.FactionId != "PLAYER") return;

        // 决定运输类型（根据已解锁科技选择最高级的）
        var faction = world.Factions.GetValueOrDefault("PLAYER");
        if (faction == null) return;

        string transportType = "PORTER"; // 默认
        if (faction.UnlockedTechs.Contains("TRANSIT_RAILWAY")) transportType = "TRAIN";
        else if (faction.UnlockedTechs.Contains("TRANSIT_WAGONS")) transportType = "CARRIAGE";
        else if (!faction.UnlockedTechs.Contains("TRANSIT_ROADS"))
        {
            logger.Log("[物流] 需要先研发「修筑道路」科技！");
            return;
        }

        var transportDef = dict.GetTransport(transportType);
        if (transportDef == null) return;

        // 查找连接两个节点的边
        string? edgeId = null;
        foreach (var (eid, edge) in world.Edges)
        {
            if ((edge.SourceNodeId == cmd.FromNodeId && edge.TargetNodeId == cmd.TargetNodeId) ||
                (edge.SourceNodeId == cmd.TargetNodeId && edge.TargetNodeId == cmd.FromNodeId))
            {
                edgeId = eid;
                break;
            }
        }

        if (edgeId == null)
        {
            logger.Log($"[物流] {cmd.FromNodeId} 和 {cmd.TargetNodeId} 之间没有直接道路！");
            return;
        }

        // 确定货物类型（默认 FOOD，可通过 cmd.BuildingType 传递）
        string cargoType = cmd.BuildingType ?? "FOOD";

        // 从源节点装货
        int capacity = transportDef.Capacity;
        int available = cargoType switch
        {
            "FOOD" => fromNode.InvFood,
            "IRON" => fromNode.InvIron,
            "AMMO" => fromNode.InvAmmo,
            _ => 0
        };
        int loadAmount = Math.Min(capacity, available);
        if (loadAmount <= 0)
        {
            logger.Log($"[物流] {fromNode.Name} 没有足够的 {cargoType} 可运输！");
            return;
        }

        // 扣除源节点资源
        switch (cargoType)
        {
            case "FOOD": fromNode.InvFood -= loadAmount; break;
            case "IRON": fromNode.InvIron -= loadAmount; break;
            case "AMMO": fromNode.InvAmmo -= loadAmount; break;
        }
        world.MarkDirty(fromNode);

        // 创建物流实体
        int entityId = world.EntityManager.CreateEntityId();
        var logistics = new LogisticsComponent
        {
            EntityId = entityId,
            FactionId = "PLAYER",
            TransportType = transportType,
            CargoType = cargoType,
            CargoAmount = loadAmount,
            FromNodeId = cmd.FromNodeId,
            ToNodeId = cmd.TargetNodeId,
            CurrentEdgeId = edgeId,
            EdgeProgress = 0,
            Returning = false
        };

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
                ["amount"] = loadAmount
            }
        });

        logger.Log($"[物流] 创建路线 {fromNode.Name} → {toNode.Name}，运输 {cargoType} × {loadAmount}（{transportType}）");
    }

    private void ProcessCancelRoute(World world, DictRegistry dict, GameCommand cmd, GameLogger logger)
    {
        // cmd.TroopCount 用于传递 entityId
        int entityId = cmd.TroopCount;
        if (world.Logistics.TryGetValue(entityId, out var logi))
        {
            // 将剩余货物返回源节点
            if (logi.CargoAmount > 0 && world.Nodes.TryGetValue(logi.FromNodeId, out var srcNode))
            {
                switch (logi.CargoType)
                {
                    case "FOOD": srcNode.InvFood += logi.CargoAmount; break;
                    case "IRON": srcNode.InvIron += logi.CargoAmount; break;
                    case "AMMO": srcNode.InvAmmo += logi.CargoAmount; break;
                }
                world.MarkDirty(srcNode);
            }

            world.Logistics.Remove(entityId);
            world.MarkRemoved(entityId);
            logger.Log($"[物流] 取消路线 #{entityId}");
        }
    }
}
