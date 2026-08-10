#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 为 WorldMap 场景配置数字键动画调试入口。
    /// 不创建可视化触发点，最终策划触发条件确定后再替换这条调试链路。
    /// </summary>
    public static class WorldMapPetAnimationTriggerAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string SceneRootName = "_SceneRoot";
        private const string LegacyRootName = "WorldMapAnimationTriggers";

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Keyboard Pet Animation Debug")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapPetAnimation] 当前处于 PlayMode，跳过数字键调试入口作者化；请停止运行后重试。");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemoveLegacyTriggerObjects();

            GameObject? sceneRoot = GameObject.Find(SceneRootName);
            if (sceneRoot == null)
            {
                Debug.LogError($"[WorldMapPetAnimation] 找不到场景根对象 '{SceneRootName}'，无法配置数字键调试入口。");
                return;
            }

            PetController? angelPet = FindPet("Pet_Angel");
            PetController? devilPet = FindPet("Pet_Devil");
            var controller = sceneRoot.GetComponent<WorldMapPetAnimationTriggerController>();
            if (controller == null)
            {
                controller = sceneRoot.AddComponent<WorldMapPetAnimationTriggerController>();
            }

            controller.ConfigureForAuthoring(angelPet, devilPet);

            EditorUtility.SetDirty(sceneRoot);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapPetAnimation] 已配置数字键动画调试：1~4 天使，5~7 恶魔；未创建任何占位交互物体。 ");
        }

        private static void RemoveLegacyTriggerObjects()
        {
            GameObject? legacyRoot = GameObject.Find(LegacyRootName);
            if (legacyRoot == null)
            {
                return;
            }

            Object.DestroyImmediate(legacyRoot);
            Debug.Log("[WorldMapPetAnimation] 已删除旧的 WorldMapAnimationTriggers 及其临时点位。");
        }

        private static PetController? FindPet(string name)
        {
            GameObject? petObject = GameObject.Find(name);
            return petObject != null
                ? petObject.GetComponent<PetController>() ?? petObject.GetComponentInChildren<PetController>(true)
                : null;
        }
    }
}
#endif
