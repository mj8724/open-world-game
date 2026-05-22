using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 建筑定义记录
/// </summary>
public record BuildingDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("category")]
    public string Category { get; init; } = "";

    [JsonPropertyName("max_level")]
    public int MaxLevel { get; init; }

    [JsonPropertyName("levels")]
    public Dictionary<string, Dictionary<string, double>> Levels { get; init; } = new();

    /// <summary>
    /// 获取指定等级的属性值
    /// </summary>
    public double GetLevelValue(int level, string property, double defaultValue = 0)
    {
        if (Levels.TryGetValue(level.ToString(), out var props) &&
            props.TryGetValue(property, out var value))
        {
            return value;
        }
        return defaultValue;
    }
}
