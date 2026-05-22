using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 资源定义记录
/// </summary>
public record ResourceDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("max_stack")]
    public int MaxStack { get; init; }

    [JsonPropertyName("decay_rate")]
    public double DecayRate { get; init; }

    [JsonPropertyName("trade_value")]
    public double TradeValue { get; init; }
}
