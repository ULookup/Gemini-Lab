#nullable enable
#if UNITY_EDITOR
using System.Linq;
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 把 Apartment / WorldMap 场景的宠物从单宠扩展为双宠。
    /// Apartment：已有 Pet_Angel 则给它写上 PetId.Angel 并复制出 Pet_Devil（位置偏移）。
    /// WorldMap：如果没有宠物则创建两只（用 SpriteRenderer 占位）。
    /// 幂等：重复执行只会修正 PetId / 避免重复创建。
    /// </summary>
    public static class DualPetAuthoring
    {
        private const string ApartmentScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string WorldMapScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Author Dual Pets (Apartment + WorldMap)")]
        public static void Author()
        {
            AuthorApartment();
            AuthorWorldMap();
        }

        private static void AuthorApartment()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ApartmentScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ApartmentScenePath, OpenSceneMode.Single);

            var all = Object.FindObjectsOfType<PetController>(includeInactive: true);
            PetController? angel = all.FirstOrDefault(p => p.gameObject.name.Contains("Angel"))
                                     ?? all.FirstOrDefault();
            if (angel == null)
            {
                Debug.LogError("[DualPetAuthoring] Apartment: 未找到任何 PetController");
                return;
            }

            SetPetId(angel, PetId.Angel);
            if (!angel.gameObject.name.Contains("Angel"))
            {
                angel.gameObject.name = "Pet_Angel";
            }

            var devilExisting = all.FirstOrDefault(p => p.gameObject.name.Contains("Devil"));
            if (devilExisting != null)
            {
                SetPetId(devilExisting, PetId.Devil);
                Debug.Log("[DualPetAuthoring] Apartment: Pet_Devil 已存在，已修正 PetId");
            }
            else
            {
                var devil = Object.Instantiate(angel.gameObject, angel.transform.parent);
                devil.name = "Pet_Devil";
                devil.transform.position = angel.transform.position + new Vector3(1.2f, 0f, 0f);
                var devilCtrl = devil.GetComponent<PetController>();
                SetPetId(devilCtrl, PetId.Devil);
                // 视觉占位：给 Devil 染一个暖色调让它能一眼和 Angel 区分
                var sr = devil.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(1f, 0.65f, 0.65f, 1f);
                }

                Debug.Log("[DualPetAuthoring] Apartment: 已复制出 Pet_Devil 占位");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void AuthorWorldMap()
        {
            var scene = EditorSceneManager.GetActiveScene().path == WorldMapScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(WorldMapScenePath, OpenSceneMode.Single);

            var sceneRoot = GameObject.Find("_SceneRoot");
            if (sceneRoot == null)
            {
                Debug.LogError("[DualPetAuthoring] WorldMap: 未找到 _SceneRoot，请先跑 Author WorldMap Scene");
                return;
            }

            EnsureWorldMapPet(sceneRoot.transform, "Pet_Angel", PetId.Angel, new Vector3(-2f, -1.5f, 0f), new Color(1f, 1f, 1f, 1f));
            EnsureWorldMapPet(sceneRoot.transform, "Pet_Devil", PetId.Devil, new Vector3(-0.8f, -1.5f, 0f), new Color(1f, 0.65f, 0.65f, 1f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DualPetAuthoring] WorldMap: 双宠占位就位");
        }

        private static void EnsureWorldMapPet(Transform parent, string name, PetId petId, Vector3 pos, Color tint)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var ctrl = existing.GetComponent<PetController>();
                if (ctrl == null) ctrl = existing.gameObject.AddComponent<PetController>();
                SetPetId(ctrl, petId);
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = tint;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1f, 1.2f);
            sr.sortingOrder = 1000;
            var ctrlNew = go.AddComponent<PetController>();
            SetPetId(ctrlNew, petId);
        }

        private static void SetPetId(PetController ctrl, PetId petId)
        {
            var so = new SerializedObject(ctrl);
            var prop = so.FindProperty("_petId");
            if (prop != null)
            {
                prop.intValue = (int)petId;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
