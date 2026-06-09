using UnityEngine;

/// <summary>
/// 迷你地图控制器 — 正交相机小地图 + 视口指示器
/// 移植自 client/src/map3d/minimap.js
/// </summary>
namespace Rendering
{
    public class MinimapController : MonoBehaviour
    {
        [Header("小地图")]
        [SerializeField] private Camera _minimapCamera;
        [SerializeField] private int _minimapWidth = 200;
        [SerializeField] private int _minimapHeight = 150;
        [SerializeField] private Color _backgroundColor = ColorFromInt(0x1a1a2e);

        [Header("视口指示器")]
        [SerializeField] private Color _viewportColor = Color.white;
        [SerializeField] private float _viewportOpacity = 0.3f;

        [Header("场景引用")]
        [SerializeField] private Camera _sceneCamera;
        [SerializeField] private RectTransform _viewportIndicator;

        private RenderTexture _renderTexture;

        private void Awake()
        {
            if (_minimapCamera == null)
            {
                // 在运行时创建一个正交相机
                var go = new GameObject("MinimapCamera");
                go.transform.SetParent(transform);
                _minimapCamera = go.AddComponent<Camera>();
            }

            if (_sceneCamera == null)
                _sceneCamera = Camera.main;

            SetupMinimapCamera();
        }

        private void SetupMinimapCamera()
        {
            // 使用 Constants 中的参数
            float size = Constants.TerrainGen.WorldSize;
            float aspect = (float)_minimapWidth / _minimapHeight;

            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = Mathf.Max(size / 2f, size / 2f / aspect);
            _minimapCamera.nearClipPlane = 0.1f;
            _minimapCamera.farClipPlane = 200f;

            // 从正上方俯视地图中心（偏移到 Three.js 场景中心 15, 12）
            _minimapCamera.transform.position = new Vector3(15f, 50f, 12f);
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 创建 RenderTexture
            _renderTexture = new RenderTexture(_minimapWidth, _minimapHeight, 16, RenderTextureFormat.Default);
            _minimapCamera.targetTexture = _renderTexture;
            _minimapCamera.clearFlags = CameraClearFlags.Color;
            _minimapCamera.backgroundColor = _backgroundColor;

            // 设置 culling mask：只渲染 map 层
            _minimapCamera.cullingMask = LayerMask.GetMask("Default");
        }

        private void LateUpdate()
        {
            UpdateViewportIndicator();
        }

        /// <summary>获取小地图 RenderTexture</summary>
        public RenderTexture GetMinimapTexture() => _renderTexture;

        /// <summary>更新视口指示器位置</summary>
        private void UpdateViewportIndicator()
        {
            if (_viewportIndicator == null || _sceneCamera == null) return;

            float size = Constants.TerrainGen.WorldSize;
            Vector3 camPos = _sceneCamera.transform.position;

            // 相机位置 → 小地图 UV 坐标
            float half = size / 2f;
            float mapX = ((camPos.x - (-half)) / size);
            float mapY = ((half - camPos.z) / size);

            // 视口大小（模拟 zoom）
            float vpRatio = 0.3f;
            float vpW = vpRatio;
            float vpH = vpRatio;

            // 应用到 RectTransform（锚定在左上角）
            _viewportIndicator.anchorMin = new Vector2(0, 1);
            _viewportIndicator.anchorMax = new Vector2(0, 1);
            _viewportIndicator.pivot = new Vector2(0.5f, 0.5f);
            _viewportIndicator.anchoredPosition = new Vector2(
                mapX * _minimapWidth,
                -mapY * _minimapHeight
            );
            _viewportIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vpW * _minimapWidth);
            _viewportIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vpH * _minimapHeight);
        }

        /// <summary>清理</summary>
        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
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
