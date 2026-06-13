using System.Collections.Generic;
using UnityEngine;
using Rendering;

namespace OpenWorld
{
    public class VehicleAgent : MonoBehaviour
    {
        public VehicleEntity Entity { get; private set; }
        public bool Selected { get; private set; }

        private OpenWorldState _world;
        private SurfacePathfinder _pathfinder;
        private SurfacePathfinder _railPathfinder;
        private readonly List<Vector2Int> _path = new();
        private int _pathIndex;
        private float _speed = 5.0f;
        private float _simulationAccumulator;
        private GameObject _selectionRing;

        public void Initialize(OpenWorldState world, SurfacePathfinder pathfinder, SurfacePathfinder railPathfinder, VehicleEntity entity)
        {
            _world = world;
            _pathfinder = pathfinder;
            _railPathfinder = railPathfinder;
            Entity = entity;
            transform.position = entity.WorldPosition;
            _speed = SpeedFor(entity.Kind);
            CreateVisual();
        }

        private void Update()
        {
            if (_world == null || Entity == null) return;
            _simulationAccumulator += Time.deltaTime;
            float interval = Entity.SimulationTier switch
            {
                SimulationTier.LowFrequency => 0.2f,
                SimulationTier.Dormant => 1.0f,
                _ => 0f
            };
            if (_simulationAccumulator < interval) return;
            float step = Mathf.Max(Time.deltaTime, _simulationAccumulator);
            _simulationAccumulator = 0f;
            FollowPath(step);
            UpdateCondition();
            Entity.WorldPosition = transform.position;
            Entity.Cell = _world.WorldToCell(transform.position);
        }

        public bool MoveTo(Vector2Int target)
        {
            if (_world == null || _pathfinder == null || Entity == null) return false;
            if (NeedsFuel(Entity.Kind) && Entity.Fuel <= 0.1f)
            {
                Entity.Task = VehicleTask.Disabled;
                Entity.StatusText = "No fuel";
                return false;
            }

            var activePathfinder = IsRail(Entity.Kind) ? _railPathfinder : _pathfinder;
            _path.Clear();
            _path.AddRange(activePathfinder.FindPath(Entity.Cell, target));
            _pathIndex = _path.Count > 1 ? 1 : 0;
            Entity.Task = _path.Count > 1 ? VehicleTask.Moving : VehicleTask.Idle;
            Entity.StatusText = _path.Count > 1 ? "Moving" : IsRail(Entity.Kind) ? "No rail path" : "No path";
            return _path.Count > 1;
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (_selectionRing != null) _selectionRing.SetActive(selected);
        }

        private void FollowPath(float deltaTime)
        {
            if (_pathIndex <= 0 || _pathIndex >= _path.Count)
            {
                if (Entity.Task == VehicleTask.Moving)
                {
                    Entity.Task = VehicleTask.Idle;
                    Entity.StatusText = "Idle";
                }
                return;
            }

            var nextCell = _path[_pathIndex];
            var cell = _world.GetCell(nextCell);
            float roadBoost = cell.HasRail && IsRail(Entity.Kind) ? 1.55f : cell.HasRoad ? 1.35f : 1f;
            if (!cell.HasRoad && !cell.HasRail && Entity.Kind is VehicleKind.Truck or VehicleKind.ArmoredCar)
                roadBoost *= 0.55f;

            var target = _world.CellToWorld(nextCell) + Vector3.up * 0.16f;
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * roadBoost * deltaTime);
            if (NeedsFuel(Entity.Kind)) Entity.Fuel = Mathf.Max(0f, Entity.Fuel - deltaTime * (cell.HasRoad ? 0.18f : 0.32f));
            Entity.Condition = Mathf.Max(0f, Entity.Condition - deltaTime * (cell.HasRoad ? 0.02f : 0.06f));

            if ((transform.position - target).sqrMagnitude < 0.05f)
                _pathIndex++;
        }

        private void UpdateCondition()
        {
            if (Entity.Condition <= 1f)
            {
                Entity.Task = VehicleTask.Disabled;
                Entity.StatusText = "Broken down";
                _path.Clear();
            }
        }

        private void CreateVisual()
        {
            var root = new GameObject("VehicleBody");
            root.transform.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Hull";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = ScaleFor(Entity.Kind);
            body.transform.localPosition = Vector3.up * 0.45f;
            body.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(ColorFor(Entity.Kind));

            var marker = GameObject.CreatePrimitive(Entity.Kind == VehicleKind.ArmoredCar ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            marker.name = "CargoMarker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
            marker.transform.localPosition = Vector3.up * 0.95f;
            marker.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(0.92f, 0.78f, 0.32f));

            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.name = "VehicleSelectionRing";
            _selectionRing.transform.SetParent(transform, false);
            _selectionRing.transform.localScale = new Vector3(1.25f, 0.025f, 1.25f);
            _selectionRing.transform.localPosition = Vector3.up * 0.04f;
            _selectionRing.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(0.20f, 0.85f, 1f));
            _selectionRing.SetActive(false);
        }

        public static bool NeedsFuel(VehicleKind kind) => kind is VehicleKind.Truck or VehicleKind.ArmoredCar or VehicleKind.Locomotive or VehicleKind.Tank or VehicleKind.Aircraft or VehicleKind.TransportPlane;

        public static bool IsRail(VehicleKind kind) => kind is VehicleKind.Locomotive or VehicleKind.CargoWagon;

        private static float SpeedFor(VehicleKind kind) => kind switch
        {
            VehicleKind.HandCart => 3.4f,
            VehicleKind.Wagon => 4.2f,
            VehicleKind.Truck => 7.2f,
            VehicleKind.ArmoredCar => 6.4f,
            VehicleKind.Locomotive => 8.0f,
            VehicleKind.CargoWagon => 7.0f,
            _ => 5.0f
        };

        private static Vector3 ScaleFor(VehicleKind kind) => kind switch
        {
            VehicleKind.HandCart => new Vector3(0.7f, 0.35f, 1.0f),
            VehicleKind.Wagon => new Vector3(1.0f, 0.55f, 1.45f),
            VehicleKind.Truck => new Vector3(1.15f, 0.65f, 1.8f),
            VehicleKind.ArmoredCar => new Vector3(1.25f, 0.72f, 1.7f),
            VehicleKind.Locomotive => new Vector3(1.2f, 0.9f, 2.4f),
            VehicleKind.CargoWagon => new Vector3(1.1f, 0.75f, 2.2f),
            _ => Vector3.one
        };

        private static Color ColorFor(VehicleKind kind) => kind switch
        {
            VehicleKind.HandCart => new Color(0.52f, 0.34f, 0.16f),
            VehicleKind.Wagon => new Color(0.44f, 0.28f, 0.12f),
            VehicleKind.Truck => new Color(0.22f, 0.36f, 0.44f),
            VehicleKind.ArmoredCar => new Color(0.22f, 0.28f, 0.24f),
            VehicleKind.Locomotive => new Color(0.08f, 0.08f, 0.08f),
            VehicleKind.CargoWagon => new Color(0.30f, 0.22f, 0.16f),
            _ => new Color(0.45f, 0.45f, 0.45f)
        };

        private static Material MaterialFor(Color color) => MaterialCache.GetLit(color);
    }
}
