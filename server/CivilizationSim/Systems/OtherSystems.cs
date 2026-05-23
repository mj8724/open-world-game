using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Ecs.Components;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>科技研发系统 — Phase 1 基础实现</summary>
public class TechSystem : IGameSystem
{
    public int Order => 50;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        foreach (var (factionId, faction) in world.Factions)
        {
            if (string.IsNullOrEmpty(faction.ResearchingTechId)) continue;

            if (!dict.HasTech(faction.ResearchingTechId)) continue;

            var techDef = dict.GetTech(faction.ResearchingTechId);
            faction.ResearchProgress++;
            world.MarkDirty(faction);

            if (faction.ResearchProgress >= techDef.ResearchTicks)
            {
                faction.UnlockedTechs.Add(faction.ResearchingTechId);
                world.AddEvent(new GameEvent
                {
                    Type = "TECH_COMPLETE",
                    TextKey = "event.tech_complete",
                    Params = new() { ["tech"] = faction.ResearchingTechId, ["faction"] = factionId }
                });
                logger.Log($"[科技完成] {factionId} 解锁 {faction.ResearchingTechId}");
                faction.ResearchingTechId = null;
                faction.ResearchProgress = 0;
            }
        }
    }
}

/// <summary>运输工具生产系统</summary>
public class TransportProductionSystem : IGameSystem
{
    public int Order => 25;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        for (int i = world.TransportProductionQueue.Count - 1; i >= 0; i--)
        {
            var item = world.TransportProductionQueue[i];
            item.RemainingTicks--;
            if (item.RemainingTicks > 0) continue;

            if (world.Nodes.TryGetValue(item.NodeId, out var node))
            {
                if (!world.TransportStocks.TryGetValue(item.NodeId, out var stock))
                {
                    stock = new TransportStockComponent { NodeId = item.NodeId, FactionId = item.FactionId };
                    world.TransportStocks[item.NodeId] = stock;
                }
                if (!stock.Stock.TryGetValue(item.TransportType, out var entry))
                {
                    entry = new TransportStockEntry { TransportType = item.TransportType };
                    stock.Stock[item.TransportType] = entry;
                }
                entry.Total += item.Quantity;
                entry.Idle += item.Quantity;
                world.MarkDirty(stock);
                logger.Log($"[物流] {node.Name} 完成生产 {item.TransportType} × {item.Quantity}");
            }

            world.TransportProductionQueue.RemoveAt(i);
        }
    }
}

/// <summary>物流自动规划系统</summary>
public class LogisticsPlanningSystem : IGameSystem
{
    public int Order => 55;
    private int _ticks;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        _ticks++;
        if (_ticks % 5 != 0) return;
        if (!world.Factions.TryGetValue("PLAYER", out var faction)) return;

        var demands = world.RallyPoints.Values
            .Where(r => r.Enabled && r.FactionId == "PLAYER")
            .SelectMany(r => r.CargoPolicies.Values
                .Where(p => p.Enabled && (p.Unlimited || (p.TargetQuantity ?? 0) > GetCargo(world.Nodes[r.NodeId], p.CargoType)))
                .Select(p => (rally: r, policy: p)))
            .OrderByDescending(x => x.policy.Priority)
            .ToList();

        foreach (var (rally, policy) in demands)
        {
            if (!world.Nodes.TryGetValue(rally.NodeId, out var dest)) continue;
            if (HasAutoRoute(world, rally.NodeId, policy.CargoType)) continue;

            var source = FindBestSource(world, rally.NodeId, policy.CargoType, policy.Priority);
            if (source == null) continue;

            string transportType = ChooseAvailableTransport(world, dict, faction, source.Id);
            if (transportType == "") continue;

            var pathNodes = FindCompatiblePath(world, transportType, source.Id, rally.NodeId);
            if (pathNodes.Count < 2) continue;
            var pathEdges = GetPathEdges(world, pathNodes);
            if (pathEdges.Count != pathNodes.Count - 1) continue;

            var stock = world.TransportStocks[source.Id];
            var stockEntry = stock.Stock[transportType];
            stockEntry.Idle -= 1;
            stockEntry.Assigned += 1;
            world.MarkDirty(stock);

            var transport = dict.GetTransport(transportType);
            int id = world.EntityManager.CreateEntityId();
            var route = new LogisticsComponent
            {
                EntityId = id,
                FactionId = "PLAYER",
                Mode = "AUTO",
                TransportType = transportType,
                AssignedTransportCount = 1,
                CargoType = policy.CargoType,
                FromNodeId = source.Id,
                ToNodeId = rally.NodeId,
                CurrentEdgeId = pathEdges.FirstOrDefault(),
                PathNodeIds = pathNodes,
                PathEdgeIds = pathEdges,
                Priority = policy.Priority,
                DesiredTargetQuantity = policy.TargetQuantity,
                UnlimitedTarget = policy.Unlimited,
                SourceKind = IsProductionSource(source, policy.CargoType) ? "PRODUCTION" : "RALLY_SURPLUS",
                AutoManagedKey = $"AUTO:{rally.NodeId}:{policy.CargoType}:{source.Id}:{transportType}",
                Trips = new() { new LogisticsTripState { TripId = 1 } }
            };
            UpdateRouteEstimates(world, dict, route);
            world.Logistics[id] = route;
            world.MarkDirty(route);
            logger.Log($"[物流] 自动生成路线 {source.Name} → {dest.Name}，{policy.CargoType}");
        }
    }

    private static bool HasAutoRoute(World world, string toNodeId, string cargoType) => world.Logistics.Values.Any(r =>
        r.Mode == "AUTO" && r.Enabled && r.ToNodeId == toNodeId && r.CargoType == cargoType);

    private static NodeComponent? FindBestSource(World world, string targetNodeId, string cargoType, int destPriority)
    {
        var production = world.Nodes.Values
            .Where(n => n.FactionId == "PLAYER" && n.Id != targetNodeId && IsProductionSource(n, cargoType) && GetCargo(n, cargoType) > 120)
            .OrderByDescending(n => GetCargo(n, cargoType))
            .FirstOrDefault();
        if (production != null) return production;

        return world.RallyPoints.Values
            .Where(r => r.NodeId != targetNodeId && r.FactionId == "PLAYER" && world.Nodes.ContainsKey(r.NodeId))
            .Where(r => !r.CargoPolicies.TryGetValue(cargoType, out var policy) || policy.Priority < destPriority)
            .Select(r => world.Nodes[r.NodeId])
            .Where(n => GetCargo(n, cargoType) > 200)
            .OrderByDescending(n => GetCargo(n, cargoType))
            .FirstOrDefault();
    }

    private static bool IsProductionSource(NodeComponent node, string cargoType) => cargoType switch
    {
        "FOOD" => node.FarmLevel > 0,
        "IRON" => node.MineLevel > 0,
        "AMMO" => node.ArsenalLevel > 0,
        _ => false
    };

    private static string ChooseAvailableTransport(World world, DictRegistry dict, FactionComponent faction, string nodeId)
    {
        if (!world.TransportStocks.TryGetValue(nodeId, out var stock)) return "";
        foreach (var type in new[] { "TRAIN", "CARRIAGE", "PORTER" })
        {
            if (!dict.HasTransport(type)) continue;
            var def = dict.GetTransport(type);
            if (!string.IsNullOrEmpty(def.RequiredTech) && !faction.UnlockedTechs.Contains(def.RequiredTech)) continue;
            if (stock.Stock.TryGetValue(type, out var entry) && entry.Idle > 0) return type;
        }
        return "";
    }

    private static int GetCargo(NodeComponent node, string cargoType) => cargoType switch
    {
        "FOOD" => node.InvFood,
        "IRON" => node.InvIron,
        "AMMO" => node.InvAmmo,
        _ => 0
    };

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

    private static void UpdateRouteEstimates(World world, DictRegistry dict, LogisticsComponent route)
    {
        var transport = dict.GetTransport(route.TransportType);
        float oneWay = route.PathEdgeIds.Select(id => world.Edges[id].Length / Math.Max(1, transport.Speed)).Sum();
        route.EstimatedRoundTripTicks = oneWay * 2;
        route.EstimatedThroughputPerTick = route.EstimatedRoundTripTicks > 0
            ? route.AssignedTransportCount * transport.Capacity / route.EstimatedRoundTripTicks
            : 0;
        route.EstimatedRequiredTransportCount = Math.Max(1, (int)Math.Ceiling((route.DesiredTargetQuantity ?? transport.Capacity) / (float)Math.Max(1, transport.Capacity)));
    }
}

/// <summary>运输工具维护系统</summary>
public class TransportMaintenanceSystem : IGameSystem
{
    public int Order => 58;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        foreach (var stock in world.TransportStocks.Values)
        {
            if (!world.Nodes.TryGetValue(stock.NodeId, out var node)) continue;
            bool changed = false;
            foreach (var entry in stock.Stock.Values)
            {
                if (!dict.HasTransport(entry.TransportType)) continue;
                var def = dict.GetTransport(entry.TransportType);
                int active = Math.Max(0, entry.Total - entry.MaintenanceBlocked);
                int foodCost = active * def.MaintenanceFoodPerTick;
                int ironCost = active * def.MaintenanceIronPerTick;
                if (foodCost == 0 && ironCost == 0) continue;

                if (node.InvFood >= foodCost && node.InvIron >= ironCost)
                {
                    node.InvFood -= foodCost;
                    node.InvIron -= ironCost;
                    entry.MaintenanceBlocked = 0;
                    changed = true;
                    world.MarkDirty(node);
                }
                else
                {
                    entry.MaintenanceBlocked = entry.Total;
                    changed = true;
                }
            }
            if (changed) world.MarkDirty(stock);
        }
    }
}

/// <summary>物流系统 — 处理运输路线上的货物移动</summary>
public class LogisticsSystem : IGameSystem
{
    public int Order => 60;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        foreach (var (_, logi) in world.Logistics)
        {
            if (!logi.Enabled || logi.PathEdgeIds.Count == 0) continue;
            var transportDef = dict.GetTransport(logi.TransportType);
            int deliveredThisTick = 0;

            foreach (var trip in logi.Trips)
            {
                if (trip.CargoAmount == 0 && !trip.Returning)
                {
                    if (!world.Nodes.TryGetValue(logi.FromNodeId, out var srcNode)) continue;
                    int available = GetCargo(srcNode, logi.CargoType);
                    int load = Math.Min(transportDef.Capacity, Math.Max(0, available - 50));
                    if (load <= 0) continue;
                    RemoveCargo(srcNode, logi.CargoType, load);
                    trip.CargoAmount = load;
                    world.MarkDirty(srcNode);
                }

                var edgeIndex = Math.Clamp(trip.CurrentPathIndex, 0, logi.PathEdgeIds.Count - 1);
                var edgeId = logi.PathEdgeIds[edgeIndex];
                if (!world.Edges.TryGetValue(edgeId, out var edge)) continue;
                trip.EdgeProgress += transportDef.Speed / Math.Max(1, edge.Length);
                logi.CurrentEdgeId = edgeId;
                logi.EdgeProgress = trip.EdgeProgress;
                logi.Returning = trip.Returning;
                logi.CargoAmount = logi.Trips.Sum(t => t.CargoAmount);

                if (trip.EdgeProgress < 1) continue;
                trip.EdgeProgress = 0;

                if (!trip.Returning)
                {
                    if (trip.CurrentPathIndex < logi.PathEdgeIds.Count - 1)
                    {
                        trip.CurrentPathIndex++;
                    }
                    else
                    {
                        if (world.Nodes.TryGetValue(logi.ToNodeId, out var destNode))
                        {
                            AddCargo(destNode, logi.CargoType, trip.CargoAmount);
                            deliveredThisTick += trip.CargoAmount;
                            world.MarkDirty(destNode);
                        }
                        trip.CargoAmount = 0;
                        trip.Returning = true;
                    }
                }
                else
                {
                    if (trip.CurrentPathIndex > 0)
                    {
                        trip.CurrentPathIndex--;
                    }
                    else
                    {
                        trip.Returning = false;
                    }
                }
            }

            logi.DeliveredLastTick = deliveredThisTick;
            logi.DeliveredTotal += deliveredThisTick;
            if (deliveredThisTick > 0)
            {
                world.AddEvent(new GameEvent
                {
                    Type = "LOGISTICS_DELIVER",
                    TextKey = "event.logistics_deliver",
                    Params = new() { ["from"] = logi.FromNodeId, ["to"] = logi.ToNodeId, ["cargo"] = logi.CargoType, ["amount"] = deliveredThisTick }
                });
            }
            world.MarkDirty(logi);
        }
    }

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
}

/// <summary>战斗系统 — Phase 1 桩实现</summary>
public class CombatSystem : IGameSystem
{
    public int Order => 70;
    public void Execute(World world, DictRegistry dict, GameLogger logger) { /* Phase 4 实现 */ }
}

/// <summary>士气系统 — Phase 1 桩实现</summary>
public class MoraleSystem : IGameSystem
{
    public int Order => 80;
    public void Execute(World world, DictRegistry dict, GameLogger logger) { /* Phase 4 实现 */ }
}

/// <summary>AI决策系统 — Phase 1 桩实现</summary>
public class AISystem : IGameSystem
{
    public int Order => 90;
    public void Execute(World world, DictRegistry dict, GameLogger logger) { /* Phase 4 实现 */ }
}

/// <summary>事件系统 — Phase 1 桩实现</summary>
public class EventSystem : IGameSystem
{
    public int Order => 100;
    public void Execute(World world, DictRegistry dict, GameLogger logger) { /* Phase 5 实现 */ }
}
