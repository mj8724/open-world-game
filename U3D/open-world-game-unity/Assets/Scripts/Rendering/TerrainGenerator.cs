using System;
using System.Collections.Generic;
using GameState;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 大陆地形生成器 — IDW 高度图 + 顶点色网格 + 水面
/// 移植自 client/src/map3d/terrain-generator.js (362 行)
/// </summary>
namespace Rendering
{
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("生成参数")]
        [SerializeField] private int _gridSize = 200;
        [SerializeField] private float _worldSize = 60f;

        [Header("材质")]
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private Material _waterMaterial;

        private Mesh _groundMesh;
        private Mesh _waterMesh;
        private GameObject _groundObject;
        private GameObject _waterObject;
        private float[] _heightData;

        // 节点 3D 位置用于 IDW
        private struct NodeTerrainInfo
        {
            public float x, z;
            public string terrain;
            public float elevation;
            public string factionId;
        }
        private List<NodeTerrainInfo> _nodePositions3D = new();

        /// <summary>生成大陆地形</summary>
        public void Generate(Dictionary<string, NodeComponent> nodes)
        {
            Cleanup();

            if (nodes == null || nodes.Count == 0)
            {
                Debug.LogWarning("[Terrain] 节点数据为空，跳过地形生成");
                return;
            }

            // 准备节点 3D 位置
            PrepareNodePositions(nodes);

            // 生成高度图网格
            var (mesh, heightMap) = GenerateHeightMap();
            _heightData = heightMap;

            // 地面
            _groundMesh = mesh;
            _groundObject = new GameObject("Terrain");
            _groundObject.transform.SetParent(transform, false);
            var mf = _groundObject.AddComponent<MeshFilter>();
            mf.sharedMesh = _groundMesh;
            var mr = _groundObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _terrainMaterial ?? CreateDefaultTerrainMaterial();
            _groundObject.isStatic = true;

            // 水面
            _waterMesh = CreateWaterMesh();
            _waterObject = new GameObject("Water");
            _waterObject.transform.SetParent(transform, false);
            var wf = _waterObject.AddComponent<MeshFilter>();
            wf.sharedMesh = _waterMesh;
            var wr = _waterObject.AddComponent<MeshRenderer>();
            wr.sharedMaterial = _waterMaterial ?? CreateDefaultWaterMaterial();
        }

        private void PrepareNodePositions(Dictionary<string, NodeComponent> nodes)
        {
            _nodePositions3D.Clear();

            foreach (var (id, node) in nodes)
            {
                var xz = CoordinateUtils.ToWorldXZ(node.X, node.Y);
                float elevation = GetTerrainElevation(node.Terrain);
                _nodePositions3D.Add(new NodeTerrainInfo
                {
                    x = xz.x,
                    z = xz.y,
                    terrain = node.Terrain ?? "PLAINS",
                    elevation = elevation * Constants.ElevationScale,
                    factionId = node.FactionId,
                });
            }
        }

        private (Mesh mesh, float[] heightMap) GenerateHeightMap()
        {
            int seg = _gridSize;
            float size = _worldSize;
            float half = size / 2f;

            int vertCount = (seg + 1) * (seg + 1);
            var vertices = new Vector3[vertCount];
            var colors = new Color[vertCount];
            var heightMap = new float[vertCount];
            var indices = new List<int>(seg * seg * 6);

            for (int iz = 0; iz <= seg; iz++)
            {
                for (int ix = 0; ix <= seg; ix++)
                {
                    int idx = iz * (seg + 1) + ix;
                    float wx = -half + (float)ix / seg * size;
                    float wz = -half + (float)iz / seg * size;

                    // IDW 插值高度
                    float height = 0f;
                    float weightSum = 0f;
                    string dominantTerrain = "PLAINS";
                    float maxWeight = 0f;

                    foreach (var node in _nodePositions3D)
                    {
                        float dx = wx - node.x;
                        float dz = wz - node.z;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);

                        if (dist < 0.01f)
                        {
                            height = node.elevation;
                            weightSum = 1f;
                            dominantTerrain = node.terrain;
                            maxWeight = 1f;
                            break;
                        }

                        float w = 1f / Mathf.Pow(dist, Constants.TerrainGen.HeightBlendPower);
                        height += w * node.elevation;
                        weightSum += w;

                        if (w > maxWeight)
                        {
                            maxWeight = w;
                            dominantTerrain = node.terrain;
                        }
                    }

                    if (weightSum > 0f) height /= weightSum;

                    // 添加噪声
                    height += SimplexNoise(wx * 0.3f, wz * 0.3f) * 0.3f;

                    heightMap[idx] = height;

                    // 顶点位置
                    vertices[idx] = new Vector3(wx, height, wz);

                    // 顶点颜色
                    Color terrainColor = GetTerrainColor(dominantTerrain);
                    float variation = SimplexNoise(wx * 0.5f, wz * 0.5f) * 0.05f;
                    colors[idx] = new Color(
                        Mathf.Clamp01(terrainColor.r + variation),
                        Mathf.Clamp01(terrainColor.g + variation),
                        Mathf.Clamp01(terrainColor.b + variation)
                    );
                }
            }

            // 索引
            for (int iz = 0; iz < seg; iz++)
            {
                for (int ix = 0; ix < seg; ix++)
                {
                    int a = iz * (seg + 1) + ix;
                    int b = a + 1;
                    int c = a + (seg + 1);
                    int d = c + 1;
                    indices.Add(a); indices.Add(c); indices.Add(b);
                    indices.Add(b); indices.Add(c); indices.Add(d);
                }
            }

            // 构建 Mesh
            var mesh = new Mesh();
            mesh.indexFormat = vertCount > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "TerrainHeightmap";

            return (mesh, heightMap);
        }

        private Mesh CreateWaterMesh()
        {
            float size = _worldSize * 1.5f;
            var mesh = new Mesh();
            var verts = new Vector3[]
            {
                new Vector3(-size / 2f, Constants.TerrainGen.WaterLevel, -size / 2f),
                new Vector3( size / 2f, Constants.TerrainGen.WaterLevel, -size / 2f),
                new Vector3(-size / 2f, Constants.TerrainGen.WaterLevel,  size / 2f),
                new Vector3( size / 2f, Constants.TerrainGen.WaterLevel,  size / 2f),
            };
            var tris = new int[] { 0, 2, 1, 1, 2, 3 };
            mesh.vertices = verts;
            mesh.SetIndices(tris, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.name = "Water";
            return mesh;
        }

        /// <summary>查询地面高度（双线性插值）</summary>
        public float GetHeightAt(float wx, float wz)
        {
            if (_heightData == null || _heightData.Length == 0) return 0f;

            int seg = _gridSize;
            float size = _worldSize;
            float half = size / 2f;

            // 世界坐标 → 网格坐标
            float gx = ((wx + half) / size) * seg;
            float gz = ((wz + half) / size) * seg;

            if (gx < 0 || gx >= seg || gz < 0 || gz >= seg) return 0f;

            int ix = Mathf.FloorToInt(gx);
            int iz = Mathf.FloorToInt(gz);
            float fx = gx - ix;
            float fz = gz - iz;

            int ix1 = Mathf.Min(ix + 1, seg);
            int iz1 = Mathf.Min(iz + 1, seg);

            int w = seg + 1;
            float h00 = _heightData[iz * w + ix];
            float h10 = _heightData[iz * w + ix1];
            float h01 = _heightData[iz1 * w + ix];
            float h11 = _heightData[iz1 * w + ix1];

            float h0 = h00 * (1f - fx) + h10 * fx;
            float h1 = h01 * (1f - fx) + h11 * fx;
            return h0 * (1f - fz) + h1 * fz;
        }

        /// <summary>简化 2D 伪随机噪声</summary>
        private static float SimplexNoise(float x, float y)
        {
            float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return (n - Mathf.Floor(n)) * 2f - 1f;
        }

        /// <summary>根据地形类型获取海拔</summary>
        private static float GetTerrainElevation(string terrainType)
        {
            foreach (var t in Constants.TerrainConfig)
            {
                if (t.Name == terrainType) return t.Elevation;
            }
            return 0f;
        }

        /// <summary>根据地形类型获取颜色</summary>
        private static Color GetTerrainColor(string terrainType)
        {
            foreach (var t in Constants.TerrainConfig)
            {
                if (t.Name == terrainType)
                    return ColorFromInt(t.Color);
            }
            return ColorFromInt(Constants.TerrainConfig[0].Color);
        }

        /// <summary>int 颜色 → Color</summary>
        private static Color ColorFromInt(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b);
        }

        private static Material CreateDefaultTerrainMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null)
            {
                mat.EnableKeyword("_VERTEX_COLORS");
                mat.SetFloat("_Glossiness", 0f);
            }
            return mat;
        }

        private static Material CreateDefaultWaterMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null)
            {
                mat.color = new Color(0x1E / 255f, 0x90 / 255f, 0xFF / 255f, 0.6f);
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_Glossiness", 0.3f);
                mat.SetFloat("_Metallic", 0.3f);
            }
            return mat;
        }

        /// <summary>清理所有地形对象</summary>
        public void Cleanup()
        {
            if (_groundObject != null) DestroyImmediate(_groundObject);
            if (_waterObject != null) DestroyImmediate(_waterObject);
            if (_groundMesh != null) DestroyImmediate(_groundMesh);
            if (_waterMesh != null) DestroyImmediate(_waterMesh);
            _heightData = null;
            _nodePositions3D.Clear();
        }

        private void OnDestroy() => Cleanup();
    }
}
