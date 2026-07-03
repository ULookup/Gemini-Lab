#nullable enable
using System.Collections.Generic;
using System.Linq;
using GeminiLab.Core;
using GeminiLab.Modules.Tarot;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor
{
    /// <summary>
    /// Editor window for inspecting and managing the tarot system at runtime.
    /// View sessions, delete records, inspect card states, and manage the deck.
    /// </summary>
    public sealed class TarotSystemInspector : EditorWindow
    {
        private ITarotService? _tarotService;
        private ITarotSessionRecordStore? _recordStore;
        private TarotDeckSO? _deck;
        private readonly List<TarotSessionRecord> _records = new();
        private Vector2 _scrollSessions, _scrollDeck;
        private bool _foldoutSessions = true, _foldoutDeck = true;
        private string _filterText = string.Empty;

        [MenuItem("Tools/Gemini-Lab/Tarot System Inspector")]
        private static void Open()
        {
            var window = GetWindow<TarotSystemInspector>(false, "Tarot Inspector");
            window.minSize = new Vector2(650f, 450f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            RefreshState();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
                RefreshState();
        }

        private void RefreshState()
        {
            _tarotService = null;
            _recordStore = null;
            _deck = null;
            _records.Clear();

            if (!Application.isPlaying) return;

            if (ServiceLocator.TryResolve(out ITarotService? tarot) && tarot != null)
            {
                _tarotService = tarot;
                _deck = tarot.Deck;
            }

            if (ServiceLocator.TryResolve(out ITarotSessionRecordStore? store) && store != null)
            {
                _recordStore = store;
                _records.Clear();
                _records.AddRange(store.GetAll());
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tarot System Status",
                EditorStyles.boldLabel, GUILayout.Width(200));

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect.", MessageType.Info);
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(80), GUILayout.Height(22)))
                RefreshState();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Service status
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"ITarotService: {(_tarotService != null ? "Registered" : "MISSING")}");
            EditorGUILayout.LabelField($"ITarotSessionRecordStore: {(_recordStore != null ? "Registered" : "MISSING")}");
            EditorGUILayout.LabelField($"Deck: {(_deck != null ? _deck.name + $" ({_deck.Cards.Count} cards)" : "MISSING")}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Session Records
            _foldoutSessions = EditorGUILayout.Foldout(_foldoutSessions,
                $"Session Records ({_records.Count})", true);
            if (_foldoutSessions)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                _filterText = EditorGUILayout.TextField("Filter:", _filterText);
                if (GUILayout.Button("Delete All", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Delete All Records",
                        $"Delete all {_records.Count} tarot session records?", "Delete All", "Cancel"))
                    {
                        foreach (var r in _records.ToList())
                            _recordStore?.Remove(r.SessionId);
                        _records.Clear();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                _scrollSessions = EditorGUILayout.BeginScrollView(_scrollSessions, GUILayout.Height(200));

                var filtered = string.IsNullOrWhiteSpace(_filterText)
                    ? _records
                    : _records.Where(r =>
                        r.SessionId.Contains(_filterText) ||
                        r.Question.Contains(_filterText) ||
                        r.SessionDateIso.Contains(_filterText)).ToList();

                foreach (var record in filtered)
                {
                    DrawSessionRecord(record);
                }

                if (filtered.Count == 0 && _records.Count > 0)
                    EditorGUILayout.LabelField("No records match filter.");

                EditorGUILayout.EndScrollView();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            // Deck Cards
            if (_deck != null)
            {
                _foldoutDeck = EditorGUILayout.Foldout(_foldoutDeck, $"Deck Cards ({_deck.Cards.Count})", true);
                if (_foldoutDeck)
                {
                    _scrollDeck = EditorGUILayout.BeginScrollView(_scrollDeck, GUILayout.Height(150));
                    foreach (var card in _deck.Cards)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.LabelField(card.Id, GUILayout.Width(100));
                        EditorGUILayout.LabelField(card.DisplayNameZh ?? "", GUILayout.Width(80));
                        EditorGUILayout.LabelField(card.DisplayNameEn ?? "", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Upright: {(card.UprightKeywords.Count > 0 ? string.Join(", ", card.UprightKeywords) : "-")}");
                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            EditorGUIUtility.PingObject(card);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawSessionRecord(TarotSessionRecord record)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"ID: {record.SessionId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(record.SessionDateIso, EditorStyles.miniLabel, GUILayout.Width(150));

            GUI.color = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Width(55), GUILayout.Height(18)))
            {
                if (_recordStore?.Remove(record.SessionId) == true)
                {
                    _records.Remove(record);
                    Repaint();
                }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(record.Question))
                EditorGUILayout.LabelField($"Q: {record.Question}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            DrawSlot("Past", record.PastCardId, record.PastOrientation,
                record.PastAngelReading, record.PastDevilReading);
            DrawSlot("Present", record.PresentCardId, record.PresentOrientation,
                record.PresentAngelReading, record.PresentDevilReading);
            DrawSlot("Future", record.FutureCardId, record.FutureOrientation,
                record.FutureAngelReading, record.FutureDevilReading);
            EditorGUILayout.EndHorizontal();

            if (record.FortuneLevel > 0)
            {
                EditorGUILayout.LabelField(
                    $"Fortune: {record.FortuneLevel}/5 | Color: {record.LuckyColor} | " +
                    $"Number: {record.LuckyNumber} | Time: {record.LuckyTime} | " +
                    $"Action: {record.LuckyAction}",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawSlot(string label, string cardId, string orientation,
            string angelReading, string devilReading)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.LabelField($"{label}: {cardId} ({orientation})", EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(angelReading))
                EditorGUILayout.LabelField($"Angel: {Truncate(angelReading, 40)}", EditorStyles.miniLabel);
            if (!string.IsNullOrWhiteSpace(devilReading))
                EditorGUILayout.LabelField($"Devil: {Truncate(devilReading, 40)}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private static string Truncate(string text, int maxLen)
            => text.Length <= maxLen ? text : text[..maxLen] + "...";
    }
}
