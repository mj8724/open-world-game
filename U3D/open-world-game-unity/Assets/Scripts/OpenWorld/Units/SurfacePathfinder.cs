using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class SurfacePathfinder
    {
        private readonly OpenWorldState _world;
        private readonly SurfaceTerrainSystem _terrain;
        private readonly float _maxStep;
        private readonly bool _railOnly;
        private readonly Dictionary<long, List<Vector2Int>> _pathCache = new();
        private int _cacheHits;
        private int _cacheMisses;
        private const int MaxCacheSize = 256;
        private readonly List<Vector2Int> _neighbors = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };
        // reusable per-find-path allocations
        private HashSet<Vector2Int> _openSet;

        public bool CacheEnabled { get; set; } = true;
        public int CacheHits => _cacheHits;
        public int CacheMisses => _cacheMisses;

        public SurfacePathfinder(OpenWorldState world, SurfaceTerrainSystem terrain, float maxStep, bool railOnly = false)
        {
            _world = world;
            _terrain = terrain;
            _maxStep = maxStep;
            _railOnly = railOnly;
            _openSet = new HashSet<Vector2Int>();
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int maxIterations = 12000)
        {
            _openSet.Clear();

            var openList = new List<Vector2Int>(64) { start };
            _openSet.Add(start);

            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new SortedDictionary<float, List<Vector2Int>>();
            float h = Heuristic(start, goal);
            AddToFScore(fScore, start, h);

            var closed = new HashSet<Vector2Int>();

            int iterations = 0;
            while (openList.Count > 0 && iterations++ < maxIterations)
            {
                var current = PopBestFScore(openList, fScore);
                if (current == new Vector2Int(-1, -1)) break; // shouldn't happen

                if (current == goal)
                    return Reconstruct(cameFrom, current);

                openList.Remove(current);
                _openSet.Remove(current);
                closed.Add(current);

                foreach (var offset in _neighbors)
                {
                    var next = current + offset;
                    if (closed.Contains(next) || !_terrain.IsReachableStep(current, next, _maxStep)) continue;

                    var nextCell = _world.GetCell(next);
                    if (_railOnly && !nextCell.HasRail) continue;
                    float diagonal = Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 2 ? 1.414f : 1f;
                    float slopePenalty = Mathf.Abs(_world.GetHeight(next) - _world.GetHeight(current)) * 0.7f;
                    float networkCost = _railOnly ? 0.45f : nextCell.HasRoad ? 0.65f : nextCell.MoveCost;
                    float tentative = gScore[current] + networkCost * diagonal + slopePenalty;

                    if (!gScore.TryGetValue(next, out float old) || tentative < old)
                    {
                        cameFrom[next] = current;
                        gScore[next] = tentative;
                        float newF = tentative + Heuristic(next, goal);
                        fScore.TryGetValue(old, out var oldFList);
                        AddToFScore(fScore, next, newF);
                        if (oldFList != null)
                            oldFList.Remove(next);

                        if (!_openSet.Contains(next))
                        {
                            openList.Add(next);
                            _openSet.Add(next);
                        }
                    }
                }
            }

            return new List<Vector2Int>();
        }

        private static void AddToFScore(SortedDictionary<float, List<Vector2Int>> fScore, Vector2Int node, float value)
        {
            if (!fScore.TryGetValue(value, out var list))
                fScore[value] = list = new List<Vector2Int>();
            list.Add(node);
        }

        private static Vector2Int PopBestFScore(List<Vector2Int> openList, SortedDictionary<float, List<Vector2Int>> fScore)
        {
            foreach (var kvp in fScore)
            {
                if (kvp.Value.Count > 0)
                {
                    var best = kvp.Value[0];
                    kvp.Value.RemoveAt(0);
                    if (kvp.Value.Count == 0)
                        fScore.Remove(kvp.Key);
                    return best;
                }
            }
            return openList.Count > 0 ? openList[0] : new Vector2Int(-1, -1);
        }

        private static float Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.TryGetValue(current, out var previous))
            {
                current = previous;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
