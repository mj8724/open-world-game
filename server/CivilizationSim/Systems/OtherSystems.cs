using CivilizationSim.Dict;
using CivilizationSim.Ecs;
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

/// <summary>物流系统 — 处理运输路线上的货物移动</summary>
public class LogisticsSystem : IGameSystem
{
    public int Order => 60;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        var completed = new List<int>();

        foreach (var (entityId, logi) in world.Logistics)
        {
            // 获取运输类型定义
            var transportDef = dict.GetTransport(logi.TransportType);
            float speed = transportDef?.Speed ?? 1;

            // 在边上移动
            if (logi.CurrentEdgeId != null && world.Edges.TryGetValue(logi.CurrentEdgeId, out var edge))
            {
                float edgeLen = edge.Length > 0 ? edge.Length : 1;
                logi.EdgeProgress += speed / edgeLen;

                if (logi.EdgeProgress >= 1.0f)
                {
                    logi.EdgeProgress = 0;

                    if (!logi.Returning)
                    {
                        // 到达目的地 — 卸货
                        if (world.Nodes.TryGetValue(logi.ToNodeId, out var destNode))
                        {
                            switch (logi.CargoType)
                            {
                                case "FOOD": destNode.InvFood += logi.CargoAmount; break;
                                case "IRON": destNode.InvIron += logi.CargoAmount; break;
                                case "AMMO": destNode.InvAmmo += logi.CargoAmount; break;
                            }
                            world.MarkDirty(destNode);
                            world.AddEvent(new GameEvent
                            {
                                Type = "LOGISTICS_DELIVER",
                                TextKey = "event.logistics_deliver",
                                Params = new()
                                {
                                    ["from"] = logi.FromNodeId,
                                    ["to"] = destNode.Name,
                                    ["cargo"] = logi.CargoType,
                                    ["amount"] = logi.CargoAmount
                                }
                            });
                        }
                        logi.CargoAmount = 0;
                        logi.Returning = true; // 开始返程
                    }
                    else
                    {
                        // 返程到达 — 重新装货
                        if (world.Nodes.TryGetValue(logi.FromNodeId, out var srcNode))
                        {
                            int capacity = transportDef?.Capacity ?? 20;
                            int available = logi.CargoType switch
                            {
                                "FOOD" => srcNode.InvFood,
                                "IRON" => srcNode.InvIron,
                                "AMMO" => srcNode.InvAmmo,
                                _ => 0
                            };
                            int load = Math.Min(capacity, available);
                            if (load > 0)
                            {
                                switch (logi.CargoType)
                                {
                                    case "FOOD": srcNode.InvFood -= load; break;
                                    case "IRON": srcNode.InvIron -= load; break;
                                    case "AMMO": srcNode.InvAmmo -= load; break;
                                }
                                logi.CargoAmount = load;
                                world.MarkDirty(srcNode);
                            }
                        }
                        logi.Returning = false; // 重新出发
                    }
                }

                world.MarkDirty(logi);
            }
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
