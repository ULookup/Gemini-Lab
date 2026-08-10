#nullable enable
using GeminiLab.Core;
using UnityEngine;
using UnityEngine.Events;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 可点击的场景交互入口。
    /// 当前阶段保留日志和序列化 UnityEvent，具体业务交互由后续任务接入。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ClickableSceneObject : MonoBehaviour
    {
        [SerializeField] private string _displayName = "场景物";
        [SerializeField] private string _clickMessage = "点击了 {0}";
        [SerializeField] private UnityEvent _onClicked = new();
        private Collider2D? _clickCollider;

        private void Awake()
        {
            _clickCollider = GetComponent<Collider2D>();
        }

        private void OnMouseDown()
        {
            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return;
            }

            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(_clickCollider))
            {
                return;
            }

            Debug.Log($"[ClickableSceneObject] {string.Format(_clickMessage, _displayName)}");
            _onClicked.Invoke();
        }
    }
}
