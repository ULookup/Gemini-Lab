#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.Travel
{
    public interface ITravelService
    {
        IReadOnlyList<TravelTimelineEntry> Timeline { get; }
        bool IsTraveling { get; }
        Task<string> DepartAsync(string playerId, string topic, CancellationToken cancellationToken = default);
    }
}
