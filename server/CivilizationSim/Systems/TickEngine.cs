using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>
/// Tick 引擎 — 游戏主循环调度器。
/// 每 Tick 按固定顺序执行所有 System，收集 Delta 状态。
/// </summary>
public class TickEngine
{
    private readonly List<IGameSystem> _systems;
    private readonly World _world;
    private readonly DictRegistry _dict;
    private readonly GameLogger _logger;
    private int _currentTick;

    /// <summary>当前 Tick</summary>
    public int CurrentTick => _currentTick;

    /// <summary>游戏速度（0=暂停, 1=正常, 2=二倍, 5=五倍）</summary>
    public int Speed { get; set; } = 1;

    /// <summary>获取 CommandProcessor 以便入队指令</summary>
    public CommandProcessor CommandProcessor { get; }

    public TickEngine(World world, DictRegistry dict, GameLogger logger)
    {
        _world = world;
        _dict = dict;
        _logger = logger;

        CommandProcessor = new CommandProcessor();

        // 注册所有 System，按 Order 排序
        _systems = new List<IGameSystem>
        {
            CommandProcessor,        // Order 10
            new BuildSystem(),       // Order 20
            new ResourceSystem(),    // Order 30
            new PopulationSystem(),  // Order 40
            new TechSystem(),        // Order 50
            new LogisticsSystem(),   // Order 60
            new CombatSystem(),      // Order 70
            new MoraleSystem(),      // Order 80
            new AISystem(),          // Order 90
            new EventSystem()        // Order 100
        };

        _systems.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    /// <summary>执行单个 Tick，返回 Delta 状态</summary>
    public TickDelta ExecuteTick()
    {
        _currentTick++;
        _world.BeginDeltaTracking();

        // 按顺序执行所有 System
        foreach (var system in _systems)
        {
            system.Execute(_world, _dict, _logger);
        }

        var delta = _world.CollectDelta(_currentTick);

        // 将 logger 的日志作为事件附加
        foreach (var log in _logger.FlushLogs())
        {
            delta.Events.Add(new GameEvent
            {
                Type = "LOG",
                TextKey = log,
                Params = new()
            });
        }

        return delta;
    }

    /// <summary>获取完整的世界状态快照</summary>
    public FullStateSnapshot GetFullState()
    {
        return new FullStateSnapshot
        {
            Tick = _currentTick,
            Nodes = new Dictionary<string, Ecs.Components.NodeComponent>(_world.Nodes),
            Edges = new Dictionary<string, Ecs.Components.EdgeComponent>(_world.Edges),
            Armies = new Dictionary<int, Ecs.Components.ArmyComponent>(_world.Armies),
            LogisticsEntities = new Dictionary<int, Ecs.Components.LogisticsComponent>(_world.Logistics),
            Factions = new Dictionary<string, Ecs.Components.FactionComponent>(_world.Factions),
            BuildQueue = new List<Ecs.Components.BuildQueueItem>(_world.BuildQueue)
        };
    }
}

/// <summary>完整世界状态快照</summary>
public class FullStateSnapshot
{
    public int Tick { get; set; }
    public Dictionary<string, Ecs.Components.NodeComponent> Nodes { get; set; } = new();
    public Dictionary<string, Ecs.Components.EdgeComponent> Edges { get; set; } = new();
    public Dictionary<int, Ecs.Components.ArmyComponent> Armies { get; set; } = new();
    public Dictionary<int, Ecs.Components.LogisticsComponent> LogisticsEntities { get; set; } = new();
    public Dictionary<string, Ecs.Components.FactionComponent> Factions { get; set; } = new();
    public List<Ecs.Components.BuildQueueItem> BuildQueue { get; set; } = new();
}
