using UnityEngine;
using UnityEngine.SceneManagement;

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

        isLoading = true;

        Debug.Log(
            $"[SceneSwitchButton] Loading scene: {targetSceneName}"
        );

        SceneManager.LoadScene(targetSceneName);
    }
}
