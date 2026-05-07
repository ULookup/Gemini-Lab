#if UNITY_EDITOR
#nullable enable
using System;
using System.IO;
using GeminiLab.Modules.Furniture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.EditorTools.Furniture
{
    /// <summary>
    /// Creates real furniture definition assets and prefab assets from the current Apartment scene bindings.
    /// </summary>
    public static class ApartmentFurnitureAuthoringBootstrapEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string SpriteRoot = "Assets/_Project/Art/Sprites/Furniture";
        private const string DefinitionRoot = "Assets/_Project/ScriptableObjects/FurnitureConfig";
        private const string PrefabRoot = "Assets/_Project/Prefabs/Furniture";

        [MenuItem("Tools/GeminiLab/Furniture/Bootstrap Apartment Assets")]
        public static void BootstrapFromMenu()
        {
            ExecuteInternal(saveScene: true);
        }

        public static void ExecuteBatch()
        {
            ExecuteInternal(saveScene: true);
        }

        private static void ExecuteInternal(bool saveScene)
        {
            EnsureFolder("Assets/_Project/ScriptableObjects");
            EnsureFolder(DefinitionRoot);
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PrefabRoot);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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

            int createdDefinitions = 0;
            int createdPrefabs = 0;
            int processed = 0;

            for (int i = 0; i < bindingsProperty.arraySize; i++)
            {
                SerializedProperty entry = bindingsProperty.GetArrayElementAtIndex(i);
                GameObject? target = entry.FindPropertyRelative("_target").objectReferenceValue as GameObject;
                if (target is null || !target.TryGetComponent(out SpriteRenderer renderer) || renderer.sprite is null)
                {
                    continue;
                }

                string definitionId = entry.FindPropertyRelative("_definitionId").stringValue;
                FurnitureCategory category = (FurnitureCategory)entry.FindPropertyRelative("_category").enumValueIndex;
                FurnitureInteractionType interactionType = (FurnitureInteractionType)entry.FindPropertyRelative("_interactionType").enumValueIndex;
                float interactionDurationSeconds = entry.FindPropertyRelative("_interactionDurationSeconds").floatValue;
                FurniturePlacementType placementType = (FurniturePlacementType)entry.FindPropertyRelative("_placementType").enumValueIndex;
                Vector2Int occupiedCells = ReadVector2Int(entry.FindPropertyRelative("_occupiedCells"));
                EnvironmentalBuff buff = ReadBuff(entry.FindPropertyRelative("_buff"));
                bool includeInBuildPalette = entry.FindPropertyRelative("_includeInBuildPalette").boolValue;
                bool isAvailable = entry.FindPropertyRelative("_isAvailable").boolValue;

                string definitionPath = BuildDefinitionAssetPath(category, definitionId);
                bool definitionExisted = AssetDatabase.LoadAssetAtPath<FurnitureDefinitionSO>(definitionPath) is not null;
                FurnitureDefinitionSO definition = CreateOrUpdateDefinitionAsset(
                    definitionPath,
                    definitionId,
                    renderer.sprite,
                    category,
                    interactionType,
                    interactionDurationSeconds,
                    placementType,
                    occupiedCells,
                    buff);
                if (!definitionExisted)
                {
                    createdDefinitions++;
                }

                EnsureAuthoringComponents(target, definition, isAvailable, includeInBuildPalette, category, interactionType, interactionDurationSeconds, placementType, occupiedCells, buff);

                string prefabPath = BuildPrefabAssetPath(category, definitionId);
                EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/') ?? PrefabRoot);
                bool prefabExisted = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) is not null;
                PrefabUtility.SaveAsPrefabAssetAndConnect(target, prefabPath, InteractionMode.AutomatedAction);
                if (!prefabExisted)
                {
                    createdPrefabs++;
                }

                processed++;
            }

            EnsureDefinitionsAndPrefabsForAllFurnitureSprites(ref createdDefinitions, ref createdPrefabs, ref processed);

            bindingsObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bindings);
            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ApartmentFurnitureAuthoringBootstrap] Processed={processed}, CreatedDefinitions={createdDefinitions}, CreatedPrefabs={createdPrefabs}");
        }

        private static void EnsureDefinitionsAndPrefabsForAllFurnitureSprites(ref int createdDefinitions, ref int createdPrefabs, ref int processed)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { SpriteRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string spritePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite is null)
                {
                    continue;
                }

                string definitionId = sprite.name;
                string prefabPath = BuildPrefabAssetPath(InferCategoryFromSpritePath(spritePath, definitionId), definitionId);
                bool prefabExisted = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) is not null;
                bool definitionExisted = AssetDatabase.LoadAssetAtPath<FurnitureDefinitionSO>(BuildDefinitionAssetPath(InferCategoryFromSpritePath(spritePath, definitionId), definitionId)) is not null;
                if (prefabExisted && definitionExisted)
                {
                    continue;
                }

                FurnitureCategory category = InferCategoryFromSpritePath(spritePath, definitionId);
                FurnitureInteractionType interactionType = InferInteractionType(definitionId, category);
                float interactionDurationSeconds = InferInteractionDuration(interactionType);
                FurniturePlacementType placementType = InferPlacementType(definitionId);
                Vector2Int occupiedCells = InferOccupiedCells(category);
                EnvironmentalBuff buff = InferBuff(definitionId, category);

                string definitionPath = BuildDefinitionAssetPath(category, definitionId);
                FurnitureDefinitionSO definition = CreateOrUpdateDefinitionAsset(
                    definitionPath,
                    definitionId,
                    sprite,
                    category,
                    interactionType,
                    interactionDurationSeconds,
                    placementType,
                    occupiedCells,
                    buff);
                if (!definitionExisted)
                {
                    createdDefinitions++;
                }

                if (!prefabExisted)
                {
                    EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/') ?? PrefabRoot);
                    GameObject temp = BuildPrefabSourceObject(definition, occupiedCells);
                    PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
                    UnityEngine.Object.DestroyImmediate(temp);
                    createdPrefabs++;
                }

                processed++;
            }
        }

        private static GameObject BuildPrefabSourceObject(FurnitureDefinitionSO definition, Vector2Int occupiedCells)
        {
            GameObject temp = new(definition.Id);
            SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
            renderer.sprite = definition.Sprite;
            _ = temp.AddComponent<SortingGroup>();
            BoxCollider2D collider = temp.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(
                Mathf.Max(0.5f, occupiedCells.x),
                Mathf.Max(0.5f, occupiedCells.y));

            InteractionAnchor anchor = temp.AddComponent<InteractionAnchor>();
            GeminiLab.Modules.Furniture.Furniture furniture = temp.AddComponent<GeminiLab.Modules.Furniture.Furniture>();
            SerializedObject furnitureObject = new(furniture);
            furnitureObject.FindProperty("_instanceId").stringValue = Guid.NewGuid().ToString("N");
            furnitureObject.FindProperty("_definition").objectReferenceValue = definition;
            furnitureObject.FindProperty("_anchor").objectReferenceValue = anchor;
            furnitureObject.ApplyModifiedPropertiesWithoutUndo();
            furniture.Initialize(Guid.NewGuid().ToString("N"), definition);
            return temp;
        }

        private static FurnitureDefinitionSO CreateOrUpdateDefinitionAsset(
            string assetPath,
            string definitionId,
            Sprite sprite,
            FurnitureCategory category,
            FurnitureInteractionType interactionType,
            float interactionDurationSeconds,
            FurniturePlacementType placementType,
            Vector2Int occupiedCells,
            EnvironmentalBuff buff)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? DefinitionRoot);

            FurnitureDefinitionSO? definition = AssetDatabase.LoadAssetAtPath<FurnitureDefinitionSO>(assetPath);
            if (definition is null)
            {
                definition = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            SerializedObject definitionObject = new(definition);
            definitionObject.FindProperty("_id").stringValue = definitionId;
            definitionObject.FindProperty("_sprite").objectReferenceValue = sprite;
            definitionObject.FindProperty("_category").enumValueIndex = (int)category;
            definitionObject.FindProperty("_interactionType").enumValueIndex = (int)interactionType;
            definitionObject.FindProperty("_interactionDurationSeconds").floatValue = interactionDurationSeconds > 0f ? interactionDurationSeconds : 1f;
            definitionObject.FindProperty("_placementType").enumValueIndex = (int)placementType;

            SerializedProperty occupiedCellsProperty = definitionObject.FindProperty("_occupiedCells");
            occupiedCellsProperty.FindPropertyRelative("x").intValue = Mathf.Max(1, occupiedCells.x);
            occupiedCellsProperty.FindPropertyRelative("y").intValue = Mathf.Max(1, occupiedCells.y);

            SerializedProperty buffProperty = definitionObject.FindProperty("_buff");
            buffProperty.FindPropertyRelative("MoodDelta").floatValue = buff.MoodDelta;
            buffProperty.FindPropertyRelative("EnergyDelta").floatValue = buff.EnergyDelta;

            definitionObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void EnsureAuthoringComponents(
            GameObject target,
            FurnitureDefinitionSO definition,
            bool isAvailable,
            bool includeInBuildPalette,
            FurnitureCategory category,
            FurnitureInteractionType interactionType,
            float interactionDurationSeconds,
            FurniturePlacementType placementType,
            Vector2Int occupiedCells,
            EnvironmentalBuff buff)
        {
            if (!target.TryGetComponent(out GeminiLab.Modules.Furniture.Furniture? furniture))
            {
                furniture = target.AddComponent<GeminiLab.Modules.Furniture.Furniture>();
            }

            if (!target.TryGetComponent(out InteractionAnchor? anchor))
            {
                anchor = target.AddComponent<InteractionAnchor>();
            }

            if (!target.TryGetComponent(out SceneFurnitureDefinitionHint? hint))
            {
                hint = target.AddComponent<SceneFurnitureDefinitionHint>();
            }

            if (!target.TryGetComponent(out SortingGroup? sortingGroup))
            {
                sortingGroup = target.AddComponent<SortingGroup>();
            }

            if (!target.TryGetComponent(out BoxCollider2D? collider))
            {
                collider = target.AddComponent<BoxCollider2D>();
            }

            collider.size = new Vector2(
                Mathf.Max(0.5f, occupiedCells.x),
                Mathf.Max(0.5f, occupiedCells.y));

            SerializedObject furnitureObject = new(furniture);
            SerializedProperty instanceIdProperty = furnitureObject.FindProperty("_instanceId");
            if (string.IsNullOrWhiteSpace(instanceIdProperty.stringValue))
            {
                instanceIdProperty.stringValue = Guid.NewGuid().ToString("N");
            }

            furnitureObject.FindProperty("_definition").objectReferenceValue = definition;
            furnitureObject.FindProperty("_anchor").objectReferenceValue = anchor;
            furnitureObject.ApplyModifiedPropertiesWithoutUndo();

            anchor.SetAvailable(isAvailable);
            hint.Configure(
                definition.Id,
                category,
                interactionType,
                interactionDurationSeconds,
                placementType,
                occupiedCells,
                buff,
                includeInBuildPalette);

            furniture.Initialize(instanceIdProperty.stringValue, definition);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(furniture);
            EditorUtility.SetDirty(anchor);
            EditorUtility.SetDirty(hint);
            EditorUtility.SetDirty(collider);
        }

        private static string BuildDefinitionAssetPath(FurnitureCategory category, string definitionId)
        {
            string categoryFolder = SanitizeFileName(category.ToString());
            string fileName = SanitizeFileName(definitionId);
            return $"{DefinitionRoot}/{categoryFolder}/{fileName}.asset";
        }

        private static string BuildPrefabAssetPath(FurnitureCategory category, string definitionId)
        {
            string categoryFolder = SanitizeFileName(category.ToString());
            string fileName = SanitizeFileName(definitionId);
            return $"{PrefabRoot}/{categoryFolder}/{fileName}.prefab";
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            string normalized = assetFolderPath.Replace('\\', '/');
            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Vector2Int ReadVector2Int(SerializedProperty property)
        {
            return new Vector2Int(
                property.FindPropertyRelative("x").intValue,
                property.FindPropertyRelative("y").intValue);
        }

        private static EnvironmentalBuff ReadBuff(SerializedProperty property)
        {
            return new EnvironmentalBuff
            {
                MoodDelta = property.FindPropertyRelative("MoodDelta").floatValue,
                EnergyDelta = property.FindPropertyRelative("EnergyDelta").floatValue
            };
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Trim();
        }

        private static FurnitureCategory InferCategoryFromSpritePath(string spritePath, string definitionId)
        {
            if (spritePath.Contains("/WorkDesk/"))
            {
                return FurnitureCategory.WorkDesk;
            }

            if (spritePath.Contains("/Bed/"))
            {
                return FurnitureCategory.Bed;
            }

            if (spritePath.Contains("/Leisure/"))
            {
                return FurnitureCategory.Leisure;
            }

            if (spritePath.Contains("/Decoration/"))
            {
                return FurnitureCategory.Decoration;
            }

            if (definitionId.Contains("工作桌", StringComparison.OrdinalIgnoreCase) ||
                definitionId.Contains("工作台", StringComparison.OrdinalIgnoreCase) ||
                definitionId.Contains("书桌", StringComparison.OrdinalIgnoreCase))
            {
                return FurnitureCategory.WorkDesk;
            }

            if (definitionId.Contains("床", StringComparison.OrdinalIgnoreCase))
            {
                return FurnitureCategory.Bed;
            }

            if (definitionId.Contains("休闲", StringComparison.OrdinalIgnoreCase) ||
                definitionId.Contains("竖琴", StringComparison.OrdinalIgnoreCase))
            {
                return FurnitureCategory.Leisure;
            }

            return FurnitureCategory.Decoration;
        }

        private static FurnitureInteractionType InferInteractionType(string definitionId, FurnitureCategory category)
        {
            if (definitionId.Contains("床", StringComparison.OrdinalIgnoreCase))
            {
                return FurnitureInteractionType.SleepInBed;
            }

            if (ContainsAnyKeyword(definitionId, "工作桌", "工作台", "书桌", "公告板"))
            {
                return FurnitureInteractionType.WorkFocus;
            }

            if (ContainsAnyKeyword(definitionId, "高书柜", "书柜"))
            {
                return FurnitureInteractionType.InspectBookshelf;
            }

            if (ContainsAnyKeyword(definitionId, "镜子", "小圆镜"))
            {
                return FurnitureInteractionType.InspectMirror;
            }

            if (ContainsAnyKeyword(definitionId, "床头柜"))
            {
                return FurnitureInteractionType.InspectNightstand;
            }

            if (ContainsAnyKeyword(definitionId, "竖琴"))
            {
                return FurnitureInteractionType.PlayHarp;
            }

            if (ContainsAnyKeyword(definitionId, "吉他"))
            {
                return FurnitureInteractionType.PlayGuitar;
            }

            if (ContainsAnyKeyword(definitionId, "画架"))
            {
                return FurnitureInteractionType.PaintAtEasel;
            }

            if (ContainsAnyKeyword(definitionId, "照片板"))
            {
                return FurnitureInteractionType.ViewPhotoBoard;
            }

            if (ContainsAnyKeyword(definitionId, "盆栽", "窗台上的盆栽"))
            {
                return FurnitureInteractionType.ObservePlant;
            }

            if (ContainsAnyKeyword(definitionId, "窗台"))
            {
                return FurnitureInteractionType.ObserveWindow;
            }

            if (ContainsAnyKeyword(definitionId, "玩偶"))
            {
                return FurnitureInteractionType.InspectToy;
            }

            if (ContainsAnyKeyword(definitionId, "枕头"))
            {
                return FurnitureInteractionType.ArrangePillow;
            }

            if (ContainsAnyKeyword(definitionId, "纸张"))
            {
                return FurnitureInteractionType.InspectPapers;
            }

            if (ContainsAnyKeyword(definitionId, "音响", "耳机", "乐器"))
            {
                return FurnitureInteractionType.ListenToAudio;
            }

            if (ContainsAnyKeyword(definitionId, "左下小家具", "左下窄家具", "柜子", "储物", "边柜"))
            {
                return FurnitureInteractionType.OrganizeStorage;
            }

            if (ContainsAnyKeyword(definitionId, "地毯", "园地毯"))
            {
                return FurnitureInteractionType.RestOnRug;
            }

            if (ContainsAnyKeyword(definitionId, "凳子", "椅子"))
            {
                return FurnitureInteractionType.SitOnSeat;
            }

            if (ContainsAnyKeyword(definitionId, "沙发"))
            {
                return FurnitureInteractionType.LoungeOnSofa;
            }

            return category switch
            {
                FurnitureCategory.Bed => FurnitureInteractionType.SleepRest,
                FurnitureCategory.WorkDesk => FurnitureInteractionType.WorkFocus,
                FurnitureCategory.Leisure => FurnitureInteractionType.LeisureEngage,
                FurnitureCategory.Decoration => FurnitureInteractionType.DecorInspect,
                _ => FurnitureInteractionType.Unknown
            };
        }

        private static float InferInteractionDuration(FurnitureInteractionType interactionType)
        {
            return interactionType switch
            {
                FurnitureInteractionType.SleepRest => 2.5f,
                FurnitureInteractionType.SleepInBed => 3.2f,
                FurnitureInteractionType.WorkFocus => 1.4f,
                FurnitureInteractionType.DecorInspect => 1.6f,
                FurnitureInteractionType.LeisureEngage => 2.0f,
                FurnitureInteractionType.InspectBookshelf => 2.2f,
                FurnitureInteractionType.InspectMirror => 1.5f,
                FurnitureInteractionType.InspectNightstand => 1.8f,
                FurnitureInteractionType.PlayHarp => 2.4f,
                FurnitureInteractionType.PlayGuitar => 2.3f,
                FurnitureInteractionType.PaintAtEasel => 2.2f,
                FurnitureInteractionType.ViewPhotoBoard => 1.8f,
                FurnitureInteractionType.ObservePlant => 1.5f,
                FurnitureInteractionType.ObserveWindow => 1.8f,
                FurnitureInteractionType.InspectToy => 1.6f,
                FurnitureInteractionType.ArrangePillow => 1.4f,
                FurnitureInteractionType.InspectPapers => 1.4f,
                FurnitureInteractionType.ListenToAudio => 1.9f,
                FurnitureInteractionType.OrganizeStorage => 1.7f,
                FurnitureInteractionType.RestOnRug => 2.1f,
                FurnitureInteractionType.SitOnSeat => 1.6f,
                FurnitureInteractionType.LoungeOnSofa => 2.4f,
                _ => 1.0f
            };
        }

        private static FurniturePlacementType InferPlacementType(string definitionId)
        {
            return ContainsAnyKeyword(definitionId, "墙", "壁", "公告板")
                ? FurniturePlacementType.Wall
                : FurniturePlacementType.Floor;
        }

        private static Vector2Int InferOccupiedCells(FurnitureCategory category)
        {
            return category switch
            {
                FurnitureCategory.Bed => new Vector2Int(2, 1),
                FurnitureCategory.WorkDesk => new Vector2Int(2, 1),
                _ => Vector2Int.one
            };
        }

        private static EnvironmentalBuff InferBuff(string definitionId, FurnitureCategory category)
        {
            if (ContainsAnyKeyword(definitionId, "床头柜"))
            {
                return new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 1f };
            }

            if (ContainsAnyKeyword(definitionId, "竖琴"))
            {
                return new EnvironmentalBuff { MoodDelta = 5f, EnergyDelta = 0f };
            }

            if (ContainsAnyKeyword(definitionId, "恶魔"))
            {
                return new EnvironmentalBuff { MoodDelta = 3f, EnergyDelta = -1f };
            }

            return category switch
            {
                FurnitureCategory.Bed => new EnvironmentalBuff { MoodDelta = 2f, EnergyDelta = 6f },
                FurnitureCategory.WorkDesk => new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 2f },
                FurnitureCategory.Leisure => new EnvironmentalBuff { MoodDelta = 4f, EnergyDelta = 0f },
                FurnitureCategory.Decoration => new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 0f },
                _ => default
            };
        }

        private static bool ContainsAnyKeyword(string source, params string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (source.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
