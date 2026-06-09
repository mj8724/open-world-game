using UnityEngine;

/// <summary>
/// 轨道相机控制器 — 类似 Three.js OrbitControls
/// 移植自 client/src/map3d/camera-controller.js
/// </summary>
namespace CameraControl
{
    public class CameraController : MonoBehaviour
    {
        [Header("相机引用")]
        [SerializeField] private Camera _targetCamera;

        [Header("轨道参数")]
        [SerializeField] private float _distance = 30f;
        [SerializeField] private float _minDistance = 5f;
        [SerializeField] private float _maxDistance = 80f;
        [SerializeField] private float _polarAngle = 45f;    // 度
        [SerializeField] private float _minPolarAngle = 8.5f;
        [SerializeField] private float _maxPolarAngle = 82f;
        [SerializeField] private float _azimuthalAngle = -45f;

        [Header("阻尼")]
        [SerializeField, Range(0, 1)] private float _dampingFactor = 0.1f;
        [SerializeField] private float _focusLerpSpeed = 0.05f;

        [Header("鼠标灵敏度")]
        [SerializeField] private float _rotateSpeed = 0.5f;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _panSpeed = 1.0f;

        // ─── 状态 ───
        private Vector3 _targetPosition = Vector3.zero; // 相机注视的目标点
        private float _currentDistance;
        private float _currentPolar;
        private float _currentAzimuth;
        private Vector3? _focusTarget;
        private bool _isDragging;
        private Vector3 _lastMousePos;
        private Vector3 _dragStart;
        private bool _isPanning;
        private bool _enabled = true;

        public Camera ControlledCamera => _targetCamera;
        public Vector3 Target => _targetPosition;

        private void Awake()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;

            // 从 Constants 加载默认值
            _distance = Constants.CameraDefaults.Distance;
            _minDistance = Constants.CameraDefaults.MinDistance;
            _maxDistance = Constants.CameraDefaults.MaxDistance;
            _polarAngle = Constants.CameraDefaults.PolarAngle;
            _minPolarAngle = Constants.CameraDefaults.MinPolarAngle;
            _maxPolarAngle = Constants.CameraDefaults.MaxPolarAngle;
            _azimuthalAngle = Constants.CameraDefaults.AzimuthalAngle;
            _dampingFactor = Constants.CameraDefaults.DampingFactor;
            _focusLerpSpeed = Constants.CameraDefaults.FocusLerpSpeed;

            _currentDistance = _distance;
            _currentPolar = _polarAngle * Mathf.Deg2Rad;
            _currentAzimuth = _azimuthalAngle * Mathf.Deg2Rad;

            UpdateCameraTransform(1f); // 直接跳转到初始位置
        }

        private void Update()
        {
            if (!_enabled) return;

            HandleInput();

            // 如果开启了阻尼，使用阻尼更新
            float lerp = _dampingFactor > 0 ? _dampingFactor : 1f;
            UpdateCameraTransform(lerp);
        }

        private void HandleInput()
        {
            // 鼠标左键旋转
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _isPanning = false;
                _lastMousePos = Input.mousePosition;
            }

            // 鼠标右键或中键平移
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _isPanning = true;
                _isDragging = false;
                _lastMousePos = Input.mousePosition;
                _dragStart = _lastMousePos;
            }

            if (Input.GetMouseButtonUp(0)) _isDragging = false;
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) _isPanning = false;

            if (_isDragging && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                _currentAzimuth -= delta.x * _rotateSpeed * 0.005f;
                _currentPolar = Mathf.Clamp(
                    _currentPolar + delta.y * _rotateSpeed * 0.005f,
                    _minPolarAngle * Mathf.Deg2Rad,
                    _maxPolarAngle * Mathf.Deg2Rad
                );
            }

            if (_isPanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                // 平移速度随距离缩放
                float panScale = _currentDistance * 0.002f * _panSpeed;
                Vector3 right = _targetCamera.transform.right;
                Vector3 forward = _targetCamera.transform.forward;
                forward.y = 0;
                forward.Normalize();
                _targetPosition -= right * delta.x * panScale;
                _targetPosition -= forward * delta.y * panScale;
            }

            // 滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance = Mathf.Clamp(
                    _currentDistance - scroll * _zoomSpeed * 2f,
                    _minDistance,
                    _maxDistance
                );
            }

            _lastMousePos = Input.mousePosition;

            // 聚焦动画
            if (_focusTarget.HasValue)
            {
                Vector3 target = _focusTarget.Value;
                _targetPosition.x += (target.x - _targetPosition.x) * _focusLerpSpeed;
                _targetPosition.z += (target.z - _targetPosition.z) * _focusLerpSpeed;

                if (Vector3.Distance(_focusTarget.Value, _targetPosition) < 0.01f)
                {
                    _targetPosition = target;
                    _focusTarget = null;
                }
            }
        }

        private void UpdateCameraTransform(float lerp)
        {
            if (_targetCamera == null) return;

            // 球坐标 → 笛卡尔坐标
            float x = _currentDistance * Mathf.Sin(_currentPolar) * Mathf.Sin(_currentAzimuth);
            float y = _currentDistance * Mathf.Cos(_currentPolar);
            float z = _currentDistance * Mathf.Sin(_currentPolar) * Mathf.Cos(_currentAzimuth);

            Vector3 desiredPos = _targetPosition + new Vector3(x, y, z);

            if (lerp < 1f && _dampingFactor > 0)
            {
                _targetCamera.transform.position = Vector3.Lerp(
                    _targetCamera.transform.position, desiredPos, lerp
                );
            }
            else
            {
                _targetCamera.transform.position = desiredPos;
            }

            _targetCamera.transform.LookAt(_targetPosition);
        }

        /// <summary>聚焦到 3D 世界坐标</summary>
        public void FocusOnPosition(Vector3 worldPos, float? distance = null)
        {
            _focusTarget = worldPos;

            if (distance.HasValue)
            {
                _currentDistance = Mathf.Clamp(distance.Value, _minDistance, _maxDistance);
            }
        }

        /// <summary>聚焦到节点（后端坐标）</summary>
        public void FocusOnNode(float backendX, float backendY)
        {
            var xz = CoordinateUtils.ToWorldXZ(backendX, backendY);
            float y = 0f; // 可以通过 TerrainGenerator 查询
            FocusOnPosition(new Vector3(xz.x, y, xz.y));
        }

        /// <summary>框选全地图</summary>
        public void FitAll()
        {
            _targetPosition = new Vector3(15f, 0f, 12f);
            _currentDistance = 35f;
            _currentPolar = 45f * Mathf.Deg2Rad;
            _currentAzimuth = -45f * Mathf.Deg2Rad;
            _focusTarget = null;
        }

        /// <summary>启用/禁用控制器</summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
            {
                _isDragging = false;
                _isPanning = false;
            }
        }
    }
}
