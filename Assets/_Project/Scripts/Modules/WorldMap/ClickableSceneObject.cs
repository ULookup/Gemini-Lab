#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 可点击的场景占位物。点击时输出日志，后续可接入具体交互逻辑。
    /// </summary>
    public sealed class ClickableSceneObject : MonoBehaviour
    {
        [SerializeField] private string _displayName = "场景物";
        [SerializeField] private string _clickMessage = "点击了 {0}";

        private void OnMouseDown()
        {
            Debug.Log($"[ClickableSceneObject] {string.Format(_clickMessage, _displayName)}");
        }
    }
}
