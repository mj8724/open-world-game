using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Net;
using CivilizationSim.Systems;
using CivilizationSim.Utils;

// ═══ 加载数据字典 ═══
var dataPath = Path.Combine(AppContext.BaseDirectory, "Dict", "Data");
Console.WriteLine($"[启动] 加载数据字典: {dataPath}");
var dict = DictRegistry.LoadFromDirectory(dataPath);

// ═══ 初始化游戏世界 ═══
var world = new World();
world.InitializeFromMap(dict.Map);
Console.WriteLine($"[启动] 世界初始化完成: {world.Nodes.Count} 节点, {world.Edges.Count} 边, {world.Factions.Count} 势力");

// ═══ 创建 Tick 引擎 ═══
var logger = new GameLogger();
var tickEngine = new TickEngine(world, dict, logger);
var wsHandler = new WebSocketHandler(tickEngine);

// ═══ 配置 Web 应用 ═══
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddCors();

var app = builder.Build();

// CORS 允许前端开发服务器
app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.UseWebSockets();

// ═══ WebSocket 端点 ═══
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await wsHandler.HandleConnection(ws);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// ═══ 健康检查端点 ═══
app.MapGet("/", () => new
{
    name = "CivilizationSim Server",
    version = "0.1.0",
    tick = tickEngine.CurrentTick,
    speed = tickEngine.Speed,
    clients = wsHandler.ClientCount,
    nodes = world.Nodes.Count,
    edges = world.Edges.Count
});

// ═══ 启动 Tick 循环 ═══
var tickTimer = new System.Timers.Timer(1000); // 默认 1 秒/Tick
tickTimer.Elapsed += async (s, e) =>
{
    if (tickEngine.Speed <= 0) return;

    // 按速度执行多次 Tick
    for (int i = 0; i < tickEngine.Speed; i++)
    {
        var delta = tickEngine.ExecuteTick();
        await wsHandler.BroadcastTickUpdate(delta);
    }
};
tickTimer.Start();

Console.WriteLine("═══════════════════════════════════════════");
Console.WriteLine("  《文明模拟器》 Demo Server v0.1.0");
Console.WriteLine("  http://localhost:5000");
Console.WriteLine("  WebSocket: ws://localhost:5000/ws");
Console.WriteLine("═══════════════════════════════════════════");

app.Run();
