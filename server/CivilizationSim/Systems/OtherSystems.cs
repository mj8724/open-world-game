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
        ProcessAutoRouteLifecycle(world, logger);
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
        r.Mode == "AUTO" && r.Enabled && !r.RetireWhenIdle && r.ToNodeId == toNodeId && r.CargoType == cargoType);

    private static void ProcessAutoRouteLifecycle(World world, GameLogger logger)
    {
        var routeIds = world.Logistics.Keys.ToList();
        foreach (var routeId in routeIds)
        {
            if (!world.Logistics.TryGetValue(routeId, out var route) || route.Mode != "AUTO" || route.UnlimitedTarget) continue;
            if (!route.DesiredTargetQuantity.HasValue || !world.Nodes.TryGetValue(route.ToNodeId, out var destination)) continue;

            if (GetCargo(destination, route.CargoType) >= route.DesiredTargetQuantity.Value)
            {
                route.RetireWhenIdle = true;
                world.MarkDirty(route);
            }

            if (route.RetireWhenIdle && IsRouteIdleAtSource(route))
            {
                ReleaseRouteTransport(world, route);
                world.Logistics.Remove(route.EntityId);
                world.MarkRemoved(route.EntityId);
                logger.Log($"[物流] 自动路线 #{route.EntityId} 已达成目标并退休");
            }
        }
    }

    private static bool IsRouteIdleAtSource(LogisticsComponent route) => route.Trips.All(t =>
        !t.Returning && t.CargoAmount == 0 && t.CurrentPathIndex == 0 && t.EdgeProgress == 0);

    private static void ReleaseRouteTransport(World world, LogisticsComponent route)
    {
        if (!world.TransportStocks.TryGetValue(route.FromNodeId, out var stock)) return;
        if (!stock.Stock.TryGetValue(route.TransportType, out var entry)) return;
        entry.Assigned = Math.Max(0, entry.Assigned - route.AssignedTransportCount);
        entry.Idle += route.AssignedTransportCount;
        world.MarkDirty(stock);
    }

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
        var routeBudgets = BuildRouteBudgets(world);
        foreach (var (_, logi) in world.Logistics.ToList())
        {
            if (!logi.Enabled || logi.PathEdgeIds.Count == 0) continue;
            var transportDef = dict.GetTransport(logi.TransportType);
            int deliveredThisTick = 0;
            var budgetKey = (logi.FromNodeId, logi.TransportType);
            routeBudgets.TryGetValue(budgetKey, out var remainingBudget);

            foreach (var trip in logi.Trips)
            {
                if (remainingBudget <= 0) continue;
                remainingBudget--;
                if (trip.CargoAmount == 0 && !trip.Returning && !logi.RetireWhenIdle)
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
                        if (logi.RetireWhenIdle && IsRouteIdleAtSource(logi))
                        {
                            ReleaseRouteTransport(world, logi);
                            world.Logistics.Remove(logi.EntityId);
                            world.MarkRemoved(logi.EntityId);
                            logger.Log($"[物流] 自动路线 #{logi.EntityId} 已返回并退休");
                            break;
                        }
                    }
                }
            }

            routeBudgets[budgetKey] = remainingBudget;
            logi.CargoAmount = logi.Trips.Sum(t => t.CargoAmount);
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

    private static Dictionary<(string NodeId, string TransportType), int> BuildRouteBudgets(World world)
    {
        var budgets = new Dictionary<(string, string), int>();
        foreach (var stock in world.TransportStocks.Values)
        {
            foreach (var entry in stock.Stock.Values)
            {
                var blockedAssigned = Math.Max(0, entry.MaintenanceBlocked - entry.Idle);
                budgets[(stock.NodeId, entry.TransportType)] = Math.Max(0, entry.Assigned - blockedAssigned);
            }
        }
        return budgets;
    }

    private static bool IsRouteIdleAtSource(LogisticsComponent route) => route.Trips.All(t =>
        !t.Returning && t.CargoAmount == 0 && t.CurrentPathIndex == 0 && t.EdgeProgress == 0);

    private static void ReleaseRouteTransport(World world, LogisticsComponent route)
    {
        if (!world.TransportStocks.TryGetValue(route.FromNodeId, out var stock)) return;
        if (!stock.Stock.TryGetValue(route.TransportType, out var entry)) return;
        entry.Assigned = Math.Max(0, entry.Assigned - route.AssignedTransportCount);
        entry.Idle += route.AssignedTransportCount;
        world.MarkDirty(stock);
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

/// <summary>军队补给系统</summary>
public class MilitarySupplySystem : IGameSystem
{
    public int Order => 65;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        foreach (var army in world.Armies.Values.ToList())
        {
            if (army.UnitKind != "COMPANY") continue;
            if (!dict.HasUnit(army.UnitDefId)) continue;
            var unit = dict.GetUnit(army.UnitDefId);
            var strength = Math.Max(army.Strength, army.TroopCount);
            if (strength <= 0) continue;

            var foodNeed = Math.Max(1, unit.UpkeepFood * strength / 10);
            if (army.SupplyFood >= foodNeed)
            {
                army.SupplyFood -= foodNeed;
                army.Morale = Math.Min(1.2f, army.Morale + 0.01f);
            }
            else
            {
                army.SupplyFood = 0;
                army.Morale = Math.Max(0.2f, army.Morale - 0.05f);
                if (army.Morale <= 0.3f && strength > 1)
                {
                    army.Strength = strength - 1;
                }
            }

            if (army.CurrentEdgeId == null && army.CurrentNodeId != null && world.Nodes.TryGetValue(army.CurrentNodeId, out var node) && node.FactionId == army.FactionId)
            {
                var foodLoad = Math.Min(node.InvFood, Math.Max(0, army.MaxSupplyFood - army.SupplyFood));
                node.InvFood -= foodLoad;
                army.SupplyFood += foodLoad;
                var ammoLoad = Math.Min(node.InvAmmo, Math.Max(0, army.MaxSupplyAmmo - army.SupplyAmmo));
                node.InvAmmo -= ammoLoad;
                army.SupplyAmmo += ammoLoad;
                SyncNodeGarrisonCountFromCompanies(world, node.Id);
                world.MarkDirty(node);
            }

            SyncLegacyCompanyFields(army);
            world.MarkDirty(army);
        }
    }

    private static void SyncLegacyCompanyFields(ArmyComponent army)
    {
        army.TroopCount = army.Strength;
        army.MeleeTroops = army.Strength;
        army.CarryFood = army.SupplyFood;
        army.CarryAmmo = army.SupplyAmmo;
    }

    private static void SyncNodeGarrisonCountFromCompanies(World world, string nodeId)
    {
        if (!world.Nodes.TryGetValue(nodeId, out var node)) return;
        node.GarrisonCount = world.Armies.Values
            .Where(army => army.CurrentNodeId == nodeId && army.CurrentEdgeId == null && army.State == "IDLE" && army.FactionId == node.FactionId)
            .Sum(army => Math.Max(army.Strength, army.TroopCount));
    }
}

/// <summary>战斗系统 — 连队移动与结算</summary>
public class CombatSystem : IGameSystem
{
    public int Order => 70;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        foreach (var army in world.Armies.Values.ToList())
        {
            if (army.State != "MOVING" || army.CurrentEdgeId == null || army.TargetNodeId == null) continue;
            if (!world.Edges.TryGetValue(army.CurrentEdgeId, out var edge) || !world.Nodes.TryGetValue(army.TargetNodeId, out var target))
            {
                RemoveArmy(world, army);
                continue;
            }

            var unitSpeed = dict.HasUnit(army.UnitDefId) ? dict.GetUnit(army.UnitDefId).Speed : 2;
            army.EdgeProgress += Math.Max(1, unitSpeed) * 5f / Math.Max(1f, edge.Length);
            if (army.EdgeProgress < 1f)
            {
                world.MarkDirty(army);
                continue;
            }

            var sourceNodeId = army.CurrentNodeId;
            army.EdgeProgress = 1f;
            army.CurrentNodeId = target.Id;
            army.CurrentEdgeId = null;
            army.State = "FIGHTING";
            if (sourceNodeId != null) SyncNodeGarrisonCountFromCompanies(world, sourceNodeId);
            ResolveCombat(world, dict, army, target, logger);
        }
    }

    private static void ResolveCombat(World world, DictRegistry dict, ArmyComponent army, NodeComponent target, GameLogger logger)
    {
        var oldFactionId = target.FactionId;
        var defenders = world.Armies.Values
            .Where(unit => unit.EntityId != army.EntityId && unit.CurrentNodeId == target.Id && unit.CurrentEdgeId == null && unit.State == "IDLE" && unit.FactionId != army.FactionId)
            .ToList();
        var attackerPower = AttackPower(dict, army);
        var defenderPower = defenders.Sum(defender => DefensePower(dict, defender));
        if (oldFactionId != army.FactionId) defenderPower += target.WallHpCurrent + target.WallLevel * 25f;
        if (defenders.Count == 0) defenderPower += target.GarrisonCount * 2f;

        if (attackerPower > defenderPower)
        {
            foreach (var defender in defenders) RemoveArmy(world, defender);
            var survivors = Math.Clamp((int)Math.Ceiling((attackerPower - defenderPower) / Math.Max(1f, UnitAttack(dict, army))), 1, Math.Max(1, army.Strength));
            army.Strength = Math.Min(army.MaxStrength, survivors);
            army.State = "IDLE";
            army.Stance = "DEFEND";
            army.TargetNodeId = null;
            SyncLegacyCompanyFields(army);
            CaptureNode(world, target, oldFactionId, army.FactionId);
            target.WallHpCurrent = Math.Max(0, target.WallHpCurrent - Math.Max(1, army.Strength) * 5);
            SyncNodeGarrisonCountFromCompanies(world, target.Id);
            world.MarkDirty(army);
            world.MarkDirty(target);
            world.AddEvent(new GameEvent
            {
                Type = "COMBAT_CAPTURE",
                TextKey = "event.combat_capture",
                Params = new() { ["node"] = target.Name, ["troops"] = army.Strength }
            });
            logger.Log($"[战斗] {target.Name} 被 {army.FactionId} 占领，{army.Name} 剩余 {army.Strength}");
            return;
        }

        var losses = Math.Min(Math.Max(1, army.Strength), Math.Max(1, (int)Math.Ceiling(defenderPower / Math.Max(1f, UnitDefense(dict, army)))));
        foreach (var defender in defenders)
        {
            var defenderLoss = Math.Max(0, (int)Math.Floor(attackerPower / Math.Max(1, defenders.Count) / Math.Max(1f, UnitDefense(dict, defender))));
            defender.Strength = Math.Max(0, defender.Strength - defenderLoss);
            SyncLegacyCompanyFields(defender);
            if (defender.Strength <= 0) RemoveArmy(world, defender);
            else world.MarkDirty(defender);
        }
        var wallDamage = Math.Min(target.WallHpCurrent, (int)Math.Floor(attackerPower / 2f));
        target.WallHpCurrent -= wallDamage;
        army.Strength = Math.Max(0, army.Strength - losses);
        RemoveArmy(world, army);
        SyncNodeGarrisonCountFromCompanies(world, target.Id);
        world.MarkDirty(target);
        world.AddEvent(new GameEvent
        {
            Type = "COMBAT_DEFEAT",
            TextKey = "event.combat_defeat",
            Params = new() { ["node"] = target.Name, ["losses"] = Math.Max(army.TroopCount, losses) }
        });
        logger.Log($"[战斗] 攻击 {target.Name} 失败，守军剩余 {target.GarrisonCount}");
    }

    private static float AttackPower(DictRegistry dict, ArmyComponent army)
    {
        var attack = UnitAttack(dict, army);
        var supplyModifier = army.SupplyFood <= 0 ? 0.65f : 1f;
        if (dict.HasUnit(army.UnitDefId))
        {
            var unit = dict.GetUnit(army.UnitDefId);
            if (unit.AmmoPerAttack > 0)
            {
                var ammoNeed = Math.Max(1, unit.AmmoPerAttack * Math.Max(1, army.Strength / 5));
                if (army.SupplyAmmo < ammoNeed) supplyModifier *= 0.55f;
                else army.SupplyAmmo -= ammoNeed;
            }
        }
        return Math.Max(1, army.Strength) * attack * Math.Max(0.1f, army.Morale) * supplyModifier;
    }

    private static float DefensePower(DictRegistry dict, ArmyComponent army) => Math.Max(1, army.Strength) * UnitDefense(dict, army) * Math.Max(0.1f, army.Morale) * (army.SupplyFood <= 0 ? 0.75f : 1f);

    private static float UnitAttack(DictRegistry dict, ArmyComponent army) => dict.HasUnit(army.UnitDefId) ? Math.Max(1, dict.GetUnit(army.UnitDefId).Attack) : 3;

    private static float UnitDefense(DictRegistry dict, ArmyComponent army) => dict.HasUnit(army.UnitDefId) ? Math.Max(1, dict.GetUnit(army.UnitDefId).Defense) : 2;

    private static void CaptureNode(World world, NodeComponent node, string oldFactionId, string newFactionId)
    {
        if (world.Factions.TryGetValue(oldFactionId, out var oldFaction))
        {
            oldFaction.OwnedNodeIds.Remove(node.Id);
            world.MarkDirty(oldFaction);
        }
        if (world.Factions.TryGetValue(newFactionId, out var newFaction) && !newFaction.OwnedNodeIds.Contains(node.Id))
        {
            newFaction.OwnedNodeIds.Add(node.Id);
            world.MarkDirty(newFaction);
        }

        node.FactionId = newFactionId;
        node.Loyalty = 0.6f;
        if (!world.TransportStocks.ContainsKey(node.Id))
        {
            var stock = new TransportStockComponent { NodeId = node.Id, FactionId = newFactionId };
            world.TransportStocks[node.Id] = stock;
            world.MarkDirty(stock);
        }
    }

    private static void SyncLegacyCompanyFields(ArmyComponent army)
    {
        army.TroopCount = army.Strength;
        army.MeleeTroops = army.Strength;
        army.CarryFood = army.SupplyFood;
        army.CarryAmmo = army.SupplyAmmo;
    }

    private static void SyncNodeGarrisonCountFromCompanies(World world, string nodeId)
    {
        if (!world.Nodes.TryGetValue(nodeId, out var node)) return;
        node.GarrisonCount = world.Armies.Values
            .Where(army => army.CurrentNodeId == nodeId && army.CurrentEdgeId == null && army.State == "IDLE" && army.FactionId == node.FactionId)
            .Sum(army => Math.Max(army.Strength, army.TroopCount));
    }

    private static void RemoveArmy(World world, ArmyComponent army)
    {
        world.Armies.Remove(army.EntityId);
        world.MarkRemoved(army.EntityId);
    }
}

/// <summary>士气系统 — Phase 1 桩实现</summary>
public class MoraleSystem : IGameSystem
{
    public int Order => 80;
    public void Execute(World world, DictRegistry dict, GameLogger logger) { /* Phase 4 实现 */ }
}

/// <summary>AI决策系统</summary>
public class AISystem : IGameSystem
{
    private const int AttackIntervalTicks = 12;
    private const int MinGarrisonToAttack = 4;
    private const int BuildIntervalTicks = 12;
    private const int ResearchIntervalTicks = 15;
    private const int DefenseIntervalTicks = 8;
    private const int ProductionIntervalTicks = 20;
    private int _ticks;

    public int Order => 90;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        _ticks++;

        if (!world.Factions.TryGetValue("AI", out var aiFaction)) return;

        if (_ticks % AttackIntervalTicks == 0)
            ProcessAttack(world, dict, aiFaction, logger);

        if (_ticks % BuildIntervalTicks == 2)
            ProcessBuild(world, dict, aiFaction, logger);

        if (_ticks % ResearchIntervalTicks == 5)
            ProcessResearch(world, dict, aiFaction, logger);

        if (_ticks % DefenseIntervalTicks == 0)
            ProcessDefense(world, dict, aiFaction, logger);

        if (_ticks % ProductionIntervalTicks == 3)
            ProcessProduction(world, dict, aiFaction, logger);
    }

    private static void ProcessBuild(World world, DictRegistry dict, FactionComponent aiFaction, GameLogger logger)
    {
        var aiNodes = aiFaction.OwnedNodeIds
            .Select(id => world.Nodes.TryGetValue(id, out var n) ? n : null)
            .Where(n => n != null)
            .ToList();

        foreach (var node in aiNodes.OrderBy(n => n.GarrisonCount))
        {
            if (node.FactionId != "AI") continue;

            if (world.BuildQueue.Any(b => b.NodeId == node.Id)) continue;

            string? choice = null;
            int foodPerTick = node.FarmLevel > 0 ? (int)dict.GetBuilding("FARM").GetLevelValue(node.FarmLevel, "food_per_tick", 3) : 0;
            int ironPerTick = node.MineLevel > 0 ? (int)dict.GetBuilding("MINE").GetLevelValue(node.MineLevel, "iron_per_tick", 2) : 0;
            int popCap = 5 + (node.FarmLevel) * 25;

            if (node.PopCount >= popCap * 0.8 && node.FarmLevel < 5)
                choice = "FARM";
            else if (node.InvIron < 100 && node.MineLevel < 5)
                choice = "MINE";
            else if (node.InvAmmo < 50 && node.ArsenalLevel < 5)
                choice = "ARSENAL";
            else if (IsBorderNode(world, node) && node.WallLevel < 3)
                choice = "WALL";
            else if (node.FarmLevel < 3)
                choice = "FARM";
            else if (node.MineLevel < 3)
                choice = "MINE";

            if (choice == null) continue;

            var def = dict.GetBuilding(choice);
            int targetLevel = choice switch
            {
                "FARM" => node.FarmLevel + 1,
                "MINE" => node.MineLevel + 1,
                "ARSENAL" => node.ArsenalLevel + 1,
                "WALL" => node.WallLevel + 1,
                _ => 1
            };
            if (targetLevel > def.MaxLevel) continue;

            int cost = (int)def.GetLevelValue(targetLevel, "build_cost_iron", 20);
            if (node.InvIron < cost) continue;

            node.InvIron -= cost;
            world.BuildQueue.Add(new BuildQueueItem
            {
                NodeId = node.Id,
                BuildingType = choice,
                TargetLevel = targetLevel,
                RemainingTicks = (int)def.GetLevelValue(targetLevel, "build_ticks", 8),
                FactionId = "AI"
            });
            world.MarkDirty(node);
            logger.Log($"[AI建造] {node.Name} 开始建造 {choice} Lv.{targetLevel}");
            break;
        }
    }

    private static void ProcessResearch(World world, DictRegistry dict, FactionComponent aiFaction, GameLogger logger)
    {
        if (!string.IsNullOrEmpty(aiFaction.ResearchingTechId)) return;

        var available = dict.AllTechs.Values
            .Where(t => !aiFaction.UnlockedTechs.Contains(t.Id))
            .Where(t => t.Prerequisites.All(p => aiFaction.UnlockedTechs.Contains(p)))
            .ToList();

        if (available.Count == 0) return;

        var choice = available
            .OrderByDescending(t => t.Category switch
            {
                "FOOD" => 3,
                "TRANSIT" => 2,
                "WEAPON" => 1,
                "BUILD" => 0,
                _ => -1
            })
            .ThenBy(t => t.Tier)
            .FirstOrDefault();

        if (choice == null) return;

        if (world.Nodes.TryGetValue(aiFaction.CapitalNodeId, out var capital) && capital.InvIron >= choice.ResearchCostIron)
        {
            capital.InvIron -= choice.ResearchCostIron;
            world.MarkDirty(capital);
            aiFaction.ResearchingTechId = choice.Id;
            aiFaction.ResearchProgress = 0;
            logger.Log($"[AI科技] 开始研发 {choice.Id}");
        }
    }

    private static void ProcessDefense(World world, DictRegistry dict, FactionComponent aiFaction, GameLogger logger)
    {
        foreach (var node in aiFaction.OwnedNodeIds.Select(id => world.Nodes.TryGetValue(id, out var n) ? n : null).Where(n => n != null))
        {
            if (!IsBorderNode(world, node)) continue;

            var threatPower = GetAdjacentEnemyStrength(world, node);
            var ownPower = node.GarrisonCount * 2f + (node.WallHpCurrent + node.WallLevel * 25f);
            if (threatPower <= ownPower * 1.5f) continue;

            var reinforcement = world.Armies.Values
                .Where(army => army.FactionId == "AI" && army.State == "IDLE" && army.CurrentNodeId != null && Math.Max(army.Strength, army.TroopCount) >= 2)
                .Where(army => {
                    if (army.CurrentNodeId != null && world.Nodes.TryGetValue(army.CurrentNodeId, out var n))
                        return !IsBorderNode(world, n);
                    return false;
                })
                .OrderBy(army => Math.Max(army.Strength, army.TroopCount))
                .FirstOrDefault();

            if (reinforcement == null || reinforcement.CurrentNodeId == null) continue;

            var edge = FindAdjacentPath(world, reinforcement.CurrentNodeId, node.Id);
            if (edge == null) continue;

            var currentNode = world.Nodes.TryGetValue(reinforcement.CurrentNodeId, out var currentNode2) ? currentNode2 : null;
            reinforcement.CurrentEdgeId = edge.Id;
            reinforcement.TargetNodeId = node.Id;
            reinforcement.EdgeProgress = 0;
            reinforcement.State = "MOVING";
            reinforcement.Stance = "DEFEND";
            SyncNodeGarrisonCountFromCompanies(world, reinforcement.CurrentNodeId);
            world.MarkDirty(reinforcement);
            if (currentNode != null) world.MarkDirty(currentNode);
            logger.Log($"[AI防御] {reinforcement.Name} 从 {(reinforcement.CurrentNodeId)} 调往 {node.Name}");
        }
    }

    private static void ProcessProduction(World world, DictRegistry dict, FactionComponent aiFaction, GameLogger logger)
    {
        int totalStrength = world.Armies.Values
            .Where(army => army.FactionId == "AI")
            .Sum(army => Math.Max(army.Strength, army.TroopCount));

        int aiNodeCount = aiFaction.OwnedNodeIds.Count;
        int targetStrength = aiNodeCount * 15;

        if (totalStrength >= targetStrength) return;

        var productionNode = aiFaction.OwnedNodeIds
            .Select(id => world.Nodes.TryGetValue(id, out var n) ? n : null)
            .Where(n => n != null && n.InvFood >= 50 && n.InvIron >= 30)
            .OrderByDescending(n => n.PopCount)
            .FirstOrDefault();

        if (productionNode == null) return;

        string unitDefId = "MILITIA";
        if (aiFaction.UnlockedTechs.Contains("WEAPON_MACHINEGUN")) unitDefId = "MAXIM_GUN";
        else if (aiFaction.UnlockedTechs.Contains("WEAPON_GUNPOWDER")) unitDefId = "MUSKETEER";
        else if (aiFaction.UnlockedTechs.Contains("WEAPON_BLADES")) unitDefId = "SWORDSMAN";

        if (!dict.HasUnit(unitDefId)) return;
        var unit = dict.GetUnit(unitDefId);

        if (productionNode.InvFood < unit.RecruitCostFood || productionNode.InvIron < unit.RecruitCostIron) return;

        productionNode.InvFood -= unit.RecruitCostFood;
        productionNode.InvIron -= unit.RecruitCostIron;

        var entityId = world.EntityManager.CreateEntityId();
        var strength = Math.Max(1, unit.PopCost * 5);
        var army = new ArmyComponent
        {
            EntityId = entityId,
            FactionId = "AI",
            CurrentNodeId = productionNode.Id,
            State = "IDLE",
            UnitKind = "COMPANY",
            UnitDefId = unitDefId,
            Name = $"{unit.Name}连 #{entityId}",
            Strength = strength,
            MaxStrength = strength,
            Morale = 1.0f
        };
        army.MaxSupplyFood = Math.Max(10, strength * Math.Max(1, unit.UpkeepFood) * 3);
        army.SupplyFood = army.MaxSupplyFood;
        army.TroopCount = strength;
        world.Armies[entityId] = army;
        SyncNodeGarrisonCountFromCompanies(world, productionNode.Id);
        world.MarkDirty(army);
        world.MarkDirty(productionNode);
        logger.Log($"[AI生产] {productionNode.Name} 组建 {army.Name}");
    }

    private static void ProcessAttack(World world, DictRegistry dict, FactionComponent aiFaction, GameLogger logger)
    {
        var source = world.Armies.Values
            .Where(army => army.FactionId == "AI" && army.State == "IDLE" && army.CurrentNodeId != null && Math.Max(army.Strength, army.TroopCount) >= MinGarrisonToAttack)
            .Where(army => world.Nodes.TryGetValue(army.CurrentNodeId!, out var node) && FindAdjacentTargets(world, node).Any())
            .OrderByDescending(army => Math.Max(army.Strength, army.TroopCount))
            .ThenBy(army => army.EntityId)
            .FirstOrDefault();
        if (source == null || source.CurrentNodeId == null || !world.Nodes.TryGetValue(source.CurrentNodeId, out var sourceNode)) return;

        var target = FindAdjacentTargets(world, sourceNode)
            .OrderByDescending(node => node.FactionId == "NEUTRAL")
            .ThenBy(EstimateDefenderPower)
            .ThenBy(node => node.Id)
            .FirstOrDefault();
        if (target == null) return;

        var edge = FindAdjacentEdge(world, sourceNode.Id, target.Id);
        if (edge == null) return;

        LaunchAttack(world, source, sourceNode, target, edge, logger);
    }

    private static bool IsBorderNode(World world, NodeComponent node)
    {
        return world.Edges.Values.Any(e =>
            (e.SourceNodeId == node.Id && world.Nodes.TryGetValue(e.TargetNodeId, out var t1) && t1.FactionId != node.FactionId) ||
            (e.TargetNodeId == node.Id && world.Nodes.TryGetValue(e.SourceNodeId, out var t2) && t2.FactionId != node.FactionId));
    }

    private static float GetAdjacentEnemyStrength(World world, NodeComponent node)
    {
        float total = 0;
        foreach (var edge in world.Edges.Values)
        {
            var otherId = edge.SourceNodeId == node.Id ? edge.TargetNodeId : edge.TargetNodeId == node.Id ? edge.SourceNodeId : null;
            if (otherId == null || !world.Nodes.TryGetValue(otherId, out var other) || other.FactionId == node.FactionId) continue;
            total += other.GarrisonCount * 2f;
            total += world.Armies.Values
                .Where(a => a.FactionId != node.FactionId && a.CurrentNodeId == otherId && a.CurrentEdgeId == null && a.State == "IDLE")
                .Sum(a => Math.Max(a.Strength, a.TroopCount));
        }
        return total;
    }

    private static EdgeComponent? FindAdjacentPath(World world, string from, string to)
    {
        return world.Edges.Values.FirstOrDefault(e =>
            (e.SourceNodeId == from && e.TargetNodeId == to) ||
            (e.SourceNodeId == to && e.TargetNodeId == from));
    }

    private static IEnumerable<NodeComponent> FindAdjacentTargets(World world, NodeComponent source)
    {
        foreach (var edge in world.Edges.Values)
        {
            var targetId = edge.SourceNodeId == source.Id
                ? edge.TargetNodeId
                : edge.TargetNodeId == source.Id ? edge.SourceNodeId : null;
            if (targetId == null || !world.Nodes.TryGetValue(targetId, out var target) || target.FactionId == source.FactionId) continue;
            yield return target;
        }
    }

    private static EdgeComponent? FindAdjacentEdge(World world, string from, string to) => world.Edges.Values.FirstOrDefault(edge =>
        (edge.SourceNodeId == from && edge.TargetNodeId == to) ||
        (edge.SourceNodeId == to && edge.TargetNodeId == from));

    private static float EstimateDefenderPower(NodeComponent node) => node.GarrisonCount * 2f + node.WallHpCurrent + node.WallLevel * 25f;

    private static void LaunchAttack(World world, ArmyComponent army, NodeComponent source, NodeComponent target, EdgeComponent edge, GameLogger logger)
    {
        army.CurrentEdgeId = edge.Id;
        army.TargetNodeId = target.Id;
        army.EdgeProgress = 0;
        army.State = "MOVING";
        army.Stance = "ATTACK";
        SyncNodeGarrisonCountFromCompanies(world, source.Id);
        world.MarkDirty(source);
        world.MarkDirty(army);
        world.AddEvent(new GameEvent
        {
            Type = "COMBAT_ATTACK",
            TextKey = "event.combat_attack",
            Params = new() { ["from"] = source.Name, ["to"] = target.Name, ["troops"] = Math.Max(army.Strength, army.TroopCount) }
        });
        logger.Log($"[AI] {source.Name} 派出 {army.Name} 攻击 {target.Name}");
    }

    private static void SyncNodeGarrisonCountFromCompanies(World world, string nodeId)
    {
        if (!world.Nodes.TryGetValue(nodeId, out var node)) return;
        node.GarrisonCount = world.Armies.Values
            .Where(army => army.CurrentNodeId == nodeId && army.CurrentEdgeId == null && army.State == "IDLE" && army.FactionId == node.FactionId)
            .Sum(army => Math.Max(army.Strength, army.TroopCount));
    }
}

/// <summary>事件系统 — Phase 1 桩实现</summary>
public class EventSystem : IGameSystem
{
    public int Order => 100;
    public void Execute(World world, DictRegistry dict, GameLogger logger) { /* Phase 5 实现 */ }
}
