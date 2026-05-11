#nullable enable
using GeminiLab.Modules.Furniture;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class FurnitureServiceSceneHintTests
    {
        [Test]
        public void Awake_UsesSceneHintDefinitionInsteadOfNameInference()
        {
            Sprite sprite = CreateNamedSprite("RandomSpriteName");
            GameObject furnitureGo = new("RandomSceneObject");
            SpriteRenderer renderer = furnitureGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            Furniture furniture = furnitureGo.AddComponent<Furniture>();
            InteractionAnchor anchor = furnitureGo.AddComponent<InteractionAnchor>();
            SceneFurnitureDefinitionHint hint = furnitureGo.AddComponent<SceneFurnitureDefinitionHint>();
            hint.Configure(
                "Furniture_WorkDesk_Hint_01",
                FurnitureCategory.WorkDesk,
                FurnitureInteractionType.WorkFocus,
                1.8f,
                FurniturePlacementType.Floor,
                new Vector2Int(2, 1),
                new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 2f },
                includeInBuildPalette: false);
            anchor.SetAvailable(true);

            GameObject serviceHost = new("FurnitureServiceHost");
            FurnitureService service = serviceHost.AddComponent<FurnitureService>();

            bool found = service.TryGetBestInteractionTarget(Vector2.zero, FurnitureInteractionQuery.WorkDeskOnly, out FurnitureInteractionTarget target);

            Assert.IsTrue(found);
            Assert.AreEqual("Furniture_WorkDesk_Hint_01", target.DefinitionId);
            Assert.AreEqual(FurnitureCategory.WorkDesk, target.Category);
            Assert.AreEqual(FurnitureInteractionType.WorkFocus, target.InteractionType);
            Assert.AreEqual(1.8f, target.InteractionDurationSeconds, 0.01f);
            Assert.AreEqual(furniture.InstanceId, target.FurnitureId);

            Object.DestroyImmediate(serviceHost);
            Object.DestroyImmediate(furnitureGo);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void Awake_UsesExistingSpriteNameDefinition_WhenSceneTargetNameDoesNotMatchDefinitionId()
        {
            const string definitionId = "Furniture_Window_Angel_01";
            Sprite sprite = CreateNamedSprite(definitionId);
            GameObject furnitureGo = new("StaticWindowFlower");
            SpriteRenderer renderer = furnitureGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            Furniture furniture = furnitureGo.AddComponent<Furniture>();
            InteractionAnchor anchor = furnitureGo.AddComponent<InteractionAnchor>();
            SceneFurnitureDefinitionHint hint = furnitureGo.AddComponent<SceneFurnitureDefinitionHint>();
            hint.Configure(
                definitionId,
                FurnitureCategory.Decoration,
                FurnitureInteractionType.ObserveWindow,
                1.8f,
                FurniturePlacementType.Floor,
                new Vector2Int(2, 1),
                new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 0f },
                includeInBuildPalette: false);
            anchor.SetAvailable(true);

            GameObject serviceHost = new("FurnitureServiceHost");
            FurnitureService service = serviceHost.AddComponent<FurnitureService>();

            bool found = service.TryGetBestInteractionTarget(Vector2.zero, FurnitureInteractionQuery.Any, out FurnitureInteractionTarget target);

            Assert.IsTrue(found);
            Assert.AreEqual(definitionId, target.DefinitionId);
            Assert.AreEqual(FurnitureInteractionType.ObserveWindow, target.InteractionType);
            Assert.AreEqual(furniture.InstanceId, target.FurnitureId);

            Object.DestroyImmediate(serviceHost);
            Object.DestroyImmediate(furnitureGo);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void Awake_PreservesSceneSortingOrder_ForSceneFurniture()
        {
            const string definitionId = "Furniture_Mirror_Devil_01";
            Sprite sprite = CreateNamedSprite(definitionId);
            GameObject furnitureGo = new(definitionId);
            SpriteRenderer renderer = furnitureGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 321;

            InteractionAnchor anchor = furnitureGo.AddComponent<InteractionAnchor>();
            SceneFurnitureDefinitionHint hint = furnitureGo.AddComponent<SceneFurnitureDefinitionHint>();
            hint.Configure(
                definitionId,
                FurnitureCategory.Decoration,
                FurnitureInteractionType.InspectMirror,
                1.5f,
                FurniturePlacementType.Floor,
                Vector2Int.one,
                new EnvironmentalBuff { MoodDelta = 1f, EnergyDelta = 0f },
                includeInBuildPalette: false);
            anchor.SetAvailable(true);
            _ = furnitureGo.AddComponent<Furniture>();

            GameObject serviceHost = new("FurnitureServiceHost");
            _ = serviceHost.AddComponent<FurnitureService>();

            Assert.AreEqual(321, renderer.sortingOrder);

            Object.DestroyImmediate(serviceHost);
            Object.DestroyImmediate(furnitureGo);
            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }

        private static Sprite CreateNamedSprite(string name)
        {
            Texture2D texture = new(8, 8);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = name;
            return sprite;
        }
    }
}
