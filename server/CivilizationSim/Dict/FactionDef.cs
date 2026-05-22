using System.Text.Json.Serialization;

namespace CivilizationSim.Dict;

/// <summary>
/// 阵营定义记录
/// </summary>
public record FactionDef
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("race")]
    public string Race { get; init; } = "";

    [JsonPropertyName("is_player")]
    public bool IsPlayer { get; init; }

    [JsonPropertyName("color")]
    public string Color { get; init; } = "";

    [JsonPropertyName("starting_bonus")]
    public FactionStartingBonus StartingBonus { get; init; } = new();
}

public record FactionStartingBonus
{
    [JsonPropertyName("food")]
    public int Food { get; init; }

    [JsonPropertyName("iron")]
    public int Iron { get; init; }

    [JsonPropertyName("ammo")]
    public int Ammo { get; init; }
}
