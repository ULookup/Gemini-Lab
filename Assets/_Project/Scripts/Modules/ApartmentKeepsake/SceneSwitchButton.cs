#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using UnityEngine;

public class SceneSwitchButton : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string targetSceneName = "WorldMap_Main";

    private bool isLoading;

    /// <summary>
    /// 给 UI Button 的 OnClick 调用。
    /// </summary>
    public void SwitchScene()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                $"[SceneSwitchButton] {gameObject.name} 没有设置目标场景。",
                this
            );
            return;
        }

        if (!TryResolveSceneId(targetSceneName, out SceneId targetScene))
        {
            Debug.LogError(
                $"[SceneSwitchButton] 目标场景未登记到 SceneCatalog：{targetSceneName}",
                this
            );
            return;
        }

        if (!ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow) || sceneFlow is null)
        {
            Debug.LogError("[SceneSwitchButton] 未找到 ISceneFlowService", this);
            return;
        }

        isLoading = true;
        Debug.Log($"[SceneSwitchButton] 通过 SceneFlow 请求场景：{targetScene}", this);

        try
        {
            AsyncOperation? operation = sceneFlow.LoadAsync(
                targetScene,
                onCompleted: HandleSceneLoadCompleted);

            if (operation is null)
            {
                isLoading = false;
                Debug.LogWarning(
                    $"[SceneSwitchButton] 场景请求未启动：{targetScene}（可能已有加载进行中或目标已是当前场景）",
                    this
                );
            }
        }
        catch (Exception exception)
        {
            isLoading = false;
            Debug.LogException(exception, this);
        }
    }

    private void HandleSceneLoadCompleted()
    {
        isLoading = false;
    }

    private static bool TryResolveSceneId(string sceneName, out SceneId sceneId)
    {
        sceneId = sceneName switch
        {
            "Boot" => SceneId.Boot,
            "MainMenu" => SceneId.MainMenu,
            "Prologue" => SceneId.Prologue,
            "Apartment_Main" => SceneId.Apartment,
            "WorldMap_Main" => SceneId.WorldMap,
            "Desktop_Overlay" => SceneId.DesktopOverlay,
            _ => default
        };

        return sceneName is "Boot" or "MainMenu" or "Prologue" or "Apartment_Main"
            or "WorldMap_Main" or "Desktop_Overlay";
    }
}
