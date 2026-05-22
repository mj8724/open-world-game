using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 公式系数定义 - 按系统领域组织
/// </summary>
public record FormulaDef
{
    [JsonPropertyName("population")]
    public PopulationFormula Population { get; init; } = new();

    [JsonPropertyName("combat")]
    public CombatFormula Combat { get; init; } = new();

    [JsonPropertyName("loyalty")]
    public LoyaltyFormula Loyalty { get; init; } = new();

    [JsonPropertyName("tech")]
    public TechFormula Tech { get; init; } = new();

    [JsonPropertyName("logistics")]
    public LogisticsFormula Logistics { get; init; } = new();

    /// <summary>通过点分路径查询公式系数，如 "population.base_growth_rate"</summary>
    public double GetValue(string path, double defaultValue = 0)
    {
        var parts = path.Split('.');
        if (parts.Length != 2) return defaultValue;

        object? section = parts[0] switch
        {
            "population" => Population,
            "combat" => Combat,
            "loyalty" => Loyalty,
            "tech" => Tech,
            "logistics" => Logistics,
            _ => null
        };

        if (section == null) return defaultValue;

        var prop = section.GetType().GetProperties()
            .FirstOrDefault(p =>
            {
                var attr = p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                            .FirstOrDefault() as JsonPropertyNameAttribute;
                return attr?.Name == parts[1];
            });

        if (prop == null) return defaultValue;

        var val = prop.GetValue(section);
        return val switch
        {
            double d => d,
            int i => i,
            float f => f,
            _ => defaultValue
        };
    }
}

public record PopulationFormula
{
    [JsonPropertyName("base_growth_rate")]
    public double BaseGrowthRate { get; init; }

    [JsonPropertyName("food_per_pop_per_tick")]
    public int FoodPerPopPerTick { get; init; }

    [JsonPropertyName("max_pop_no_farm")]
    public int MaxPopNoFarm { get; init; }

    [JsonPropertyName("pop_per_farm_level")]
    public int PopPerFarmLevel { get; init; }

    [JsonPropertyName("starvation_loss_rate")]
    public double StarvationLossRate { get; init; }

    [JsonPropertyName("starvation_loyalty_penalty")]
    public int StarvationLoyaltyPenalty { get; init; }
}

public record CombatFormula
{
    [JsonPropertyName("wall_damage_reduction_per_level")]
    public double WallDamageReductionPerLevel { get; init; }

    [JsonPropertyName("morale_break_threshold")]
    public int MoraleBreakThreshold { get; init; }

    [JsonPropertyName("morale_rout_flee_chance")]
    public double MoraleRoutFleeChance { get; init; }

    [JsonPropertyName("flanking_bonus")]
    public double FlankingBonus { get; init; }

    [JsonPropertyName("defender_terrain_bonus")]
    public double DefenderTerrainBonus { get; init; }

    [JsonPropertyName("siege_wall_damage_multiplier")]
    public double SiegeWallDamageMultiplier { get; init; }
}

public record LoyaltyFormula
{
    [JsonPropertyName("base_loyalty")]
    public int BaseLoyalty { get; init; }

    [JsonPropertyName("max_loyalty")]
    public int MaxLoyalty { get; init; }

    [JsonPropertyName("min_loyalty")]
    public int MinLoyalty { get; init; }

    [JsonPropertyName("loyalty_recovery_per_tick")]
    public int LoyaltyRecoveryPerTick { get; init; }

    [JsonPropertyName("garrison_loyalty_bonus_per_unit")]
    public int GarrisonLoyaltyBonusPerUnit { get; init; }

    [JsonPropertyName("no_garrison_penalty")]
    public int NoGarrisonPenalty { get; init; }

    [JsonPropertyName("conquered_loyalty_start")]
    public int ConqueredLoyaltyStart { get; init; }

    [JsonPropertyName("rebellion_threshold")]
    public int RebellionThreshold { get; init; }
}

public record TechFormula
{
    [JsonPropertyName("base_research_speed")]
    public double BaseResearchSpeed { get; init; }

    [JsonPropertyName("beacon_speed_multiplier")]
    public double BeaconSpeedMultiplier { get; init; }

    [JsonPropertyName("max_concurrent_research")]
    public int MaxConcurrentResearch { get; init; }
}

public record LogisticsFormula
{
    [JsonPropertyName("base_travel_ticks_per_edge")]
    public int BaseTravelTicksPerEdge { get; init; }

    [JsonPropertyName("speed_divisor")]
    public double SpeedDivisor { get; init; }

    [JsonPropertyName("overload_speed_penalty")]
    public double OverloadSpeedPenalty { get; init; }
}
