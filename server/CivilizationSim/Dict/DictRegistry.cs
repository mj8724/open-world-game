using System.Text.Json;

namespace CivilizationSim.Dict;

/// <summary>
/// 全局数据字典注册中心。
/// 所有游戏常量/配置/公式系数通过此类查询。
/// 禁止在 System 中硬编码任何数值。
/// </summary>
public class DictRegistry
{
    private readonly Dictionary<string, ResourceDef> _resources = new();
    private readonly Dictionary<string, BuildingDef> _buildings = new();
    private readonly Dictionary<string, TechDef> _techs = new();
    private readonly Dictionary<string, UnitDef> _units = new();
    private readonly Dictionary<string, TransportDef> _transports = new();
    private readonly Dictionary<string, FactionDef> _factions = new();
    private FormulaDef _formulas = new();
    private MapDef _map = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>从 Dict/Data/ 目录加载所有字典 JSON</summary>
    public static DictRegistry LoadFromDirectory(string dataPath)
    {
        var reg = new DictRegistry();

        reg._resources.Clear();
        var resources = LoadDict<ResourceDef>(Path.Combine(dataPath, "resources.json"));
        foreach (var kv in resources) reg._resources[kv.Key] = kv.Value;

        reg._buildings.Clear();
        var buildings = LoadDict<BuildingDef>(Path.Combine(dataPath, "buildings.json"));
        foreach (var kv in buildings) reg._buildings[kv.Key] = kv.Value;

        reg._techs.Clear();
        var techs = LoadDict<TechDef>(Path.Combine(dataPath, "techs.json"));
        foreach (var kv in techs) reg._techs[kv.Key] = kv.Value;

        reg._units.Clear();
        var units = LoadDict<UnitDef>(Path.Combine(dataPath, "units.json"));
        foreach (var kv in units) reg._units[kv.Key] = kv.Value;

        reg._transports.Clear();
        var transports = LoadDict<TransportDef>(Path.Combine(dataPath, "transports.json"));
        foreach (var kv in transports) reg._transports[kv.Key] = kv.Value;

        reg._factions.Clear();
        var factions = LoadDict<FactionDef>(Path.Combine(dataPath, "factions.json"));
        foreach (var kv in factions) reg._factions[kv.Key] = kv.Value;

        var formulasJson = File.ReadAllText(Path.Combine(dataPath, "formulas.json"));
        reg._formulas = JsonSerializer.Deserialize<FormulaDef>(formulasJson, JsonOpts) ?? new();

        var mapJson = File.ReadAllText(Path.Combine(dataPath, "map_default.json"));
        reg._map = JsonSerializer.Deserialize<MapDef>(mapJson, JsonOpts) ?? new();

        Console.WriteLine($"[DictRegistry] 已加载: {reg._resources.Count} 资源, {reg._buildings.Count} 建筑, " +
                          $"{reg._techs.Count} 科技, {reg._units.Count} 兵种, {reg._transports.Count} 载具, " +
                          $"{reg._factions.Count} 势力, {reg._map.Nodes.Count} 节点, {reg._map.Edges.Count} 边");

        return reg;
    }

    private static Dictionary<string, T> LoadDict<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, T>>(json, JsonOpts)
               ?? new Dictionary<string, T>();
    }

    // ─── 类型安全的查询方法 ───

    public ResourceDef GetResource(string id) => _resources[id];
    public BuildingDef GetBuilding(string id) => _buildings[id];
    public TechDef GetTech(string id) => _techs[id];
    public UnitDef GetUnit(string id) => _units[id];
    public TransportDef GetTransport(string id) => _transports[id];
    public FactionDef GetFaction(string id) => _factions[id];
    public FormulaDef Formulas => _formulas;
    public MapDef Map => _map;

    public IReadOnlyDictionary<string, ResourceDef> AllResources => _resources;
    public IReadOnlyDictionary<string, BuildingDef> AllBuildings => _buildings;
    public IReadOnlyDictionary<string, TechDef> AllTechs => _techs;
    public IReadOnlyDictionary<string, UnitDef> AllUnits => _units;
    public IReadOnlyDictionary<string, TransportDef> AllTransports => _transports;
    public IReadOnlyDictionary<string, FactionDef> AllFactions => _factions;

    public bool HasResource(string id) => _resources.ContainsKey(id);
    public bool HasBuilding(string id) => _buildings.ContainsKey(id);
    public bool HasTech(string id) => _techs.ContainsKey(id);
    public bool HasTransport(string id) => _transports.ContainsKey(id);
    public bool HasUnit(string id) => _units.ContainsKey(id);
}
