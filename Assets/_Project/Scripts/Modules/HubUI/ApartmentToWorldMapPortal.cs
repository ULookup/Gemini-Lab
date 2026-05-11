#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using UnityEngine;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// 公寓场景里通往 WorldMap 的传送点。挂在门 / 按钮上，调用 GoToWorldMap 即可切场景。
    /// </summary>
    public sealed class ApartmentToWorldMapPortal : MonoBehaviour
    {
        public void GoToWorldMap()
        {
            if (!ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow))
            {
                Debug.LogError("[ApartmentPortal] 未找到 ISceneFlowService");
                return;
            }

            sceneFlow!.LoadAsync(SceneId.WorldMap);
        }
    }
}
