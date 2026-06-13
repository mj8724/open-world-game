using System.Collections.Generic;
using GameState;
using UnityEngine;
using Rendering;

/// <summary>
/// 物流渲染器 — 运输车模型 + 路线可视化
/// 移植自 client/src/map3d/logistics-renderer.js
/// </summary>
namespace Rendering
{
    public class LogisticsRenderer : MonoBehaviour
    {
        // entityId → { root, cart }
        private readonly Dictionary<int, LogisticsVisual> _logisticsObjects = new();
        private TerrainGenerator _terrain;

        private class LogisticsVisual
        {
            public GameObject root;
            public GameObject cart;
        }

        private void Awake()
        {
            _terrain = FindObjectOfType<TerrainGenerator>();
        }

        /// <summary>刷新所有物流可视化</summary>
        public void RefreshLogistics(Dictionary<int, LogisticsComponent> logistics)
        {
            if (logistics == null) return;

            // 移除已消失的
            var toRemove = new List<int>();
            foreach (var (id, _) in _logisticsObjects)
            {
                if (!logistics.ContainsKey(id))
                    toRemove.Add(id);
            }
            foreach (var id in toRemove)
                RemoveLogistics(id);

            // 更新或创建
            foreach (var (id, route) in logistics)
            {
                if (!_logisticsObjects.ContainsKey(id))
                {
                    var vis = CreateLogisticsVisual(route);
                    if (vis != null)
                    {
                        vis.root.transform.SetParent(transform, false);
                        _logisticsObjects[id] = vis;
                    }
                }

                if (_logisticsObjects.TryGetValue(id, out var existing))
                {
                    UpdateCartPosition(existing.cart, route);
                }
            }
        }

        /// <summary>创建物流路线可视化</summary>
        private LogisticsVisual CreateLogisticsVisual(LogisticsComponent route)
        {
            var root = new GameObject($"Logistics_{route.EntityId}");

            var nodes = global::GameApp.Instance?.State?.Nodes;
            var pathPoints = new List<Vector3>();

            if (route.PathNodeIds != null && nodes != null)
            {
                foreach (var nodeId in route.PathNodeIds)
                {
                    if (nodes.TryGetValue(nodeId, out var node))
                    {
                        var xz = CoordinateUtils.ToWorldXZ(node.X, node.Y);
                        float y = (_terrain?.GetHeightAt(xz.x, xz.y) ?? 0f) + 0.05f;
                        pathPoints.Add(new Vector3(xz.x, y, xz.y));
                    }
                }
            }

            // 路线线
            if (pathPoints.Count >= 2)
            {
                var lineObj = new GameObject("RouteLine");
                lineObj.transform.SetParent(root.transform, false);

                var lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = pathPoints.Count;
                lr.SetPositions(pathPoints.ToArray());
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                Color lineColor = route.Mode == "AUTO"
                    ? new Color(0.09f, 0.63f, 0.29f, 0.6f)
                    : new Color(0.15f, 0.39f, 0.92f, 0.6f);
                lr.startColor = lineColor;
                lr.endColor = lineColor;
                lr.material = MaterialCache.GetUnlit(lineColor);
                lr.material.color = lineColor;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // 运输车
            var cart = CreateCart(route);
            if (cart != null)
                cart.transform.SetParent(root.transform, false);

            return new LogisticsVisual { root = root, cart = cart };
        }

        /// <summary>创建运输车模型</summary>
        private static GameObject CreateCart(LogisticsComponent route)
        {
            var cart = new GameObject("Cart");

            // 车体
            var bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObj.name = "Body";
            DestroyImmediate(bodyObj.GetComponent<Collider>());
            bodyObj.transform.SetParent(cart.transform, false);
            bodyObj.transform.localScale = new Vector3(0.3f, 0.15f, 0.2f);
            bodyObj.transform.localPosition = new Vector3(0, 0.12f, 0);
            bodyObj.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetLit(new Color(0.54f, 0.45f, 0.33f));

            // 车轮
            var wheelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheelObj.name = "Wheel";
            DestroyImmediate(wheelObj.GetComponent<Collider>());
            wheelObj.transform.SetParent(cart.transform, false);
            wheelObj.transform.localScale = new Vector3(0.06f, 0.02f, 0.06f);
            wheelObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var wheelMat = MaterialCache.GetLit(new Color(0.2f, 0.2f, 0.2f));

            foreach (var (wx, wz) in new[] { (-0.1f, -0.12f), (-0.1f, 0.12f), (0.1f, -0.12f), (0.1f, 0.12f) })
            {
                var w = Object.Instantiate(wheelObj, cart.transform);
                w.transform.localPosition = new Vector3(wx, 0.06f, wz);
                w.GetComponent<MeshRenderer>().sharedMaterial = wheelMat;
            }
            Object.DestroyImmediate(wheelObj);

            // 货物
            var cargoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargoObj.name = "Cargo";
            DestroyImmediate(cargoObj.GetComponent<Collider>());
            cargoObj.transform.SetParent(cart.transform, false);
            cargoObj.transform.localScale = new Vector3(0.15f, 0.08f, 0.1f);
            cargoObj.transform.localPosition = new Vector3(0, 0.23f, 0);
            Color cargoColor = route.CargoType switch
            {
                "IRON" => new Color(0.5f, 0.5f, 0.5f),
                "AMMO" => new Color(0.86f, 0.15f, 0.15f),
                _ => new Color(0.56f, 0.93f, 0.56f), // FOOD = 绿
            };
            cargoObj.GetComponent<MeshRenderer>().sharedMaterial = MaterialCache.GetLit(cargoColor);

            return cart;
        }

        /// <summary>更新运输车位置</summary>
        private void UpdateCartPosition(GameObject cart, LogisticsComponent route)
        {
            if (cart == null || _terrain == null) return;

            var store = global::GameApp.Instance?.State;
            var nodes = store?.Nodes;
            var edges = store?.Edges;

            if (edges == null || nodes == null) return;

            string edgeId = route.CurrentEdgeId ?? route.CurrentEdgeId;
            if (!string.IsNullOrEmpty(edgeId) && edges.TryGetValue(edgeId, out var edge))
            {
                if (nodes.TryGetValue(edge.SourceNodeId, out var src) &&
                    nodes.TryGetValue(edge.TargetNodeId, out var tgt))
                {
                    var srcW = CoordinateUtils.ToWorldXZ(src.X, src.Y);
                    var tgtW = CoordinateUtils.ToWorldXZ(tgt.X, tgt.Y);
                    float progress = Mathf.Clamp01(route.EdgeProgress);

                    float wx = srcW.x + (tgtW.x - srcW.x) * progress;
                    float wz = srcW.y + (tgtW.y - srcW.y) * progress;
                    float wy = _terrain.GetHeightAt(wx, wz) + 0.05f;

                    cart.transform.position = new Vector3(wx, wy, wz);
                }
            }
        }

        private void RemoveLogistics(int entityId)
        {
            if (_logisticsObjects.TryGetValue(entityId, out var vis))
            {
                Destroy(vis.root);
                _logisticsObjects.Remove(entityId);
            }
        }

        /// <summary>清理所有物流对象</summary>
        public void ClearAllLogistics()
        {
            foreach (var (id, vis) in _logisticsObjects)
                Destroy(vis.root);
            _logisticsObjects.Clear();
        }

        private void OnDestroy() => ClearAllLogistics();
    }
}
