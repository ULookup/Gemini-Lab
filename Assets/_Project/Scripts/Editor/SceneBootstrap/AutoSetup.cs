#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    [InitializeOnLoad]
    public static class AutoSetup
    {
        private const string SetupDoneKey = "GeminiLab.AutoSetupDone";
        private const int ExpectedVersion = 46;

        static AutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                int currentVersion = EditorPrefs.GetInt(SetupDoneKey, 0);
                if (currentVersion >= ExpectedVersion) return;

                if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                {
                    Debug.LogWarning("[AutoSetup] 当前处于 PlayMode，暂不执行场景作者化；停止运行后请重新触发 AutoSetup。 ");
                    return;
                }

                try
                {
                    Debug.Log($"[AutoSetup] 版本 {currentVersion} → {ExpectedVersion}，开始升级...");

                    if (currentVersion < 1)
                    {
                        BootEmotionGardenBootstrapAuthoring.Author();

                        if (GameObject.Find("_SceneRoot") == null)
                            WorldMapSceneAuthoring.Author();

                        WorldMapSceneObjectsPatch.Patch();
                        WorldMapGardenZonePatch.Patch();
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 2)
                    {
                        WorldMapSceneObjectsPatch.Patch();
                        WorldMapGardenZonePatch.Patch();
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 3)
                    {
                        FixPetHorizontalOnly();
                    }

                    if (currentVersion < 4)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 5)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 6)
                    {
                        WorldMapGardenZonePatch.Patch();
                    }

                    if (currentVersion < 7)
                    {
                        WorldMapGardenZonePatch.Patch();
                    }

                    if (currentVersion < 8)
                    {
                        WorldMapGardenZonePatch.Patch();
                    }

                    if (currentVersion < 9)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 10)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 11)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 12)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 13)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 14)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 15)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 16)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 17)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 18)
                    {
                        WorldMapSceneObjectsPatch.Patch();
                    }

                    if (currentVersion < 19)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 20)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 21)
                    {
                        WorldMapSceneObjectsPatch.Patch();
                    }

                    if (currentVersion < 22)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 23)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 24)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 25)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 26)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 27)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 28)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 29)
                    {
                        WorldMapDayNightAuthoring.Patch();
                    }

                    if (currentVersion < 30)
                    {
                        WorldMapPetAnimationTriggerAuthoring.Patch();
                    }

                    if (currentVersion < 31)
                    {
                        WorldMapInteractiveObjectAuthoring.Patch();
                    }

                    if (currentVersion < 32)
                    {
                        WorldMapPetAnimationTriggerAuthoring.Patch();
                    }

                    if (currentVersion < 36)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 37)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 38)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 39)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 40)
                    {
                        // 修复每周培育瓶子/星期标签与图鉴锁定态的 Scene 作者化。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 41)
                    {
                        // 瓶子使用固定作者化资源，不再绑定不存在的状态变体。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 42)
                    {
                        // 修复每周空状态、UIbar真实数据、图鉴变体和详情页逐花土壤布局。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 43)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 44)
                    {
                        // 每周培育面板改为单一集中 UIbar，并作者化瓶子选择交互。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 45)
                    {
                        // 修正瓶子选中外圈节点的作者化默认状态。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 46)
                    {
                        // 将瓶子选中效果改为只输出 Sprite Alpha 边缘，避免整瓶染色。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    EditorPrefs.SetInt(SetupDoneKey, ExpectedVersion);
                    Debug.Log($"[AutoSetup] 升级到版本 {ExpectedVersion} 完成。");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AutoSetup] 失败: {e.Message}\n{e.StackTrace}");
                }
            };
        }

        /// <summary>修复 WorldMap 场景中桌宠的横板移动配置。</summary>
        private static void FixPetHorizontalOnly()
        {
            const string scenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
            if (!System.IO.File.Exists(scenePath)) return;

            var scene = EditorSceneManager.GetActiveScene().path == scenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool dirty = false;

            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var input in go.GetComponentsInChildren<PetPlayerInputController>(true))
                {
                    // 移除重复的 RandomWander
                    var wanders = input.GetComponents<RandomWander>();
                    if (wanders.Length > 1)
                    {
                        for (int i = wanders.Length - 1; i >= 0; i--)
                        {
                            if (wanders[i] != null)
                                Object.DestroyImmediate(wanders[i]);
                        }
                        // 重新加一个干净的
                        var w = input.gameObject.AddComponent<RandomWander>();
                        SetupHorizontalWander(w);
                        dirty = true;
                    }
                    else if (wanders.Length == 1)
                    {
                        SetupHorizontalWander(wanders[0]);
                        dirty = true;
                    }

                    // PetPlayerInputController 横板模式
                    var inputSo = new SerializedObject(input);
                    var hProp = inputSo.FindProperty("_horizontalOnly");
                    if (hProp != null && !hProp.boolValue)
                    {
                        hProp.boolValue = true;
                        inputSo.ApplyModifiedProperties();
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[AutoSetup] 桌宠横板移动配置已修复（RandomWander + PetPlayerInputController._horizontalOnly = true）");
            }
        }

        private static void SetupHorizontalWander(RandomWander w)
        {
            var boundsGo = GameObject.Find("PetMovementBounds");
            if (boundsGo == null) return;

            var col = boundsGo.GetComponent<BoxCollider2D>();
            if (col == null) return;

            Vector2 center = (Vector2)boundsGo.transform.position + col.offset;
            Vector2 halfSize = col.size * 0.5f;
            var so = new SerializedObject(w);
            so.FindProperty("_boundsMin").vector2Value = center - halfSize;
            so.FindProperty("_boundsMax").vector2Value = center + halfSize;
            so.FindProperty("_moveSpeed").floatValue = 1.2f;
            so.FindProperty("_horizontalOnly").boolValue = true;
            so.ApplyModifiedProperties();
        }

        public static void Reset()
        {
            EditorPrefs.DeleteKey(SetupDoneKey);
            Debug.Log("[AutoSetup] 已重置，下次编译后重新执行初始化。");
        }
    }
}
#endif
