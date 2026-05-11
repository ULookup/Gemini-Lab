#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 场景的横板摄像头控制器。
    /// 支持键盘/鼠标拖拽左右平移，摄像机 x 被限制在 [minX, maxX] 内。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class WorldMapCameraController : MonoBehaviour
    {
        [Header("移动范围（世界坐标 X 上下限）")]
        [SerializeField] private float _minX = -20f;
        [SerializeField] private float _maxX = 20f;

        [Header("输入")]
        [SerializeField] private float _keyboardSpeed = 8f;
        [SerializeField] private float _dragSpeed = 1f;
        [SerializeField] private KeyCode _leftKey = KeyCode.A;
        [SerializeField] private KeyCode _rightKey = KeyCode.D;

        private Camera? _camera;
        private Vector3? _lastDragMouseWorld;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_camera is null)
            {
                return;
            }

            Vector3 pos = transform.position;

            float delta = 0f;
            if (Input.GetKey(_leftKey)) delta -= _keyboardSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(_rightKey)) delta += _keyboardSpeed * Time.unscaledDeltaTime;

            if (Input.GetMouseButton(1))
            {
                Vector3 cur = _camera.ScreenToWorldPoint(Input.mousePosition);
                if (_lastDragMouseWorld.HasValue)
                {
                    float diff = _lastDragMouseWorld.Value.x - cur.x;
                    delta += diff * _dragSpeed;
                }

                _lastDragMouseWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            }
            else
            {
                _lastDragMouseWorld = null;
            }

            pos.x = Mathf.Clamp(pos.x + delta, _minX, _maxX);
            transform.position = pos;
        }

        public void SetBounds(float minX, float maxX)
        {
            _minX = minX;
            _maxX = maxX;
        }
    }
}
