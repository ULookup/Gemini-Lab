#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 生成 ScriptableObjects/PersonalityConfig/PersonalityEvolutionRules.asset，
    /// 预填一批"塔罗 / 家具交互 → 性格维度"规则作为首轮可玩平衡。
    /// </summary>
    public static class PersonalityRulesAuthoring
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/PersonalityConfig";
        private const string AssetPath = Folder + "/PersonalityEvolutionRules.asset";

        [MenuItem("Tools/Gemini-Lab/Author Personality Evolution Rules")]
        public static void Author()
        {
            EnsureFolder(Folder);

            var rules = AssetDatabase.LoadAssetAtPath<PersonalityEvolutionRulesSO>(AssetPath);
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<PersonalityEvolutionRulesSO>();
                AssetDatabase.CreateAsset(rules, AssetPath);
            }

            var tarotRules = new List<PersonalityEvolutionRulesSO.TarotRule>
            {
                // 大部分正位 → Angel 加善良/正直/冷静；逆位 → Devil 加邪恶/勇敢/好奇
                new() { CardId = "", Filter = PersonalityEvolutionRulesSO.OrientationFilter.UprightOnly,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Angel,
                    Delta = new PersonalityVector { Kindness = 1f, Integrity = 0.5f, Calmness = 0.3f } },
                new() { CardId = "", Filter = PersonalityEvolutionRulesSO.OrientationFilter.ReversedOnly,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Devil,
                    Delta = new PersonalityVector { Evilness = 1f, Bravery = 0.4f, Curiosity = 0.4f } },

                // 几张特征牌的特写规则
                new() { CardId = "the_fool", Filter = PersonalityEvolutionRulesSO.OrientationFilter.Both,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Curiosity = 1.5f, Bravery = 0.5f } },
                new() { CardId = "the_hermit", Filter = PersonalityEvolutionRulesSO.OrientationFilter.UprightOnly,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Calmness = 1f, Shyness = 0.5f } },
                new() { CardId = "strength", Filter = PersonalityEvolutionRulesSO.OrientationFilter.UprightOnly,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Bravery = 1f, Calmness = 0.5f } },
                new() { CardId = "the_devil", Filter = PersonalityEvolutionRulesSO.OrientationFilter.UprightOnly,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Devil,
                    Delta = new PersonalityVector { Evilness = 1.5f, Curiosity = 0.5f } }
            };

            var furnitureRules = new List<PersonalityEvolutionRulesSO.FurnitureInteractionRule>
            {
                new() { Type = FurnitureInteractionType.InspectBookshelf,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Calmness = 0.5f, Integrity = 0.3f } },
                new() { Type = FurnitureInteractionType.PlayHarp,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Angel,
                    Delta = new PersonalityVector { Kindness = 0.4f, Calmness = 0.3f } },
                new() { Type = FurnitureInteractionType.SleepInBed,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Calmness = 0.3f, Shyness = 0.2f } },
                new() { Type = FurnitureInteractionType.ObserveWindow,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Curiosity = 0.5f } },
                new() { Type = FurnitureInteractionType.InspectMirror,
                    TargetPet = PersonalityEvolutionRulesSO.PetIdFilter.Any,
                    Delta = new PersonalityVector { Shyness = 0.3f, Evilness = 0.2f } }
            };

            var so = new SerializedObject(rules);
            WriteTarotRules(so.FindProperty("TarotRules"), tarotRules);
            WriteFurnitureRules(so.FindProperty("FurnitureRules"), furnitureRules);
            so.FindProperty("GlobalDeltaScale").floatValue = 0.02f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(rules);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PersonalityRulesAuthoring] 已生成 / 刷新 {AssetPath}（tarot={tarotRules.Count}, furniture={furnitureRules.Count}）");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }

        private static void WriteTarotRules(SerializedProperty prop, List<PersonalityEvolutionRulesSO.TarotRule> rules)
        {
            prop.arraySize = rules.Count;
            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("CardId").stringValue = r.CardId;
                el.FindPropertyRelative("Filter").enumValueIndex = (int)r.Filter;
                el.FindPropertyRelative("TargetPet").enumValueIndex = (int)r.TargetPet;
                WriteVector(el.FindPropertyRelative("Delta"), r.Delta);
            }
        }

        private static void WriteFurnitureRules(SerializedProperty prop, List<PersonalityEvolutionRulesSO.FurnitureInteractionRule> rules)
        {
            prop.arraySize = rules.Count;
            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("Type").enumValueIndex = (int)r.Type;
                el.FindPropertyRelative("TargetPet").enumValueIndex = (int)r.TargetPet;
                WriteVector(el.FindPropertyRelative("Delta"), r.Delta);
            }
        }

        private static void WriteVector(SerializedProperty p, PersonalityVector v)
        {
            p.FindPropertyRelative("Kindness").floatValue = v.Kindness;
            p.FindPropertyRelative("Evilness").floatValue = v.Evilness;
            p.FindPropertyRelative("Calmness").floatValue = v.Calmness;
            p.FindPropertyRelative("Bravery").floatValue = v.Bravery;
            p.FindPropertyRelative("Shyness").floatValue = v.Shyness;
            p.FindPropertyRelative("Integrity").floatValue = v.Integrity;
            p.FindPropertyRelative("Curiosity").floatValue = v.Curiosity;
        }
    }
}
#endif
