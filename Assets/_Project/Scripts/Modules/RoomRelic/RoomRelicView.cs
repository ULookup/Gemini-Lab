#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 场景作者化的一个可切换变体槽。
    /// 运行时只切换预置 GameObject 的 active，不创建 GameObject，不赋 Sprite。
    /// </summary>
    public sealed class RoomRelicView : MonoBehaviour
    {
        [Serializable]
        public sealed class VariantBinding
        {
            [SerializeField] private string _id = string.Empty;
            [SerializeField] private GameObject? _target;

            public string Id => _id;
            public GameObject? Target => _target;
        }

        [SerializeField] private VariantBinding[] _variants = Array.Empty<VariantBinding>();

        public bool HasAnyActiveTarget { get; private set; }

        public void Apply(string? currentId)
        {
            bool hasAnyActiveTarget = false;
            for (int i = 0; i < _variants.Length; i++)
            {
                VariantBinding variant = _variants[i];
                if (variant.Target == null)
                {
                    continue;
                }

                bool active = string.Equals(variant.Id, currentId, StringComparison.Ordinal);
                variant.Target.SetActive(active);
                hasAnyActiveTarget |= active;
            }

            HasAnyActiveTarget = hasAnyActiveTarget;
        }
    }
}
