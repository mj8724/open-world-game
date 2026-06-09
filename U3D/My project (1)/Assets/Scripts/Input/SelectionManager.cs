using System;
using UnityEngine;

/// <summary>
/// 选中处理器 — 射线检测点击/悬停选中
/// 移植自 client/src/map3d/selection-handler.js
/// </summary>
namespace Input
{
    public class SelectionManager : MonoBehaviour
    {
        [Header("相机引用")]
        [SerializeField] private Camera _raycastCamera;

        [Header("交互层")]
        [SerializeField] private LayerMask _interactableLayers = -1;

        // ─── 事件 ───
        public event Action<string>? OnNodeSelected;
        public event Action<int>? OnArmySelected;
        public event Action<string>? OnWildResourceSelected;
        public event Action<string>? OnNeutralStructureSelected;
        public event Action? OnDeselected;

        // ─── 模式状态 ───
        private bool _placingMode;
        private bool _wallBuildMode;

        // ─── 悬停 ───
        private GameObject _hoveredObject;

        private void Awake()
        {
            if (_raycastCamera == null)
                _raycastCamera = Camera.main;
        }

        private void Update()
        {
            // 鼠标左键点击
            if (Input.GetMouseButtonDown(0))
                HandleLeftClick();

            // 鼠标右键
            if (Input.GetMouseButtonDown(1))
                HandleRightClick();

            // 悬停
            HandleHover();
        }

        /// <summary>射线检测</summary>
        private RaycastHit? Raycast()
        {
            var ray = _raycastCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, _interactableLayers))
            {
                return hit;
            }
            return null;
        }

        /// <summary>查找交互根节点（向上遍历 EntityReference）</summary>
        private static EntityReference? FindEntityRef(GameObject obj)
        {
            // 先检查自身
            var refComp = obj.GetComponent<EntityReference>();
            if (refComp != null && !string.IsNullOrEmpty(refComp.entityType))
                return refComp;

            // 向上遍历父节点
            Transform current = obj.transform.parent;
            while (current != null)
            {
                refComp = current.GetComponent<EntityReference>();
                if (refComp != null && !string.IsNullOrEmpty(refComp.entityType))
                    return refComp;
                current = current.parent;
            }
            return null;
        }

        private void HandleLeftClick()
        {
            var hit = Raycast();

            if (hit.HasValue)
            {
                var entityRef = FindEntityRef(hit.Value.collider.gameObject);
                if (entityRef != null)
                {
                    HandleSelection(entityRef);
                    return;
                }
            }

            // 点击空白处取消选中
            OnDeselected?.Invoke();
        }

        private void HandleRightClick()
        {
            // 右键取消放置/绘制模式
            if (_placingMode || _wallBuildMode)
            {
                CancelModes();
            }
        }

        private void HandleHover()
        {
            var hit = Raycast();
            EntityReference? entityRef = null;

            if (hit.HasValue)
            {
                entityRef = FindEntityRef(hit.Value.collider.gameObject);
            }

            if (entityRef != null)
            {
                if (_hoveredObject != entityRef.gameObject)
                {
                    _hoveredObject = entityRef.gameObject;
                    // 可以在这里触发悬停 tooltip 事件
                }
            }
            else
            {
                _hoveredObject = null;
            }
        }

        private void HandleSelection(EntityReference refComp)
        {
            switch (refComp.entityType)
            {
                case "node":
                    OnNodeSelected?.Invoke(refComp.nodeId);
                    break;
                case "army":
                    OnArmySelected?.Invoke(refComp.entityId);
                    break;
                case "wildResource":
                    OnWildResourceSelected?.Invoke(refComp.nodeId);
                    break;
                case "neutralStructure":
                    OnNeutralStructureSelected?.Invoke(refComp.nodeId);
                    break;
            }
        }

        /// <summary>设置放置模式状态</summary>
        public void SetPlacingMode(bool active)
        {
            _placingMode = active;
            if (active) _wallBuildMode = false;
        }

        /// <summary>设置城墙绘制模式状态</summary>
        public void SetWallBuildMode(bool active)
        {
            _wallBuildMode = active;
            if (active) _placingMode = false;
        }

        /// <summary>取消所有模式</summary>
        public void CancelModes()
        {
            _placingMode = false;
            _wallBuildMode = false;
        }

        /// <summary>获取当前悬停的实体引用</summary>
        public GameObject? GetHoveredObject() => _hoveredObject;
    }
}
