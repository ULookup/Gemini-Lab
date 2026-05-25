#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 解读气泡 Prefab 脚本。支持弹出动画和天使/恶魔不同配色。
    /// </summary>
    public sealed class ReadingBubble : MonoBehaviour
    {
        [SerializeField] private Image? _bubbleBg;
        [SerializeField] private TMP_Text? _readingText;
        [SerializeField] private TMP_Text? _personaLabel;
        [SerializeField] private Image? _avatarImage;
        [SerializeField] private Animator? _animator;

        [Header("配色")]
        [SerializeField] private Color _angelColor = new Color(1f, 0.95f, 0.7f, 1f);
        [SerializeField] private Color _devilColor = new Color(0.7f, 0.3f, 0.3f, 1f);

        private static readonly int AppearTrigger = Animator.StringToHash("Appear");

        public void Show(string personaName, string text, bool isAngel, Action? onComplete = null)
        {
            if (_personaLabel != null) _personaLabel.text = personaName;
            if (_readingText != null) _readingText.text = text;
            if (_bubbleBg != null) _bubbleBg.color = isAngel ? _angelColor : _devilColor;

            gameObject.SetActive(true);

            if (_animator != null)
            {
                _animator.SetTrigger(AppearTrigger);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
