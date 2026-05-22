using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 运输定义记录
/// </summary>
public record TransportDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("capacity")]
    public int Capacity { get; init; }

    [JsonPropertyName("speed")]
    public int Speed { get; init; }

    [JsonPropertyName("cost_food")]
    public int CostFood { get; init; }

    [JsonPropertyName("cost_iron")]
    public int CostIron { get; init; }

    [JsonPropertyName("required_tech")]
    public string? RequiredTech { get; init; }
}
