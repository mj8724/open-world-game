using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class VehicleSystem : MonoBehaviour
    {
        public IReadOnlyList<VehicleAgent> SelectedVehicles => _selected;
        public SurfacePathfinder Pathfinder { get; private set; }
        public SurfacePathfinder RailPathfinder { get; private set; }
        public string LastProductionStatus { get; private set; } = "No vehicle production";

        private OpenWorldState _world;
        private readonly Dictionary<int, VehicleAgent> _agents = new();
        private readonly List<VehicleAgent> _selected = new();

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain)
        {
            _world = world;
            Pathfinder = new SurfacePathfinder(world, terrain, 1.1f);
            RailPathfinder = new SurfacePathfinder(world, terrain, 0.65f, true);
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

        public VehicleAgent SpawnScenarioVehicle(VehicleKind kind, Vector2Int cell, int factionId)
        {
            var entity = _world.AddVehicle(kind, cell, factionId);
            LastProductionStatus = $"Scenario vehicle ready: #{entity.Id} {kind}";
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
            agent.Initialize(_world, Pathfinder, RailPathfinder, entity);
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
                var source = _world.FindNearestStorage(agent.Entity.Cell, agent.Entity.FactionId, 8);
                if (source == null)
                {
                    agent.Entity.StatusText = "No nearby storage";
                    continue;
                }
                int amount = Mathf.Min(space, source.Storage.Get(cargo));
                if (amount <= 0)
                {
                    agent.Entity.StatusText = $"No {cargo} at #{source.Id}";
                    continue;
                }
                if (!source.Storage.Spend(cargo, amount)) continue;
                agent.Entity.CargoKind = cargo;
                agent.Entity.CargoAmount += amount;
                agent.Entity.Task = VehicleTask.Loading;
                agent.Entity.StatusText = $"Loaded {amount} {cargo} from #{source.Id}";
                source.LastStorageStatus = agent.Entity.StatusText;
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
                var target = _world.FindNearestStorage(agent.Entity.Cell, agent.Entity.FactionId, 8);
                if (target == null)
                {
                    agent.Entity.StatusText = "No nearby storage";
                    continue;
                }
                int unloaded = _world.AddToStorage(target, agent.Entity.CargoKind, agent.Entity.CargoAmount);
                unloadedTotal += unloaded;
                agent.Entity.CargoAmount -= unloaded;
                agent.Entity.AssignedRouteId = 0;
                agent.Entity.Task = VehicleTask.Unloading;
                agent.Entity.StatusText = unloaded > 0
                    ? $"Unloaded {unloaded} {agent.Entity.CargoKind} to #{target.Id}"
                    : "Target storage full";
                target.LastStorageStatus = agent.Entity.StatusText;
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

        public VehicleAgent GetAgent(int vehicleId) => _agents.TryGetValue(vehicleId, out var agent) ? agent : null;

        public void QueueServiceForSelected(bool refuel, bool repair)
        {
            foreach (var agent in _selected)
            {
                if (agent?.Entity == null) continue;
                var service = FindServiceBuilding(agent.Entity.Cell, agent.Entity.FactionId);
                var order = new RepairRefuelOrder
                {
                    Id = NextServiceOrderId(),
                    VehicleId = agent.Entity.Id,
                    Refuel = refuel,
                    Repair = repair,
                    ServiceBuildingId = service?.Id ?? 0,
                    Status = service == null ? "No garage or station" : "Travelling to service"
                };
                _world.ServiceOrders.Add(order);
                if (service == null)
                {
                    agent.Entity.StatusText = order.Status;
                    continue;
                }
                agent.Entity.Task = repair ? VehicleTask.Repair : VehicleTask.Refuel;
                agent.Entity.StatusText = order.Status;
                if (!agent.MoveTo(service.Origin))
                {
                    order.Status = "No path to service";
                    agent.Entity.StatusText = order.Status;
                }
            }
        }

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
            var requiredFactory = kind is VehicleKind.Locomotive or VehicleKind.CargoWagon
                ? BuildableKind.TrainFactory
                : BuildableKind.VehicleFactory;
            if (!HasOperationalBuilding(requiredFactory, factionId))
            {
                reason = $"needs {requiredFactory}";
                return false;
            }
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

        private bool HasOperationalBuilding(BuildableKind kind, int factionId)
        {
            foreach (var building in _world.Buildings.Values)
                if (building.Kind == kind && building.FactionId == factionId && !building.UnderConstruction && building.Hp > 0)
                    return true;
            return false;
        }

        private BuildingEntity FindServiceBuilding(Vector2Int cell, int factionId)
        {
            BuildingEntity best = null;
            int bestDistance = int.MaxValue;
            foreach (var building in _world.Buildings.Values)
            {
                if (building.FactionId != factionId || building.UnderConstruction) continue;
                if (building.Kind is not (BuildableKind.Garage or BuildableKind.Station)) continue;
                int distance = Mathf.Abs(building.Origin.x - cell.x) + Mathf.Abs(building.Origin.y - cell.y);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = building;
            }
            return best;
        }

        private int NextServiceOrderId()
        {
            int next = 1;
            foreach (var order in _world.ServiceOrders)
                next = Mathf.Max(next, order.Id + 1);
            return next;
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
