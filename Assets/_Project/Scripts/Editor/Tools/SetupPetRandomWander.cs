#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// 为 Pet_Angel 和 Pet_Devil 挂载 RandomWander 组件并配置漫游边界。
    /// </summary>
    public static class SetupPetRandomWander
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Setup Random Wander On Pets")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = false;

            changed |= SetupPet("Pet_Angel",
                boundsMin: new Vector2(2f, -3.5f),
                boundsMax: new Vector2(8.5f, 1f),
                moveSpeed: 1.5f);

            changed |= SetupPet("Pet_Devil",
                boundsMin: new Vector2(-12f, -3.5f),
                boundsMax: new Vector2(-5f, 1f),
                moveSpeed: 1.5f);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[SetupRandomWander] 已为 Pet_Angel / Pet_Devil 挂载 RandomWander");
            }
            else
            {
                Debug.Log("[SetupRandomWander] RandomWander 已存在，跳过");
            }
        }

        private static bool SetupPet(string petName, Vector2 boundsMin, Vector2 boundsMax, float moveSpeed)
        {
            var go = GameObject.Find(petName);
            if (go == null)
            {
                Debug.LogError($"[SetupRandomWander] 找不到 {petName}");
                return false;
            }

            var wander = go.GetComponent<RandomWander>();
            if (wander != null)
            {
                Debug.Log($"[SetupRandomWander] {petName} 已有 RandomWander，更新边界");
            }
            else
            {
                wander = Undo.AddComponent<RandomWander>(go);
            }

            var so = new SerializedObject(wander);
            SetVector2(so, "_boundsMin", boundsMin);
            SetVector2(so, "_boundsMax", boundsMax);
            var sp = so.FindProperty("_moveSpeed");
            if (sp != null) sp.floatValue = moveSpeed;
            so.ApplyModifiedProperties();

            return true;
        }

        private static void SetVector2(SerializedObject so, string name, Vector2 value)
        {
            var sp = so.FindProperty(name);
            if (sp != null) sp.vector2Value = value;
        }
    }
}
#endif
