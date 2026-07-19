#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 从 upstream/dev 的 Apartment 场景中恢复 PhoneChatRoot（因合并时被覆盖丢失）。
    /// </summary>
    public static class PhoneChatRestoreAuthoring
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Restore PhoneChatRoot from dev")]
        public static void Restore()
        {
            // Step 1: Export dev scene via git to a temp file
            string tempPath = Path.GetTempFileName() + ".unity";
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"show upstream/dev:\"Assets/_Project/Scenes/Apartment/Apartment_Main.unity\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Application.dataPath + "/.."
                    }
                };
                process.Start();
                string sceneContent = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                File.WriteAllText(tempPath, sceneContent);

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(sceneContent))
                {
                    Debug.LogError("[PhoneChatRestore] Failed to export dev scene from git.");
                    return;
                }

                // Step 2: Open main scene
                var mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

                // Step 3: Open dev scene additively
                var devScene = EditorSceneManager.OpenScene(tempPath, OpenSceneMode.Additive);

                // Step 4: Find PhoneChatRoot in dev scene
                GameObject? phoneChatRoot = null;
                foreach (var rootGo in devScene.GetRootGameObjects())
                {
                    if (rootGo.name == "PhoneChatRoot")
                    {
                        phoneChatRoot = rootGo;
                        break;
                    }
                }

                if (phoneChatRoot == null)
                {
                    Debug.LogError("[PhoneChatRestore] PhoneChatRoot not found in dev scene.");
                    EditorSceneManager.CloseScene(devScene, true);
                    return;
                }

                // Check if already exists in main scene
                var existing = GameObject.Find("PhoneChatRoot");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }

                // Step 5: Move to main scene
                SceneManager.MoveGameObjectToScene(phoneChatRoot, mainScene);

                // PhoneChatRoot should be at root level (no parent) — its Canvas component handles rendering
                // Make sure it's active and positioned correctly
                phoneChatRoot.transform.SetParent(null);
                phoneChatRoot.SetActive(true);

                // Step 6: Close dev scene, save main scene
                EditorSceneManager.CloseScene(devScene, true);
                EditorSceneManager.MarkSceneDirty(mainScene);
                EditorSceneManager.SaveScene(mainScene);

                Debug.Log("[PhoneChatRestore] PhoneChatRoot restored successfully from dev scene.");
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}
#endif
