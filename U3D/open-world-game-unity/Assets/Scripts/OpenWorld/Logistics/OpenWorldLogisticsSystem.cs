using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    public class OpenWorldLogisticsSystem : MonoBehaviour
    {
        public string LastStatus { get; private set; } = "No routes";
        public IReadOnlyList<string> RouteLines => _routeLines;
        public IReadOnlyList<string> VehicleLines => _vehicleLines;

        private readonly List<string> _routeLines = new();
        private readonly List<string> _vehicleLines = new();
        private OpenWorldState _world;
        private VehicleSystem _vehicles;
        private float _tickTimer;

        public void Initialize(OpenWorldState world, VehicleSystem vehicles)
        {
            _world = world;
            _vehicles = vehicles;
        }

        private void Update()
        {
            if (_world == null) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < 1.0f) return;
            _tickTimer = 0f;
            TickRoutes();
        }

        public LogisticsRoute EnsureStarterRoute(Vector2Int source, Vector2Int target)
        {
            if (_world.LogisticsRoutes.Count > 0) return _world.LogisticsRoutes[0];
            return _world.AddRoute(source, target, ResourceKind.Food, VehicleKind.HandCart, 4, LogisticsMode.Automatic);
        }

        public LogisticsRoute EnsureStarterRoute(int sourceBuildingId, int targetBuildingId)
        {
            if (_world.LogisticsRoutes.Count > 0)
            {
                var existing = _world.LogisticsRoutes[0];
                _world.BindRouteBuildings(existing);
                return existing;
            }
            if (!_world.Buildings.TryGetValue(sourceBuildingId, out var source) || !_world.Buildings.TryGetValue(targetBuildingId, out var target))
                return null;
            return _world.AddRoute(source.Id, target.Id, source.Origin, target.Origin, ResourceKind.Food, VehicleKind.HandCart, 4, LogisticsMode.Automatic);
        }

        public void TickNow() => TickRoutes();

        public LogisticsRoute CreateRoute(int sourceBuildingId, int targetBuildingId, ResourceKind cargo, VehicleKind vehicleKind, int priority, LogisticsMode mode)
        {
            if (!_world.Buildings.TryGetValue(sourceBuildingId, out var source) || !_world.Buildings.TryGetValue(targetBuildingId, out var target))
                return null;
            var route = _world.AddRoute(source.Id, target.Id, source.Origin, target.Origin, cargo, vehicleKind, priority, mode);
            route.Status = "Route created";
            RefreshLines();
            return route;
        }

        public void ToggleRouteMode(int routeId)
        {
            var route = FindRoute(routeId);
            if (route == null) return;
            route.Mode = route.Mode == LogisticsMode.Automatic ? LogisticsMode.Manual : LogisticsMode.Automatic;
            route.Status = route.Mode == LogisticsMode.Automatic ? "Automatic route" : "Manual route";
            LastStatus = route.Status;
            RefreshLines();
        }

        public void AdjustRoutePriority(int routeId, int delta)
        {
            var route = FindRoute(routeId);
            if (route == null) return;
            route.Priority = Mathf.Clamp(route.Priority + delta, 1, 9);
            route.Status = $"Priority {route.Priority}";
            LastStatus = route.Status;
            RefreshLines();
        }

        public void AdjustRouteTargetStock(int routeId, int delta)
        {
            var route = FindRoute(routeId);
            if (route == null) return;
            route.TargetStock = Mathf.Clamp(route.TargetStock + delta, 10, 500);
            route.Status = $"Target stock {route.TargetStock}";
            LastStatus = route.Status;
            RefreshLines();
        }

        public void CycleRouteCargo(int routeId)
        {
            var route = FindRoute(routeId);
            if (route == null) return;
            route.CargoKind = NextCargo(route.CargoKind);
            route.Status = $"Cargo set to {route.CargoKind}";
            LastStatus = route.Status;
            RefreshLines();
        }

        private void TickRoutes()
        {
            foreach (var vehicle in _vehicles.AllAgents())
                AdvanceAssignedVehicle(vehicle);

            LastStatus = "Routes idle";
            if (_world.LogisticsRoutes.Count == 0)
                LastStatus = "No routes";

            var routes = new List<LogisticsRoute>(_world.LogisticsRoutes);
            routes.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            foreach (var route in routes)
            {
                _world.BindRouteBuildings(route);
                if (route.Mode != LogisticsMode.Automatic)
                {
                    route.Status = "Manual route";
                    continue;
                }

                if (!TryGetRouteBuildings(route, out var source, out var target))
                {
                    route.Status = "Missing source/target building";
                    LastStatus = route.Status;
                    continue;
                }

                if (IsRailVehicle(route.PreferredVehicle) && (source.Kind != BuildableKind.Station || target.Kind != BuildableKind.Station))
                {
                    route.Status = "Rail route requires two stations";
                    LastStatus = route.Status;
                    continue;
                }

                int targetCurrent = target.Storage.Get(route.CargoKind);
                int targetSpace = Mathf.Max(0, target.StorageCapacity - target.Storage.Total);
                int needed = Mathf.Min(Mathf.Max(0, route.TargetStock - targetCurrent), targetSpace);
                if (needed <= 0)
                {
                    route.Status = targetSpace <= 0 ? "Target storage full" : $"Target stock reached ({targetCurrent}/{route.TargetStock})";
                    LastStatus = route.Status;
                    continue;
                }

                var assigned = FindAssignedVehicle(route.Id);
                if (assigned != null)
                {
                    route.Status = assigned.Entity.StatusText;
                    continue;
                }

                int available = source.Storage.Get(route.CargoKind);
                if (available <= 0)
                {
                    route.Status = $"No source stock at #{source.Id}";
                    source.LastStorageStatus = $"Route needs {route.CargoKind}";
                    LastStatus = route.Status;
                    continue;
                }

                var vehicle = _vehicles.FirstIdleFor(route.PreferredVehicle);
                if (vehicle == null)
                {
                    route.Status = "No idle vehicle";
                    LastStatus = route.Status;
                    continue;
                }
                if (vehicle.Entity.Condition <= 15f || vehicle.Entity.Hp <= 0)
                {
                    route.Status = "Vehicle damaged";
                    LastStatus = route.Status;
                    continue;
                }

                if (NeedsFuel(vehicle.Entity.Kind) && vehicle.Entity.Fuel <= 0.1f)
                {
                    route.Status = "Vehicle lacks fuel";
                    LastStatus = route.Status;
                    continue;
                }

                vehicle.Entity.AssignedRouteId = route.Id;
                route.AssignedVehicleId = vehicle.Entity.Id;
                if (IsRailVehicle(vehicle.Entity.Kind)) EnsureRailSchedule(route, vehicle.Entity, source, target);
                if (Near(vehicle.Entity.Cell, route.Source))
                {
                    TryLoadAndDispatch(vehicle, route, source, target);
                }
                else
                {
                    vehicle.Entity.StatusText = $"To source #{source.Id}";
                    if (!vehicle.MoveTo(route.Source))
                    {
                        vehicle.Entity.AssignedRouteId = 0;
                        route.Status = vehicle.Entity.StatusText == "No fuel" ? "Vehicle lacks fuel" : "No path to source";
                        LastStatus = route.Status;
                        continue;
                    }
                    vehicle.Entity.StatusText = $"To source #{source.Id}";
                }
                route.Status = vehicle.Entity.StatusText;
                LastStatus = route.Status;
            }

            RefreshLines();
        }

        public void CompleteDeliveryIfArrived(VehicleAgent vehicle) => AdvanceAssignedVehicle(vehicle);

        private void AdvanceAssignedVehicle(VehicleAgent vehicle)
        {
            if (vehicle == null || vehicle.Entity == null || vehicle.Entity.AssignedRouteId == 0) return;
            var route = FindRoute(vehicle.Entity.AssignedRouteId);
            if (route == null || !TryGetRouteBuildings(route, out var source, out var target))
            {
                vehicle.Entity.AssignedRouteId = 0;
                vehicle.Entity.StatusText = "Route missing";
                return;
            }

            if (vehicle.Entity.CargoAmount <= 0)
            {
                if (Near(vehicle.Entity.Cell, route.Source))
                    TryLoadAndDispatch(vehicle, route, source, target);
                else if (vehicle.Entity.Task == VehicleTask.Idle || vehicle.Entity.Task == VehicleTask.Disabled)
                {
                    if (vehicle.MoveTo(route.Source))
                        vehicle.Entity.StatusText = $"Resuming to source #{source.Id}";
                    else
                        route.Status = "No path to source";
                }
                return;
            }

            if (!Near(vehicle.Entity.Cell, route.Target))
            {
                if (vehicle.Entity.Task == VehicleTask.Idle || vehicle.Entity.Task == VehicleTask.Disabled)
                {
                    if (vehicle.MoveTo(route.Target))
                        vehicle.Entity.StatusText = $"Resuming delivery to #{target.Id}";
                    else
                        route.Status = "No path to target";
                }
                return;
            }
            int delivered = _world.AddToStorage(target, vehicle.Entity.CargoKind, vehicle.Entity.CargoAmount);
            vehicle.Entity.CargoAmount -= delivered;
            route.Status = delivered > 0 ? $"Delivered {delivered} {route.CargoKind} to #{target.Id}" : "Target storage full";
            target.LastStorageStatus = route.Status;
            if (vehicle.Entity.CargoAmount > 0)
            {
                source.Storage.Add(vehicle.Entity.CargoKind, vehicle.Entity.CargoAmount);
                vehicle.Entity.CargoAmount = 0;
            }
            vehicle.Entity.AssignedRouteId = 0;
            route.AssignedVehicleId = 0;
            vehicle.Entity.Task = VehicleTask.Idle;
            vehicle.Entity.StatusText = route.Status;
        }

        private void TryLoadAndDispatch(VehicleAgent vehicle, LogisticsRoute route, BuildingEntity source, BuildingEntity target)
        {
            int targetNeed = Mathf.Max(0, route.TargetStock - target.Storage.Get(route.CargoKind));
            int targetSpace = Mathf.Max(0, target.StorageCapacity - target.Storage.Total);
            int load = Mathf.Min(vehicle.Entity.CargoCapacity, Mathf.Min(source.Storage.Get(route.CargoKind), Mathf.Min(targetNeed, targetSpace)));
            if (load <= 0)
            {
                route.Status = targetNeed <= 0 ? "Target stock reached" : targetSpace <= 0 ? "Target storage full" : "No source stock";
                vehicle.Entity.AssignedRouteId = 0;
                vehicle.Entity.StatusText = route.Status;
                return;
            }
            if (!source.Storage.Spend(route.CargoKind, load))
            {
                route.Status = "Cannot reserve source cargo";
                vehicle.Entity.AssignedRouteId = 0;
                return;
            }

            source.LastStorageStatus = $"Loaded {load} {route.CargoKind} onto vehicle #{vehicle.Entity.Id}";
            vehicle.Entity.CargoKind = route.CargoKind;
            vehicle.Entity.CargoAmount = load;
            vehicle.Entity.StatusText = $"Delivering {load} {route.CargoKind} #{source.Id}->#{target.Id}";
            if (!vehicle.MoveTo(route.Target))
            {
                source.Storage.Add(route.CargoKind, load);
                vehicle.Entity.CargoAmount = 0;
                vehicle.Entity.AssignedRouteId = 0;
                route.AssignedVehicleId = 0;
                route.Status = vehicle.Entity.StatusText == "No fuel" ? "Vehicle lacks fuel" : "No path to target";
                return;
            }
            vehicle.Entity.StatusText = $"Delivering {load} {route.CargoKind} #{source.Id}->#{target.Id}";
            route.Status = vehicle.Entity.StatusText;
        }

        private static bool NeedsFuel(VehicleKind kind) => kind is VehicleKind.Truck or VehicleKind.ArmoredCar or VehicleKind.Locomotive or VehicleKind.Tank or VehicleKind.Aircraft or VehicleKind.TransportPlane;

        private static bool IsRailVehicle(VehicleKind kind) => kind is VehicleKind.Locomotive or VehicleKind.CargoWagon;

        private void EnsureRailSchedule(LogisticsRoute route, VehicleEntity locomotive, BuildingEntity source, BuildingEntity target)
        {
            RailSchedule schedule = null;
            foreach (var candidate in _world.RailSchedules)
                if (candidate.LocomotiveId == locomotive.Id) { schedule = candidate; break; }

            if (schedule == null)
            {
                int nextId = 1;
                foreach (var candidate in _world.RailSchedules) nextId = Mathf.Max(nextId, candidate.Id + 1);
                schedule = new RailSchedule { Id = nextId, LocomotiveId = locomotive.Id };
                _world.RailSchedules.Add(schedule);
            }

            schedule.StationBuildingIds.Clear();
            schedule.StationBuildingIds.Add(source.Id);
            schedule.StationBuildingIds.Add(target.Id);
            schedule.CargoKind = route.CargoKind;
            schedule.Active = true;
            schedule.CurrentStop = Near(locomotive.Cell, source.Origin) ? 0 : 1;
            schedule.Status = route.Status;
        }

        private void RefreshLines()
        {
            _routeLines.Clear();
            _vehicleLines.Clear();

            if (_world.LogisticsRoutes.Count == 0)
            {
                _routeLines.Add("No logistics routes configured");
            }
            else
            {
                foreach (var route in _world.LogisticsRoutes)
                {
                    if (_routeLines.Count >= 4) break;
                    _routeLines.Add(RouteLine(route));
                }
            }

            foreach (var vehicle in _vehicles.AllAgents())
            {
                if (vehicle == null || vehicle.Entity == null) continue;
                if (_vehicleLines.Count >= 5) break;
                var entity = vehicle.Entity;
                string fuel = NeedsFuel(entity.Kind) ? $"fuel {entity.Fuel:0}" : "fuel n/a";
                string cargo = entity.CargoAmount > 0 ? $"{entity.CargoAmount}/{entity.CargoCapacity} {entity.CargoKind}" : $"0/{entity.CargoCapacity}";
                _vehicleLines.Add($"#{entity.Id} {entity.Kind} {entity.Task}: {cargo}, {fuel}, cond {entity.Condition:0}, {entity.StatusText}");
            }

            if (_vehicleLines.Count == 0)
                _vehicleLines.Add("No vehicles available");
        }

        public string RouteLine(LogisticsRoute route)
        {
            if (route == null) return "Missing route";
            string source = route.SourceBuildingId > 0 && _world.Buildings.TryGetValue(route.SourceBuildingId, out var sourceBuilding)
                ? $"#{sourceBuilding.Id} {sourceBuilding.Kind}({sourceBuilding.Storage.Get(route.CargoKind)})" : "missing source";
            string target = route.TargetBuildingId > 0 && _world.Buildings.TryGetValue(route.TargetBuildingId, out var targetBuilding)
                ? $"#{targetBuilding.Id} {targetBuilding.Kind}({targetBuilding.Storage.Get(route.CargoKind)}/{route.TargetStock})" : "missing target";
            return $"#{route.Id} {route.CargoKind} {source} -> {target} P{route.Priority} {route.Mode}: {route.Status}";
        }

        private bool TryGetRouteBuildings(LogisticsRoute route, out BuildingEntity source, out BuildingEntity target)
        {
            _world.BindRouteBuildings(route);
            bool hasSource = _world.Buildings.TryGetValue(route.SourceBuildingId, out source);
            bool hasTarget = _world.Buildings.TryGetValue(route.TargetBuildingId, out target);
            if (hasSource) _world.EnsureBuildingStorage(source);
            if (hasTarget) _world.EnsureBuildingStorage(target);
            return hasSource && hasTarget && source.StorageCapacity > 0 && target.StorageCapacity > 0;
        }

        private VehicleAgent FindAssignedVehicle(int routeId)
        {
            foreach (var vehicle in _vehicles.AllAgents())
                if (vehicle?.Entity != null && vehicle.Entity.AssignedRouteId == routeId) return vehicle;
            return null;
        }

        private static bool Near(Vector2Int a, Vector2Int b) => (a - b).sqrMagnitude <= 9;

        private LogisticsRoute FindRoute(int routeId)
        {
            foreach (var route in _world.LogisticsRoutes)
                if (route.Id == routeId) return route;
            return null;
        }

        private static ResourceKind NextCargo(ResourceKind current) => current switch
        {
            ResourceKind.Food => ResourceKind.Wood,
            ResourceKind.Wood => ResourceKind.Stone,
            ResourceKind.Stone => ResourceKind.IronOre,
            ResourceKind.IronOre => ResourceKind.Coal,
            ResourceKind.Coal => ResourceKind.IronIngot,
            ResourceKind.IronIngot => ResourceKind.Steel,
            ResourceKind.Steel => ResourceKind.MachineParts,
            ResourceKind.MachineParts => ResourceKind.RailParts,
            ResourceKind.RailParts => ResourceKind.Weapons,
            ResourceKind.Weapons => ResourceKind.Ammo,
            ResourceKind.Ammo => ResourceKind.Fuel,
            _ => ResourceKind.Food
        };
    }
}
