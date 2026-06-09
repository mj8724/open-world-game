using System;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// 主线程调度器 — 将回调从后台线程派发到 Unity 主线程
/// </summary>
namespace Networking
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private readonly ConcurrentQueue<Action> _queue = new();

        public void Post(Action action)
        {
            _queue.Enqueue(action);
        }

        private void Update()
        {
            while (_queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogError($"[Dispatcher] {ex.Message}"); }
            }
        }
    }
}
