#nullable enable
using GeminiLab.Modules.Furniture;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.Editor
{
    /// <summary>
    /// Custom inspector for Furniture components that previews all interaction state:
    /// interaction type, animation mappings, buff effects, and binding status.
    /// </summary>
    [CustomEditor(typeof(Furniture))]
    public sealed class FurnitureInteractionPreviewEditor : UnityEditor.Editor
    {
        private Furniture _furniture = null!;

        private void OnEnable()
        {
            _furniture = (Furniture)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Interaction Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var definition = _furniture.Definition;

            if (definition == null)
            {
                EditorGUILayout.HelpBox("No FurnitureDefinitionSO assigned.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Definition info
            EditorGUILayout.LabelField("Definition", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"  ID: {definition.Id}");
            EditorGUILayout.LabelField($"  Sprite: {(definition.Sprite != null ? definition.Sprite.name : "MISSING")}");
            if (definition.Sprite == null)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("  [WARNING] No sprite assigned in definition.");
                GUI.color = Color.white;
            }

            EditorGUILayout.Space(4);

            // Category & Interaction
            EditorGUILayout.LabelField("Interaction", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"  Category: {definition.Category}");

            var interactionType = definition.InteractionType;
            GUI.color = interactionType == FurnitureInteractionType.Unknown
                ? Color.yellow : Color.green;
            EditorGUILayout.LabelField($"  Type: {interactionType} ({interactionType.ToDisplayLabel()})");
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"  Duration: {definition.InteractionDurationSeconds:F1}s");
            EditorGUILayout.LabelField($"  Placement: {definition.PlacementType}");
            EditorGUILayout.LabelField($"  Occupied Cells: {definition.OccupiedCells}");

            EditorGUILayout.Space(4);

            // Buff effects
            EditorGUILayout.LabelField("Environmental Buff", EditorStyles.miniBoldLabel);
            var buff = definition.Buff;

            var hasBuff = buff.MoodDelta != 0f || buff.EnergyDelta != 0f;
            if (hasBuff)
            {
                if (buff.MoodDelta != 0f)
                {
                    var label = buff.MoodDelta > 0 ? $"+{buff.MoodDelta:F1}" : $"{buff.MoodDelta:F1}";
                    GUI.color = buff.MoodDelta > 0 ? Color.green : Color.red;
                    EditorGUILayout.LabelField($"  Mood: {label}/s");
                }
                if (buff.EnergyDelta != 0f)
                {
                    var label = buff.EnergyDelta > 0 ? $"+{buff.EnergyDelta:F1}" : $"{buff.EnergyDelta:F1}";
                    GUI.color = buff.EnergyDelta > 0 ? Color.green : Color.red;
                    EditorGUILayout.LabelField($"  Energy: {label}/s");
                }
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("  (No environmental buff)");
            }

            EditorGUILayout.Space(4);

            // Scene binding status
            EditorGUILayout.LabelField("Scene Binding", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"  Is Scene Furniture: {_furniture.IsSceneFurniture}");
            EditorGUILayout.LabelField($"  Sorting Order: {_furniture.CurrentSortingOrder}");

            var hint = _furniture.GetComponent<SceneFurnitureDefinitionHint>();
            if (hint != null)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField($"  SceneFurnitureDefinitionHint: Present");
                EditorGUILayout.LabelField($"    Hint DefId: {hint.DefinitionId}");
                EditorGUILayout.LabelField($"    Hint Category: {hint.Category}");
                EditorGUILayout.LabelField($"    Hint Interaction: {hint.InteractionType.ToDisplayLabel()}");
                GUI.color = Color.white;

                // Check consistency between hint and definition
                if (hint.DefinitionId != definition.Id)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField(
                        $"  [WARNING] Hint DefId '{hint.DefinitionId}' != Definition Id '{definition.Id}'");
                    GUI.color = Color.white;
                }
                if (hint.InteractionType != FurnitureInteractionType.Unknown &&
                    hint.InteractionType != definition.InteractionType)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField(
                        $"  [WARNING] Hint Interaction '{hint.InteractionType.ToDisplayLabel()}' != Definition '{definition.InteractionType.ToDisplayLabel()}'");
                    GUI.color = Color.white;
                }
            }
            else
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"  SceneFurnitureDefinitionHint: MISSING");
                GUI.color = Color.white;
            }

            var anchor = _furniture.Anchor;
            if (anchor != null)
            {
                EditorGUILayout.LabelField($"  InteractionAnchor: Present (Available: {anchor.IsAvailable})");
            }
            else
            {
                EditorGUILayout.LabelField($"  InteractionAnchor: MISSING");
            }

            EditorGUILayout.Space(4);

            // Component health
            EditorGUILayout.LabelField("Components", EditorStyles.miniBoldLabel);
            var sr = _furniture.GetComponent<SpriteRenderer>();
            EditorGUILayout.LabelField($"  SpriteRenderer: {(sr != null ? "Present" : "MISSING")}");
            if (sr != null && sr.sprite == null)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"  [WARNING] SpriteRenderer has no sprite");
                GUI.color = Color.white;
            }

            var sortingGroup = _furniture.GetComponent<SortingGroup>();
            EditorGUILayout.LabelField($"  SortingGroup: {(sortingGroup != null ? "Present" : "None")}");

            var collider = _furniture.GetComponent<Collider2D>();
            EditorGUILayout.LabelField($"  Collider2D: {(collider != null ? "Present" : "None")}");

            EditorGUILayout.EndVertical();

            // Action buttons
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Definition", GUILayout.Height(24)))
            {
                if (definition != null)
                    EditorGUIUtility.PingObject(definition);
            }
            if (GUILayout.Button("Select Hint", GUILayout.Height(24)))
            {
                if (hint != null)
                    EditorGUIUtility.PingObject(hint);
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
