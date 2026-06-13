using System.Collections.Generic;
using UnityEngine;
using Rendering;

namespace OpenWorld
{
    public class BuildingSystem : MonoBehaviour
    {
        public IReadOnlyList<BuildableDef> Defs => _defs;

        private OpenWorldState _world;
        private SurfaceTerrainSystem _terrain;
        private readonly List<BuildableDef> _defs = new();
        private Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
        private Dictionary<int, TextMesh> _labels = new Dictionary<int, TextMesh>();

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain)
        {
            _world = world;
            _terrain = terrain;
            _defs.Clear();
            _defs.AddRange(BuildableDef.Defaults());

            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
            I18nSystem.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            if (_world == null) return;
            foreach (var kvp in _labels)
            {
                var id = kvp.Key;
                var textMesh = kvp.Value;
                if (textMesh != null && _world.Buildings.TryGetValue(id, out var b))
                {
                    var def = GetDef(b.Kind);
                    textMesh.text = I18nSystem.Get(def.DisplayName);
                }
            }
        }

        public BuildableDef GetDef(BuildableKind kind) => _defs.Find(d => d.Kind == kind) ?? _defs[0];

        public bool CanPlace(BuildableKind kind, Vector2Int origin, int rotation, out string reason)
        {
            reason = "";
            var def = GetDef(kind);
            var size = RotatedSize(def.Size, rotation);
            if (!FootprintInBounds(origin, size))
            {
                reason = "Out of bounds";
                return false;
            }

            float firstHeight = _world.GetHeight(origin);
            for (int z = 0; z < size.y; z++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var p = origin + new Vector2Int(x, z);
                    var cell = _world.GetCell(p);
                    if (cell.Occupied)
                    {
                        reason = "Occupied";
                        return false;
                    }
                    if (cell.HasTrench)
                    {
                        reason = "Trench blocks foundation";
                        return false;
                    }
                    if (def.RequiresFlatGround && Mathf.Abs(cell.Height - firstHeight) > 0.2f)
                    {
                        reason = "Ground must be flat";
                        return false;
                    }
                    if (cell.Terrain == SurfaceTerrain.Water)
                    {
                        reason = "Cannot build on water";
                        return false;
                    }
                }
            }
            return true;
        }

        public bool TryPlace(BuildableKind kind, Vector2Int origin, int rotation, int factionId, bool spendCost = true)
        {
            if (!CanPlace(kind, origin, rotation, out _)) return false;
            var def = GetDef(kind);
            if (spendCost && !_world.Inventory.Spend(def.Cost)) return false;

            var building = _world.AddBuilding(def, origin, rotation, factionId);
            OccupyFootprint(building, def);
            SpawnBuildingObject(building, def);
            return true;
        }

        public void RebuildFromWorld()
        {
            ClearObjects();
            foreach (var building in _world.Buildings.Values)
            {
                var def = GetDef(building.Kind);
                OccupyFootprint(building, def);
                SpawnBuildingObject(building, def);
            }
        }

        public bool TryDemolish(int buildingId)
        {
            if (!_world.Buildings.TryGetValue(buildingId, out var building)) return false;
            var def = GetDef(building.Kind);
            var size = RotatedSize(def.Size, building.Rotation);
            for (int z = 0; z < size.y; z++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var p = building.Origin + new Vector2Int(x, z);
                    var cell = _world.GetCell(p);
                    if (cell.BuildingId == buildingId)
                    {
                        cell.Occupied = false;
                        cell.BuildingId = 0;
                        _world.SetCell(p, cell);
                    }
                }
            }
            _world.Buildings.Remove(buildingId);
            RemoveBuildingObject(buildingId);
            return true;
        }

        private void OccupyFootprint(BuildingEntity building, BuildableDef def)
        {
            var size = RotatedSize(def.Size, building.Rotation);
            for (int z = 0; z < size.y; z++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var p = building.Origin + new Vector2Int(x, z);
                    var cell = _world.GetCell(p);
                    cell.Occupied = def.BlocksMovement;
                    cell.BuildingId = building.Id;
                    if (building.Kind == BuildableKind.Farm)
                        cell.Terrain = SurfaceTerrain.Plains;
                    _world.SetCell(p, cell);
                }
            }
        }

        private void SpawnBuildingObject(BuildingEntity building, BuildableDef def)
        {
            var size = RotatedSize(def.Size, building.Rotation);
            var center = new Vector2Int(building.Origin.x + size.x / 2, building.Origin.y + size.y / 2);
            float h = _world.GetHeight(building.Origin);

            var root = new GameObject($"Building_{building.Id}_{building.Kind}");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(building.Origin.x + size.x * 0.5f, h + 0.05f, building.Origin.y + size.y * 0.5f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            float height = def.IsDefense ? 1.6f : 1.0f;
            if (def.Kind == BuildableKind.Tower) height = 3.0f;
            if (def.Kind == BuildableKind.Farm) height = 0.08f;
            body.transform.localScale = new Vector3(size.x * 0.92f, height, size.y * 0.92f);
            body.transform.localPosition = new Vector3(0, height * 0.5f, 0);
            var renderer = body.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialFor(def.Color);

            if (def.IsDefense)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "DefenseMarker";
                marker.transform.SetParent(root.transform, false);
                marker.transform.localScale = new Vector3(0.2f, 0.05f, 0.2f);
                marker.transform.localPosition = new Vector3(0, height + 0.08f, 0);
                marker.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(0.85f, 0.16f, 0.12f));
            }

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(0, height + 0.5f, 0);
            textGo.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var textMesh = textGo.AddComponent<TextMesh>();
            textMesh.text = I18nSystem.Get(def.DisplayName);
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 64;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            _objects[building.Id] = root;
            _labels[building.Id] = textMesh;
        }

        private bool FootprintInBounds(Vector2Int origin, Vector2Int size)
        {
            return _world.InBounds(origin) && _world.InBounds(origin + size - Vector2Int.one);
        }

        private static Vector2Int RotatedSize(Vector2Int size, int rotation) => Mathf.Abs(rotation / 90) % 2 == 1 ? new Vector2Int(size.y, size.x) : size;

        private static Material MaterialFor(Color color) => MaterialCache.GetLit(color);

        private void RemoveBuildingObject(int id)
        {
            if (_objects.TryGetValue(id, out var go))
            {
                Destroy(go);
                _objects.Remove(id);
                _labels.Remove(id);
            }
        }

        private void ClearObjects()
        {
            foreach (var obj in _objects.Values)
            {
                if (obj == null) continue;
                if (Application.isPlaying) Destroy(obj);
                else DestroyImmediate(obj);
            }
            _objects.Clear();
            _labels.Clear();
        }
    }
}
