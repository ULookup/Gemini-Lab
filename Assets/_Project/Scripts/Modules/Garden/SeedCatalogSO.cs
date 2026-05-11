#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Garden
{
    /// <summary>
    /// 所有 <see cref="SeedDefinitionSO"/> 的目录；GardenService 按 SeedItemId 查询。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Garden/Seed Catalog", fileName = "SeedCatalog")]
    public sealed class SeedCatalogSO : ScriptableObject
    {
        public List<SeedDefinitionSO> Seeds = new();

        public SeedDefinitionSO? FindBySeedId(string seedItemId)
        {
            for (int i = 0; i < Seeds.Count; i++)
            {
                if (Seeds[i] != null && Seeds[i].SeedItemId == seedItemId) return Seeds[i];
            }
            return null;
        }
    }
}
