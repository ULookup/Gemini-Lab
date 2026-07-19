#nullable enable
using GeminiLab;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels
{
    [ExecuteAlways]
    public sealed class TarotSummaryPreview : MonoBehaviour
    {
        [Header("TMP 引用")]
        [SerializeField] private TMP_Text? _fortuneStarsText;
        [SerializeField] private TMP_Text? _luckyColorText;
        [SerializeField] private TMP_Text? _luckyNumberText;
        [SerializeField] private TMP_Text? _luckyTimeText;
        [SerializeField] private TMP_Text? _luckyActionText;
        [SerializeField] private TMP_Text? _adviceText;

        [Header("预览内容")]
        [SerializeField] private int _previewFortuneLevel = 4;
        [SerializeField] private string _previewLuckyColor = "蓝色";
        [SerializeField] private string _previewLuckyNumber = "7";
        [SerializeField] private string _previewLuckyTime = "午后";
        [SerializeField] private string _previewLuckyAction = "保持平常心";
        [SerializeField] [TextArea(2, 5)] private string _previewAdvice = "今日运势平稳，保持平常心，关注身边的小确幸。";

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
            if (!DebugDisplaySettingsSO.Instance.IsTarotPreviewEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            int level = Mathf.Clamp(_previewFortuneLevel, 1, 5);
            if (_fortuneStarsText != null)
                _fortuneStarsText.text = new string('★', level) + new string('☆', 5 - level);
            if (_luckyColorText != null) _luckyColorText.text = _previewLuckyColor;
            if (_luckyNumberText != null) _luckyNumberText.text = _previewLuckyNumber;
            if (_luckyTimeText != null) _luckyTimeText.text = _previewLuckyTime;
            if (_luckyActionText != null) _luckyActionText.text = _previewLuckyAction;
            if (_adviceText != null) _adviceText.text = _previewAdvice;

            gameObject.SetActive(true);
        }
#endif
    }
}
