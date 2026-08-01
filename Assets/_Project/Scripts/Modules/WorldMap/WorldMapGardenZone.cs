#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 情绪花园种植入口。点击后打开情绪输入面板，owner 由场景中配置决定。
    /// 挂到天使/恶魔各自的入口 GameObject 上，_owner 分别填 "angel" / "demon"。
    /// </summary>
    public sealed class WorldMapGardenZone : MonoBehaviour
    {
        [SerializeField] private string _owner = "angel";
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

            if (!TryResolveRouter(out var router)) return;
            router.Open(PanelId.EmotionInput, _owner);
        }

        private static bool TryResolveRouter(out IUIRouter router)
        {
            if (ServiceLocator.TryResolve(out IUIRouter? r) && r != null)
            {
                router = r;
                return true;
            }

            router = null!;
            Debug.LogWarning("[WorldMapGardenZone] IUIRouter 未注册");
            return false;
        }
    }
}
