#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.Gateway
{
    public interface IGatewayMessageService
    {
        Task<string> HandlePlayerMessageAsync(string playerId, string message, bool forceWake, CancellationToken cancellationToken = default);
    }
}
