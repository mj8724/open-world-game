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
        private readonly List<Vector2Int> _neighbors = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        public SurfacePathfinder(OpenWorldState world, SurfaceTerrainSystem terrain, float maxStep, bool railOnly = false)
        {
            _world = world;
            _terrain = terrain;
            _maxStep = maxStep;
            _railOnly = railOnly;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int maxIterations = 12000)
        {
            var open = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };
            var closed = new HashSet<Vector2Int>();

            int iterations = 0;
            while (open.Count > 0 && iterations++ < maxIterations)
            {
                var current = Best(open, fScore);
                if (current == goal)
                    return Reconstruct(cameFrom, current);

                open.Remove(current);
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
                        fScore[next] = tentative + Heuristic(next, goal);
                        if (!open.Contains(next)) open.Add(next);
                    }
                }
            }

            return new List<Vector2Int>();
        }

        private static Vector2Int Best(List<Vector2Int> open, Dictionary<Vector2Int, float> score)
        {
            var best = open[0];
            float bestScore = score.TryGetValue(best, out var s) ? s : float.MaxValue;
            for (int i = 1; i < open.Count; i++)
            {
                float nextScore = score.TryGetValue(open[i], out var ns) ? ns : float.MaxValue;
                if (nextScore < bestScore)
                {
                    best = open[i];
                    bestScore = nextScore;
                }
            }
            return best;
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
