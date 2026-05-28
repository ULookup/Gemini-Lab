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

        private static readonly (int idx, string id, string zh, string en, string[] upright, string[] reversed, string upDesc, string revDesc)[] Majors = new[]
        {
            (0,  "the_fool",          "愚者",      "The Fool",          new[]{"新开始","纯真","跃入未知"},    new[]{"鲁莽","迟疑","错失冒险"},
                "愚者象征着一段全新旅程的开启。你正站在起点，带着纯真的心和对未知的好奇，准备跃入全新的体验。这是拥抱可能性、放下顾虑的时刻，宇宙邀请你迈出第一步。",
                "逆位的愚者提示你可能因冲动而忽视了风险。你需要暂停脚步，重新审视计划是否周全。不要因为害怕错过而仓促行动，谨慎并非胆怯。"),
            (1,  "the_magician",      "魔术师",    "The Magician",      new[]{"行动力","专注","聚合能量"},    new[]{"操纵","散乱","夸大"},
                "魔术师代表你拥有将想法化为现实的能力。所有需要的元素都已就位，你只需集中意志、果断行动。你的技能和资源充足，现在是创造奇迹的时刻。",
                "逆位的魔术师提醒你能量可能被分散，或有人正以不诚实的方式影响局面。检视自己的动机，确保你不是在操纵或被操纵。专注力是当下的课题。"),
            (2,  "the_high_priestess","女祭司",    "The High Priestess",new[]{"直觉","内省","沉默的智慧"},    new[]{"压抑","自我怀疑","秘密"},
                "女祭司召唤你向内探索，倾听潜意识的低语。有些答案不在逻辑推理中，而藏在你的直觉和梦境里。静下心来，让内在的智慧引导你前行。",
                "逆位的女祭司意味着你可能忽视了自己的直觉，或有人对你不坦诚。隐藏的信息即将浮出水面。请重新连接你的内在声音，它一直在等待你聆听。"),
            (3,  "the_empress",       "皇后",      "The Empress",       new[]{"丰盛","滋养","感官满足"},      new[]{"依赖","过度溺爱","创造力受阻"},
                "皇后是大自然的丰饶化身，预示创造力与生命力的蓬勃。你正处在一个滋养与被滋养的阶段，享受感官的愉悦，让美好自然生长。拥抱你的温柔与力量。",
                "逆位的皇后可能暗示过度依赖他人或过度保护自己。创造力受到阻滞，你需要重新找到自我滋养的方式。给内心花园浇水，而不是等待他人灌溉。"),
            (4,  "the_emperor",       "皇帝",      "The Emperor",       new[]{"秩序","掌控","结构"},          new[]{"专断","僵化","失控"},
                "皇帝代表秩序、权威和稳定的结构。你需要以理性和纪律来管理当前的局面。确立边界、制定规则，用成熟的态度处理事务。领导力是你的关键词。",
                "逆位的皇帝暗示权力滥用或结构过于僵化。你可能感到失控，或被专断的力量压制。审视你与权威的关系——无论你是施压者还是承受者，弹性是解方。"),
            (5,  "the_hierophant",    "教皇",      "The Hierophant",    new[]{"传统","指引","归属"},          new[]{"叛逆","教条","脱轨"},
                "教皇代表传统的智慧和精神的引导。你或许正寻求导师的建议，或在既有的体系中寻找意义。遵从内心的信念，同时尊重前人的经验与教导。",
                "逆位的教皇鼓励你打破常规，质疑既定的教条。传统的道路未必适合此刻的你。敢于走自己的路，真正的成长往往源于对旧结构的超越。"),
            (6,  "the_lovers",        "恋人",      "The Lovers",        new[]{"契合","选择","关系深入"},      new[]{"失衡","犹豫","诱惑"},
                "恋人牌不仅关乎爱情，更关乎价值观的抉择。你正面临一个重要的选择，需要忠于内心的真实欲望。真诚的连接与沟通，将带来深刻的关系升华。",
                "逆位的恋人暗示关系中的不和谐或内心价值的冲突。你可能在犹豫中徘徊，无法做出选择。正视真实的渴望，避免因恐惧而回避必要的决定。"),
            (7,  "the_chariot",       "战车",      "The Chariot",       new[]{"冲劲","胜利","驾驭方向"},      new[]{"失控","急躁","停滞"},
                "战车预示你将凭借意志力克服障碍、取得胜利。你需要驾驭内心矛盾的力量，将它们整合为前进的动力。方向明确、决心坚定，你势不可挡。",
                "逆位的战车提示你可能正失去对局势的掌控。内心的冲动和外部的阻力让你举步维艰。停下来重新调整方向，比盲目猛冲更明智。"),
            (8,  "strength",          "力量",      "Strength",          new[]{"柔韧","慈悲","驯服"},          new[]{"怯懦","焦虑","自我怀疑"},
                "力量并非蛮力，而是以柔克刚的智慧。你用耐心、慈悲和内在的坚定驯服了内心的野兽。温柔是最强大的武器，信心让你无所畏惧。",
                "逆位的力量暗示你正被恐惧或自我怀疑所困。内心的焦虑让你感到脆弱无力。请记住，真正的力量来自接纳自己的不完美，而非控制一切。"),
            (9,  "the_hermit",        "隐士",      "The Hermit",        new[]{"独处","反省","内在之光"},      new[]{"孤立","退缩","拒绝援手"},
                "隐士邀请你暂时退隐，向内寻找答案。你需要的不是更多的信息，而是安静的沉思。在独处中，那盏内在的明灯会指引你真正渴望的方向。",
                "逆位的隐士警告过度的孤独可能变成孤立。你或许因为害怕受伤而拒绝他人的援手。是时候走出内心的洞窟，让信任的人重新进入你的世界。"),
            (10, "wheel_of_fortune",  "命运之轮",  "Wheel of Fortune",  new[]{"转机","流动","机缘"},          new[]{"停滞","坏运","逆转"},
                "命运之轮转动，带来无法预料的转变。好运正在靠近，你要做的是顺势而为。接受变化是宇宙的常态，在流动中找到自己的节奏与契机。",
                "逆位的命运之轮意味着你正经历一段停滞或逆风的时期。运势的循环总有低谷，这并非终点。反省你与变化的关系，抗拒只会延长困境。"),
            (11, "justice",           "正义",      "Justice",           new[]{"公正","因果","清明决断"},      new[]{"偏颇","回避","审判压力"},
                "正义牌提醒你因果法则永不缺席。你的决定将带来相应的后果，请以客观和公正的态度做出判断。真相将被揭示，公平将得到伸张。",
                "逆位的正义暗示不公平的局面或逃避责任的倾向。你可能在某个问题上偏颇对待自己或他人。诚实地面对因果，拖延只会让账本更沉重。"),
            (12, "the_hanged_man",    "倒吊人",    "The Hanged Man",    new[]{"暂停","换视角","牺牲换洞见"},  new[]{"拖延","徒劳","固执"},
                "倒吊人邀请你换一个角度看世界。有时放下执念、暂停行动，反而能获得更深的洞见。这份牺牲并非损失，而是以新的方式理解生命的礼物。",
                "逆位的倒吊人暗示你可能在无谓地拖延或固执地拒绝改变视角。牺牲若不能带来成长便只是受苦。问问自己：你在等待什么？行动也许才是答案。"),
            (13, "death",             "死神",      "Death",             new[]{"结束","蜕变","新生前的释放"},  new[]{"抗拒改变","停滞","恐惧转化"},
                "死神牌并非字面意义上的死亡，而是旧阶段的彻底终结。只有放下不再服务于你的事物，才能为新生腾出空间。拥抱这场蜕变，它是你进化的必经之路。",
                "逆位的死神提示你正在抗拒必要的改变。紧抓着过时的关系、工作或信念，只会让你停滞不前。允许旧的死去，新的才能诞生——你准备好了吗？"),
            (14, "temperance",        "节制",      "Temperance",        new[]{"平衡","调和","耐心"},          new[]{"过度","失衡","急躁"},
                "节制天使教导你平衡与调和的艺术。不偏不倚、不急不缓，在看似对立的元素中找到和谐。耐心等待事物自然融合，美将在恰到好处的时刻显现。",
                "逆位的节制暗示生活中某处失去了平衡。过度沉迷或过度压抑都在消耗你。找回中道，让节奏慢下来，在简单中重建身心的和谐。"),
            (15, "the_devil",         "恶魔",      "The Devil",         new[]{"欲望","执念","被束缚"},        new[]{"觉醒","挣脱","释放"},
                "恶魔牌提醒你审视那些束缚你的欲望和执念。你被什么所困？金钱、权力、不健康的关系？真相是枷锁的钥匙就在你手中，意识到束缚的存在就是解脱的开始。",
                "逆位的恶魔是觉醒的信号。你正挣脱束缚、重获自由。直面内心的阴影，打破自我设限的锁链。这是一次深刻的解放，光明正照进曾经黑暗的角落。"),
            (16, "the_tower",         "塔",        "The Tower",         new[]{"崩塌","真相","必要的摧毁"},    new[]{"苟延","延迟","拒绝真相"},
                "高塔的崩塌看似灾难，实则是必要的摧毁。建立在虚伪或脆弱基础上的结构终将倒塌，真相如闪电般照亮黑暗。拥抱这场清理，废墟之上才能建起更坚固的家园。",
                "逆位的高塔暗示你正在竭力避免一场不可避免的改变。你越是修补即将崩塌的结构，痛苦就越深。放手吧，让该倒的倒下，如此你才能重新站立。"),
            (17, "the_star",          "星星",      "The Star",          new[]{"希望","宁静","未来的光"},      new[]{"失望","心灰","断开信念"},
                "星星是暴风雨后的宁静，是希望的灯塔。经历磨难后的你，正沐浴在疗愈的光芒中。相信宇宙的恩典，你的愿望正在星空下悄然成形，保持信念。",
                "逆位的星星意味着你可能正经历失望或信念的动摇。那道光似乎暂时暗淡了，但它从未消失。重新连接你的希望之源，哪怕只是一丝微光也足以指引方向。"),
            (18, "the_moon",          "月亮",      "The Moon",          new[]{"潜意识","错觉","隐藏之物"},    new[]{"真相显现","困惑消散","迷雾散去"},
                "月亮带你走进潜意识的迷雾，那里藏着未被察觉的真相与恐惧。画面可能扭曲，路径可能不清，但请相信你的直觉。穿越这场幻觉，你将更接近真实的自己。",
                "逆位的月亮预示迷雾正在消散，隐藏的真相即将显现。困惑和恐惧将逐渐褪去，你开始看清事物的本来面目。这是一个释放和澄明的时刻。"),
            (19, "the_sun",           "太阳",      "The Sun",           new[]{"喜悦","活力","清明"},          new[]{"暂时乌云","自信受挫","欢愉延迟"},
                "太阳是塔罗中最明亮的牌之一，带来纯粹的喜悦和生命力。你正沐浴在温暖与成功的阳光下，一切都清晰可见。享受这份欢愉，这是你值得拥有的幸福时光。",
                "逆位的太阳并不代表黑暗，只是暂时的乌云遮住了光芒。你或许正经历信心不足或欢愉的延迟。请记住太阳始终在那里，云层终将散去。"),
            (20, "judgement",         "审判",      "Judgement",         new[]{"觉醒","召唤","重生"},          new[]{"自责","回避","错过信号"},
                "审判牌呼唤你回应内心的召唤，觉醒于更高的自我。你正被邀请进行一次深刻的自我审视与重生。放下过去的包袱，回应那个让你灵魂振动的使命。",
                "逆位的审判暗示你可能正在逃避内心的召唤，或被过度的自责所困。不要忽视那些来自深层自我的信号。宽恕自己，接纳过去，然后勇敢前行。"),
            (21, "the_world",         "世界",      "The World",         new[]{"圆满","整合","阶段完成"},      new[]{"未完","拖尾","缺一口"},
                "世界牌标志着一段旅程的圆满结束。你已完成整合，收获了知识与智慧。站在新的起点回望，一切都是值得的。庆祝你的成就，然后迎接下一个循环的开启。",
                "逆位的世界暗示一个未能圆满的循环，或某个项目欠缺最后的收尾。你可能感到接近目标却始终差一步。检视剩余未完成的部分，完成它们才能真正迈向新的阶段。")
        };

        [MenuItem("Tools/Gemini-Lab/Author Tarot Deck (22 Majors)")]
        public static void Author()
        {
            EnsureFolder(CardsFolder);
            EnsureFolder(CardArtFolder);

            // 先生成卡面 Sprite
            List<Sprite?> artworks = new();
            foreach (var (idx, id, zh, en, _, _, _, _) in Majors)
            {
                string artPath = $"{CardArtFolder}/{idx:00}_{id}.png";
                if (!File.Exists(artPath))
                {
                    GeneratePlaceholderCardArt(artPath, idx, zh, en);
                }
            }
            AssetDatabase.Refresh();
            foreach (var (idx, id, _, _, _, _, _, _) in Majors)
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
                var (idx, id, zh, en, upright, reversed, upDesc, revDesc) = Majors[i];
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
                serialized.FindProperty("_uprightDescription").stringValue = upDesc;
                serialized.FindProperty("_reversedDescription").stringValue = revDesc;
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
