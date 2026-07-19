#nullable enable
using GeminiLab.Modules.Pet;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 场景的横板摄像头控制器。
    /// 未选中桌宠时：A/D 键、左键拖拽、右键拖拽平移。
    /// 选中桌宠时：自动跟随桌宠 X 坐标（平滑插值）。
    /// 点击空白区域取消选中桌宠。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class WorldMapCameraController : MonoBehaviour
    {
        [Header("移动范围（世界坐标 X 上下限）")]
        [SerializeField] private float _minX = -20f;
        [SerializeField] private float _maxX = 20f;

        [Header("键盘输入")]
        [SerializeField] private float _keyboardSpeed = 8f;
        [SerializeField] private KeyCode _leftKey = KeyCode.A;
        [SerializeField] private KeyCode _rightKey = KeyCode.D;

        [Header("拖拽")]
        [SerializeField] private float _dragSpeed = 1f;

        [Header("跟随")]
        [SerializeField] private float _followSmoothTime = 0.3f;

        private Camera? _camera;
        private Vector3? _lastDragMouseWorld;
        private float _followVelocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_camera is null) return;

            Transform? followTarget = PetPlayerInputController.ActiveTransform;

            Vector3 pos = transform.position;

            if (followTarget != null)
            {
                // 跟随模式：平滑跟随选中桌宠的 X 坐标
                float targetX = followTarget.position.x;
                pos.x = Mathf.SmoothDamp(pos.x, targetX, ref _followVelocity, _followSmoothTime);
                pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
                transform.position = pos;
                _lastDragMouseWorld = null;
                return;
            }

            // 自由平移模式
            float delta = 0f;
            if (Input.GetKey(_leftKey)) delta -= _keyboardSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(_rightKey)) delta += _keyboardSpeed * Time.unscaledDeltaTime;

            // 左键或右键拖拽平移（不穿透 UI）
            if ((Input.GetMouseButton(0) || Input.GetMouseButton(1))
                && !IsPointerOverUI())
            {
                Vector3 cur = _camera.ScreenToWorldPoint(Input.mousePosition);
                if (_lastDragMouseWorld.HasValue)
                {
                    delta += (_lastDragMouseWorld.Value.x - cur.x) * _dragSpeed;
                }
                _lastDragMouseWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            }
            else
            {
                _lastDragMouseWorld = null;
            }

            // 左键点击空白区域 → 取消选中桌宠
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                Vector2 worldPoint = _camera.ScreenToWorldPoint(Input.mousePosition);
                Collider2D? hit = Physics2D.OverlapPoint(worldPoint);
                if (hit == null)
                {
                    PetPlayerInputController.ReleaseAllControl();
                }
            }

            pos.x = Mathf.Clamp(pos.x + delta, _minX, _maxX);
            transform.position = pos;
        }

        public void SetBounds(float minX, float maxX)
        {
            _minX = minX;
            _maxX = maxX;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
