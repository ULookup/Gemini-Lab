#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    [ExecuteAlways]
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

        [Header("预览内容")]
        [SerializeField] private string _previewPersona = "Angel";
        [SerializeField] [TextArea(2, 5)] private string _previewText = "这是一段示例解读文字，用于在编辑器中预览排版效果。";

        [Header("解读文字排版")]
        [SerializeField] private float _readingFontSize = 16f;
        [SerializeField] private bool _readingAutoSize = true;
        [SerializeField] private float _readingFontMin = 14f;
        [SerializeField] private float _readingFontMax = 22f;
        [SerializeField] private float _readingLineSpacing = 4f;

        [Header("角色标签排版")]
        [SerializeField] private float _personaFontSize = 16f;
        [SerializeField] private bool _personaBold = false;

        private static readonly int AppearTrigger = Animator.StringToHash("Appear");

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (Application.isPlaying) return;
            ApplyPreview();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying) return;
                ApplyPreview();
            };
        }

        private void ApplyPreview()
        {
            if (_personaLabel != null)
            {
                _personaLabel.text = string.IsNullOrWhiteSpace(_previewPersona) ? "Angel" : _previewPersona;
                _personaLabel.fontSize = _personaFontSize;
                _personaLabel.fontSizeMin = _personaFontSize - 2f;
                _personaLabel.fontSizeMax = _personaFontSize + 4f;
                _personaLabel.enableAutoSizing = false;
                _personaLabel.fontStyle = _personaBold ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_readingText != null)
            {
                _readingText.text = string.IsNullOrWhiteSpace(_previewText) ? "示例解读文字" : _previewText;
                _readingText.fontSize = _readingFontSize;
                _readingText.enableAutoSizing = _readingAutoSize;
                _readingText.fontSizeMin = _readingFontMin;
                _readingText.fontSizeMax = _readingFontMax;
                _readingText.lineSpacing = _readingLineSpacing;
                _readingText.fontStyle = FontStyles.Normal;
            }

            if (_bubbleBg != null)
                _bubbleBg.color = _angelColor;

            gameObject.SetActive(true);
        }
#endif

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
