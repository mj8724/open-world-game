using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// MessageDispatcher — 消息分发器
/// 解析 WebSocket 消息，路由到 StateStore
/// 移植自 server/Net/WebSocketHandler.cs (协议解析层)
/// </summary>
namespace Networking
{
    public class MessageDispatcher
    {
        private readonly StateStore _stateStore;

        public MessageDispatcher(StateStore stateStore)
        {
            _stateStore = stateStore;
        }

        /// <summary>
        /// 处理原始 JSON 消息
        /// </summary>
        public void HandleMessage(string rawJson)
        {
            try
            {
                var msg = JObject.Parse(rawJson);
                var type = msg["type"]?.ToString();

                switch (type)
                {
                    case "FULL_STATE":
                        HandleFullState(msg["data"]);
                        break;

                    case "TICK_UPDATE":
                        HandleTickUpdate(msg["data"]);
                        break;

                    default:
                        Debug.LogWarning($"[Dispatcher] 未知消息类型: {type}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[Dispatcher] JSON 解析失败: {ex.Message}");
            }
        }

        private void HandleFullState(JToken? data)
        {
            if (data == null) return;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var fullState = data.ToObject<GameState.FullStateSnapshot>(
                JsonSerializer.Create(settings));

            if (fullState != null)
            {
                Debug.Log($"[Dispatcher] FULL_STATE: {fullState.Nodes.Count} 节点, {fullState.Armies.Count} 军队");
                _stateStore.ApplyFullState(fullState);
            }
        }

        private void HandleTickUpdate(JToken? data)
        {
            if (data == null) return;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var delta = data.ToObject<GameState.TickDelta>(
                JsonSerializer.Create(settings));

            if (delta != null)
            {
                _stateStore.ApplyDelta(delta);
            }
        }
    }
}
