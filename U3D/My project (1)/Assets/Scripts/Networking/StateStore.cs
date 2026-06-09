using System;
using System.Collections.Generic;
using GameState;
using UnityEngine;

/// <summary>
/// StateStore — 客户端状态缓存
/// 移植自 client/src/bridge/state-store.js
/// 存储从服务器接收的游戏状态，应用增量更新，通知监听器
/// </summary>
namespace Networking
{
    public class StateStore
    {
        // ─── 状态数据 ───
        public Dictionary<string, NodeComponent> Nodes { get; private set; } = new();
        public Dictionary<string, EdgeComponent> Edges { get; private set; } = new();
        public Dictionary<string, FactionComponent> Factions { get; private set; } = new();
        public Dictionary<int, ArmyComponent> Armies { get; private set; } = new();
        public Dictionary<int, FormationComponent> Formations { get; private set; } = new();
        public Dictionary<int, LogisticsComponent> Logistics { get; private set; } = new();
        public Dictionary<string, RallyPointComponent> RallyPoints { get; private set; } = new();
        public Dictionary<string, TransportStockComponent> TransportStocks { get; private set; } = new();
        public Dictionary<string, WildResource> WildResources { get; private set; } = new();
        public Dictionary<string, NeutralStructure> NeutralStructures { get; private set; } = new();
        public List<BuildQueueItem> BuildQueue { get; private set; } = new();
        public List<TransportProductionQueueItem> TransportProductionQueue { get; private set; } = new();
        public int CurrentTick { get; private set; }

        public bool Initialized { get; private set; }

        // ─── 事件 ───
        public event Action<StateStore>? OnFullStateUpdate;
        public event Action<TickDelta>? OnTickUpdate;
        public event Action<GameEvent>? OnGameEvent;

        // ─── 方法 ───

        /// <summary>应用完整状态快照</summary>
        public void ApplyFullState(FullStateSnapshot fullState)
        {
            CurrentTick = fullState.Tick;
            Nodes = fullState.Nodes ?? new();
            Edges = fullState.Edges ?? new();
            Factions = fullState.Factions ?? new();
            Armies = fullState.Armies ?? new();
            Formations = fullState.Formations ?? new();
            Logistics = fullState.LogisticsEntities ?? new();
            RallyPoints = fullState.RallyPoints ?? new();
            TransportStocks = fullState.TransportStocks ?? new();
            WildResources = fullState.WildResources ?? new();
            NeutralStructures = fullState.NeutralStructures ?? new();
            BuildQueue = fullState.BuildQueue ?? new();
            TransportProductionQueue = fullState.TransportProductionQueue ?? new();
            Initialized = true;

            Debug.Log($"[StateStore] 收到完整状态: {Nodes.Count} 节点, {Armies.Count} 军队, tick={CurrentTick}");
            OnFullStateUpdate?.Invoke(this);
        }

        /// <summary>应用增量更新 (TickDelta)</summary>
        public void ApplyDelta(TickDelta delta)
        {
            CurrentTick = delta.Tick;

            // 合并节点
            if (delta.Nodes != null)
            {
                foreach (var (id, node) in delta.Nodes)
                {
                    if (Nodes.ContainsKey(id))
                    {
                        // 合并字段（使用反射简化，生产环境可用 AutoMapper）
                        MergeNode(Nodes[id], node);
                    }
                    else
                    {
                        Nodes[id] = node;
                    }
                }
            }

            // 合并边
            if (delta.Edges != null)
            {
                foreach (var (id, edge) in delta.Edges)
                {
                    if (Edges.ContainsKey(id))
                        MergeEdge(Edges[id], edge);
                    else
                        Edges[id] = edge;
                }
            }

            // 合并军队
            if (delta.Armies != null)
            {
                foreach (var (id, army) in delta.Armies)
                {
                    if (Armies.ContainsKey(id))
                        MergeArmy(Armies[id], army);
                    else
                        Armies[id] = army;
                }
            }

            // 合并编组
            if (delta.Formations != null)
            {
                foreach (var (id, formation) in delta.Formations)
                {
                    if (Formations.ContainsKey(id))
                        MergeFormation(Formations[id], formation);
                    else
                        Formations[id] = formation;
                }
            }

            // 合并物流
            if (delta.LogisticsEntities != null)
            {
                foreach (var (id, logi) in delta.LogisticsEntities)
                {
                    if (Logistics.ContainsKey(id))
                        MergeLogistics(Logistics[id], logi);
                    else
                        Logistics[id] = logi;
                }
            }

            // 合并集结点
            if (delta.RallyPoints != null)
            {
                foreach (var (id, rally) in delta.RallyPoints)
                {
                    if (RallyPoints.ContainsKey(id))
                        MergeRallyPoint(RallyPoints[id], rally);
                    else
                        RallyPoints[id] = rally;
                }
            }

            // 合并运输工具库存
            if (delta.TransportStocks != null)
            {
                foreach (var (id, stock) in delta.TransportStocks)
                {
                    if (TransportStocks.ContainsKey(id))
                        MergeTransportStock(TransportStocks[id], stock);
                    else
                        TransportStocks[id] = stock;
                }
            }

            // 合并势力
            if (delta.Factions != null)
            {
                foreach (var (id, faction) in delta.Factions)
                {
                    if (Factions.ContainsKey(id))
                        MergeFaction(Factions[id], faction);
                    else
                        Factions[id] = faction;
                }
            }

            // 合野外资源点
            if (delta.WildResources != null)
            {
                foreach (var (id, wr) in delta.WildResources)
                {
                    if (WildResources.ContainsKey(id))
                        MergeWildResource(WildResources[id], wr);
                    else
                        WildResources[id] = wr;
                }
            }

            // 合并中立建筑
            if (delta.NeutralStructures != null)
            {
                foreach (var (id, ns) in delta.NeutralStructures)
                {
                    if (NeutralStructures.ContainsKey(id))
                        MergeNeutralStructure(NeutralStructures[id], ns);
                    else
                        NeutralStructures[id] = ns;
                }
            }

            // 移除实体
            if (delta.RemovedEntityIds != null)
            {
                foreach (var id in delta.RemovedEntityIds)
                {
                    Armies.Remove(id);
                    Formations.Remove(id);
                    Logistics.Remove(id);
                }
            }

            // 移除集结点
            if (delta.RemovedRallyPointIds != null)
            {
                foreach (var id in delta.RemovedRallyPointIds)
                    RallyPoints.Remove(id);
            }

            // 移除野外资源点
            if (delta.RemovedWildResourceIds != null)
            {
                foreach (var id in delta.RemovedWildResourceIds)
                    WildResources.Remove(id);
            }

            // 移除中立建筑
            if (delta.RemovedNeutralStructureIds != null)
            {
                foreach (var id in delta.RemovedNeutralStructureIds)
                    NeutralStructures.Remove(id);
            }

            // 更新队列（直接替换）
            if (delta.BuildQueue != null)
                BuildQueue = delta.BuildQueue;
            if (delta.TransportProductionQueue != null)
                TransportProductionQueue = delta.TransportProductionQueue;

            // 触发更新事件
            OnTickUpdate?.Invoke(delta);

            // 广播游戏事件
            if (delta.Events != null)
            {
                foreach (var evt in delta.Events)
                    OnGameEvent?.Invoke(evt);
            }
        }

        // ─── 查询帮助方法 ───

        public NodeComponent? GetNode(string id) =>
            Nodes.TryGetValue(id, out var node) ? node : null;

        public EdgeComponent? GetEdge(string id) =>
            Edges.TryGetValue(id, out var edge) ? edge : null;

        public FactionComponent? GetFaction(string id) =>
            Factions.TryGetValue(id, out var faction) ? faction : null;

        public FactionComponent? GetPlayerFaction() =>
            Factions.TryGetValue("PLAYER", out var faction) ? faction : null;

        public string GetTerrain(string nodeId) =>
            Nodes.TryGetValue(nodeId, out var node) ? node.Terrain : "PLAINS";

        /// <summary>计算玩家全局资源总量</summary>
        public PlayerTotals GetPlayerTotals()
        {
            var player = GetPlayerFaction();
            if (player == null) return new PlayerTotals();

            int food = 0, iron = 0, ammo = 0, pop = 0;
            if (player.OwnedNodeIds != null)
            {
                foreach (var nodeId in player.OwnedNodeIds)
                {
                    if (Nodes.TryGetValue(nodeId, out var n))
                    {
                        food += n.InvFood;
                        iron += n.InvIron;
                        ammo += n.InvAmmo;
                        pop += n.PopCount;
                    }
                }
            }
            return new PlayerTotals { Food = food, Iron = iron, Ammo = ammo, Pop = pop };
        }

        // ─── 字段合并方法（简易反射版本，仅合并值类型和字符串） ───

        private static void MergeNode(NodeComponent target, NodeComponent source)
        {
            target.PopCount = source.PopCount;
            target.InvFood = source.InvFood;
            target.InvIron = source.InvIron;
            target.InvAmmo = source.InvAmmo;
            target.FarmLevel = source.FarmLevel;
            target.MineLevel = source.MineLevel;
            target.ArsenalLevel = source.ArsenalLevel;
            target.WallLevel = source.WallLevel;
            target.WallHpCurrent = source.WallHpCurrent;
            target.BeaconLevel = source.BeaconLevel;
            target.GarrisonCount = source.GarrisonCount;
            target.Loyalty = source.Loyalty;
            target.FactionId = source.FactionId;
            if (source.PlacedBuildings.Count > 0) target.PlacedBuildings = source.PlacedBuildings;
            if (source.WallSegments.Count > 0) target.WallSegments = source.WallSegments;
        }

        private static void MergeEdge(EdgeComponent target, EdgeComponent source)
        {
            target.EdgeType = source.EdgeType;
        }

        private static void MergeArmy(ArmyComponent target, ArmyComponent source)
        {
            target.Strength = source.Strength;
            target.TroopCount = source.TroopCount;
            target.Morale = source.Morale;
            target.SupplyFood = source.SupplyFood;
            target.SupplyAmmo = source.SupplyAmmo;
            target.CarryFood = source.CarryFood;
            target.CarryAmmo = source.CarryAmmo;
            target.CurrentNodeId = source.CurrentNodeId;
            target.CurrentEdgeId = source.CurrentEdgeId;
            target.TargetNodeId = source.TargetNodeId;
            target.EdgeProgress = source.EdgeProgress;
            target.State = source.State;
            target.Stance = source.Stance;
        }

        private static void MergeFormation(FormationComponent target, FormationComponent source)
        {
            target.ParentFormationId = source.ParentFormationId;
            target.CurrentNodeId = source.CurrentNodeId;
            if (source.ChildUnitIds.Count > 0) target.ChildUnitIds = source.ChildUnitIds;
        }

        private static void MergeLogistics(LogisticsComponent target, LogisticsComponent source)
        {
            target.Enabled = source.Enabled;
            target.CargoAmount = source.CargoAmount;
            target.CurrentEdgeId = source.CurrentEdgeId;
            target.EdgeProgress = source.EdgeProgress;
            target.Returning = source.Returning;
            target.DeliveredLastTick = source.DeliveredLastTick;
            target.DeliveredTotal = source.DeliveredTotal;
            if (source.Trips.Count > 0) target.Trips = source.Trips;
        }

        private static void MergeRallyPoint(RallyPointComponent target, RallyPointComponent source)
        {
            target.Enabled = source.Enabled;
            if (source.CargoPolicies.Count > 0) target.CargoPolicies = source.CargoPolicies;
        }

        private static void MergeTransportStock(TransportStockComponent target, TransportStockComponent source)
        {
            if (source.Stock.Count > 0) target.Stock = source.Stock;
        }

        private static void MergeFaction(FactionComponent target, FactionComponent source)
        {
            target.ResearchingTechId = source.ResearchingTechId;
            target.ResearchProgress = source.ResearchProgress;
            if (source.OwnedNodeIds.Count > 0) target.OwnedNodeIds = source.OwnedNodeIds;
            if (source.UnlockedTechs.Count > 0) target.UnlockedTechs = source.UnlockedTechs;
        }

        private static void MergeWildResource(WildResource target, WildResource source)
        {
            target.Yield = source.Yield;
            target.OwnerFactionId = source.OwnerFactionId;
        }

        private static void MergeNeutralStructure(NeutralStructure target, NeutralStructure source)
        {
            target.OwnerFactionId = source.OwnerFactionId;
        }
    }

    /// <summary>玩家全局资源总量</summary>
    public class PlayerTotals
    {
        public int Food { get; set; }
        public int Iron { get; set; }
        public int Ammo { get; set; }
        public int Pop { get; set; }
    }
}
