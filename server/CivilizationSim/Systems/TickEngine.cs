using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Ecs.Components;
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
            CommandProcessor,              // Order 10
            new BuildSystem(),             // Order 20
            new TransportProductionSystem(), // Order 25
            new ResourceSystem(),          // Order 30
            new PopulationSystem(),        // Order 40
            new TechSystem(),              // Order 50
            new LogisticsPlanningSystem(), // Order 55
            new TransportMaintenanceSystem(), // Order 58
            new LogisticsSystem(),         // Order 60
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
            Nodes = new Dictionary<string, NodeComponent>(_world.Nodes),
            Edges = new Dictionary<string, EdgeComponent>(_world.Edges),
            Armies = new Dictionary<int, ArmyComponent>(_world.Armies),
            LogisticsEntities = new Dictionary<int, LogisticsComponent>(_world.Logistics),
            RallyPoints = new Dictionary<string, RallyPointComponent>(_world.RallyPoints),
            TransportStocks = new Dictionary<string, TransportStockComponent>(_world.TransportStocks),
            Factions = new Dictionary<string, FactionComponent>(_world.Factions),
            BuildQueue = new List<BuildQueueItem>(_world.BuildQueue),
            TransportProductionQueue = new List<TransportProductionQueueItem>(_world.TransportProductionQueue)
        };
    }
}

/// <summary>完整世界状态快照</summary>
public class FullStateSnapshot
{
    public int Tick { get; set; }
    public Dictionary<string, NodeComponent> Nodes { get; set; } = new();
    public Dictionary<string, EdgeComponent> Edges { get; set; } = new();
    public Dictionary<int, ArmyComponent> Armies { get; set; } = new();
    public Dictionary<int, LogisticsComponent> LogisticsEntities { get; set; } = new();
    public Dictionary<string, RallyPointComponent> RallyPoints { get; set; } = new();
    public Dictionary<string, TransportStockComponent> TransportStocks { get; set; } = new();
    public Dictionary<string, FactionComponent> Factions { get; set; } = new();
    public List<BuildQueueItem> BuildQueue { get; set; } = new();
    public List<TransportProductionQueueItem> TransportProductionQueue { get; set; } = new();
}
