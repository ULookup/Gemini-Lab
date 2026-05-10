#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.UI.Catalogs
{
    /// <summary>
    /// 静态 UI Sprite 查询服务。
    /// </summary>
    public interface IUIArtService
    {
        /// <summary>获取指定 key 对应的 Sprite；未配置时返回 null。</summary>
        Sprite? Get(string key);
    }
}
