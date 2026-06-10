using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldPerformanceSystem : MonoBehaviour
    {
        public int HighFrequencyCount { get; private set; }
        public int LowFrequencyCount { get; private set; }
        public int DormantCount { get; private set; }

        private OpenWorldState _world;
        private Camera _camera;
        private UnitSystem _units;
        private VehicleSystem _vehicles;
        private float _timer;

        public void Initialize(OpenWorldState world, Camera camera, UnitSystem units, VehicleSystem vehicles)
        {
            _world = world;
            _camera = camera;
            _units = units;
            _vehicles = vehicles;
            RefreshTiers();
        }

        private void Update()
        {
            if (_world == null) return;
            _timer += Time.deltaTime;
            if (_timer < 1f) return;
            _timer = 0f;
            RefreshTiers();
        }

        private void RefreshTiers()
        {
            var focus = _camera != null
                ? _world.WorldToCell(_camera.transform.position + _camera.transform.forward * 55f)
                : new Vector2Int(_world.MapSize / 2, _world.MapSize / 2);

            HighFrequencyCount = 0;
            LowFrequencyCount = 0;
            DormantCount = 0;

            foreach (var unit in _world.Units.Values)
            {
                bool active = unit.Task is UnitTask.Attacking or UnitTask.Building or UnitTask.Digging or UnitTask.Surveying or UnitTask.Drilling or UnitTask.Healing;
                unit.SimulationTier = TierFor(unit.Cell, focus, active);
                SetRepresentationActive(_units?.GetAgent(unit.Id), unit.SimulationTier != SimulationTier.Dormant);
                Count(unit.SimulationTier);
            }

            foreach (var vehicle in _world.Vehicles.Values)
            {
                bool active = vehicle.AssignedRouteId > 0 || vehicle.Task is VehicleTask.Escort or VehicleTask.Patrol or VehicleTask.Repair or VehicleTask.Refuel;
                vehicle.SimulationTier = TierFor(vehicle.Cell, focus, active);
                SetRepresentationActive(_vehicles?.GetAgent(vehicle.Id), vehicle.SimulationTier != SimulationTier.Dormant);
                Count(vehicle.SimulationTier);
            }
        }

        private static void SetRepresentationActive(Component agent, bool active)
        {
            if (agent != null && agent.gameObject.activeSelf != active)
                agent.gameObject.SetActive(active);
        }

        private static SimulationTier TierFor(Vector2Int cell, Vector2Int focus, bool active)
        {
            int distance = Mathf.Abs(cell.x - focus.x) + Mathf.Abs(cell.y - focus.y);
            if (active || distance <= 90) return SimulationTier.HighFrequency;
            if (distance <= 240) return SimulationTier.LowFrequency;
            return SimulationTier.Dormant;
        }

        private void Count(SimulationTier tier)
        {
            switch (tier)
            {
                case SimulationTier.HighFrequency: HighFrequencyCount++; break;
                case SimulationTier.LowFrequency: LowFrequencyCount++; break;
                default: DormantCount++; break;
            }
        }
    }
}
