using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// WebSocket 客户端 — 连接 .NET 游戏服务器，自动重连
/// 移植自 client/src/bridge/game-bridge.js
/// </summary>
namespace Networking
{
    public class WebSocketClient : IDisposable
    {
        public event Action<bool>? OnConnectionChanged;
        public event Action<string>? OnMessageReceived; // raw JSON string

        public bool IsConnected => _connected;

        private ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;
        private string _url = "ws://localhost:5000/ws";
        private bool _connected;
        private bool _shouldReconnect = true;
        private int _seq;

        // 重连参数
        private int _reconnectDelay = 1000;
        private const int MaxReconnectDelay = 10000;

        // 主线程同步上下文
        private readonly MainThreadDispatcher _dispatcher;

        public WebSocketClient(MainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>设置服务器地址</summary>
        public void SetUrl(string url)
        {
            _url = url;
        }

        /// <summary>开始连接</summary>
        public async void Connect()
        {
            _shouldReconnect = true;
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _ws = new ClientWebSocket();

                Debug.Log($"[WS] 正在连接 {_url}...");
                await _ws.ConnectAsync(new Uri(_url), _cts.Token);

                _connected = true;
                _reconnectDelay = 1000;
                Debug.Log("[WS] 已连接到服务器");

                _dispatcher.Post(() => OnConnectionChanged?.Invoke(true));

                // 启动接收循环
                await ReceiveLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WS] 连接失败: {ex.Message}");
                _connected = false;
                _dispatcher.Post(() => OnConnectionChanged?.Invoke(false));
                ScheduleReconnect();
            }
        }

        /// <summary>发送游戏指令</summary>
        public void SendCommand(string action, object payload)
        {
            _seq++;
            var msg = new
            {
                type = "COMMAND",
                action,
                payload,
                seq = _seq
            };

            if (_connected && _ws?.State == WebSocketState.Open)
            {
                var json = JsonConvert.SerializeObject(msg, Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var bytes = Encoding.UTF8.GetBytes(json);
                var segment = new ArraySegment<byte>(bytes);

                try
                {
                    _ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WS] 发送失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[WS] 未连接，指令未发送: {action}");
            }
        }

        /// <summary>接收消息循环</summary>
        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[4096];

            try
            {
                while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("[WS] 服务器关闭连接");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // 处理分片消息
                        if (!result.EndOfMessage)
                        {
                            var sb = new StringBuilder(json);
                            while (!result.EndOfMessage)
                            {
                                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                            }
                            json = sb.ToString();
                        }

                        var capturedJson = json;
                        _dispatcher.Post(() => OnMessageReceived?.Invoke(capturedJson));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WS] 接收异常: {ex.Message}");
            }
            finally
            {
                _connected = false;
                _dispatcher.Post(() => OnConnectionChanged?.Invoke(false));
                ScheduleReconnect();
            }
        }

        /// <summary>计划重连（指数退避）</summary>
        private void ScheduleReconnect()
        {
            if (!_shouldReconnect) return;

            var delay = _reconnectDelay;
            _reconnectDelay = Math.Min(_reconnectDelay * 2, MaxReconnectDelay);

            Debug.Log($"[WS] 将在 {delay}ms 后重连...");
            Task.Delay(delay).ContinueWith(_ =>
            {
                if (_shouldReconnect)
                {
                    Debug.Log("[WS] 尝试重连...");
                    _ = ConnectAsync();
                }
            });
        }

        /// <summary>断开连接</summary>
        public void Disconnect()
        {
            _shouldReconnect = false;
            _cts?.Cancel();
            _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None)
                .ConfigureAwait(false);
            _ws?.Dispose();
            _ws = null;
            _connected = false;
        }

        public void Dispose()
        {
            Disconnect();
            _cts?.Dispose();
        }
    }
}
