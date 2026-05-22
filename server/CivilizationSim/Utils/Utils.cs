namespace CivilizationSim.Utils;

/// <summary>游戏日志收集器 — 收集每 Tick 的战报和事件日志</summary>
public class GameLogger
{
    private readonly List<string> _logs = new();
    private readonly object _lock = new();

    /// <summary>写入一条日志</summary>
    public void Log(string message)
    {
        lock (_lock)
        {
            _logs.Add(message);
        }
    }

    /// <summary>刷出所有日志并清空</summary>
    public List<string> FlushLogs()
    {
        lock (_lock)
        {
            var result = new List<string>(_logs);
            _logs.Clear();
            return result;
        }
    }
}

/// <summary>可种子化的确定性随机数生成器</summary>
public class SeededRandom
{
    private Random _random;

    public SeededRandom(int seed = 42)
    {
        _random = new Random(seed);
    }

    public int Next(int min, int max) => _random.Next(min, max);
    public double NextDouble() => _random.NextDouble();
    public float NextFloat() => (float)_random.NextDouble();
}

/// <summary>图寻路 — Dijkstra 最短路径</summary>
public class Pathfinding
{
    /// <summary>
    /// 在图上求最短路径
    /// </summary>
    /// <param name="edges">边列表（source, target, weight）</param>
    /// <param name="from">起点ID</param>
    /// <param name="to">终点ID</param>
    /// <returns>路径节点ID列表，空列表表示不可达</returns>
    public static List<string> FindShortestPath(
        IEnumerable<(string source, string target, float weight)> edges,
        string from, string to)
    {
        var graph = new Dictionary<string, List<(string target, float weight)>>();

        foreach (var (source, target, weight) in edges)
        {
            if (!graph.ContainsKey(source)) graph[source] = new();
            if (!graph.ContainsKey(target)) graph[target] = new();
            graph[source].Add((target, weight));
            graph[target].Add((source, weight));  // 双向
        }

        if (!graph.ContainsKey(from) || !graph.ContainsKey(to))
            return new List<string>();

        var dist = new Dictionary<string, float>();
        var prev = new Dictionary<string, string?>();
        var pq = new PriorityQueue<string, float>();

        foreach (var node in graph.Keys)
        {
            dist[node] = float.MaxValue;
            prev[node] = null;
        }

        dist[from] = 0;
        pq.Enqueue(from, 0);

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();
            if (current == to) break;

            if (!graph.ContainsKey(current)) continue;

            foreach (var (neighbor, weight) in graph[current])
            {
                float newDist = dist[current] + weight;
                if (newDist < dist[neighbor])
                {
                    dist[neighbor] = newDist;
                    prev[neighbor] = current;
                    pq.Enqueue(neighbor, newDist);
                }
            }
        }

        // 回溯路径
        var path = new List<string>();
        var step = to;
        while (step != null)
        {
            path.Add(step);
            step = prev.GetValueOrDefault(step);
        }
        path.Reverse();

        return path.Count > 0 && path[0] == from ? path : new List<string>();
    }
}
