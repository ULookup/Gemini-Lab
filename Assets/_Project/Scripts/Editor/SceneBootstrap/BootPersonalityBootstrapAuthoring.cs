#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet.Personality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 PersonalityEvolutionBootstrap，
    /// 并把 PersonalityEvolutionRules.asset 绑上。幂等。
    /// </summary>
    public static class BootPersonalityBootstrapAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string RulesAssetPath = "Assets/_Project/ScriptableObjects/PersonalityConfig/PersonalityEvolutionRules.asset";

        [MenuItem("Tools/Gemini-Lab/Author Boot Personality Bootstrap")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("BootstrapRoot");
            if (root == null)
            {
                Debug.LogError("[BootPersonalityBootstrap] 未找到 BootstrapRoot");
                return;
            }

            var boot = root.GetComponent<PersonalityEvolutionBootstrap>();
            if (boot == null)
            {
                boot = root.AddComponent<PersonalityEvolutionBootstrap>();
            }

            var rules = AssetDatabase.LoadAssetAtPath<PersonalityEvolutionRulesSO>(RulesAssetPath);
            if (rules == null)
            {
                Debug.LogError($"[BootPersonalityBootstrap] Rules 未找到：{RulesAssetPath}，请先跑 Author Personality Evolution Rules");
            }
            else
            {
                var so = new SerializedObject(boot);
                so.FindProperty("_rules").objectReferenceValue = rules;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootPersonalityBootstrap] PersonalityEvolutionBootstrap 已挂到 BootstrapRoot");
        }
    }
}
#endif
