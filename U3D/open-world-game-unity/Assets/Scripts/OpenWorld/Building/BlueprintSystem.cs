using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class BlueprintSystem : MonoBehaviour
    {
        public IReadOnlyDictionary<int, GameObject> BlueprintObjects => _objects;

        private OpenWorldState _world;
        private SurfaceTerrainSystem _terrain;
        private BuildingSystem _buildings;
        private readonly Dictionary<int, GameObject> _objects = new();
        private float _tickTimer;

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, BuildingSystem buildings)
        {
            _world = world;
            _terrain = terrain;
            _buildings = buildings;
        }

        private void Update()
        {
            if (_world == null) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < 0.25f) return;
            _tickTimer = 0f;
            ProcessBlueprints(0.25f);
        }

        public BlueprintJob QueueBuilding(BuildableKind kind, Vector2Int cell, int factionId, int priority = 3)
        {
            var blueprint = _world.AddBlueprint(BlueprintKind.Building, cell, 0, factionId);
            blueprint.BuildKind = kind;
            blueprint.Priority = priority;
            blueprint.WorkRemaining = Mathf.Max(1.5f, _buildings.GetDef(kind).Size.x + _buildings.GetDef(kind).Size.y);
            SpawnVisual(blueprint);
            return blueprint;
        }

        public BlueprintJob QueueTerrain(TerrainTool tool, Vector2Int cell, int radius, int factionId, int priority = 3)
        {
            var kind = tool switch
            {
                TerrainTool.Road => BlueprintKind.Road,
                TerrainTool.Rail => BlueprintKind.Rail,
                TerrainTool.Bridge => BlueprintKind.Bridge,
                TerrainTool.Mine or TerrainTool.Dig => BlueprintKind.MiningZone,
                TerrainTool.Trench => BlueprintKind.DefenseZone,
                _ => BlueprintKind.Terrain
            };
            var blueprint = _world.AddBlueprint(kind, cell, radius, factionId);
            blueprint.Tool = tool;
            blueprint.Priority = priority;
            blueprint.WorkRemaining = Mathf.Max(1.0f, radius + 1.0f);
            SpawnVisual(blueprint);
            return blueprint;
        }

        public void RebuildFromWorld()
        {
            ClearObjects();
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                SpawnVisual(blueprint);
            }
        }

        public bool CancelNearest(Vector2Int cell, float maxDistance = 7f)
        {
            BlueprintJob best = null;
            float bestDistance = maxDistance * maxDistance;
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                float distance = (blueprint.Cell - cell).sqrMagnitude;
                if (distance > bestDistance) continue;
                best = blueprint;
                bestDistance = distance;
            }

            if (best == null) return false;
            best.Status = BlueprintStatus.Cancelled;
            if (_objects.TryGetValue(best.Id, out var obj)) Destroy(obj);
            _objects.Remove(best.Id);
            return true;
        }

        public bool CancelById(int id)
        {
            var blueprint = FindBlueprint(id);
            if (blueprint == null) return false;
            return Cancel(blueprint);
        }

        public int CancelAll()
        {
            int count = 0;
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                if (Cancel(blueprint)) count++;
            }
            return count;
        }

        public void AdjustNearestPriority(Vector2Int cell, int delta)
        {
            BlueprintJob best = null;
            float bestDistance = 100f;
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                float distance = (blueprint.Cell - cell).sqrMagnitude;
                if (distance > bestDistance) continue;
                best = blueprint;
                bestDistance = distance;
            }

            if (best == null) return;
            best.Priority = Mathf.Clamp(best.Priority + delta, 1, 9);
        }

        public void AdjustPriorityById(int id, int delta)
        {
            var blueprint = FindBlueprint(id);
            if (blueprint == null || blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) return;
            blueprint.Priority = Mathf.Clamp(blueprint.Priority + delta, 1, 9);
        }

        public void TogglePauseNearest(Vector2Int cell)
        {
            BlueprintJob best = null;
            float bestDistance = 100f;
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                float distance = (blueprint.Cell - cell).sqrMagnitude;
                if (distance > bestDistance) continue;
                best = blueprint;
                bestDistance = distance;
            }

            if (best == null) return;
            if (best.Status == BlueprintStatus.Paused) Resume(best.Id);
            else Pause(best.Id);
        }

        public bool Pause(int id)
        {
            var blueprint = FindBlueprint(id);
            if (blueprint == null || blueprint.Status != BlueprintStatus.Active) return false;
            blueprint.Status = BlueprintStatus.Paused;
            blueprint.BlockedReason = "Paused by player";
            UpdateVisualState(blueprint);
            return true;
        }

        public bool Resume(int id)
        {
            var blueprint = FindBlueprint(id);
            if (blueprint == null || blueprint.Status is BlueprintStatus.Cancelled or BlueprintStatus.Complete) return false;
            blueprint.Status = BlueprintStatus.Active;
            blueprint.BlockedReason = "";
            UpdateVisualState(blueprint);
            return true;
        }

        private void ProcessBlueprints(float delta)
        {
            BlueprintJob best = null;
            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status != BlueprintStatus.Active) continue;
                if (best == null || blueprint.Priority > best.Priority || (blueprint.Priority == best.Priority && blueprint.Id < best.Id))
                    best = blueprint;
            }

            if (best == null) return;
            best.WorkRemaining -= Mathf.Max(0.15f, _world.Population.Workers * 0.02f) * delta * 4f;
            if (best.WorkRemaining > 0f) return;

            Complete(best);
        }

        private void Complete(BlueprintJob blueprint)
        {
            bool success;
            switch (blueprint.Kind)
            {
                case BlueprintKind.Building:
                    success = _buildings.TryPlace(blueprint.BuildKind, blueprint.Cell, 0, blueprint.FactionId);
                    break;
                case BlueprintKind.Road:
                    _terrain.ApplyBrush(TerrainTool.Road, blueprint.Cell, Mathf.Max(0, blueprint.Radius), 0.5f);
                    success = true;
                    break;
                case BlueprintKind.Rail:
                    _terrain.ApplyBrush(TerrainTool.Rail, blueprint.Cell, Mathf.Max(0, blueprint.Radius), 0.5f);
                    success = true;
                    break;
                case BlueprintKind.Bridge:
                    _terrain.ApplyBrush(TerrainTool.Bridge, blueprint.Cell, Mathf.Max(1, blueprint.Radius), 0.5f);
                    success = true;
                    break;
                case BlueprintKind.MiningZone:
                    _terrain.ApplyBrush(TerrainTool.Dig, blueprint.Cell, Mathf.Max(0, blueprint.Radius), 0.5f);
                    success = true;
                    break;
                default:
                    _terrain.ApplyBrush(blueprint.Tool, blueprint.Cell, Mathf.Max(0, blueprint.Radius), 0.5f);
                    success = true;
                    break;
            }

            blueprint.Status = success ? BlueprintStatus.Complete : BlueprintStatus.Blocked;
            blueprint.BlockedReason = success ? "" : "Missing resources or blocked site";
            if (_objects.TryGetValue(blueprint.Id, out var obj)) Destroy(obj);
            _objects.Remove(blueprint.Id);
        }

        private void SpawnVisual(BlueprintJob blueprint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Blueprint_{blueprint.Id}_{blueprint.Kind}";
            go.transform.SetParent(transform, false);
            go.transform.position = _world.CellToWorld(blueprint.Cell) + Vector3.up * 0.12f;
            float size = Mathf.Max(1f, blueprint.Radius * 2f + 1f);
            if (blueprint.Kind == BlueprintKind.Building)
            {
                var def = _buildings.GetDef(blueprint.BuildKind);
                go.transform.localScale = new Vector3(def.Size.x, 0.15f, def.Size.y);
            }
            else go.transform.localScale = new Vector3(size, 0.08f, size);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = BlueprintMaterial(blueprint.Kind);
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            _objects[blueprint.Id] = go;
            UpdateVisualState(blueprint);
        }

        private BlueprintJob FindBlueprint(int id)
        {
            foreach (var blueprint in _world.Blueprints)
                if (blueprint.Id == id) return blueprint;
            return null;
        }

        private bool Cancel(BlueprintJob blueprint)
        {
            if (blueprint == null || blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) return false;
            blueprint.Status = BlueprintStatus.Cancelled;
            blueprint.BlockedReason = "Cancelled";
            if (_objects.TryGetValue(blueprint.Id, out var obj)) Destroy(obj);
            _objects.Remove(blueprint.Id);
            return true;
        }

        private void UpdateVisualState(BlueprintJob blueprint)
        {
            if (blueprint == null || !_objects.TryGetValue(blueprint.Id, out var obj) || obj == null) return;
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            var material = renderer.sharedMaterial;
            if (material == null) return;
            material.color = blueprint.Status switch
            {
                BlueprintStatus.Paused => new Color(0.95f, 0.78f, 0.18f, 0.55f),
                BlueprintStatus.Blocked => new Color(0.95f, 0.18f, 0.14f, 0.60f),
                _ => BlueprintColor(blueprint.Kind)
            };
        }

        private static Material BlueprintMaterial(BlueprintKind kind)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = BlueprintColor(kind) };
            return mat;
        }

        private static Color BlueprintColor(BlueprintKind kind) => kind switch
        {
            BlueprintKind.Road => new Color(0.76f, 0.61f, 0.34f, 0.45f),
            BlueprintKind.Rail => new Color(0.45f, 0.45f, 0.50f, 0.55f),
            BlueprintKind.Bridge => new Color(0.75f, 0.43f, 0.22f, 0.55f),
            BlueprintKind.MiningZone => new Color(0.40f, 0.48f, 0.55f, 0.45f),
            BlueprintKind.DefenseZone => new Color(0.80f, 0.18f, 0.14f, 0.45f),
            _ => new Color(0.20f, 0.68f, 0.95f, 0.45f)
        };

        private void ClearObjects()
        {
            foreach (var obj in _objects.Values)
            {
                if (obj == null) continue;
                if (Application.isPlaying) Destroy(obj);
                else DestroyImmediate(obj);
            }
            _objects.Clear();
        }
    }
}
