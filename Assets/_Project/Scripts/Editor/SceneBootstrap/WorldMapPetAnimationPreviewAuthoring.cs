#nullable enable
#if UNITY_EDITOR
using System;
using System.Linq;
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 创建只包含室外天使、恶魔和必要相机的动画调整场景。
    /// 预览场景与 WorldMap_Main 共用 WorldMap/Pet 下的 AnimatorController 和 AnimationClip，
    /// 不复制动画资源，也不连接 Apartment 的宠物视觉资源。
    /// </summary>
    public static class WorldMapPetAnimationPreviewAuthoring
    {
        private const string MainScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string PreviewScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_PetAnimationPreview.unity";
        private const string AnimationRoot = "Assets/_Project/Animations/WorldMap/Pet";
        private const string AngelControllerPath = AnimationRoot + "/WorldMap_Angel.controller";
        private const string DevilControllerPath = AnimationRoot + "/WorldMap_Devil.controller";
        private const string AngelIdleFolder = "Assets/_Project/Art/WorldMap/pets/天使室外/待机";
        private const string DevilIdleFolder = "Assets/_Project/Art/WorldMap/pets/恶魔室外/待机";
        private const string SceneRootName = "_SceneRoot";

        [MenuItem("Tools/Gemini-Lab/WorldMap/Create Pet Animation Preview Scene")]
        public static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapPetAnimationPreview] 当前处于 PlayMode，已跳过场景作者化。请停止运行后重试。");
                return;
            }

            RuntimeAnimatorController? angelController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AngelControllerPath);
            RuntimeAnimatorController? devilController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DevilControllerPath);
            if (angelController == null || devilController == null)
            {
                Debug.LogError(
                    $"[WorldMapPetAnimationPreview] 缺少共享 AnimatorController：" +
                    $" angel={angelController != null}, devil={devilController != null}");
                return;
            }

            Scene mainScene = EnsureMainSceneBindings(angelController, devilController);
            if (mainScene.IsValid() && mainScene.isLoaded &&
                mainScene.path != EditorSceneManager.GetActiveScene().path)
            {
                EditorSceneManager.CloseScene(mainScene, true);
            }

            Scene previewScene = OpenOrCreatePreviewScene();
            GameObject sceneRoot = EnsureSceneRoot(previewScene);
            EnsurePreviewCamera(previewScene, sceneRoot);
            EnsurePreviewPet(
                previewScene,
                sceneRoot,
                "Pet_Angel",
                new Vector3(-1.45f, -0.25f, 0f),
                LoadFirstSprite(AngelIdleFolder),
                angelController);
            EnsurePreviewPet(
                previewScene,
                sceneRoot,
                "Pet_Devil",
                new Vector3(1.45f, -0.25f, 0f),
                LoadFirstSprite(DevilIdleFolder),
                devilController);

            SceneManager.SetActiveScene(previewScene);
            EditorSceneManager.MarkSceneDirty(previewScene);
            EditorSceneManager.SaveScene(previewScene, PreviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject? angel = FindObjectInScene(previewScene, "Pet_Angel");
            Selection.activeGameObject = angel;
            Debug.Log(
                "[WorldMapPetAnimationPreview] 已创建/更新动画预览场景。" +
                " 预览场景和 WorldMap_Main 共用 WorldMap/Pet 下的 Controller 与 Clip；" +
                " 请在共享动画资源上调整动画以同步室外主场景。 ");
        }

        private static Scene EnsureMainSceneBindings(
            RuntimeAnimatorController angelController,
            RuntimeAnimatorController devilController)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            Scene mainScene;
            bool openedAdditively = false;
            if (activeScene.path == MainScenePath)
            {
                mainScene = activeScene;
            }
            else
            {
                mainScene = SceneManager.GetSceneByPath(MainScenePath);
                if (!mainScene.IsValid() || !mainScene.isLoaded)
                {
                    mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
                    openedAdditively = true;
                }
            }

            BindMainScenePet(mainScene, "Pet_Angel", angelController);
            BindMainScenePet(mainScene, "Pet_Devil", devilController);
            EditorSceneManager.MarkSceneDirty(mainScene);
            EditorSceneManager.SaveScene(mainScene);

            if (openedAdditively)
            {
                Debug.Log("[WorldMapPetAnimationPreview] 已校准 WorldMap_Main 的共享动画引用。");
            }

            return mainScene;
        }

        private static void BindMainScenePet(
            Scene scene,
            string petName,
            RuntimeAnimatorController controller)
        {
            GameObject? pet = FindObjectInScene(scene, petName);
            if (pet == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimationPreview] WorldMap_Main 未找到 {petName}，未修改该对象。");
                return;
            }

            Animator animator = pet.GetComponent<Animator>() ?? pet.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            EditorUtility.SetDirty(animator);

            PetController? petController = pet.GetComponent<PetController>();
            if (petController == null)
            {
                return;
            }

            SerializedObject serialized = new(petController);
            SerializedProperty? movementController = serialized.FindProperty("_movementController");
            if (movementController != null)
            {
                movementController.objectReferenceValue = controller;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(petController);
        }

        private static Scene OpenOrCreatePreviewScene()
        {
            Scene previewScene = SceneManager.GetSceneByPath(PreviewScenePath);
            if (previewScene.IsValid() && previewScene.isLoaded)
            {
                return previewScene;
            }

            if (System.IO.File.Exists(PreviewScenePath))
            {
                return EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static GameObject EnsureSceneRoot(Scene scene)
        {
            GameObject? sceneRoot = FindObjectInScene(scene, SceneRootName);
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject(SceneRootName);
                SceneManager.MoveGameObjectToScene(sceneRoot, scene);
            }

            sceneRoot.transform.position = Vector3.zero;
            sceneRoot.transform.rotation = Quaternion.identity;
            sceneRoot.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(sceneRoot);
            return sceneRoot;
        }

        private static void EnsurePreviewCamera(Scene scene, GameObject sceneRoot)
        {
            GameObject? cameraObject = FindObjectInScene(scene, "Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }

            cameraObject.transform.SetParent(sceneRoot.transform, true);

            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;

            Camera? camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            if (camera == null)
            {
                Debug.LogError("[WorldMapPetAnimationPreview] 无法为预览场景 Main Camera 添加 Camera 组件。");
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 3.35f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.23f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            EditorUtility.SetDirty(cameraObject);
            EditorUtility.SetDirty(camera);
        }

        private static void EnsurePreviewPet(
            Scene scene,
            GameObject sceneRoot,
            string petName,
            Vector3 defaultPosition,
            Sprite? idleSprite,
            RuntimeAnimatorController controller)
        {
            GameObject? pet = FindObjectInScene(scene, petName);
            if (pet == null)
            {
                pet = new GameObject(petName);
                SceneManager.MoveGameObjectToScene(pet, scene);
                pet.transform.position = defaultPosition;
                pet.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }

            pet.transform.SetParent(sceneRoot.transform, true);

            SpriteRenderer? renderer = pet.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = pet.AddComponent<SpriteRenderer>();
            }

            if (renderer == null)
            {
                Debug.LogError($"[WorldMapPetAnimationPreview] 无法为 {petName} 添加 SpriteRenderer。");
                return;
            }

            renderer.sprite = idleSprite;
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.sortingOrder = 10;
            EditorUtility.SetDirty(renderer);

            Animator? animator = pet.GetComponent<Animator>();
            if (animator == null)
            {
                animator = pet.AddComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError($"[WorldMapPetAnimationPreview] 无法为 {petName} 添加 Animator。");
                return;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(pet);
        }

        private static Sprite? LoadFirstSprite(string folder)
        {
            string? path = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault();
            return path == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject? FindObjectInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform? match = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == objectName);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }
    }
}
#endif
