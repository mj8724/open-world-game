using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 科技定义记录
/// </summary>
public record TechDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("category")]
    public string Category { get; init; } = "";

    [JsonPropertyName("tier")]
    public int Tier { get; init; }

    [JsonPropertyName("research_ticks")]
    public int ResearchTicks { get; init; }

    [JsonPropertyName("research_cost_iron")]
    public int ResearchCostIron { get; init; }

    [JsonPropertyName("prerequisites")]
    public List<string> Prerequisites { get; init; } = new();

    [JsonPropertyName("effects")]
    public Dictionary<string, object>? Effects { get; init; }
}
