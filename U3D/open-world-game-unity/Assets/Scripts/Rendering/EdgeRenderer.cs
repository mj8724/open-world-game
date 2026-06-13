using System.Collections.Generic;
using GameState;
using UnityEngine;
using Rendering;

/// <summary>
/// 道路渲染器 — ROAD / TRAIL / RAILWAY 3D 路径
/// 移植自 client/src/map3d/edge-renderer.js
/// </summary>
namespace Rendering
{
    public class EdgeRenderer : MonoBehaviour
    {
        // edgeId → root GameObject
        private readonly Dictionary<string, GameObject> _edgeObjects = new();
        private TerrainGenerator _terrain;

        private void Awake()
        {
            _terrain = FindObjectOfType<TerrainGenerator>();
        }

        /// <summary>创建所有道路</summary>
        public void CreateAllEdges(Dictionary<string, EdgeComponent> edges, Dictionary<string, NodeComponent> nodes)
        {
            ClearAllEdges();

            if (edges == null || nodes == null) return;

            foreach (var (id, edge) in edges)
            {
                var obj = CreateEdgeMesh(edge, nodes);
                if (obj != null)
                {
                    obj.transform.SetParent(transform, false);
                    _edgeObjects[id] = obj;
                }
            }
        }

        /// <summary>创建单条道路 3D 网格</summary>
        private GameObject CreateEdgeMesh(EdgeComponent edge, Dictionary<string, NodeComponent> nodes)
        {
            if (!nodes.TryGetValue(edge.SourceNodeId, out var srcNode) ||
                !nodes.TryGetValue(edge.TargetNodeId, out var tgtNode))
                return null;

            var src = CoordinateUtils.ToWorldXZ(srcNode.X, srcNode.Y);
            var tgt = CoordinateUtils.ToWorldXZ(tgtNode.X, tgtNode.Y);

            string edgeType = edge.EdgeType ?? "ROAD";
            var config = GetRoadConfig(edgeType);

            int segments = config?.Segments ?? 20;

            // 生成沿地形表面的路径点
            var pathPoints = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float wx = src.x + (tgt.x - src.x) * t;
                float wz = src.y + (tgt.y - src.y) * t;
                float wy = (_terrain?.GetHeightAt(wx, wz) ?? 0f) + 0.02f;
                pathPoints[i] = new Vector3(wx, wy, wz);
            }

            var root = new GameObject($"Edge_{edge.Id}");
            root.transform.SetParent(transform, false);

            if (edgeType == "RAILWAY")
            {
                CreateRailwayMesh(root, pathPoints, config);
            }
            else
            {
                CreateRoadMesh(root, pathPoints, config);
            }

            return root;
        }

        /// <summary>创建普通道路</summary>
        private static void CreateRoadMesh(GameObject root, Vector3[] pathPoints, RoadConfigEntry? config)
        {
            if (pathPoints.Length < 2) return;

            float width = config?.Width ?? 0.3f;
            var color = ColorFromInt(config?.Color ?? 0x8B7355);

            // 使用分段四边形沿路径生成路面
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                var p0 = pathPoints[i];
                var p1 = pathPoints[i + 1];
                var dir = (p1 - p0).normalized;
                var right = Vector3.Cross(Vector3.up, dir).normalized * (width / 2f);

                var segment = new GameObject($"Segment_{i}");
                segment.transform.SetParent(root.transform, false);

                var mf = segment.AddComponent<MeshFilter>();
                var mr = segment.AddComponent<MeshRenderer>();

                var mesh = new Mesh { name = $"RoadSeg_{i}" };
                mesh.MarkDynamic();
                var verts = new Vector3[]
                {
                    p0 - right,
                    p0 + right,
                    p1 - right,
                    p1 + right,
                };
                // 转换为局部坐标
                for (int v = 0; v < verts.Length; v++)
                    verts[v] -= root.transform.position;

                mesh.vertices = verts;
                mesh.SetIndices(new int[] { 0, 2, 1, 1, 2, 3 }, MeshTopology.Triangles, 0);
                mesh.RecalculateNormals();
                mf.sharedMesh = mesh;

                mr.sharedMaterial = MaterialCache.GetLitWithParams(color, 0.1f, 0f);
            }
        }

        /// <summary>创建铁路：两条铁轨 + 枕木</summary>
        private static void CreateRailwayMesh(GameObject root, Vector3[] pathPoints, RoadConfigEntry? config)
        {
            float width = config?.Width ?? 0.25f;
            float railWidth = 0.03f;
            float tieSpacing = 0.3f;
            var railColor = ColorFromInt(0x666666);
            var tieColor = ColorFromInt(0x6b4f3a);

            // 两条铁轨
            foreach (var side in new[] { -1f, 1f })
            {
                var railObj = new GameObject($"Rail_{(side > 0 ? "Right" : "Left")}");
                railObj.transform.SetParent(root.transform, false);

                var mf = railObj.AddComponent<MeshFilter>();
                var mr = railObj.AddComponent<MeshRenderer>();

                for (int i = 0; i < pathPoints.Length - 1; i++)
                {
                    var p0 = pathPoints[i];
                    var p1 = pathPoints[i + 1];
                    var dir = (p1 - p0).normalized;
                    var right = Vector3.Cross(Vector3.up, dir).normalized * (width / 2f) * side;

                    // 每段使用长方体
                    var segObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    segObj.name = $"RailSeg_{i}";
                    DestroyImmediate(segObj.GetComponent<Collider>());
                    segObj.transform.SetParent(railObj.transform, false);

                    float len = Vector3.Distance(p0, p1);
                    Vector3 mid = (p0 + p1) / 2f + right;
                    segObj.transform.position = mid;
                    segObj.transform.localScale = new Vector3(railWidth, railWidth, len);
                    segObj.transform.LookAt(mid + dir);

                    segObj.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetLitWithParams(railColor, 0.2f, 0f);
                }
            }

            // 枕木
            float totalLength = 0f;
            for (int i = 0; i < pathPoints.Length - 1; i++)
                totalLength += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);

            int tieCount = Mathf.FloorToInt(totalLength / tieSpacing);
            var tieMat = MaterialCache.GetLit(tieColor);

            for (int i = 0; i <= tieCount; i++)
            {
                float t = (float)i / Mathf.Max(1, tieCount);
                Vector3 pos = EvaluatePath(pathPoints, t);

                var tieObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tieObj.name = $"Tie_{i}";
                DestroyImmediate(tieObj.GetComponent<Collider>());
                tieObj.transform.SetParent(root.transform, false);
                tieObj.transform.position = pos + Vector3.up * 0.02f;
                tieObj.transform.localScale = new Vector3(width * 1.2f, 0.04f, 0.08f);
                tieObj.GetComponent<MeshRenderer>().sharedMaterial = tieMat;
            }
        }

        /// <summary>沿路径求值（线性插值）</summary>
        private static Vector3 EvaluatePath(Vector3[] points, float t)
        {
            if (points.Length == 0) return Vector3.zero;
            if (points.Length == 1 || t <= 0f) return points[0];
            if (t >= 1f) return points[^1];

            float totalLength = 0f;
            var lengths = new float[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                lengths[i] = Vector3.Distance(points[i], points[i + 1]);
                totalLength += lengths[i];
            }

            float targetDist = t * totalLength;
            float accumulated = 0f;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (accumulated + lengths[i] >= targetDist)
                {
                    float segT = (targetDist - accumulated) / lengths[i];
                    return Vector3.Lerp(points[i], points[i + 1], segT);
                }
                accumulated += lengths[i];
            }

            return points[^1];
        }

        /// <summary>清理所有道路</summary>
        public void ClearAllEdges()
        {
            foreach (var (id, obj) in _edgeObjects)
                Destroy(obj);
            _edgeObjects.Clear();
        }

        private void OnDestroy() => ClearAllEdges();

        // ─── 辅助 ───

        private static RoadConfigEntry? GetRoadConfig(string edgeType)
        {
            foreach (var r in Constants.RoadConfigs)
            {
                if (r.Type == edgeType) return r;
            }
            return null;
        }

        private static Color ColorFromInt(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b);
        }
    }
}
