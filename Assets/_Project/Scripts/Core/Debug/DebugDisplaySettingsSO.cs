#nullable enable
using UnityEngine;

namespace GeminiLab
{
    [CreateAssetMenu(menuName = "GeminiLab/Debug Display Settings", fileName = "DebugDisplaySettings", order = 1000)]
    public sealed class DebugDisplaySettingsSO : ScriptableObject
    {
        private const string ResourcesKey = "DebugDisplaySettings";

        private static DebugDisplaySettingsSO? _instance;

        public static DebugDisplaySettingsSO Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = Resources.Load<DebugDisplaySettingsSO>(ResourcesKey);
                if (_instance == null)
                {
                    // No asset in Resources — create an in-memory default (all enabled).
                    _instance = CreateInstance<DebugDisplaySettingsSO>();
                }
                return _instance;
            }
        }

        public static void InvalidateCache()
        {
            _instance = null;
        }

        [Header("Master")]
        [Tooltip("When off, all debug displays are hidden.")]
        [SerializeField] private bool _enableDebugDisplay = true;

        [Header("Categories")]
        [Tooltip("Chat message list editor preview bubbles.")]
        [SerializeField] private bool _enableChatPreview = true;

        [Tooltip("Tarot card / reading bubble / summary editor preview content.")]
        [SerializeField] private bool _enableTarotPreview = true;

        [Tooltip("Placeholder GameObjects created at runtime (pet placeholders, etc.).")]
        [SerializeField] private bool _enablePlaceholderObjects = true;

        [Tooltip("Verbose runtime debug logs from Pet / Furniture / Gateway subsystems.")]
        [SerializeField] private bool _enableVerboseLogging = true;

        public bool IsDebugDisplayEnabled => _enableDebugDisplay;

        public bool IsChatPreviewEnabled => _enableDebugDisplay && _enableChatPreview;
        public bool IsTarotPreviewEnabled => _enableDebugDisplay && _enableTarotPreview;
        public bool IsPlaceholderObjectsEnabled => _enableDebugDisplay && _enablePlaceholderObjects;
        public bool IsVerboseLoggingEnabled => _enableDebugDisplay && _enableVerboseLogging;
    }
}
