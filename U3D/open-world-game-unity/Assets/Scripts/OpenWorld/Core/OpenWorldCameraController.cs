using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldCameraController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 45f;
        [SerializeField] private float _zoomSpeed = 180f;
        [SerializeField] private float _rotateSpeed = 90f;
        [SerializeField] private float _minHeight = 18f;
        [SerializeField] private float _maxHeight = 140f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            var keyboard = OpenWorldInput.Keyboard;
            var mouse = OpenWorldInput.Mouse;

            var forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            var right = transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 move = Vector3.zero;
            if (keyboard != null && (OpenWorldInput.Held(keyboard.wKey) || OpenWorldInput.Held(keyboard.upArrowKey))) move += forward;
            if (keyboard != null && (OpenWorldInput.Held(keyboard.sKey) || OpenWorldInput.Held(keyboard.downArrowKey))) move -= forward;
            if (keyboard != null && (OpenWorldInput.Held(keyboard.dKey) || OpenWorldInput.Held(keyboard.rightArrowKey))) move += right;
            if (keyboard != null && (OpenWorldInput.Held(keyboard.aKey) || OpenWorldInput.Held(keyboard.leftArrowKey))) move -= right;

            if (move.sqrMagnitude > 1f) move.Normalize();

            float heightFactor = Mathf.Lerp(0.5f, 2.5f, Mathf.InverseLerp(_minHeight, _maxHeight, transform.position.y));
            transform.position += move * (_moveSpeed * heightFactor * Time.deltaTime);

            float scroll = mouse != null ? OpenWorldInput.Scroll.y * 0.01f : 0f;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                var pos = transform.position + transform.forward * (scroll * _zoomSpeed * Time.deltaTime);
                if (pos.y >= _minHeight && pos.y <= _maxHeight)
                {
                    transform.position = pos;
                }
            }

            if (keyboard != null && OpenWorldInput.Held(keyboard.qKey))
                transform.RotateAround(transform.position + forward * 20f, Vector3.up, -_rotateSpeed * Time.deltaTime);
            if (keyboard != null && OpenWorldInput.Held(keyboard.eKey))
                transform.RotateAround(transform.position + forward * 20f, Vector3.up, _rotateSpeed * Time.deltaTime);
        }
    }
}
