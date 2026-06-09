using System.Collections.Generic;
using GameState;
using UnityEngine;

/// <summary>
/// 军队渲染器 — 士兵模型 + 方阵 + 血条 + 移动插值
/// 移植自 client/src/map3d/army-renderer.js (273 行)
/// </summary>
namespace Rendering
{
    public class ArmyRenderer : MonoBehaviour
    {
        // entityId → root GameObject
        private readonly Dictionary<int, GameObject> _armyObjects = new();
        private TerrainGenerator _terrain;

        private void Awake()
        {
            _terrain = FindObjectOfType<TerrainGenerator>();
        }

        /// <summary>创建所有军队</summary>
        public void CreateAllArmies(Dictionary<int, ArmyComponent> armies)
        {
            if (armies == null) return;

            foreach (var (id, army) in armies)
            {
                var obj = CreateArmyObject(army);
                if (obj != null)
                {
                    obj.transform.SetParent(transform, false);
                    _armyObjects[id] = obj;
                }
            }
        }

        /// <summary>创建单个军队 3D 模型</summary>
        private GameObject CreateArmyObject(ArmyComponent army)
        {
            var root = new GameObject($"Army_{army.EntityId}");
            var data = root.AddComponent<EntityReference>();
            data.entityType = "army";
            data.entityId = army.EntityId;
            data.nodeId = army.FactionId;

            Color factionColor = GetFactionColor(army.FactionId);
            int strength = Mathf.Max(1, army.Strength > 0 ? army.Strength : army.TroopCount);
            int soldierCount = Mathf.Min(strength, Constants.ArmyConfig.MaxVisibleSoldiers);

            // 士兵方阵
            int cols = Mathf.CeilToInt(Mathf.Sqrt(soldierCount));
            for (int i = 0; i < soldierCount; i++)
            {
                var soldier = CreateSoldier(factionColor, army.UnitDefId);
                int row = i / cols;
                int col = i % cols;
                float offsetX = (col - cols / 2f) * Constants.ArmyConfig.FormationSpacing;
                float offsetZ = (row - Mathf.Ceil(soldierCount / (float)cols) / 2f) * Constants.ArmyConfig.FormationSpacing;
                soldier.transform.localPosition = new Vector3(offsetX, 0, offsetZ);
                root.transform.SetParent(root.transform, false);
            }

            // 血条
            var healthBar = CreateHealthBar(army);
            if (healthBar != null) healthBar.transform.SetParent(root.transform, false);

            // 设置初始位置
            UpdateArmyPosition(root, army);

            return root;
        }

        /// <summary>创建单个士兵模型</summary>
        private static GameObject CreateSoldier(Color factionColor, string unitDefId)
        {
            var soldier = new GameObject("Soldier");
            float h = Constants.ArmyConfig.SoldierHeight;
            float r = Constants.ArmyConfig.SoldierRadius;

            // 身体
            var bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bodyObj.name = "Body";
            DestroyImmediate(bodyObj.GetComponent<Collider>());
            bodyObj.transform.SetParent(soldier.transform, false);
            bodyObj.transform.localScale = new Vector3(r * 2f, h * 0.6f, r * 2f);
            bodyObj.transform.localPosition = new Vector3(0, h * 0.3f, 0);
            var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyMat.color = factionColor;
            bodyObj.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

            // 头
            var headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "Head";
            DestroyImmediate(headObj.GetComponent<Collider>());
            headObj.transform.SetParent(soldier.transform, false);
            headObj.transform.localScale = Vector3.one * r * 1.6f;
            headObj.transform.localPosition = new Vector3(0, h * 0.7f, 0);
            var headMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            headMat.color = new Color(0.96f, 0.82f, 0.66f);
            headObj.GetComponent<MeshRenderer>().sharedMaterial = headMat;

            // 武器
            if (unitDefId == "MUSKETEER" || unitDefId == "MAXIM_GUN")
            {
                // 枪
                var gunObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gunObj.name = "Gun";
                DestroyImmediate(gunObj.GetComponent<Collider>());
                gunObj.transform.SetParent(soldier.transform, false);
                gunObj.transform.localScale = new Vector3(0.04f, h * 0.5f, 0.04f);
                gunObj.transform.localPosition = new Vector3(r * 1.5f, h * 0.4f, 0);
                var gunMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                gunMat.color = new Color(0.29f, 0.29f, 0.29f);
                gunObj.GetComponent<MeshRenderer>().sharedMaterial = gunMat;
            }
            else
            {
                // 剑
                var swordObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                swordObj.name = "Sword";
                DestroyImmediate(swordObj.GetComponent<Collider>());
                swordObj.transform.SetParent(soldier.transform, false);
                swordObj.transform.localScale = new Vector3(0.04f, h * 0.4f, 0.08f);
                swordObj.transform.localPosition = new Vector3(r * 1.5f, h * 0.3f, 0);
                var swordMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                swordMat.color = new Color(0.75f, 0.75f, 0.75f);
                swordMat.SetFloat("_Glossiness", 0.8f);
                swordObj.GetComponent<MeshRenderer>().sharedMaterial = swordMat;
            }

            return soldier;
        }

        /// <summary>创建血条</summary>
        private static GameObject CreateHealthBar(ArmyComponent army)
        {
            var root = new GameObject("HealthBar");
            float w = Constants.ArmyConfig.HealthBarWidth;
            float h = Constants.ArmyConfig.HealthBarHeight;
            float y = Constants.ArmyConfig.SoldierHeight + 0.3f;

            // 背景
            var bgObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            bgObj.name = "BG";
            DestroyImmediate(bgObj.GetComponent<Collider>());
            bgObj.transform.SetParent(root.transform, false);
            bgObj.transform.localScale = new Vector3(w / 10f, 1, h / 10f);
            bgObj.transform.localPosition = new Vector3(0, y, 0);
            bgObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            bgMat.color = new Color(0.2f, 0.2f, 0.2f);
            bgMat.SetFloat("_Cull", 0f);
            bgObj.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

            // 前景（HP 比例）
            float hpRatio = Mathf.Clamp01(army.Morale > 0 ? army.Morale : 1f);
            var fgObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fgObj.name = "FG";
            DestroyImmediate(fgObj.GetComponent<Collider>());
            fgObj.transform.SetParent(root.transform, false);
            fgObj.transform.localScale = new Vector3(w * hpRatio / 10f, 1, h / 10f);
            fgObj.transform.localPosition = new Vector3(-(w * (1 - hpRatio)) / 2f, y, 0.001f);
            fgObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            var fgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            fgMat.color = hpRatio > 0.5f ? Color.green : Color.red;
            fgMat.SetFloat("_Cull", 0f);
            fgObj.GetComponent<MeshRenderer>().sharedMaterial = fgMat;

            return root;
        }

        /// <summary>从数据更新军队位置</summary>
        public void UpdateArmyPosition(GameObject root, ArmyComponent army)
        {
            if (root == null || _terrain == null) return;

            var nodes = FindObjectOfType<Networking.StateStore>()?.Nodes;
            if (nodes == null) return;

            float GetHeight(float wx, float wz) => _terrain.GetHeightAt(wx, wz) + 0.05f;

            if (!string.IsNullOrEmpty(army.CurrentEdgeId))
            {
                var edgeStore = FindObjectOfType<Networking.StateStore>()?.Edges;
                if (edgeStore != null && edgeStore.TryGetValue(army.CurrentEdgeId, out var edge))
                {
                    if (nodes.TryGetValue(edge.SourceNodeId, out var src) &&
                        nodes.TryGetValue(edge.TargetNodeId, out var tgt))
                    {
                        var srcW = CoordinateUtils.ToWorldXZ(src.X, src.Y);
                        var tgtW = CoordinateUtils.ToWorldXZ(tgt.X, tgt.Y);
                        float progress = Mathf.Clamp01(army.EdgeProgress);
                        bool isReverse = army.TargetNodeId == edge.SourceNodeId;
                        float t = isReverse ? (1f - progress) : progress;

                        float wx = srcW.x + (tgtW.x - srcW.x) * t;
                        float wz = srcW.y + (tgtW.y - srcW.y) * t;
                        float wy = GetHeight(wx, wz);

                        root.transform.position = new Vector3(wx, wy, wz);

                        // 朝向
                        float dx = tgtW.x - srcW.x;
                        float dz = tgtW.y - srcW.y;
                        float angle = Mathf.Atan2(dx, dz) + (isReverse ? Mathf.PI : 0f);
                        root.transform.rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(army.CurrentNodeId))
            {
                if (nodes.TryGetValue(army.CurrentNodeId, out var node))
                {
                    var xz = CoordinateUtils.ToWorldXZ(node.X, node.Y);
                    float y = GetHeight(xz.x, xz.y);

                    // 多军队同节点时分散
                    var armiesAtNode = GetOtherArmiesAtNode(army.CurrentNodeId, army.EntityId);
                    int idx = armiesAtNode.IndexOf(army.EntityId);
                    int count = armiesAtNode.Count;

                    if (count > 1)
                    {
                        float angle = (float)idx / count * Mathf.PI * 2f;
                        float radius = 1.5f;
                        root.transform.position = new Vector3(
                            xz.x + Mathf.Cos(angle) * radius,
                            y,
                            xz.y + Mathf.Sin(angle) * radius
                        );
                    }
                    else
                    {
                        root.transform.position = new Vector3(xz.x, y, xz.y);
                    }
                }
            }
        }

        private List<int> GetOtherArmiesAtNode(string nodeId, int excludeEntityId)
        {
            var store = FindObjectOfType<Networking.StateStore>();
            if (store?.Armies == null) return new List<int>();

            var result = new List<int>();
            foreach (var (id, a) in store.Armies)
            {
                if (a.CurrentNodeId == nodeId && string.IsNullOrEmpty(a.CurrentEdgeId))
                {
                    if (!result.Contains(id)) result.Add(id);
                }
            }
            return result;
        }

        /// <summary>刷新所有军队（每帧调用）</summary>
        public void RefreshArmies(Dictionary<int, ArmyComponent> armies)
        {
            if (armies == null) return;

            // 移除已消失的
            var toRemove = new List<int>();
            foreach (var (id, obj) in _armyObjects)
            {
                if (!armies.ContainsKey(id))
                    toRemove.Add(id);
            }
            foreach (var id in toRemove)
                RemoveArmy(id);

            // 更新或创建
            foreach (var (id, army) in armies)
            {
                if (_armyObjects.TryGetValue(id, out var obj))
                {
                    UpdateArmyPosition(obj, army);
                }
                else
                {
                    var newObj = CreateArmyObject(army);
                    if (newObj != null)
                    {
                        newObj.transform.SetParent(transform, false);
                        _armyObjects[id] = newObj;
                    }
                }
            }
        }

        private void RemoveArmy(int entityId)
        {
            if (_armyObjects.TryGetValue(entityId, out var obj))
            {
                Destroy(obj);
                _armyObjects.Remove(entityId);
            }
        }

        /// <summary>清理所有军队</summary>
        public void ClearAllArmies()
        {
            foreach (var (id, obj) in _armyObjects)
                Destroy(obj);
            _armyObjects.Clear();
        }

        private void OnDestroy() => ClearAllArmies();

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
    }
}
