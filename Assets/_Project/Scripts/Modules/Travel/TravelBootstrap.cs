#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Travel
{
    /// <summary>
    /// Registers travel service after runtime dependencies are available.
    /// </summary>
    public sealed class TravelBootstrap : MonoBehaviour
    {
        private bool _registered;

        private void Update()
        {
            if (_registered)
            {
                return;
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus) ||
                !ServiceLocator.TryResolve(out IGatewayMessageService? messageService))
            {
                return;
            }

            if (eventBus is null || messageService is null)
            {
                return;
            }

            PetController? petController = FindFirstObjectByType<PetController>();
            if (petController is null)
            {
                return;
            }

            ServiceLocator.Register<ITravelService>(new TravelService(messageService, eventBus, petController));
            _registered = true;
        }
    }
}
