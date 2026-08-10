#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 随机漫游目标选择器。只负责选目标 + 计时，不直接操作 Transform/Rigidbody。
    /// 实际位移由 PetController.TickInactivePlayerControlled 统一驱动。
    /// 当玩家选中此宠物（WASD 控制）时自动暂停，取消选中后恢复。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RandomWander : MonoBehaviour
    {
        [Header("漫游区域（世界坐标）")]
        [SerializeField] private Vector2 _boundsMin = new(-5f, -3f);
        [SerializeField] private Vector2 _boundsMax = new(5f, 3f);

        [Header("横板模式")]
        [SerializeField] private bool _horizontalOnly = false;

        [Header("移动参数")]
        [SerializeField] private float _moveSpeed = 1.5f;
        [SerializeField] private float _arrivalThreshold = 0.15f;

        [Header("等待时间")]
        [SerializeField] private float _minWaitSeconds = 2f;
        [SerializeField] private float _maxWaitSeconds = 5f;

        private PetController? _controller;
        private Vector2 _targetPosition;
        private float _waitTimer;
        private bool _isMoving;
        private float _horizontalBaselineY;
        private bool _hasHorizontalBaselineY;

        public bool IsMoving => _isMoving;
        public Vector2 TargetPosition => _targetPosition;
        public float MoveSpeed => _moveSpeed;
        public float ArrivalThreshold => _arrivalThreshold;
        public bool HorizontalOnly => _horizontalOnly;
        public float HorizontalBaselineY
        {
            get
            {
                EnsureHorizontalBaselineY();
                return _horizontalBaselineY;
            }
        }

        /// <summary>PetController 在宠物到达目标后调用。</summary>
        public void NotifyArrived()
        {
            _isMoving = false;
            _waitTimer = Random.Range(_minWaitSeconds, _maxWaitSeconds);
        }

        /// <summary>PetController 在宠物卡住超过 2 秒后调用，放弃当前目标重新等待。</summary>
        public void AbandonTarget()
        {
            _isMoving = false;
            _waitTimer = Random.Range(_minWaitSeconds, _maxWaitSeconds);
        }

        private void Awake()
        {
            _controller = GetComponent<PetController>();
            EnsureHorizontalBaselineY();
            PickNewTarget();
            _waitTimer = Random.Range(_minWaitSeconds, _maxWaitSeconds);
        }

        private void Update()
        {
            if (_controller == null) return;
            var data = _controller.RuntimeData;
            if (data == null) return;

            if (_controller.IsMovementLocked)
            {
                if (_isMoving) NotifyArrived();
                return;
            }

            if (_controller.IsPlayerControlEnabled)
            {
                if (_isMoving) NotifyArrived();
                return;
            }

            if (data.CurrentState == "Sleeping" ||
                data.CurrentState == "Interacting" ||
                data.CurrentState == "Working" ||
                data.IsPlayerInteractionActive)
            {
                if (_isMoving) NotifyArrived();
                return;
            }

            if (_isMoving) return; // 移动中，等 PetController 通知到达

            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                PickNewTarget();
                _isMoving = true;
            }
        }

        private void PickNewTarget()
        {
            EnsureHorizontalBaselineY();
            float x = Random.Range(_boundsMin.x, _boundsMax.x);
            float y = _horizontalOnly
                ? _horizontalBaselineY
                : Random.Range(_boundsMin.y, _boundsMax.y);
            _targetPosition = new Vector2(x, y);
        }

        private void EnsureHorizontalBaselineY()
        {
            if (_hasHorizontalBaselineY)
            {
                return;
            }

            _horizontalBaselineY = transform.position.y;
            _hasHorizontalBaselineY = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.3f);
            Vector2 center = (_boundsMin + _boundsMax) * 0.5f;
            Vector2 size = _boundsMax - _boundsMin;
            Gizmos.DrawWireCube(center, size);

            if (_isMoving)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_targetPosition, 0.2f);
                Gizmos.DrawLine(transform.position, _targetPosition);
            }
        }
#endif
    }
}
