using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>人口系统 — 管理人口增长与饥荒</summary>
public class PopulationSystem : IGameSystem
{
    public int Order => 40;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        var formulas = dict.Formulas;

        foreach (var (nodeId, node) in world.Nodes)
        {
            if (node.FactionId == "NEUTRAL") continue;

            // 人口上限 = 基础5 + 农田等级 * 25
            int popCap = 5 + node.FarmLevel * 25;

            // 有粮食盈余且未达上限时人口增长
            if (node.InvFood > node.PopCount * 5 && node.PopCount < popCap)
            {
                var growthRate = formulas.GetValue("population.base_growth_rate", 0.02);
                int growth = Math.Max(1, (int)(node.PopCount * growthRate));
                node.PopCount = Math.Min(popCap, node.PopCount + growth);
            }
            // 无粮食时人口死亡
            else if (node.InvFood <= 0)
            {
                var deathRate = formulas.GetValue("population.starvation_death_rate", 0.05);
                int deaths = Math.Max(1, (int)(node.PopCount * deathRate));
                node.PopCount = Math.Max(1, node.PopCount - deaths);

                world.AddEvent(new GameEvent
                {
                    Type = "STARVATION",
                    TextKey = "event.starvation",
                    Params = new() { ["node"] = node.Name, ["deaths"] = deaths }
                });

                logger.Log($"[饥荒] {node.Name} 饿死 {deaths} 人");
            }

            world.MarkDirty(node);
        }
    }
}
