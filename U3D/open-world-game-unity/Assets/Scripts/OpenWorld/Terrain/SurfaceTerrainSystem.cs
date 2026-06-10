using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class SurfaceTerrainSystem : MonoBehaviour
    {
        public OpenWorldState World { get; private set; }
        public Material TerrainMaterial { get; private set; }

        private readonly Dictionary<Vector2Int, SurfaceChunkView> _views = new();
        private int _visibleRadius;
        private OpenWorldGeologySystem _geology;
        private Vector2Int _lastCenterChunk = new(int.MinValue, int.MinValue);

        public void Initialize(OpenWorldState world, int visibleRadius)
        {
            World = world;
            _visibleRadius = visibleRadius;
            TerrainMaterial = CreateTerrainMaterial();
            RefreshVisibleChunks(Vector2Int.one * (world.MapSize / (2 * world.ChunkSize)));
        }

        public void SetGeology(OpenWorldGeologySystem geology) => _geology = geology;

        private void LateUpdate()
        {
            if (World == null) return;
            foreach (var view in _views.Values)
            {
                if (view.Chunk.DirtyVisual)
                    view.Rebuild();
            }
        }

        public void UpdateStreaming(Vector3 worldPosition)
        {
            if (World == null) return;
            var cell = World.WorldToCell(worldPosition);
            if (!World.InBounds(cell)) return;
            var center = World.ToChunkCoord(cell);
            if (center == _lastCenterChunk) return;
            RefreshVisibleChunks(center);
        }

        public void RefreshVisibleChunks(Vector2Int centerChunk)
        {
            _lastCenterChunk = centerChunk;
            for (int z = -_visibleRadius; z <= _visibleRadius; z++)
            {
                for (int x = -_visibleRadius; x <= _visibleRadius; x++)
                {
                    var coord = centerChunk + new Vector2Int(x, z);
                    if (coord.x < 0 || coord.y < 0 || coord.x * World.ChunkSize >= World.MapSize || coord.y * World.ChunkSize >= World.MapSize)
                        continue;
                    EnsureView(coord);
                }
            }
        }

        public bool TryRaycastCell(Camera cam, Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            var ray = cam.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 2000f)) return false;
            cell = World.WorldToCell(hit.point);
            return World.InBounds(cell);
        }

        public void ApplyBrush(TerrainTool tool, Vector2Int center, int radius, float amount)
        {
            if (World == null || tool == TerrainTool.None) return;
            float targetHeight = World.GetHeight(center);

            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    var cellPos = center + new Vector2Int(x, z);
                    if (!World.InBounds(cellPos)) continue;
                    if (new Vector2(x, z).magnitude > radius + 0.25f) continue;

                    var cell = World.GetCell(cellPos);
                    if (cell.Occupied && tool != TerrainTool.Road) continue;

                    switch (tool)
                    {
                        case TerrainTool.Dig:
                            DigCell(cellPos, ref cell, Mathf.Max(0.25f, amount), 1f);
                            break;
                        case TerrainTool.Fill:
                            if (World.Inventory.Dirt <= 0 && World.Inventory.Stone <= 0) continue;
                            cell.Height += Mathf.Max(0.25f, amount);
                            if (World.Inventory.Dirt > 0) World.Inventory.Dirt--;
                            else World.Inventory.Stone--;
                            break;
                        case TerrainTool.Flatten:
                            cell.Height = Mathf.MoveTowards(cell.Height, targetHeight, Mathf.Max(0.25f, amount));
                            break;
                        case TerrainTool.Ramp:
                            cell.Height = Mathf.Lerp(cell.Height, targetHeight + (x + z) * 0.25f, 0.5f);
                            break;
                        case TerrainTool.Road:
                            cell.HasRoad = true;
                            cell.Terrain = SurfaceTerrain.Road;
                            break;
                        case TerrainTool.Rail:
                            if (World.Inventory.RailParts <= 0 && World.Inventory.IronIngot <= 0) continue;
                            cell.HasRail = true;
                            cell.HasRoad = true;
                            cell.Terrain = SurfaceTerrain.Rail;
                            if (World.Inventory.RailParts > 0) World.Inventory.RailParts--;
                            else World.Inventory.IronIngot--;
                            break;
                        case TerrainTool.Bridge:
                            cell.HasBridge = true;
                            cell.HasRoad = true;
                            cell.Occupied = false;
                            cell.Terrain = SurfaceTerrain.Bridge;
                            break;
                        case TerrainTool.Trench:
                            DigCell(cellPos, ref cell, Mathf.Max(0.5f, amount), 0.75f);
                            cell.HasTrench = true;
                            break;
                        case TerrainTool.Mine:
                            DigCell(cellPos, ref cell, Mathf.Max(0.75f, amount), 1.8f);
                            break;
                    }

                    World.SetCell(cellPos, cell);
                }
            }
        }

        private void DigCell(Vector2Int cellPos, ref SurfaceCell cell, float amount, float extractionMultiplier)
        {
            OpenWorldState.NormalizeLayers(ref cell);

            int idx = Mathf.Clamp(cell.CurrentLayer, 0, cell.Layers.Length - 1);
            var layer = cell.Layers[idx];
            if (layer.Material == GroundMaterial.Oil) return;

            cell.Height -= amount / Mathf.Max(0.5f, layer.Hardness);
            cell.HasRoad = false;

            int extracted = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.2f, layer.Grade) * extractionMultiplier));
            extracted = Mathf.Min(extracted, Mathf.Max(0, layer.RemainingAmount));
            if (extracted > 0)
            {
                if (_geology != null) _geology.ReceiveExcavatedMaterial(cellPos, layer.Material, extracted);
                else World.Inventory.Add(layer.Material, extracted);
            }

            layer.RemainingAmount = Mathf.Max(0, layer.RemainingAmount - extracted);
            layer.Thickness = Mathf.Max(0, layer.Thickness - 1);
            cell.Layers[idx] = layer;
            if (layer.RemainingAmount == 0)
                _geology?.MarkExhausted(cellPos, layer.Material);
            if (layer.Thickness == 0 && cell.CurrentLayer < cell.Layers.Length - 1)
                cell.CurrentLayer++;
            cell.ResourceRichness = Mathf.Clamp(Mathf.CeilToInt(layer.RemainingAmount / 30f), 0, 4);
        }

        public bool IsReachableStep(Vector2Int from, Vector2Int to, float maxStep)
        {
            if (!World.InBounds(to)) return false;
            var c = World.GetCell(to);
            if (c.MoveCost >= 9999f) return false;
            return Mathf.Abs(World.GetHeight(to) - World.GetHeight(from)) <= maxStep || c.HasRoad || c.HasBridge || c.HasRail;
        }

        private void EnsureView(Vector2Int coord)
        {
            if (_views.ContainsKey(coord)) return;
            var chunk = World.GetOrCreateChunk(coord);
            var go = new GameObject($"SurfaceChunk_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<SurfaceChunkView>();
            view.Initialize(World, chunk, TerrainMaterial);
            _views[coord] = view;
        }

        private static Material CreateTerrainMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = "OpenWorld Vertex Terrain";
            mat.color = Color.white;
            return mat;
        }
    }
}
