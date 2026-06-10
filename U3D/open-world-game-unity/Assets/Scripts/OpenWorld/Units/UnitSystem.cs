using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class UnitSystem : MonoBehaviour
    {
        public IReadOnlyList<UnitAgent> SelectedUnits => _selected;
        public SurfacePathfinder Pathfinder { get; private set; }

        private OpenWorldState _world;
        private readonly Dictionary<int, UnitAgent> _agents = new();
        private readonly List<UnitAgent> _selected = new();

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain)
        {
            _world = world;
            Pathfinder = new SurfacePathfinder(world, terrain, 1.6f);
        }

        public UnitAgent Spawn(UnitKind kind, Vector2Int cell, int factionId)
        {
            var entity = _world.AddUnit(kind, cell, factionId);
            return CreateAgent(entity);
        }

        public void RebuildFromWorld()
        {
            ClearSelection();
            ClearAgents();
            foreach (var entity in _world.Units.Values)
                CreateAgent(entity);
        }

        private UnitAgent CreateAgent(UnitEntity entity)
        {
            var go = new GameObject($"Unit_{entity.Id}_{entity.Kind}");
            go.transform.SetParent(transform, false);
            var agent = go.AddComponent<UnitAgent>();
            agent.Initialize(_world, Pathfinder, entity);
            _agents[entity.Id] = agent;
            return agent;
        }

        public void MoveSelected(Vector2Int target)
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                var offset = FormationOffset(i);
                _selected[i].MoveTo(target + offset);
            }
        }

        public void AttackSelected(Vector2Int target, int targetEntityId)
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                _selected[i].IssueOrder(new UnitOrder
                {
                    Kind = UnitOrderKind.Attack,
                    TargetCell = target + FormationOffset(i),
                    TargetEntityId = targetEntityId,
                    Priority = 5
                });
            }
        }

        public void PatrolSelected(Vector2Int target)
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                _selected[i].IssueOrder(new UnitOrder
                {
                    Kind = UnitOrderKind.Patrol,
                    TargetCell = target + FormationOffset(i),
                    SecondaryCell = _selected[i].Entity.Cell,
                    Priority = 3
                });
            }
        }

        public void DefendSelected(Vector2Int center, int radius)
        {
            int spread = Mathf.Max(1, radius);
            for (int i = 0; i < _selected.Count; i++)
            {
                var offset = FormationOffset(i) * spread;
                _selected[i].IssueOrder(new UnitOrder
                {
                    Kind = UnitOrderKind.Defend,
                    TargetCell = center + offset,
                    SecondaryCell = center,
                    Priority = 4
                });
            }
        }

        public bool MoveBestScoutTo(Vector2Int target)
        {
            UnitAgent best = null;
            int bestScore = int.MinValue;
            foreach (var agent in _agents.Values)
            {
                if (agent == null || agent.Entity == null) continue;
                if (agent.Entity.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                int score = agent.Entity.Kind switch
                {
                    UnitKind.Scout => 1000,
                    UnitKind.Worker => 400,
                    UnitKind.Medic => 200,
                    _ => 100
                };
                score -= (agent.Entity.Cell - target).sqrMagnitude / 32;
                if (score <= bestScore) continue;
                best = agent;
                bestScore = score;
            }

            if (best == null) return false;
            best.MoveTo(target);
            return true;
        }

        public void SelectSingle(UnitAgent agent)
        {
            ClearSelection();
            if (agent != null)
            {
                _selected.Add(agent);
                agent.SetSelected(true);
            }
        }

        public void SelectInBounds(Bounds bounds)
        {
            ClearSelection();
            foreach (var agent in _agents.Values)
            {
                if (bounds.Contains(agent.transform.position))
                {
                    _selected.Add(agent);
                    agent.SetSelected(true);
                }
            }
        }

        public void ClearSelection()
        {
            foreach (var agent in _selected)
                if (agent != null) agent.SetSelected(false);
            _selected.Clear();
        }

        public UnitAgent GetIdleWorker()
        {
            foreach (var agent in _agents.Values)
            {
                if (agent.Entity.Kind == UnitKind.Worker && agent.Entity.Task == UnitTask.Idle)
                    return agent;
            }
            return null;
        }

        public UnitAgent GetIdleEngineer()
        {
            foreach (var agent in _agents.Values)
            {
                if (agent.Entity.FactionId == OpenWorldConstants.PlayerFactionId &&
                    agent.Entity.Kind == UnitKind.Engineer && agent.Entity.Task == UnitTask.Idle)
                    return agent;
            }
            return null;
        }

        public UnitAgent GetAgent(int id)
        {
            _agents.TryGetValue(id, out var agent);
            return agent;
        }

        public void RemoveUnit(int id)
        {
            if (!_agents.TryGetValue(id, out var agent)) return;
            _selected.Remove(agent);
            _agents.Remove(id);
            _world.Units.Remove(id);
            if (agent != null) Destroy(agent.gameObject);
        }

        public IEnumerable<UnitAgent> AllAgents() => _agents.Values;

        private static Vector2Int FormationOffset(int index)
        {
            int row = index / 4;
            int col = index % 4;
            return new Vector2Int(col - 1, row - 1);
        }

        private void ClearAgents()
        {
            foreach (var agent in _agents.Values)
            {
                if (agent == null) continue;
                if (Application.isPlaying) Destroy(agent.gameObject);
                else DestroyImmediate(agent.gameObject);
            }
            _agents.Clear();
        }
    }
}
