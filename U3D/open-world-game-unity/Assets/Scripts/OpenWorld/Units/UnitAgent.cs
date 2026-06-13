using System.Collections.Generic;
using UnityEngine;
using Rendering;

namespace OpenWorld
{
    public class UnitAgent : MonoBehaviour
    {
        public UnitEntity Entity { get; private set; }
        public bool Selected { get; private set; }

        private OpenWorldState _world;
        private SurfacePathfinder _pathfinder;
        private readonly List<Vector2Int> _path = new();
        private int _pathIndex;
        private float _speed = 4.0f;
        private float _simulationAccumulator;
        private GameObject _selectionRing;
        private GameObject _hpBar;

        public void Initialize(OpenWorldState world, SurfacePathfinder pathfinder, UnitEntity entity)
        {
            _world = world;
            _pathfinder = pathfinder;
            Entity = entity;
            transform.position = entity.WorldPosition;
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
            Entity.WorldPosition = transform.position;
            Entity.Cell = _world.WorldToCell(transform.position);
            UpdateHpBar();
        }

        public void MoveTo(Vector2Int target)
        {
            if (_world == null || _pathfinder == null || Entity == null) return;
            Entity.CurrentOrder ??= new UnitOrder();
            Entity.CurrentOrder.Kind = UnitOrderKind.Move;
            Entity.CurrentOrder.TargetCell = target;
            BuildPath(target, UnitTask.Moving);
        }

        public void IssueOrder(UnitOrder order)
        {
            if (order == null || Entity == null) return;
            Entity.CurrentOrder = order;
            UnitTask task = order.Kind switch
            {
                UnitOrderKind.Attack => UnitTask.Attacking,
                UnitOrderKind.Patrol => UnitTask.Patrolling,
                UnitOrderKind.Defend => UnitTask.Defending,
                UnitOrderKind.Escort => UnitTask.Transporting,
                UnitOrderKind.Survey => UnitTask.Surveying,
                UnitOrderKind.Drill => UnitTask.Drilling,
                _ => UnitTask.Moving
            };
            BuildPath(order.TargetCell, task);
        }

        public void ClearOrder()
        {
            _path.Clear();
            _pathIndex = 0;
            if (Entity == null) return;
            Entity.CurrentOrder = new UnitOrder { Kind = UnitOrderKind.Move, TargetCell = Entity.Cell };
            Entity.Task = UnitTask.Idle;
        }

        private void BuildPath(Vector2Int target, UnitTask task)
        {
            _path.Clear();
            _path.AddRange(_pathfinder.FindPath(Entity.Cell, target));
            _pathIndex = _path.Count > 1 ? 1 : 0;
            Entity.Task = _path.Count > 1 ? task : UnitTask.Idle;
        }

        public bool IsAt(Vector2Int target) => (Entity.Cell - target).sqrMagnitude <= 2;

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (_selectionRing != null) _selectionRing.SetActive(selected);
        }

private void FollowPath(float deltaTime)
    {
        if (_pathIndex <= 0 || _pathIndex >= _path.Count)
        {
            // Patrol: automatically reverse direction for continuous back-and-forth
            if (Entity.CurrentOrder != null && Entity.CurrentOrder.Kind == UnitOrderKind.Patrol)
            {
                var order = Entity.CurrentOrder;
                var next = order.SecondaryCell;
                order.SecondaryCell = order.TargetCell;
                order.TargetCell = next;
                BuildPath(order.TargetCell, UnitTask.Patrolling);
                return;
            }
            // Defend: when reaching defend position, keep defending task active
            if (Entity.CurrentOrder != null && Entity.CurrentOrder.Kind == UnitOrderKind.Defend)
            {
                Entity.Task = UnitTask.Defending;
                return;
            }
            if (Entity.Task is UnitTask.Moving or UnitTask.Attacking or UnitTask.Defending or UnitTask.Transporting)
                Entity.Task = UnitTask.Idle;
            return;
        }

        var nextCell = _path[_pathIndex];
        var target = _world.CellToWorld(nextCell) + Vector3.up * 0.12f;
        float condition = Mathf.Clamp(Entity.Morale / 100f, 0.35f, 1f) * Mathf.Clamp(1f - Entity.Fatigue / 130f, 0.35f, 1f);
        if (Entity.Wounded) condition *= 0.6f;
        transform.position = Vector3.MoveTowards(transform.position, target, _speed * condition * deltaTime);
        if ((transform.position - target).sqrMagnitude < 0.04f)
            _pathIndex++;
    }

        private void CreateVisual()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "UnitBody";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            body.transform.localPosition = Vector3.up * 0.45f;
            var renderer = body.GetComponent<MeshRenderer>();
            Color color = Entity.FactionId switch
            {
                1 => new Color(0.25f, 0.54f, 0.92f),
                2 => new Color(0.85f, 0.20f, 0.12f),
                3 => new Color(0.85f, 0.75f, 0.15f),
                _ => new Color(0.45f, 0.65f, 0.35f),
            };
            renderer.sharedMaterial = MaterialFor(color);

            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.name = "SelectionRing";
            _selectionRing.transform.SetParent(transform, false);
            _selectionRing.transform.localScale = new Vector3(0.8f, 0.025f, 0.8f);
            _selectionRing.transform.localPosition = Vector3.up * 0.03f;
            _selectionRing.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(1f, 0.9f, 0.15f));
            _selectionRing.SetActive(false);

            // HP bar background
            var hpBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hpBg.name = "HpBarBg";
            hpBg.transform.SetParent(transform, false);
            hpBg.transform.localScale = new Vector3(0.6f, 0.06f, 0.06f);
            hpBg.transform.localPosition = Vector3.up * 1.15f;
            hpBg.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(0.2f, 0.2f, 0.2f));

            // HP bar fill
            _hpBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _hpBar.name = "HpBarFill";
            _hpBar.transform.SetParent(transform, false);
            _hpBar.transform.localScale = new Vector3(0.58f, 0.04f, 0.04f);
            _hpBar.transform.localPosition = Vector3.up * 1.15f + Vector3.forward * 0.01f;
            _hpBar.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(0.15f, 0.85f, 0.15f));
        }

        private void UpdateHpBar()
        {
            if (_hpBar == null || Entity == null) return;
            float ratio = Mathf.Clamp01((float)Entity.Hp / Entity.MaxHp);
            _hpBar.transform.localScale = new Vector3(0.58f * ratio, 0.04f, 0.04f);
            _hpBar.transform.localPosition = Vector3.up * 1.15f + Vector3.forward * 0.01f + Vector3.right * (0.58f * (ratio - 1f) * 0.5f);
            Color barColor = ratio > 0.5f
                ? Color.Lerp(new Color(0.85f, 0.85f, 0.15f), new Color(0.15f, 0.85f, 0.15f), (ratio - 0.5f) * 2f)
                : Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(0.85f, 0.85f, 0.15f), ratio * 2f);
            _hpBar.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(barColor);
        }

        private static Material MaterialFor(Color color) => MaterialCache.GetLit(color);
    }
}
