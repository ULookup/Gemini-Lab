#nullable enable
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public sealed class ChatMessageListView : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect = null!;
        [SerializeField] private RectTransform _contentRect = null!;
        [SerializeField] private GameObject _bubbleUserPrefab = null!;
        [SerializeField] private GameObject _bubbleAngelPrefab = null!;
        [SerializeField] private GameObject _bubbleDevilPrefab = null!;
        [SerializeField] private GameObject _emptyHint = null!;
        [SerializeField] private int _maxVisibleBubbles = 50;

        private readonly List<GameObject> _activeBubbles = new();

        public void AddBubble(ChatRole role, string text)
        {
            var prefab = GetPrefab(role);
            if (prefab == null)
            {
                Debug.LogError($"[PhoneChat] Missing bubble prefab for role {role}");
                return;
            }
            AddBubbleSilent(role, text);
            ScrollToBottom();
        }

        private void AddBubbleSilent(ChatRole role, string text)
        {
            if (_emptyHint != null) _emptyHint.SetActive(false);

            var prefab = GetPrefab(role);
            if (prefab == null) return;

            var bubble = Instantiate(prefab, _contentRect);
            var bubbleRT = (RectTransform)bubble.transform;

            bubbleRT.anchorMin = new Vector2(0f, 0.5f);
            bubbleRT.anchorMax = new Vector2(1f, 0.5f);
            bubbleRT.pivot = new Vector2(0f, 0.5f);
            bubbleRT.offsetMin = new Vector2(8f, bubbleRT.offsetMin.y);
            bubbleRT.offsetMax = new Vector2(-8f, bubbleRT.offsetMax.y);

            var tmpText = bubble.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = text;

            _activeBubbles.Add(bubble);
            RecycleOldBubbles();
        }

        public void AddMessagesFromHistory(IReadOnlyList<ChatMessage> messages)
        {
            Clear();
            foreach (var msg in messages)
            {
                AddBubbleSilent(msg.Role, msg.Text);
            }
            ScrollToBottom();
        }

        public void Clear()
        {
            foreach (var bubble in _activeBubbles)
            {
                if (bubble != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(bubble);
                    else Destroy(bubble);
#else
                    Destroy(bubble);
#endif
                }
            }
            _activeBubbles.Clear();
            if (_emptyHint != null) _emptyHint.SetActive(true);
        }

        private void RecycleOldBubbles()
        {
            while (_activeBubbles.Count > _maxVisibleBubbles)
            {
                var oldest = _activeBubbles[0];
                _activeBubbles.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }
        }

        private void ScrollToBottom()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            Canvas.ForceUpdateCanvases();
            _scrollRect.normalizedPosition = Vector2.zero;
        }

        private GameObject? GetPrefab(ChatRole role)
        {
            return role switch
            {
                ChatRole.User => _bubbleUserPrefab,
                ChatRole.Angel => _bubbleAngelPrefab,
                ChatRole.Devil => _bubbleDevilPrefab,
                _ => null
            };
        }
    }
}
