#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Collection
{
    public interface IGachaService
    {
        IReadOnlyList<string> UnlockedIds { get; }
        bool IsUnlocked(string collectibleId);
        GachaResult PullSingle();
        GachaResult PullMulti(int count);
    }
}
