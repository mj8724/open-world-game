using Networking;
using UnityEngine;

/// <summary>
/// GameApp — 游戏入口点（MonoBehaviour 单例）
/// 初始化所有子系统：WebSocket、StateStore、CommandSender、Rendering
/// 移植自 client/src/main.js (前端初始化流程)
/// </summary>
public class GameApp : MonoBehaviour
{
    [Header("网络配置")]
    [SerializeField] private string _serverUrl = "ws://localhost:5000/ws";

    [Header("场景引用")]
    [SerializeField] private Camera _mainCamera;

    // ─── 单例 ───
    public static GameApp Instance { get; private set; }

    // ─── 核心系统 ───
    public WebSocketClient WebSocket { get; private set; }
    public StateStore State { get; private set; }
    public CommandSender Commands { get; private set; }
    public MessageDispatcher Dispatcher { get; private set; }

    private MainThreadDispatcher _dispatcher;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建主线程调度器
        _dispatcher = gameObject.AddComponent<MainThreadDispatcher>();

        // 创建 StateStore
        State = new StateStore();

        // 创建 WebSocket 客户端
        WebSocket = new WebSocketClient(_dispatcher);
        WebSocket.SetUrl(_serverUrl);

        // 创建指令发送器
        Commands = new CommandSender(WebSocket);

        // 创建消息分发器
        Dispatcher = new MessageDispatcher(State);

        // 订阅事件
        WebSocket.OnConnectionChanged += OnConnectionChanged;
        WebSocket.OnMessageReceived += OnMessageReceived;

        Debug.Log("[GameApp] 初始化完成");
    }

    void Start()
    {
        // 尝试连接服务器
        WebSocket.Connect();
    }

    void OnDestroy()
    {
        WebSocket.OnConnectionChanged -= OnConnectionChanged;
        WebSocket.OnMessageReceived -= OnMessageReceived;
        WebSocket.Dispose();
    }

    private void OnConnectionChanged(bool connected)
    {
        Debug.Log($"[GameApp] 连接状态: {(connected ? "已连接" : "已断开")}");
    }

    private void OnMessageReceived(string rawJson)
    {
        Dispatcher.HandleMessage(rawJson);
    }

    /// <summary>重新连接服务器</summary>
    public void Reconnect()
    {
        WebSocket.Disconnect();
        WebSocket.Connect();
    }
}
