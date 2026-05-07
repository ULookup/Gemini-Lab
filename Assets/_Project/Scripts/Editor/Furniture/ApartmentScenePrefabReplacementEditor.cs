#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GeminiLab.EditorTools.Furniture
{
    /// <summary>
    /// Replaces selected Apartment scene furniture objects with prefab instances while
    /// preserving their hierarchy placement and transforms.
    /// </summary>
    public static class ApartmentScenePrefabReplacementEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string PrefabRoot = "Assets/_Project/Prefabs/Furniture";

        private static readonly HashSet<FurnitureInteractionType> ReplaceInteractionTypes = new()
        {
            FurnitureInteractionType.SleepInBed,
            FurnitureInteractionType.WorkFocus,
            FurnitureInteractionType.PlayHarp,
            FurnitureInteractionType.PlayGuitar,
            FurnitureInteractionType.PaintAtEasel,
            FurnitureInteractionType.ViewPhotoBoard,
            FurnitureInteractionType.InspectBookshelf,
            FurnitureInteractionType.InspectNightstand,
            FurnitureInteractionType.InspectMirror,
            FurnitureInteractionType.LoungeOnSofa,
            FurnitureInteractionType.SitOnSeat,
            FurnitureInteractionType.RestOnRug,
            FurnitureInteractionType.ObservePlant,
            FurnitureInteractionType.ObserveWindow
        };

        [MenuItem("Tools/GeminiLab/Furniture/Replace Apartment Furniture With Prefabs")]
        public static void ReplaceFromMenu()
        {
            ExecuteInternal(saveScene: true);
        }

        public static void ExecuteBatch()
        {
            ExecuteInternal(saveScene: true);
        }

        private static void ExecuteInternal(bool saveScene)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApartmentSceneFurnitureBindings? bindings = UnityEngine.Object.FindFirstObjectByType<ApartmentSceneFurnitureBindings>(FindObjectsInactive.Include);
            if (bindings is null)
            {
                throw new InvalidOperationException("ApartmentSceneFurnitureBindings was not found in Apartment_Main.unity.");
            }

            SerializedObject bindingsObject = new(bindings);
            SerializedProperty? bindingsProperty = bindingsObject.FindProperty("_bindings");
            if (bindingsProperty is null || !bindingsProperty.isArray)
            {
                throw new InvalidOperationException("ApartmentSceneFurnitureBindings._bindings could not be read.");
            }

            int replaced = 0;
            for (int i = 0; i < bindingsProperty.arraySize; i++)
            {
                SerializedProperty entry = bindingsProperty.GetArrayElementAtIndex(i);
                SerializedProperty targetProperty = entry.FindPropertyRelative("_target");
                GameObject? currentTarget = targetProperty.objectReferenceValue as GameObject;
                if (currentTarget is null)
                {
                    continue;
                }

                string definitionId = entry.FindPropertyRelative("_definitionId").stringValue;
                FurnitureCategory category = (FurnitureCategory)entry.FindPropertyRelative("_category").enumValueIndex;
                FurnitureInteractionType interactionType = (FurnitureInteractionType)entry.FindPropertyRelative("_interactionType").enumValueIndex;

                if (!ShouldReplace(category, interactionType))
                {
                    continue;
                }

                string prefabPath = BuildPrefabAssetPath(category, definitionId);
                GameObject? prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset is null)
                {
                    Debug.LogWarning($"[ApartmentScenePrefabReplacement] Missing prefab for '{definitionId}' at '{prefabPath}'.");
                    continue;
                }

                GameObject? currentSource = PrefabUtility.GetCorrespondingObjectFromSource(currentTarget);
                if (currentSource == prefabAsset)
                {
                    continue;
                }

                Transform oldTransform = currentTarget.transform;
                Transform? oldParent = oldTransform.parent;
                int siblingIndex = oldTransform.GetSiblingIndex();
                Vector3 localPosition = oldTransform.localPosition;
                Quaternion localRotation = oldTransform.localRotation;
                Vector3 localScale = oldTransform.localScale;
                Vector3 worldPosition = oldTransform.position;
                Quaternion worldRotation = oldTransform.rotation;
                string oldName = currentTarget.name;
                string oldTag = currentTarget.tag;
                int oldLayer = currentTarget.layer;
                StaticEditorFlags oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(currentTarget);
                bool oldActive = currentTarget.activeSelf;

                GameObject? newTarget = PrefabUtility.InstantiatePrefab(prefabAsset, scene) as GameObject;
                if (newTarget is null)
                {
                    Debug.LogWarning($"[ApartmentScenePrefabReplacement] Failed to instantiate prefab '{prefabPath}'.");
                    continue;
                }

                if (oldParent is not null)
                {
                    newTarget.transform.SetParent(oldParent, false);
                    newTarget.transform.localPosition = localPosition;
                    newTarget.transform.localRotation = localRotation;
                    newTarget.transform.localScale = localScale;
                    newTarget.transform.SetSiblingIndex(siblingIndex);
                }
                else
                {
                    newTarget.transform.SetParent(null, false);
                    newTarget.transform.position = worldPosition;
                    newTarget.transform.rotation = worldRotation;
                    newTarget.transform.localScale = localScale;
                }

                CopyPresentationAndAuthoringState(currentTarget, newTarget);
                newTarget.name = oldName;
                newTarget.tag = oldTag;
                newTarget.layer = oldLayer;
                newTarget.SetActive(oldActive);
                GameObjectUtility.SetStaticEditorFlags(newTarget, oldStaticFlags);
                targetProperty.objectReferenceValue = newTarget;
                UnityEngine.Object.DestroyImmediate(currentTarget);
                replaced++;
            }

            bindingsObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bindings);
            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ApartmentScenePrefabReplacement] Replaced {replaced} furniture objects with prefab instances.");
        }

        private static void CopyPresentationAndAuthoringState(GameObject source, GameObject destination)
        {
            if (source.TryGetComponent(out SpriteRenderer oldRenderer) && destination.TryGetComponent(out SpriteRenderer newRenderer))
            {
                EditorUtility.CopySerializedIfDifferent(oldRenderer, newRenderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newRenderer);
            }

            if (source.TryGetComponent(out SortingGroup oldSortingGroup) && destination.TryGetComponent(out SortingGroup newSortingGroup))
            {
                EditorUtility.CopySerializedIfDifferent(oldSortingGroup, newSortingGroup);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newSortingGroup);
            }

            if (source.TryGetComponent(out BoxCollider2D oldCollider) && destination.TryGetComponent(out BoxCollider2D newCollider))
            {
                EditorUtility.CopySerializedIfDifferent(oldCollider, newCollider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newCollider);
            }

            if (source.TryGetComponent(out GeminiLab.Modules.Furniture.Furniture oldFurniture) &&
                destination.TryGetComponent(out GeminiLab.Modules.Furniture.Furniture newFurniture))
            {
                EditorUtility.CopySerializedIfDifferent(oldFurniture, newFurniture);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newFurniture);
            }

            if (source.TryGetComponent(out InteractionAnchor oldAnchor) &&
                destination.TryGetComponent(out InteractionAnchor newAnchor))
            {
                EditorUtility.CopySerializedIfDifferent(oldAnchor, newAnchor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newAnchor);
            }

            if (source.TryGetComponent(out SceneFurnitureDefinitionHint oldHint) &&
                destination.TryGetComponent(out SceneFurnitureDefinitionHint newHint))
            {
                EditorUtility.CopySerializedIfDifferent(oldHint, newHint);
                PrefabUtility.RecordPrefabInstancePropertyModifications(newHint);
            }
        }

        private static bool ShouldReplace(FurnitureCategory category, FurnitureInteractionType interactionType)
        {
            if (category == FurnitureCategory.Bed || category == FurnitureCategory.WorkDesk)
            {
                return true;
            }

            return ReplaceInteractionTypes.Contains(interactionType);
        }

        private static string BuildPrefabAssetPath(FurnitureCategory category, string definitionId)
        {
            return $"{PrefabRoot}/{category}/{definitionId}.prefab";
        }
    }
}
#endif
