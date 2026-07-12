#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 随机漫游：在限定区域内让宠物随机走到目标点、等待、再走，循环。
    /// 挂在 PetController 同一 GameObject 上即可自动运行。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RandomWander : MonoBehaviour
    {
        [Header("漫游区域（世界坐标）")]
        [SerializeField] private Vector2 _boundsMin = new(-5f, -3f);
        [SerializeField] private Vector2 _boundsMax = new(5f, 3f);

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

        private void Awake()
        {
            _controller = GetComponent<PetController>();
            PickNewTarget();
            _waitTimer = Random.Range(_minWaitSeconds, _maxWaitSeconds);
        }

        private void Update()
        {
            if (_controller == null) return;
            var data = _controller.RuntimeData;
            if (data == null) return;

            // Pause wander when pet is in a non-idle FSM state
            if (data.CurrentState == "Sleeping" ||
                data.CurrentState == "Interacting" ||
                data.CurrentState == "Working" ||
                data.IsPlayerInteractionActive)
            {
                if (_isMoving)
                {
                    data.CurrentState = "Idle";
                    _isMoving = false;
                    _waitTimer = Random.Range(_minWaitSeconds, _maxWaitSeconds);
                }
                return;
            }

            if (_isMoving)
            {
                Vector2 current = data.Position;
                Vector2 toTarget = _targetPosition - current;

                if (toTarget.sqrMagnitude <= _arrivalThreshold * _arrivalThreshold)
                {
                    data.Position = _targetPosition;
                    data.CurrentState = "Idle";
                    _isMoving = false;
                    _waitTimer = Random.Range(_minWaitSeconds, _maxWaitSeconds);
                }
                else
                {
                    float step = _moveSpeed * Time.deltaTime;
                    data.Position = Vector2.MoveTowards(current, _targetPosition, step);
                    data.CurrentState = "Moving";
                }
            }
            else
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    PickNewTarget();
                    data.CurrentState = "Moving";
                    _isMoving = true;
                }
            }
        }

        private void PickNewTarget()
        {
            _targetPosition = new Vector2(
                Random.Range(_boundsMin.x, _boundsMax.x),
                Random.Range(_boundsMin.y, _boundsMax.y)
            );
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
                if (_controller?.RuntimeData != null)
                {
                    Gizmos.DrawLine(_controller.RuntimeData.Position, _targetPosition);
                }
            }
        }
#endif
    }
}
