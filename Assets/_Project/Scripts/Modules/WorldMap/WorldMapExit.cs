#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 返回公寓的出口。挂在场景任意按钮/触发物上，点击 / OnTrigger 后调用 ReturnToApartment。
    /// </summary>
    public sealed class WorldMapExit : MonoBehaviour
    {
        public void ReturnToApartment()
        {
            if (!ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow))
            {
                Debug.LogError("[WorldMapExit] 未找到 ISceneFlowService");
                return;
            }

            sceneFlow!.LoadAsync(SceneId.Apartment);
        }
    }
}
