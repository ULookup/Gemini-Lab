#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 小木屋返回公寓入口。挂到 Cabin 场景物上：
    /// - 点击 → 返回公寓
    /// - 鼠标悬停 → 高亮变色
    /// - 鼠标离开 → 恢复原色
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CabinReturnPortal : MonoBehaviour
    {
        [SerializeField] private Color _hoverTint = new Color(1.15f, 1.15f, 1.15f, 1f);

        private SpriteRenderer? _sprite;
        private Color _originalColor;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (_sprite != null) _originalColor = _sprite.color;
        }

        private void OnMouseEnter()
        {
            if (_sprite != null) _sprite.color = _originalColor * _hoverTint;
        }

        private void OnMouseExit()
        {
            if (_sprite != null) _sprite.color = _originalColor;
        }

        private void OnMouseDown()
        {
            // 防止 Play 模式启动时 Unity SendMouseEvents 的首帧伪点击
            if (Time.frameCount < 2) return;

            if (!ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow) || sceneFlow is null)
            {
                Debug.LogError("[CabinReturnPortal] 未找到 ISceneFlowService");
                return;
            }

            sceneFlow.LoadAsync(SceneId.Apartment);
        }
    }
}
