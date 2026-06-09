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

        private void TickRoutes()
        {
            foreach (var vehicle in _vehicles.AllAgents())
                CompleteDeliveryIfArrived(vehicle);

            LastStatus = "Routes idle";
            if (_world.LogisticsRoutes.Count == 0)
                LastStatus = "No routes";

            foreach (var route in _world.LogisticsRoutes)
            {
                if (route.Mode != LogisticsMode.Automatic)
                {
                    route.Status = "Manual route";
                    continue;
                }

                var vehicle = _vehicles.FirstIdleFor(route.PreferredVehicle);
                if (vehicle == null)
                {
                    route.Status = "No idle vehicle";
                    LastStatus = route.Status;
                    continue;
                }

                int available = _world.Inventory.Get(route.CargoKind);
                if (available <= 0)
                {
                    route.Status = "No source stock";
                    LastStatus = route.Status;
                    continue;
                }

                if (NeedsFuel(vehicle.Entity.Kind) && vehicle.Entity.Fuel <= 0.1f)
                {
                    route.Status = "Vehicle lacks fuel";
                    LastStatus = route.Status;
                    continue;
                }

                int load = Mathf.Min(vehicle.Entity.CargoCapacity, Mathf.Min(available, route.TargetStock));
                if (!_world.Inventory.Spend(route.CargoKind, load))
                {
                    route.Status = "Cannot reserve cargo";
                    LastStatus = route.Status;
                    continue;
                }

                vehicle.Entity.CargoKind = route.CargoKind;
                vehicle.Entity.CargoAmount = load;
                vehicle.Entity.AssignedRouteId = route.Id;
                vehicle.Entity.Task = VehicleTask.AutoTransport;
                vehicle.Entity.StatusText = $"Delivering {load} {route.CargoKind}";
                if (!vehicle.MoveTo(route.Target))
                {
                    _world.Inventory.Add(route.CargoKind, load);
                    vehicle.Entity.CargoAmount = 0;
                    vehicle.Entity.AssignedRouteId = 0;
                    route.Status = vehicle.Entity.StatusText == "No fuel" ? "Vehicle lacks fuel" : "No path";
                    LastStatus = route.Status;
                    continue;
                }
                route.Status = $"Dispatched {vehicle.Entity.Kind}";
                LastStatus = route.Status;
            }

            RefreshLines();
        }

        public void CompleteDeliveryIfArrived(VehicleAgent vehicle)
        {
            if (vehicle == null || vehicle.Entity.CargoAmount <= 0 || vehicle.Entity.AssignedRouteId == 0) return;
            LogisticsRoute route = null;
            foreach (var candidate in _world.LogisticsRoutes)
            {
                if (candidate.Id == vehicle.Entity.AssignedRouteId)
                {
                    route = candidate;
                    break;
                }
            }

            if (route == null) return;
            if ((vehicle.Entity.Cell - route.Target).sqrMagnitude > 6) return;
            _world.Inventory.Add(vehicle.Entity.CargoKind, vehicle.Entity.CargoAmount);
            vehicle.Entity.CargoAmount = 0;
            vehicle.Entity.AssignedRouteId = 0;
            vehicle.Entity.StatusText = "Delivered";
            vehicle.Entity.Task = VehicleTask.Idle;
            vehicle.MoveTo(route.Source);
        }

        private static bool NeedsFuel(VehicleKind kind) => kind is VehicleKind.Truck or VehicleKind.ArmoredCar or VehicleKind.Locomotive or VehicleKind.Tank or VehicleKind.Aircraft or VehicleKind.TransportPlane;

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
                    _routeLines.Add($"#{route.Id} {route.CargoKind} P{route.Priority} {route.Mode}: {route.Status}");
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
    }
}
