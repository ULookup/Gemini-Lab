#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// 一键完成 Apartment 场景的非破坏性设置：补余额 + 挂 RandomWander。
    /// 不会重建设置栏/花园（保留手动 UI 调整）。
    /// </summary>
    public static class ApartmentSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Setup Apartment Scene (non-destructive)")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // ---- 1. Add CoinBalanceDisplay to existing panels ----
            AddCoinBalanceToPanels.Execute();

            // ---- 2. Add RandomWander to Pet_Angel and Pet_Devil ----
            AttachRandomWander("Pet_Angel");
            AttachRandomWander("Pet_Devil");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApartmentSceneSetup] 完成：余额组件 + RandomWander 已添加。");
        }

        private static void AttachRandomWander(string petName)
        {
            var petGo = GameObject.Find(petName);
            if (petGo == null)
            {
                Debug.LogWarning($"[ApartmentSceneSetup] 未找到 {petName}，跳过 RandomWander");
                return;
            }

            var existing = petGo.GetComponent<RandomWander>();
            if (existing != null)
            {
                Debug.Log($"[ApartmentSceneSetup] {petName} 已有 RandomWander，跳过");
                return;
            }

            var wander = petGo.AddComponent<RandomWander>();
            var so = new SerializedObject(wander);

            // Read bounds from existing PetMovementBounds BoxCollider2D
            string boundsName = petName == "Pet_Angel" ? "PetMovementBounds" : "PetMovementBounds_Devil";
            var boundsGo = GameObject.Find(boundsName);
            if (boundsGo != null)
            {
                var col = boundsGo.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    Vector2 center = (Vector2)boundsGo.transform.position + col.offset;
                    Vector2 halfSize = col.size * 0.5f;
                    Vector2 min = center - halfSize;
                    Vector2 max = center + halfSize;

                    so.FindProperty("_boundsMin").vector2Value = min;
                    so.FindProperty("_boundsMax").vector2Value = max;
                }
            }

            so.FindProperty("_moveSpeed").floatValue = 1.2f;
            so.FindProperty("_minWaitSeconds").floatValue = 2f;
            so.FindProperty("_maxWaitSeconds").floatValue = 5f;

            so.ApplyModifiedProperties();
            Debug.Log($"[ApartmentSceneSetup] RandomWander 已添加到 {petName}");
        }
    }
}
#endif
