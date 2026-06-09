using System;

/// <summary>
/// WebSocket 通信协议消息模型
/// </summary>
namespace Networking
{
    /// <summary>服务端推送消息</summary>
    [Serializable]
    public class ServerMessage
    {
        public string type = "";
        public int tick;
        public int ack;
        public object? data;
    }

    /// <summary>客户端发送指令</summary>
    [Serializable]
    public class ClientCommand
    {
        public string type = "COMMAND";
        public string action = "";
        public object payload = new();
        public int seq;
    }
}
