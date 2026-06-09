using System.Collections.Generic;
using UnityEngine;

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
        private GameObject _selectionRing;

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
            FollowPath();
            Entity.WorldPosition = transform.position;
            Entity.Cell = _world.WorldToCell(transform.position);
        }

        public void MoveTo(Vector2Int target)
        {
            if (_world == null || _pathfinder == null || Entity == null) return;
            _path.Clear();
            _path.AddRange(_pathfinder.FindPath(Entity.Cell, target));
            _pathIndex = _path.Count > 1 ? 1 : 0;
            Entity.Task = _path.Count > 1 ? UnitTask.Moving : UnitTask.Idle;
        }

        public bool IsAt(Vector2Int target) => (Entity.Cell - target).sqrMagnitude <= 2;

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (_selectionRing != null) _selectionRing.SetActive(selected);
        }

        private void FollowPath()
        {
            if (_pathIndex <= 0 || _pathIndex >= _path.Count)
            {
                if (Entity.Task == UnitTask.Moving) Entity.Task = UnitTask.Idle;
                return;
            }

            var nextCell = _path[_pathIndex];
            var target = _world.CellToWorld(nextCell) + Vector3.up * 0.12f;
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
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
            renderer.sharedMaterial = MaterialFor(Entity.Kind == UnitKind.Worker ? new Color(0.15f, 0.35f, 0.85f) : new Color(0.85f, 0.2f, 0.12f));

            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.name = "SelectionRing";
            _selectionRing.transform.SetParent(transform, false);
            _selectionRing.transform.localScale = new Vector3(0.8f, 0.025f, 0.8f);
            _selectionRing.transform.localPosition = Vector3.up * 0.03f;
            _selectionRing.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(1f, 0.9f, 0.15f));
            _selectionRing.SetActive(false);
        }

        private static Material MaterialFor(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader) { color = color };
        }
    }
}
