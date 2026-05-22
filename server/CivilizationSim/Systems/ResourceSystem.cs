using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>资源系统 — 每 Tick 计算节点的资源产出与消耗</summary>
public class ResourceSystem : IGameSystem
{
    public int Order => 30;

    public void Execute(World world, DictRegistry dict, GameLogger logger)
    {
        var farmDef = dict.GetBuilding("FARM");
        var mineDef = dict.GetBuilding("MINE");
        var arsenalDef = dict.GetBuilding("ARSENAL");

        // 预计算各势力的科技加成
        var factionFoodBonus = new Dictionary<string, double>();
        var factionIronBonus = new Dictionary<string, double>();
        foreach (var (fid, faction) in world.Factions)
        {
            double foodBonus = 0;
            foreach (var techId in faction.UnlockedTechs)
            {
                if (!dict.HasTech(techId)) continue;
                var tech = dict.GetTech(techId);
                if (tech.Effects.TryGetValue("food_production_bonus", out var fb))
                {
                    if (fb is System.Text.Json.JsonElement je)
                        foodBonus += je.GetDouble();
                    else
                        foodBonus += Convert.ToDouble(fb);
                }
            }
            factionFoodBonus[fid] = foodBonus;
            factionIronBonus[fid] = 0; // 未来扩展
        }

        foreach (var (nodeId, node) in world.Nodes)
        {
            if (node.FactionId == "NEUTRAL") continue;

            // ─── 粮食产出（含科技加成） ───
            int baseFoodOutput = (int)farmDef.GetLevelValue(node.FarmLevel, "food_per_tick");
            double foodBonusMul = 1.0 + factionFoodBonus.GetValueOrDefault(node.FactionId, 0);
            int foodOutput = (int)(baseFoodOutput * foodBonusMul);

            // ─── 铁矿产出 ───
            int ironOutput = (int)mineDef.GetLevelValue(node.MineLevel, "iron_per_tick");

            // ─── 弹药产出（消耗铁矿） ───
            int ammoOutput = (int)arsenalDef.GetLevelValue(node.ArsenalLevel, "ammo_per_tick");
            int ironCostForAmmo = (int)arsenalDef.GetLevelValue(node.ArsenalLevel, "iron_cost_per_tick");

            // 确保有足够铁矿生产弹药
            if (node.InvIron < ironCostForAmmo)
            {
                ammoOutput = 0;
                ironCostForAmmo = 0;
            }

            // ─── 粮食消耗 ───
            // 每10人消耗1粮食/tick，驻军每人消耗0.5粮食/tick
            int foodConsumed = Math.Max(1, node.PopCount / 10 + node.GarrisonCount / 2);

            // ─── 应用变化 ───
            node.InvFood += foodOutput - foodConsumed;
            node.InvIron += ironOutput - ironCostForAmmo;
            node.InvAmmo += ammoOutput;

            // 资源不得为负
            if (node.InvFood < 0) node.InvFood = 0;
            if (node.InvIron < 0) node.InvIron = 0;
            if (node.InvAmmo < 0) node.InvAmmo = 0;

            world.MarkDirty(node);
        }
    }
}
