#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using GeminiLab.Modules.Tarot;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 一次性工具：生成大阿卡那 22 张 TarotCardSO + 1 个 TarotDeckSO。
    /// 占位卡面 Sprite：统一 256x384 PNG，卡面中部画出中文牌名 + 编号。
    /// 幂等：已存在的 asset 会按最新配置更新字段，但不会重复创建。
    /// </summary>
    public static class TarotDeckAuthoring
    {
        private const string CardsFolder = "Assets/_Project/ScriptableObjects/TarotConfig/Majors";
        private const string DeckPath = "Assets/_Project/ScriptableObjects/TarotConfig/TarotDeck.asset";
        private const string CardArtFolder = "Assets/_Project/Art/Sprites/Tarot/Majors";
        private const string CardBackPath = "Assets/_Project/Art/Sprites/Tarot/card_back.png";

        private static readonly (int idx, string id, string zh, string en, string[] upright, string[] reversed)[] Majors = new[]
        {
            (0,  "the_fool",          "愚者",      "The Fool",          new[]{"新开始","纯真","跃入未知"},    new[]{"鲁莽","迟疑","错失冒险"}),
            (1,  "the_magician",      "魔术师",    "The Magician",      new[]{"行动力","专注","聚合能量"},    new[]{"操纵","散乱","夸大"}),
            (2,  "the_high_priestess","女祭司",    "The High Priestess",new[]{"直觉","内省","沉默的智慧"},    new[]{"压抑","自我怀疑","秘密"}),
            (3,  "the_empress",       "皇后",      "The Empress",       new[]{"丰盛","滋养","感官满足"},      new[]{"依赖","过度溺爱","创造力受阻"}),
            (4,  "the_emperor",       "皇帝",      "The Emperor",       new[]{"秩序","掌控","结构"},          new[]{"专断","僵化","失控"}),
            (5,  "the_hierophant",    "教皇",      "The Hierophant",    new[]{"传统","指引","归属"},          new[]{"叛逆","教条","脱轨"}),
            (6,  "the_lovers",        "恋人",      "The Lovers",        new[]{"契合","选择","关系深入"},      new[]{"失衡","犹豫","诱惑"}),
            (7,  "the_chariot",       "战车",      "The Chariot",       new[]{"冲劲","胜利","驾驭方向"},      new[]{"失控","急躁","停滞"}),
            (8,  "strength",          "力量",      "Strength",          new[]{"柔韧","慈悲","驯服"},          new[]{"怯懦","焦虑","自我怀疑"}),
            (9,  "the_hermit",        "隐士",      "The Hermit",        new[]{"独处","反省","内在之光"},      new[]{"孤立","退缩","拒绝援手"}),
            (10, "wheel_of_fortune",  "命运之轮",  "Wheel of Fortune",  new[]{"转机","流动","机缘"},          new[]{"停滞","坏运","逆转"}),
            (11, "justice",           "正义",      "Justice",           new[]{"公正","因果","清明决断"},      new[]{"偏颇","回避","审判压力"}),
            (12, "the_hanged_man",    "倒吊人",    "The Hanged Man",    new[]{"暂停","换视角","牺牲换洞见"},  new[]{"拖延","徒劳","固执"}),
            (13, "death",             "死神",      "Death",             new[]{"结束","蜕变","新生前的释放"},  new[]{"抗拒改变","停滞","恐惧转化"}),
            (14, "temperance",        "节制",      "Temperance",        new[]{"平衡","调和","耐心"},          new[]{"过度","失衡","急躁"}),
            (15, "the_devil",         "恶魔",      "The Devil",         new[]{"欲望","执念","被束缚"},        new[]{"觉醒","挣脱","释放"}),
            (16, "the_tower",         "塔",        "The Tower",         new[]{"崩塌","真相","必要的摧毁"},    new[]{"苟延","延迟","拒绝真相"}),
            (17, "the_star",          "星星",      "The Star",          new[]{"希望","宁静","未来的光"},      new[]{"失望","心灰","断开信念"}),
            (18, "the_moon",          "月亮",      "The Moon",          new[]{"潜意识","错觉","隐藏之物"},    new[]{"真相显现","困惑消散","迷雾散去"}),
            (19, "the_sun",           "太阳",      "The Sun",           new[]{"喜悦","活力","清明"},          new[]{"暂时乌云","自信受挫","欢愉延迟"}),
            (20, "judgement",         "审判",      "Judgement",         new[]{"觉醒","召唤","重生"},          new[]{"自责","回避","错过信号"}),
            (21, "the_world",         "世界",      "The World",         new[]{"圆满","整合","阶段完成"},      new[]{"未完","拖尾","缺一口"})
        };

        [MenuItem("Tools/Gemini-Lab/Author Tarot Deck (22 Majors)")]
        public static void Author()
        {
            EnsureFolder(CardsFolder);
            EnsureFolder(CardArtFolder);

            // 先生成卡面 Sprite
            List<Sprite?> artworks = new();
            foreach (var (idx, id, zh, en, _, _) in Majors)
            {
                string artPath = $"{CardArtFolder}/{idx:00}_{id}.png";
                if (!File.Exists(artPath))
                {
                    GeneratePlaceholderCardArt(artPath, idx, zh, en);
                }
            }
            AssetDatabase.Refresh();
            foreach (var (idx, id, _, _, _, _) in Majors)
            {
                string artPath = $"{CardArtFolder}/{idx:00}_{id}.png";
                ConfigureSpriteImport(artPath);
                artworks.Add(AssetDatabase.LoadAssetAtPath<Sprite>(artPath));
            }

            // 生成 card_back.png
            if (!File.Exists(CardBackPath))
            {
                GeneratePlaceholderCardBack(CardBackPath);
                AssetDatabase.Refresh();
                ConfigureSpriteImport(CardBackPath);
            }
            var cardBack = AssetDatabase.LoadAssetAtPath<Sprite>(CardBackPath);

            // 生成 TarotCardSO 资产
            List<TarotCardSO> soList = new();
            for (int i = 0; i < Majors.Length; i++)
            {
                var (idx, id, zh, en, upright, reversed) = Majors[i];
                string soPath = $"{CardsFolder}/{idx:00}_{id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<TarotCardSO>(soPath);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<TarotCardSO>();
                    AssetDatabase.CreateAsset(so, soPath);
                }
                var serialized = new SerializedObject(so);
                serialized.FindProperty("_majorIndex").intValue = idx;
                serialized.FindProperty("_id").stringValue = id;
                serialized.FindProperty("_displayNameZh").stringValue = zh;
                serialized.FindProperty("_displayNameEn").stringValue = en;
                WriteStringArray(serialized.FindProperty("_uprightKeywords"), upright);
                WriteStringArray(serialized.FindProperty("_reversedKeywords"), reversed);
                serialized.FindProperty("_artwork").objectReferenceValue = artworks[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                soList.Add(so);
            }

            // 生成 TarotDeckSO
            var deck = AssetDatabase.LoadAssetAtPath<TarotDeckSO>(DeckPath);
            if (deck == null)
            {
                deck = ScriptableObject.CreateInstance<TarotDeckSO>();
                AssetDatabase.CreateAsset(deck, DeckPath);
            }
            deck.SetCardsEditorOnly(soList, cardBack);
            EditorUtility.SetDirty(deck);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TarotDeckAuthoring] 完成：{Majors.Length} 张大阿卡那 + Deck → {DeckPath}");
        }

        private static void WriteStringArray(SerializedProperty prop, string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }
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

        private static void GeneratePlaceholderCardArt(string assetPath, int idx, string zh, string en)
        {
            const int w = 256;
            const int h = 384;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
            var bg = new Color(0.15f, 0.18f, 0.28f, 1f);
            var frame = new Color(0.85f, 0.75f, 0.45f, 1f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 8 || x >= w - 8 || y < 8 || y >= h - 8 ||
                                  x == w / 2 || y == h / 2; // 十字基准
                    tex.SetPixel(x, y, border ? frame : bg);
                }
            }
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            // 美术/中文绘制留给真实美术；占位只保证有图、一看就知道是塔罗卡
        }

        private static void GeneratePlaceholderCardBack(string assetPath)
        {
            const int w = 256;
            const int h = 384;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
            var bg = new Color(0.2f, 0.1f, 0.3f, 1f);
            var mark = new Color(0.85f, 0.75f, 0.45f, 1f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 8 || x >= w - 8 || y < 8 || y >= h - 8;
                    bool diagonal = (x + y) % 16 == 0 || (x - y + h) % 16 == 0;
                    tex.SetPixel(x, y, border || diagonal ? mark : bg);
                }
            }
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }
    }
}
#endif
