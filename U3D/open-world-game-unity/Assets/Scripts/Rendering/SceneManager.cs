using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 场景管理器 — URP 场景设置 / 灯光 / 动画循环 / 注册每帧回调
/// 移植自 client/src/map3d/scene-manager.js
/// </summary>
namespace Rendering
{
    public class SceneManager : MonoBehaviour
    {
        [Header("场景引用")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Light _directionalLight;
        [SerializeField] private Transform _sunPivot;

        // ─── 环境光参数 ───
        [Header("环境")]
        [SerializeField] private Color _skyColor = new Color(0x87 / 255f, 0xCE / 255f, 0xEB / 255f);
        [SerializeField] private Color _ambientColor = Color.white;
        [SerializeField, Range(0, 1)] private float _ambientIntensity = 0.5f;
        [SerializeField] private float _fogNear = 40f;
        [SerializeField] private float _fogFar = 80f;

        // ─── 方向光参数 ───
        [Header("方向光")]
        [SerializeField] private Color _sunColor = new Color(0xFF / 255f, 0xF4 / 255f, 0xE6 / 255f);
        [SerializeField, Range(0, 1)] private float _sunIntensity = 0.8f;
        [SerializeField] private int _shadowMapSize = 2048;
        [SerializeField] private float _shadowCameraSize = 50f;

        // ─── 每帧回调 ───
        private readonly List<Action> _beforeRenderCallbacks = new();

        public Camera MainCamera => _mainCamera;
        public Light DirectionalLight => _directionalLight;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            SetupEnvironment();
            SetupLighting();
        }

        private void SetupEnvironment()
        {
            // 天空颜色
            RenderSettings.fog = true;
            RenderSettings.fogColor = _skyColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = _fogNear;
            RenderSettings.fogEndDistance = _fogFar;

            // 环境光（近似 Three.js AmbientLight + HemisphereLight）
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _skyColor * _ambientIntensity;
            RenderSettings.ambientEquatorColor = new Color(0x4a / 255f, 0x7c / 255f, 0x3f / 255f) * _ambientIntensity;
            RenderSettings.ambientGroundColor = new Color(0x4a / 255f, 0x7c / 255f, 0x3f / 255f) * (_ambientIntensity * 0.6f);

            RenderSettings.ambientIntensity = _ambientIntensity;
        }

        private void SetupLighting()
        {
            if (_directionalLight == null) return;

            _directionalLight.color = _sunColor;
            _directionalLight.intensity = _sunIntensity;
            _directionalLight.shadowNormalBias = 0.001f;

            // 阴影设置
            _directionalLight.shadows = LightShadows.Soft;
            _directionalLight.shadowResolution = LightShadowResolution.VeryHigh;
            _directionalLight.shadowNearPlane = 0.5f;

            // 方向光位置模拟 Three.js sun position (20, 30, 15)
            if (_sunPivot != null)
            {
                _sunPivot.rotation = Quaternion.Euler(45f, -30f, 0f);
            }
        }

        private void Update()
        {
            // 执行所有 beforeRender 回调
            for (int i = _beforeRenderCallbacks.Count - 1; i >= 0; i--)
            {
                try { _beforeRenderCallbacks[i](); }
                catch (Exception ex) { Debug.LogError($"[Scene] beforeRender error: {ex.Message}"); }
            }
        }

        /// <summary>注册每帧回调</summary>
        public void OnBeforeRender(Action callback)
        {
            if (!_beforeRenderCallbacks.Contains(callback))
                _beforeRenderCallbacks.Add(callback);
        }

        /// <summary>移除每帧回调</summary>
        public void OffBeforeRender(Action callback)
        {
            _beforeRenderCallbacks.Remove(callback);
        }

        /// <summary>获取地形平面的世界大小</summary>
        public static float GetTerrainWorldSize() => Constants.TerrainGen.WorldSize;

        /// <summary>获取地形一半大小</summary>
        public static float GetTerrainHalfSize() => Constants.TerrainGen.WorldSize / 2f;
    }
}
