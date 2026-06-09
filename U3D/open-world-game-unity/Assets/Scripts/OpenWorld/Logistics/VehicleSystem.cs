using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class VehicleSystem : MonoBehaviour
    {
        public IReadOnlyList<VehicleAgent> SelectedVehicles => _selected;
        public SurfacePathfinder Pathfinder { get; private set; }
        public string LastProductionStatus { get; private set; } = "No vehicle production";

        private OpenWorldState _world;
        private readonly Dictionary<int, VehicleAgent> _agents = new();
        private readonly List<VehicleAgent> _selected = new();

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain)
        {
            _world = world;
            Pathfinder = new SurfacePathfinder(world, terrain, 1.1f);
        }

        public VehicleAgent Spawn(VehicleKind kind, Vector2Int cell, int factionId)
        {
            if (!CanAfford(kind, factionId, out var reason))
            {
                LastProductionStatus = $"Vehicle blocked: {kind} {reason}";
                return null;
            }

            var entity = _world.AddVehicle(kind, cell, factionId);
            LastProductionStatus = $"Vehicle produced: #{entity.Id} {kind}";
            return CreateAgent(entity);
        }

        public void RebuildFromWorld()
        {
            ClearSelection();
            ClearAgents();
            foreach (var entity in _world.Vehicles.Values)
                CreateAgent(entity);
        }

        private VehicleAgent CreateAgent(VehicleEntity entity)
        {
            var go = new GameObject($"Vehicle_{entity.Id}_{entity.Kind}");
            go.transform.SetParent(transform, false);
            var agent = go.AddComponent<VehicleAgent>();
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

        public int LoadSelected(ResourceKind cargo)
        {
            int loadedTotal = 0;
            foreach (var agent in _selected)
            {
                if (agent == null || agent.Entity == null) continue;
                int space = Mathf.Max(0, agent.Entity.CargoCapacity - agent.Entity.CargoAmount);
                if (space <= 0)
                {
                    agent.Entity.StatusText = "Cargo full";
                    continue;
                }
                int amount = Mathf.Min(space, _world.Inventory.Get(cargo));
                if (amount <= 0)
                {
                    agent.Entity.StatusText = $"No {cargo}";
                    continue;
                }
                if (!_world.Inventory.Spend(cargo, amount)) continue;
                agent.Entity.CargoKind = cargo;
                agent.Entity.CargoAmount += amount;
                agent.Entity.Task = VehicleTask.Loading;
                agent.Entity.StatusText = $"Loaded {amount} {cargo}";
                loadedTotal += amount;
            }
            return loadedTotal;
        }

        public int UnloadSelected()
        {
            int unloadedTotal = 0;
            foreach (var agent in _selected)
            {
                if (agent == null || agent.Entity == null) continue;
                if (agent.Entity.CargoAmount <= 0)
                {
                    agent.Entity.StatusText = "No cargo";
                    continue;
                }
                _world.Inventory.Add(agent.Entity.CargoKind, agent.Entity.CargoAmount);
                unloadedTotal += agent.Entity.CargoAmount;
                agent.Entity.CargoAmount = 0;
                agent.Entity.AssignedRouteId = 0;
                agent.Entity.Task = VehicleTask.Unloading;
                agent.Entity.StatusText = "Unloaded";
            }
            return unloadedTotal;
        }

        public void SelectInBounds(Bounds bounds, bool append)
        {
            if (!append) ClearSelection();
            foreach (var agent in _agents.Values)
            {
                if (bounds.Contains(agent.transform.position) && !_selected.Contains(agent))
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

        public IEnumerable<VehicleAgent> AllAgents() => _agents.Values;

        public VehicleAgent FirstIdleFor(VehicleKind preferred)
        {
            foreach (var agent in _agents.Values)
            {
                if (agent.Entity.Task == VehicleTask.Idle && agent.Entity.Kind == preferred)
                    return agent;
            }
            foreach (var agent in _agents.Values)
            {
                if (agent.Entity.Task == VehicleTask.Idle)
                    return agent;
            }
            return null;
        }

        private bool CanAfford(VehicleKind kind, int factionId, out string reason)
        {
            reason = "";
            if (factionId != OpenWorldConstants.PlayerFactionId) return true;

            var def = OpenWorldDataCatalog.GetVehicle(kind);
            if (def == null) return true;
            if (!OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, def.RequiredEra))
            {
                reason = $"needs {def.RequiredEra}";
                return false;
            }
            if (!OpenWorldDataCatalog.CanSpend(_world.Inventory, def.Cost, out var missing))
            {
                reason = $"missing {missing}";
                return false;
            }
            OpenWorldDataCatalog.Spend(_world.Inventory, def.Cost);
            return true;
        }

        private static Vector2Int FormationOffset(int index)
        {
            int row = index / 3;
            int col = index % 3;
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
