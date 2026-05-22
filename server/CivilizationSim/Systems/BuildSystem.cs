using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Ecs.Components;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>建造系统 — 处理建造队列倒计时</summary>
public class BuildSystem : IGameSystem
{
    public int Order => 20;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        var completed = new List<BuildQueueItem>();

        foreach (var item in world.BuildQueue)
        {
            item.RemainingTicks--;
            if (item.RemainingTicks <= 0)
            {
                completed.Add(item);
                // 应用建造结果
                if (world.Nodes.TryGetValue(item.NodeId, out var node))
                {
                    switch (item.BuildingType)
                    {
                        case "FARM": node.FarmLevel = item.TargetLevel; break;
                        case "MINE": node.MineLevel = item.TargetLevel; break;
                        case "ARSENAL": node.ArsenalLevel = item.TargetLevel; break;
                        case "WALL":
                            node.WallLevel = item.TargetLevel;
                            var wallDef = dict.GetBuilding("WALL");
                            node.WallHpCurrent = (int)wallDef.GetLevelValue(item.TargetLevel, "wall_hp");
                            break;
                        case "ORACLE_BEACON": node.BeaconLevel = item.TargetLevel; break;
                    }
                    world.MarkDirty(node);
                    world.AddEvent(new GameEvent
                    {
                        Type = "BUILD_COMPLETE",
                        TextKey = "event.build_complete",
                        Params = new() { ["node"] = node.Name, ["building"] = item.BuildingType, ["level"] = item.TargetLevel }
                    });
                    logger.Log($"[建造完成] {node.Name} {item.BuildingType} 升级至 Lv.{item.TargetLevel}");
                }
            }
        }

        foreach (var item in completed)
            world.BuildQueue.Remove(item);
    }
}
