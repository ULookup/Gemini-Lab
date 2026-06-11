#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels.PhoneChat;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor
{
    [CustomEditor(typeof(ChatMessageListView))]
    public sealed class ChatMessageListViewEditor : UnityEditor.Editor
    {
        private SerializedProperty? _showPreviewProp;
        private bool _lastShowPreview;

        private void OnEnable()
        {
            _showPreviewProp = serializedObject.FindProperty("_showPreview");
            _lastShowPreview = _showPreviewProp?.boolValue ?? false;
            EditorApplication.delayCall += ShowPreviewIfValid;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= ShowPreviewIfValid;
            if (target != null)
                ((ChatMessageListView)target).ClearEditorPreview();
        }

        private void ShowPreviewIfValid()
        {
            if (target == null) return;
            var list = (ChatMessageListView)target;
            if (_showPreviewProp?.boolValue ?? false)
                list.ShowEditorPreview();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            if (_showPreviewProp == null) return;

            var current = _showPreviewProp.boolValue;
            if (current != _lastShowPreview)
            {
                _lastShowPreview = current;
                var list = (ChatMessageListView)target;
                if (current) list.ShowEditorPreview();
                else list.ClearEditorPreview();
            }
        }
    }
}
#endif
