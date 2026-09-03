#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.RoomRelic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentRoomRelicAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string CatalogFolder = "Assets/_Project/ScriptableObjects/RoomRelicConfig";
        private const string CatalogPath = CatalogFolder + "/RoomRelicCatalog.asset";
        private const string RelicRootName = "RoomRelic";

        private const int NoteSlotCount = 3;
        private const int RelicSlotCount = 5;
        private const int GiftSlotCount = 3;

        [MenuItem("Tools/Gemini-Lab/Apartment/Author Room Relic")]
        public static void Author()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject? artRoot = GameObject.Find("ArtGenerated");
            if (artRoot == null)
            {
                Debug.LogError("[RoomRelicAuthoring] 未找到 ArtGenerated，无法继续。");
                return;
            }

            GameObject? existingRelicRoot = GameObject.Find(RelicRootName);
            if (existingRelicRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRelicRoot);
            }

            RoomRelicCatalogSO catalog = CreateOrUpdatePlaceholderCatalog();
            GameObject relicRoot = EnsureChild(artRoot.transform, RelicRootName);
            EnsureRuntimeBootstrap(relicRoot, catalog);

            EnsureRoom(relicRoot.transform, RoomId.AngelRoom, catalog);
            EnsureRoom(relicRoot.transform, RoomId.DevilRoom, catalog);
            EnsureEntryTriggers();
            EnsurePopups();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RoomRelicAuthoring] RoomRelic 场景占位作者化完成。");
        }

        private static RoomRelicCatalogSO CreateOrUpdatePlaceholderCatalog()
        {
            EnsureAssetFolder(CatalogFolder);
            RoomRelicCatalogSO? catalog = AssetDatabase.LoadAssetAtPath<RoomRelicCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RoomRelicCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.notes = CreatePlaceholderNotes();
            catalog.relics = CreatePlaceholderRelics();
            catalog.gifts = CreatePlaceholderGifts();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void EnsureRuntimeBootstrap(GameObject relicRoot, RoomRelicCatalogSO catalog)
        {
            RoomRelicRuntimeBootstrap bootstrap = relicRoot.GetComponent<RoomRelicRuntimeBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = relicRoot.AddComponent<RoomRelicRuntimeBootstrap>();
            }

            SerializedObject so = new(bootstrap);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureRoom(Transform parent, RoomId roomId, RoomRelicCatalogSO catalog)
        {
            string roomName = roomId == RoomId.AngelRoom ? "AngelRoom" : "DevilRoom";
            GameObject roomRoot = EnsureChild(parent, roomName);
            RoomRelicRoomView roomView = roomRoot.GetComponent<RoomRelicRoomView>();
            if (roomView == null)
            {
                roomView = roomRoot.AddComponent<RoomRelicRoomView>();
            }

            SetIntField(roomView, "_roomId", (int)roomId);

            string sender = roomId == RoomId.AngelRoom ? "Demon" : "Angel";
            string receiver = roomId == RoomId.AngelRoom ? "Angel" : "Demon";
            Sprite sprite = GetPlaceholderSprite();

            Transform noteContainer = EnsureChild(roomRoot.transform, "NoteSpawns").transform;
            string[] noteIds = new string[catalog.notes.Length];
            int noteCount = 0;
            for (int i = 0; i < catalog.notes.Length; i++)
            {
                if (catalog.notes[i].senderCharacter == sender)
                {
                    noteIds[noteCount++] = catalog.notes[i].id;
                }
            }

            Array.Resize(ref noteIds, noteCount);
            RoomRelicView[] noteViews = new RoomRelicView[NoteSlotCount];
            for (int i = 0; i < NoteSlotCount; i++)
            {
                noteViews[i] = CreateSlot(
                    $"NoteSpawn_{i:D2}",
                    noteContainer,
                    roomId,
                    RoomRelicKind.Note,
                    noteIds,
                    sprite,
                    new Color(1f, 0.92f, 0.55f, 1f));
            }

            Transform relicContainer = EnsureChild(roomRoot.transform, "RelicSpawns").transform;
            string[] relicIds = new string[catalog.relics.Length];
            int relicCount = 0;
            for (int i = 0; i < catalog.relics.Length; i++)
            {
                if (catalog.relics[i].targetRoom == roomId &&
                    catalog.relics[i].ownerCharacter == sender)
                {
                    relicIds[relicCount++] = catalog.relics[i].id;
                }
            }

            Array.Resize(ref relicIds, relicCount);
            Dictionary<string, Sprite> relicSprites = new();
            for (int i = 0; i < catalog.relics.Length; i++)
            {
                RoomRelicData relic = catalog.relics[i];
                if (relic.targetRoom != roomId || relic.ownerCharacter != sender ||
                    string.IsNullOrWhiteSpace(relic.roomVisualKey))
                {
                    continue;
                }

                Sprite? loadedSprite = LoadRelicSpriteByGuid(relic.roomVisualKey);
                if (loadedSprite != null)
                {
                    relicSprites[relic.id] = loadedSprite;
                }
            }

            RoomRelicView[] relicViews = new RoomRelicView[RelicSlotCount];
            for (int i = 0; i < RelicSlotCount; i++)
            {
                relicViews[i] = CreateSlot(
                    $"RelicSpawn_{i:D2}",
                    relicContainer,
                    roomId,
                    RoomRelicKind.TemporaryRelic,
                    relicIds,
                    sprite,
                    new Color(0.55f, 0.8f, 1f, 1f),
                    relicSprites);
            }

            Transform giftContainer = EnsureChild(roomRoot.transform, "GiftSlots").transform;
            string[] giftIds = new string[catalog.gifts.Length];
            int giftCount = 0;
            for (int i = 0; i < catalog.gifts.Length; i++)
            {
                if (catalog.gifts[i].receiverCharacter == receiver)
                {
                    giftIds[giftCount++] = catalog.gifts[i].id;
                }
            }

            Array.Resize(ref giftIds, giftCount);
            RoomRelicView[] giftViews = new RoomRelicView[GiftSlotCount];
            for (int i = 0; i < GiftSlotCount; i++)
            {
                giftViews[i] = CreateSlot(
                    $"GiftSlot_{i:D2}",
                    giftContainer,
                    roomId,
                    RoomRelicKind.PermanentGift,
                    giftIds,
                    sprite,
                    new Color(1f, 0.72f, 0.86f, 1f));
            }

            SetObjectArray(roomView, "_noteSlots", noteViews);
            SetObjectArray(roomView, "_relicSlots", relicViews);
            SetObjectArray(roomView, "_giftSlots", giftViews);
        }

        private static RoomRelicView CreateSlot(
            string name,
            Transform parent,
            RoomId roomId,
            RoomRelicKind kind,
            string[] variantIds,
            Sprite sprite,
            Color color,
            Dictionary<string, Sprite>? spriteOverrides = null)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.layer = parent.gameObject.layer;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.35f, 1.35f);

            RoomRelicView view = root.AddComponent<RoomRelicView>();
            RoomRelicInteraction interaction = root.AddComponent<RoomRelicInteraction>();
            SetIntField(interaction, "_roomId", (int)roomId);
            SetIntField(interaction, "_kind", (int)kind);

            GameObject[] targets = new GameObject[variantIds.Length];
            for (int i = 0; i < variantIds.Length; i++)
            {
                GameObject variant = new(variantIds[i]);
                variant.transform.SetParent(root.transform, false);
                variant.layer = parent.gameObject.layer;
                variant.SetActive(false);

                SpriteRenderer renderer = variant.AddComponent<SpriteRenderer>();
                Sprite variantSprite = sprite;
                if (spriteOverrides != null &&
                    spriteOverrides.TryGetValue(variantIds[i], out Sprite? overriddenSprite) &&
                    overriddenSprite != null)
                {
                    variantSprite = overriddenSprite;
                }

                renderer.sprite = variantSprite;
                renderer.color = color;
                renderer.sortingOrder = 50;
                targets[i] = variant;
            }

            SetVariantBindings(view, targets, variantIds);
            return view;
        }

        private static void EnsureEntryTriggers()
        {
            EnsureEntryTrigger("PetMovementBounds", RoomId.AngelRoom, PetId.Angel);
            EnsureEntryTrigger("PetMovementBounds_Devil", RoomId.DevilRoom, PetId.Devil);
        }

        private static void EnsureEntryTrigger(string boundsName, RoomId roomId, PetId petId)
        {
            GameObject? bounds = GameObject.Find(boundsName);
            if (bounds == null)
            {
                Debug.LogWarning($"[RoomRelicAuthoring] 未找到 {boundsName}，跳过房间触发器。");
                return;
            }

            RoomRelicEntryTrigger trigger = bounds.GetComponent<RoomRelicEntryTrigger>();
            if (trigger == null)
            {
                trigger = bounds.AddComponent<RoomRelicEntryTrigger>();
            }

            SetIntField(trigger, "_roomId", (int)roomId);
            SetIntField(trigger, "_expectedPetId", (int)petId);
        }

        private static void EnsurePopups()
        {
            GameObject? uiRoot = GameObject.Find("UI_Sidebar");
            if (uiRoot == null)
            {
                Debug.LogWarning("[RoomRelicAuthoring] 未找到 UI_Sidebar，跳过弹窗作者化。");
                return;
            }

            DestroyExistingPopup(uiRoot.transform, "RoomNotePopup");
            DestroyExistingPopup(uiRoot.transform, "RoomRelicDetailPopup");
            DestroyExistingPopup(uiRoot.transform, "RoomGiftObtainedPopup");

            CreatePopup<RoomNotePopup>(uiRoot.transform, "RoomNotePopup");
            CreatePopup<RoomRelicDetailPopup>(uiRoot.transform, "RoomRelicDetailPopup");
            CreatePopup<RoomGiftObtainedPopup>(uiRoot.transform, "RoomGiftObtainedPopup");
        }

        private static void DestroyExistingPopup(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void CreatePopup<T>(Transform parent, string name) where T : RoomRelicPanelBase
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.layer = parent.gameObject.layer;

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject content = new("Content");
            content.transform.SetParent(root.transform, false);
            content.layer = parent.gameObject.layer;

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(640f, 420f);

            Image panelBg = content.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.13f, 0.18f, 0.98f);

            Button closeButton = CreateCloseButton(content.transform);
            TMP_Text title = CreateText(content.transform, "Title", 34, TextAlignmentOptions.Center);
            SetAnchoredRect(title.rectTransform, new Vector2(0f, 160f), new Vector2(560f, 60f));

            TMP_Text body = CreateText(content.transform, "Body", 24, TextAlignmentOptions.TopLeft);
            SetAnchoredRect(body.rectTransform, new Vector2(0f, -30f), new Vector2(560f, 260f));

            T component = root.AddComponent<T>();
            SerializedObject so = new(component);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;

            if (component is RoomNotePopup)
            {
                so.FindProperty("_contentText").objectReferenceValue = body;
            }
            else if (component is RoomRelicDetailPopup)
            {
                so.FindProperty("_nameText").objectReferenceValue = title;
                so.FindProperty("_descriptionText").objectReferenceValue = body;
            }
            else if (component is RoomGiftObtainedPopup)
            {
                so.FindProperty("_giftNameText").objectReferenceValue = title;
                so.FindProperty("_hintText").objectReferenceValue = body;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            content.SetActive(false);
        }

        private static Button CreateCloseButton(Transform parent)
        {
            GameObject buttonGo = new("CloseButton");
            buttonGo.transform.SetParent(parent, false);
            buttonGo.layer = parent.gameObject.layer;

            RectTransform rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(48f, 48f);

            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.16f);
            Button button = buttonGo.AddComponent<Button>();

            TMP_Text label = CreateText(buttonGo.transform, "Label", 24, TextAlignmentOptions.Center);
            label.text = "✕";
            StretchRect(label.rectTransform);
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textGo = new(name);
            textGo.transform.SetParent(parent, false);
            textGo.layer = parent.gameObject.layer;

            RectTransform rect = textGo.AddComponent<RectTransform>();
            TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetVariantBindings(RoomRelicView view, GameObject[] targets, string[] ids)
        {
            SerializedObject so = new(view);
            SerializedProperty variants = so.FindProperty("_variants");
            variants.arraySize = ids.Length;

            for (int i = 0; i < ids.Length; i++)
            {
                SerializedProperty element = variants.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_id").stringValue = ids[i];
                element.FindPropertyRelative("_target").objectReferenceValue = targets[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(RoomRelicRoomView view, string fieldName, RoomRelicView[] views)
        {
            SerializedObject so = new(view);
            SerializedProperty array = so.FindProperty(fieldName);
            array.arraySize = views.Length;

            for (int i = 0; i < views.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIntField(UnityEngine.Object target, string fieldName, int value)
        {
            SerializedObject so = new(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null)
            {
                property.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform? existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static Sprite GetPlaceholderSprite()
        {
            Sprite? sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite != null)
            {
                return sprite;
            }

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        private static Sprite? LoadRelicSpriteByGuid(string assetGuid)
        {
            if (string.IsNullOrWhiteSpace(assetGuid))
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null)
            {
                return null;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static RoomNoteData[] CreatePlaceholderNotes()
        {
            return new[]
            {
                new RoomNoteData { id = "note_demon_01", senderCharacter = "Demon", receiverCharacter = "Angel", content = "【占位】恶魔留下的纸条内容 01", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_demon_02", senderCharacter = "Demon", receiverCharacter = "Angel", content = "【占位】恶魔留下的纸条内容 02", visualType = RoomNoteVisualType.PaperBall, weight = 1f },
                new RoomNoteData { id = "note_demon_03", senderCharacter = "Demon", receiverCharacter = "Angel", content = "【占位】恶魔留下的纸条内容 03", visualType = RoomNoteVisualType.Origami, weight = 1f },
                new RoomNoteData { id = "note_demon_04", senderCharacter = "Demon", receiverCharacter = "Angel", content = "【占位】恶魔留下的纸条内容 04", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_angel_01", senderCharacter = "Angel", receiverCharacter = "Demon", content = "【占位】天使留下的纸条内容 01", visualType = RoomNoteVisualType.Note, weight = 1f },
                new RoomNoteData { id = "note_angel_02", senderCharacter = "Angel", receiverCharacter = "Demon", content = "【占位】天使留下的纸条内容 02", visualType = RoomNoteVisualType.PaperBall, weight = 1f },
                new RoomNoteData { id = "note_angel_03", senderCharacter = "Angel", receiverCharacter = "Demon", content = "【占位】天使留下的纸条内容 03", visualType = RoomNoteVisualType.Origami, weight = 1f },
                new RoomNoteData { id = "note_angel_04", senderCharacter = "Angel", receiverCharacter = "Demon", content = "【占位】天使留下的纸条内容 04", visualType = RoomNoteVisualType.Note, weight = 1f }
            };
        }

        private static RoomRelicData[] CreatePlaceholderRelics()
        {
            return new[]
            {
                new RoomRelicData { id = "relic_demon_01", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "红黑纸飞机", observationText = "折得歪歪扭扭的，飞得倒还挺远。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/纸飞机.png", roomVisualKey = "4e070b2a29c2f8b4d832e4d5c174f8b0", weight = 1f },
                new RoomRelicData { id = "relic_demon_02", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "调色盘", observationText = "颜色混得乱七八糟，像他整理到一半的心事。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/颜料盘.png", roomVisualKey = "8930f36f3efe98e468f3522ae96fa1b3", weight = 1f },
                new RoomRelicData { id = "relic_demon_03", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "拨片", observationText = "这个笨蛋不会以为竖琴和吉他一样用拨片吧。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/拨片.png", roomVisualKey = "c7a9a7cd560939b4faf921b6fca7c7f3", weight = 1f },
                new RoomRelicData { id = "relic_demon_04", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "南瓜糖果", observationText = "把最喜欢的万圣限定款送我了？", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/南瓜糖果.png", roomVisualKey = "232777944ca791d448cf4840262643c4", weight = 1f },
                new RoomRelicData { id = "relic_demon_05", ownerCharacter = "Demon", targetRoom = RoomId.AngelRoom, displayName = "速写", observationText = "把我画的傻乎乎的……扔掉算了——算了。", weight = 1f },
                new RoomRelicData { id = "relic_angel_01", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "千纸鹤", observationText = "翅膀和祂的一样轻一样薄，嗯，你是天使的信使吗？", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/千纸鹤.png", roomVisualKey = "b3bc4a7a7b73869419f8897dccbafd8a", weight = 1f },
                new RoomRelicData { id = "relic_angel_02", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "雕花小镜子", observationText = "嗯、嗯、嗯，诶这个镜子没办法和我说话吗？", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/小镜子.png", roomVisualKey = "c54d937a2c5f2b84bb6870e8a0bd3c05", weight = 1f },
                new RoomRelicData { id = "relic_angel_03", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "羽毛书签", observationText = "好看……我也想要羽毛翅膀……还能掉下来做书签……", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/羽毛书签.png", roomVisualKey = "6643c15cd7a2ec74fbbe1a119c5d1d01", weight = 1f },
                new RoomRelicData { id = "relic_angel_04", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "竖琴琴谱残页", observationText = "要是祂听到我用吉他弹出这一段会很惊讶吧。", roomSpritePath = "Assets/_Project/Art/Sprites/Relic/竖琴残页.png", roomVisualKey = "cedf53eb805316849bcc895b54650888", weight = 1f },
                new RoomRelicData { id = "relic_angel_05", ownerCharacter = "Angel", targetRoom = RoomId.DevilRoom, displayName = "小星星吊坠", observationText = "不重，亮晶晶的，系在尾巴尖上刚刚好。", weight = 1f }
            };
        }

        private static RoomGiftData[] CreatePlaceholderGifts()
        {
            return new[]
            {
                new RoomGiftData { id = "gift_demon_01", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "占位赠礼·恶魔 01", observationText = "这是恶魔赠礼的占位条目。", displaySlotId = "desk", weight = 1f },
                new RoomGiftData { id = "gift_demon_02", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "占位赠礼·恶魔 02", observationText = "这是恶魔赠礼的占位条目。", displaySlotId = "shelf", weight = 1f },
                new RoomGiftData { id = "gift_demon_03", giverCharacter = "Demon", receiverCharacter = "Angel", displayName = "占位赠礼·恶魔 03", observationText = "这是恶魔赠礼的占位条目。", displaySlotId = "nightstand", weight = 1f },
                new RoomGiftData { id = "gift_angel_01", giverCharacter = "Angel", receiverCharacter = "Demon", displayName = "占位赠礼·天使 01", observationText = "这是天使赠礼的占位条目。", displaySlotId = "desk", weight = 1f },
                new RoomGiftData { id = "gift_angel_02", giverCharacter = "Angel", receiverCharacter = "Demon", displayName = "占位赠礼·天使 02", observationText = "这是天使赠礼的占位条目。", displaySlotId = "shelf", weight = 1f },
                new RoomGiftData { id = "gift_angel_03", giverCharacter = "Angel", receiverCharacter = "Demon", displayName = "占位赠礼·天使 03", observationText = "这是天使赠礼的占位条目。", displaySlotId = "nightstand", weight = 1f }
            };
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            string leaf = folder.Substring(folder.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
