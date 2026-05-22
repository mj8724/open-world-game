using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 单位定义记录
/// </summary>
public record UnitDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("tier")]
    public int Tier { get; init; }

    [JsonPropertyName("attack")]
    public int Attack { get; init; }

    [JsonPropertyName("defense")]
    public int Defense { get; init; }

    [JsonPropertyName("hp")]
    public int Hp { get; init; }

    [JsonPropertyName("speed")]
    public int Speed { get; init; }

    [JsonPropertyName("range")]
    public int Range { get; init; }

    [JsonPropertyName("ammo_per_attack")]
    public int AmmoPerAttack { get; init; }

    [JsonPropertyName("recruit_cost_food")]
    public int RecruitCostFood { get; init; }

    [JsonPropertyName("recruit_cost_iron")]
    public int RecruitCostIron { get; init; }

    [JsonPropertyName("recruit_ticks")]
    public int RecruitTicks { get; init; }

    [JsonPropertyName("upkeep_food")]
    public int UpkeepFood { get; init; }

    [JsonPropertyName("pop_cost")]
    public int PopCost { get; init; }

    [JsonPropertyName("required_tech")]
    public string? RequiredTech { get; init; }
}
