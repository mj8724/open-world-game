using System.Collections.Generic;
using GameState;
using UnityEngine;

/// <summary>
/// 城市节点渲染器 — 3D 建筑实例 + 城墙段 + 旗帜 + 选中高亮
/// 移植自 client/src/map3d/node-renderer.js (410 行)
/// </summary>
namespace Rendering
{
    public class NodeRenderer : MonoBehaviour
    {
        // nodeId → root GameObject
        private readonly Dictionary<string, GameObject> _nodeObjects = new();
        private string _selectedNodeId;

        [Header("预制体引用（可选，默认使用程序化几何体）")]
        [SerializeField] private GameObject _buildingPrefab;
        [SerializeField] private GameObject _soldierPrefab;

        [Header("控制参数")]
        [SerializeField] private bool _useLegacyFallback = true;

        private TerrainGenerator _terrain;

        private void Awake()
        {
            _terrain = FindObjectOfType<TerrainGenerator>();
        }

        /// <summary>为所有节点创建 3D 定居点</summary>
        public void CreateAllSettlements(Dictionary<string, NodeComponent> nodes)
        {
            ClearAllSettlements();

            if (nodes == null) return;

            foreach (var (id, node) in nodes)
            {
                var obj = CreateSettlement(node);
                if (obj != null)
                {
                    obj.transform.SetParent(transform, false);
                    _nodeObjects[id] = obj;
                }
            }
        }

        /// <summary>创建单个城市定居点</summary>
        private GameObject CreateSettlement(NodeComponent node)
        {
            var xz = CoordinateUtils.ToWorldXZ(node.X, node.Y);
            float y = _terrain != null ? _terrain.GetHeightAt(xz.x, xz.y) : 0f;

            var root = new GameObject($"Node_{node.Id}");
            root.transform.position = new Vector3(xz.x, y, xz.y);
            root.tag = "NodeSettlement";
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = Constants.CityConfig.InfluenceRadius;
            root.layer = LayerMask.NameToLayer("Default");

            // 存储引用数据供选中检测
            var data = root.AddComponent<EntityReference>();
            data.entityType = "node";
            data.nodeId = node.Id;

            // 1. 城市平台（势力色半透明圆盘）
            CreatePlatform(root, node);

            // 2. 建筑实例
            if (node.PlacedBuildings != null && node.PlacedBuildings.Count > 0)
            {
                foreach (var bld in node.PlacedBuildings)
                {
                    var bldObj = CreateBuildingMesh(bld);
                    if (bldObj != null)
                        bldObj.transform.SetParent(root.transform, false);
                }
            }
            else if (_useLegacyFallback)
            {
                AddLegacyBuildings(root, node);
            }

            // 3. 城墙段
            if (node.WallSegments != null && node.WallSegments.Count > 0)
            {
                foreach (var seg in node.WallSegments)
                {
                    var wallObj = CreateWallSegment(seg, node);
                    if (wallObj != null)
                        wallObj.transform.SetParent(root.transform, false);
                }
            }
            else if (_useLegacyFallback && node.WallLevel > 0)
            {
                AddLegacyWall(root, node);
            }

            // 4. 势力旗帜
            var flag = CreateFlag(GetFactionColor(node.FactionId));
            if (flag != null)
                flag.transform.SetParent(root.transform, false);

            // 5. 选中高亮环（初始隐藏）
            var ring = CreateSelectionRing();
            ring.name = "SelectionRing";
            ring.SetActive(false);
            ring.transform.SetParent(root.transform, false);

            return root;
        }

        /// <summary>创建城市平台</summary>
        private static void CreatePlatform(GameObject root, NodeComponent node)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Platform";
            DestroyImmediate(platform.GetComponent<Collider>());

            float radius = Constants.CityConfig.InfluenceRadius;
            float height = Constants.CityConfig.PlatformHeight;
            platform.transform.localScale = new Vector3(radius * 2f / 1f, height, radius * 2f / 1f);
            platform.transform.localPosition = new Vector3(0, height / 2f, 0);

            var renderer = platform.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = GetFactionColor(node.FactionId);
            mat.SetFloat("_Surface", 1f); // Transparent
            var color = mat.color;
            color.a = 0.15f;
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        /// <summary>创建单个建筑网格</summary>
        private static GameObject CreateBuildingMesh(PlacedBuilding bld)
        {
            var model = GetBuildingModel(bld.BuildingType);
            if (model == null) return null;

            int level = Mathf.Max(1, bld.Level);
            float h = model.Value.HeightBase + model.Value.HeightPerLevel * (level - 1);

            var root = new GameObject($"Building_{bld.BuildingType}_{level}");

            // 底座
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base";
            DestroyImmediate(baseObj.GetComponent<Collider>());
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(model.Value.Width, h, model.Value.Depth);
            baseObj.transform.localPosition = new Vector3(0, h / 2f, 0);
            var baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            baseMat.color = ColorFromInt(model.Value.Color);
            baseMat.SetFloat("_Glossiness", 0.2f);
            baseObj.GetComponent<MeshRenderer>().sharedMaterial = baseMat;

            // 屋顶（四棱锥）
            if (model.Value.RoofColor != 0)
            {
                var roofObj = CreatePyramid(
                    Mathf.Max(model.Value.Width, model.Value.Depth) * 0.7f,
                    h * 0.4f
                );
                roofObj.name = "Roof";
                roofObj.transform.SetParent(root.transform, false);
                roofObj.transform.localPosition = new Vector3(0, h + h * 0.2f, 0);
                roofObj.transform.localRotation = Quaternion.Euler(0, 45f, 0);
                var roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                roofMat.color = ColorFromInt(model.Value.RoofColor);
                roofMat.SetFloat("_Glossiness", 0.3f);
                roofObj.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
            }

            // 特殊装饰
            if (bld.BuildingType == "ORACLE_BEACON")
            {
                var glowObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glowObj.name = "Glow";
                DestroyImmediate(glowObj.GetComponent<Collider>());
                glowObj.transform.SetParent(root.transform, false);
                glowObj.transform.localScale = Vector3.one * 0.3f;
                glowObj.transform.localPosition = new Vector3(0, h + 0.3f, 0);
                var glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                glowMat.color = ColorFromInt(0xFFD700);
                glowMat.EnableKeyword("_EMISSION");
                glowMat.SetColor("_EmissionColor", ColorFromInt(0xFFD700) * 0.5f);
                glowObj.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            }

            if (bld.BuildingType == "ARSENAL")
            {
                var chimneyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                chimneyObj.name = "Chimney";
                DestroyImmediate(chimneyObj.GetComponent<Collider>());
                chimneyObj.transform.SetParent(root.transform, false);
                chimneyObj.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                chimneyObj.transform.localPosition = new Vector3(model.Value.Width * 0.3f, h + 0.15f, 0);
                var chimneyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                chimneyMat.color = new Color(0.2f, 0.2f, 0.2f);
                chimneyObj.GetComponent<MeshRenderer>().sharedMaterial = chimneyMat;
            }

            // 放置位置
            root.transform.localPosition = new Vector3(bld.LocalX, 0, bld.LocalZ);
            root.transform.localRotation = Quaternion.Euler(0, bld.Rotation, 0);

            return root;
        }

        /// <summary>创建四棱锥网格</summary>
        private static GameObject CreatePyramid(float baseSize, float height)
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                // 底面 4 个顶点
                new Vector3(-baseSize / 2f, 0, -baseSize / 2f),
                new Vector3( baseSize / 2f, 0, -baseSize / 2f),
                new Vector3( baseSize / 2f, 0,  baseSize / 2f),
                new Vector3(-baseSize / 2f, 0,  baseSize / 2f),
                // 顶点
                new Vector3(0, height, 0),
            };
            var triangles = new int[]
            {
                0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4, // 侧面
                0, 2, 1, 0, 3, 2 // 底面
            };
            mesh.vertices = vertices;
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            var obj = new GameObject("Pyramid");
            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>();
            return obj;
        }

        /// <summary>创建城墙段</summary>
        private static GameObject CreateWallSegment(WallSegment seg, NodeComponent node)
        {
            float dx = seg.ToX - seg.FromX;
            float dz = seg.ToZ - seg.FromZ;
            float length = Mathf.Sqrt(dx * dx + dz * dz);
            float angle = Mathf.Atan2(dx, dz);
            int level = Mathf.Max(1, seg.Level);
            float h = Constants.WallConfig.HeightBase + Constants.WallConfig.HeightPerLevel * (level - 1);

            var root = new GameObject("WallSegment");

            // 城墙主体
            var wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObj.name = "Wall";
            DestroyImmediate(wallObj.GetComponent<Collider>());
            wallObj.transform.SetParent(root.transform, false);
            wallObj.transform.localScale = new Vector3(Constants.WallConfig.Thickness, h, length);
            wallObj.transform.localPosition = new Vector3(0, h / 2f, 0);
            var wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMat.color = new Color(0.75f, 0.75f, 0.75f);
            wallMat.SetFloat("_Glossiness", 0.15f);
            wallObj.GetComponent<MeshRenderer>().sharedMaterial = wallMat;

            // 垛口
            int battlementCount = Mathf.FloorToInt(length / Constants.WallConfig.BattlementSpacing);
            for (int i = 0; i < battlementCount; i++)
            {
                var bObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bObj.name = $"Battlement_{i}";
                DestroyImmediate(bObj.GetComponent<Collider>());
                bObj.transform.SetParent(root.transform, false);
                bObj.transform.localScale = new Vector3(
                    Constants.WallConfig.Thickness + 0.04f,
                    Constants.WallConfig.BattlementHeight,
                    Constants.WallConfig.BattlementWidth
                );
                float z = -length / 2f + (i + 0.5f) * (length / battlementCount);
                bObj.transform.localPosition = new Vector3(0, h + Constants.WallConfig.BattlementHeight / 2f, z);
                bObj.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            }

            // 中点位置
            float midX = (seg.FromX + seg.ToX) / 2f;
            float midZ = (seg.FromZ + seg.ToZ) / 2f;
            root.transform.localPosition = new Vector3(midX, 0, midZ);
            root.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

            return root;
        }

        /// <summary>创建势力旗帜</summary>
        private static GameObject CreateFlag(Color color)
        {
            var root = new GameObject("Flag");

            // 旗杆
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = "Pole";
            DestroyImmediate(poleObj.GetComponent<Collider>());
            poleObj.transform.SetParent(root.transform, false);
            poleObj.transform.localScale = new Vector3(0.04f, Constants.CityConfig.FlagPoleHeight, 0.04f);
            poleObj.transform.localPosition = new Vector3(0, Constants.CityConfig.FlagPoleHeight / 2f, 0);
            var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            poleMat.color = new Color(0.54f, 0.27f, 0.07f);
            poleObj.GetComponent<MeshRenderer>().sharedMaterial = poleMat;

            // 旗面（使用 Plane）
            var flagObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            flagObj.name = "FlagMesh";
            DestroyImmediate(flagObj.GetComponent<Collider>());
            flagObj.transform.SetParent(root.transform, false);
            float size = Constants.CityConfig.FlagSize;
            flagObj.transform.localScale = new Vector3(size / 10f, 1f, size * 0.6f / 10f);
            flagObj.transform.localPosition = new Vector3(size / 2f, Constants.CityConfig.FlagPoleHeight - size * 0.3f, 0);
            var flagMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            flagMat.color = color;
            flagMat.SetFloat("_Cull", 0f); // Double side
            flagObj.GetComponent<MeshRenderer>().sharedMaterial = flagMat;

            return root;
        }

        /// <summary>创建选中高亮环</summary>
        private static GameObject CreateSelectionRing()
        {
            var ring = new GameObject("SelectionRing");
            var mr = ring.AddComponent<MeshRenderer>();
            var mf = ring.AddComponent<MeshFilter>();

            float outerR = Constants.CityConfig.SelectionRingRadius;
            float innerR = outerR - 0.1f;

            // 环形网格
            var mesh = new Mesh();
            int segments = 32;
            var verts = new Vector3[segments * 2];
            var tris = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float nextAngle = (float)(i + 1) / segments * Mathf.PI * 2f;

                float cos1 = Mathf.Cos(angle);
                float sin1 = Mathf.Sin(angle);
                float cos2 = Mathf.Cos(nextAngle);
                float sin2 = Mathf.Sin(nextAngle);

                verts[i * 2] = new Vector3(cos1 * innerR, 0, sin1 * innerR);
                verts[i * 2 + 1] = new Vector3(cos1 * outerR, 0, sin1 * outerR);

                int vi = i * 6;
                tris[vi] = i * 2;
                tris[vi + 1] = (i * 2 + 2) % (segments * 2);
                tris[vi + 2] = i * 2 + 1;
                tris[vi + 3] = i * 2 + 1;
                tris[vi + 4] = (i * 2 + 2) % (segments * 2);
                tris[vi + 5] = (i * 2 + 3) % (segments * 2);
            }

            mesh.vertices = verts;
            mesh.SetIndices(tris, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.15f, 0.39f, 0.92f, 0.5f);
            mat.SetFloat("_Surface", 1f); // Transparent
            mr.sharedMaterial = mat;

            ring.transform.localPosition = new Vector3(0, 0.1f, 0);
            return ring;
        }

        /// <summary>兼容旧数据：从 farmLevel/mineLevel 等生成默认建筑布局</summary>
        private static void AddLegacyBuildings(GameObject root, NodeComponent node)
        {
            var buildings = new List<(string type, int level, Vector3 pos)>();
            if (node.FarmLevel > 0) buildings.Add(("FARM", node.FarmLevel, new Vector3(-1.5f, 0, 1.5f)));
            if (node.MineLevel > 0) buildings.Add(("MINE", node.MineLevel, new Vector3(1.5f, 0, 1.5f)));
            if (node.ArsenalLevel > 0) buildings.Add(("ARSENAL", node.ArsenalLevel, new Vector3(-1.5f, 0, -1.5f)));
            if (node.BeaconLevel > 0) buildings.Add(("ORACLE_BEACON", node.BeaconLevel, new Vector3(1.5f, 0, -1.5f)));

            // 市政厅（首都有）
            if (node.IsCapital)
            {
                var hall = new PlacedBuilding { BuildingType = "HALL", Level = 1, LocalX = 0, LocalZ = 0, Rotation = 0 };
                var hallObj = CreateBuildingMesh(hall);
                if (hallObj != null) hallObj.transform.SetParent(root.transform, false);
            }

            foreach (var b in buildings)
            {
                var placed = new PlacedBuilding
                {
                    BuildingType = b.type,
                    Level = b.level,
                    LocalX = b.pos.x,
                    LocalZ = b.pos.z,
                    Rotation = 0,
                };
                var obj = CreateBuildingMesh(placed);
                if (obj != null) obj.transform.SetParent(root.transform, false);
            }
        }

        /// <summary>兼容旧数据：默认圆形城墙</summary>
        private static void AddLegacyWall(GameObject root, NodeComponent node)
        {
            int level = Mathf.Max(1, node.WallLevel);
            float h = Constants.WallConfig.HeightBase + Constants.WallConfig.HeightPerLevel * (level - 1);
            float radius = 3f;
            int segments = 12;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float nextAngle = (float)(i + 1) / segments * Mathf.PI * 2f;

                float x1 = Mathf.Cos(angle) * radius;
                float z1 = Mathf.Sin(angle) * radius;
                float x2 = Mathf.Cos(nextAngle) * radius;
                float z2 = Mathf.Sin(nextAngle) * radius;

                float dx = x2 - x1;
                float dz = z2 - z1;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float midAngle = Mathf.Atan2(dx, dz);

                var wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(wallObj.GetComponent<Collider>());
                wallObj.transform.SetParent(root.transform, false);
                wallObj.transform.localScale = new Vector3(Constants.WallConfig.Thickness, h, len);
                wallObj.transform.localPosition = new Vector3((x1 + x2) / 2f, h / 2f, (z1 + z2) / 2f);
                wallObj.transform.localRotation = Quaternion.Euler(0, midAngle * Mathf.Rad2Deg, 0);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.75f, 0.75f, 0.75f);
                wallObj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        // ─── 更新 ───

        /// <summary>更新单个节点的 3D 渲染</summary>
        public void UpdateNodeSettlement(string nodeId, NodeComponent node)
        {
            if (node == null) return;

            // 移除旧的
            if (_nodeObjects.TryGetValue(nodeId, out var existing))
            {
                Destroy(existing);
                _nodeObjects.Remove(nodeId);
            }

            // 重建
            var obj = CreateSettlement(node);
            if (obj != null)
            {
                obj.transform.SetParent(transform, false);
                _nodeObjects[nodeId] = obj;
            }
        }

        /// <summary>设置节点选中/取消</summary>
        public void SetNodeSelected(string nodeId, bool selected)
        {
            _selectedNodeId = selected ? nodeId : null;

            foreach (var (id, obj) in _nodeObjects)
            {
                var ring = obj.transform.Find("SelectionRing");
                if (ring != null)
                    ring.gameObject.SetActive(id == nodeId && selected);
            }
        }

        /// <summary>获取节点 3D 位置</summary>
        public Vector3? GetNodePosition3D(string nodeId)
        {
            if (_nodeObjects.TryGetValue(nodeId, out var obj))
                return obj.transform.position;
            return null;
        }

        /// <summary>获取节点 GameObject</summary>
        public GameObject GetNodeObject(string nodeId)
        {
            _nodeObjects.TryGetValue(nodeId, out var obj);
            return obj;
        }

        /// <summary>清理所有定居点</summary>
        public void ClearAllSettlements()
        {
            foreach (var (id, obj) in _nodeObjects)
                Destroy(obj);
            _nodeObjects.Clear();
            _selectedNodeId = null;
        }

        private void OnDestroy() => ClearAllSettlements();

        // ─── 辅助 ───

        private static Color GetFactionColor(string factionId)
        {
            return factionId switch
            {
                "PLAYER" => ColorFromInt(0x2563EB),
                "AI" => ColorFromInt(0xDC2626),
                _ => ColorFromInt(0x9CA3AF),
            };
        }

        private static Color ColorFromInt(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b);
        }

        private static BuildingModelConfig? GetBuildingModel(string buildingType)
        {
            foreach (var m in Constants.BuildingModels)
            {
                if (m.Id == buildingType) return m;
            }
            return null;
        }
    }
}
