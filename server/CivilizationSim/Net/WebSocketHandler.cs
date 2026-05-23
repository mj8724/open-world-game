using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CivilizationSim.Ecs;
using CivilizationSim.Ecs.Components;
using CivilizationSim.Systems;

namespace CivilizationSim.Net;

/// <summary>WebSocket 协议消息类型</summary>
public class ClientMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }

    [JsonPropertyName("seq")]
    public int Seq { get; set; }
}

public class ServerMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("tick")]
    public int Tick { get; set; }

    [JsonPropertyName("ack")]
    public int Ack { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>WebSocket 连接处理器</summary>
public class WebSocketHandler
{
    private readonly List<WebSocket> _clients = new();
    private readonly object _lock = new();
    private readonly TickEngine _tickEngine;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public WebSocketHandler(TickEngine tickEngine)
    {
        _tickEngine = tickEngine;
    }

    /// <summary>处理新的 WebSocket 连接</summary>
    public async Task HandleConnection(WebSocket ws)
    {
        lock (_lock) { _clients.Add(ws); }
        Console.WriteLine($"[WS] 客户端已连接，当前 {_clients.Count} 个连接");

        try
        {
            // 发送完整状态快照
            var fullState = _tickEngine.GetFullState();
            var msg = new ServerMessage
            {
                Type = "FULL_STATE",
                Tick = fullState.Tick,
                Data = fullState
            };
            await SendMessage(ws, msg);

            // 接收消息循环
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessClientMessage(json);
                }
            }
        }
        catch (WebSocketException)
        {
            // 连接断开
        }
        finally
        {
            lock (_lock) { _clients.Remove(ws); }
            Console.WriteLine($"[WS] 客户端已断开，剩余 {_clients.Count} 个连接");

            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
    }

    /// <summary>处理客户端消息</summary>
    private void ProcessClientMessage(string json)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<ClientMessage>(json, JsonOpts);
            if (msg == null) return;

            if (msg.Type == "COMMAND" && msg.Action != null)
            {
                var cmd = new GameCommand { Seq = msg.Seq };

                if (Enum.TryParse<CommandAction>(msg.Action, true, out var action))
                {
                    cmd.Action = action;

                    if (msg.Payload.HasValue)
                    {
                        var p = msg.Payload.Value;
                        cmd.NodeId = GetString(p, "nodeId");
                        cmd.BuildingType = GetString(p, "buildingType");
                        cmd.TechId = GetString(p, "techId");
                        cmd.FromNodeId = GetString(p, "fromNodeId");
                        cmd.TargetNodeId = GetString(p, "targetNodeId");
                        cmd.CargoType = GetString(p, "cargoType");
                        cmd.TransportType = GetString(p, "transportType");
                        cmd.RouteMode = GetString(p, "routeMode");

                        if (p.TryGetProperty("targetLevel", out var tl)) cmd.TargetLevel = tl.GetInt32();
                        if (p.TryGetProperty("troopCount", out var tc)) cmd.TroopCount = tc.GetInt32();
                        if (p.TryGetProperty("routeId", out var rid)) cmd.TroopCount = rid.GetInt32();
                        if (p.TryGetProperty("transportCount", out var tcnt)) cmd.TransportCount = tcnt.GetInt32();
                        if (p.TryGetProperty("priority", out var pr)) cmd.Priority = pr.GetInt32();
                        if (p.TryGetProperty("quantity", out var qty)) cmd.Quantity = qty.GetInt32();
                        if (p.TryGetProperty("targetQuantity", out var tq) && tq.ValueKind != JsonValueKind.Null) cmd.TargetQuantity = tq.GetInt32();
                        if (p.TryGetProperty("unlimited", out var un)) cmd.Unlimited = un.GetBoolean();
                        if (p.TryGetProperty("speed", out var sp)) cmd.Speed = sp.GetInt32();
                        if (p.TryGetProperty("policies", out var policies)) cmd.RallyPolicies = ParseRallyPolicies(policies);
                    }

                    // 速度指令直接处理
                    if (cmd.Action == CommandAction.SET_SPEED)
                    {
                        _tickEngine.Speed = cmd.Speed;
                        Console.WriteLine($"[CMD] 游戏速度设置为 {cmd.Speed}x");
                    }
                    else
                    {
                        _tickEngine.CommandProcessor.Enqueue(cmd);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] 消息解析错误: {ex.Message}");
        }
    }

    /// <summary>向所有客户端广播 Tick 更新</summary>
    public async Task BroadcastTickUpdate(TickDelta delta)
    {
        var msg = new ServerMessage
        {
            Type = "TICK_UPDATE",
            Tick = delta.Tick,
            Data = delta
        };

        List<WebSocket> snapshot;
        lock (_lock) { snapshot = new List<WebSocket>(_clients); }

        var tasks = snapshot
            .Where(ws => ws.State == WebSocketState.Open)
            .Select(ws => SendMessage(ws, msg));

        await Task.WhenAll(tasks);
    }

    private async Task SendMessage(WebSocket ws, ServerMessage msg)
    {
        try
        {
            var json = JsonSerializer.Serialize(msg, JsonOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception)
        {
            // 发送失败，连接可能已断开
        }
    }

    private static string? GetString(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var v) ? v.GetString() : null;
    }

    private static Dictionary<string, RallyCargoPolicy> ParseRallyPolicies(JsonElement policies)
    {
        var result = new Dictionary<string, RallyCargoPolicy>();
        if (policies.ValueKind != JsonValueKind.Object) return result;

        foreach (var prop in policies.EnumerateObject())
        {
            var value = prop.Value;
            if (value.ValueKind != JsonValueKind.Object) continue;

            var policy = new RallyCargoPolicy { CargoType = prop.Name };
            if (value.TryGetProperty("enabled", out var enabled)) policy.Enabled = enabled.GetBoolean();
            if (value.TryGetProperty("unlimited", out var unlimited)) policy.Unlimited = unlimited.GetBoolean();
            if (value.TryGetProperty("targetQuantity", out var target) && target.ValueKind != JsonValueKind.Null)
                policy.TargetQuantity = target.GetInt32();
            if (value.TryGetProperty("priority", out var priority)) policy.Priority = priority.GetInt32();
            result[prop.Name] = policy;
        }

        return result;
    }

    public int ClientCount
    {
        get { lock (_lock) { return _clients.Count; } }
    }
}
