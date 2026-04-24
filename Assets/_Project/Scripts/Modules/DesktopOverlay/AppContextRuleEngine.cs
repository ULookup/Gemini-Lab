#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    /// <summary>
    /// Maps foreground app context to proactive pet behavior suggestions.
    /// </summary>
    public sealed class AppContextRuleEngine : MonoBehaviour
    {
        private EventBus? _eventBus;
        private IPetCommandLinkService? _commandLinkService;
        private IDisposable? _foregroundSub;

        private void Awake()
        {
            TryBindDependencies();
        }

        private void Update()
        {
            if (_foregroundSub is null || _commandLinkService is null)
            {
                TryBindDependencies();
            }
        }

        private void OnDestroy()
        {
            _foregroundSub?.Dispose();
            _foregroundSub = null;
        }

        private void OnForegroundChanged(ForegroundApplicationChangedEvent payload)
        {
            if (_commandLinkService is null)
            {
                return;
            }

            if (!ShouldSuggestWork(payload.ProcessName))
            {
                return;
            }

            _ = _commandLinkService.Enqueue(new PetCommandRequest(
                traceId: System.Guid.NewGuid().ToString("N"),
                commandType: PetCommandType.WorkRequest,
                source: PetCommandSource.System,
                forceWake: false,
                priority: 90,
                targetType: PetWorkTargetType.WorkDesk,
                message: $"Context suggestion from app: {payload.ProcessName}"));
        }

        private static bool ShouldSuggestWork(string processName)
        {
            return processName.IndexOf("code", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   processName.IndexOf("devenv", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   processName.IndexOf("idea", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void TryBindDependencies()
        {
            if (_eventBus is null)
            {
                _ = ServiceLocator.TryResolve(out _eventBus);
            }

            if (_commandLinkService is null)
            {
                _ = ServiceLocator.TryResolve(out _commandLinkService);
            }

            if (_foregroundSub is null && _eventBus is not null)
            {
                _foregroundSub = _eventBus.Subscribe<ForegroundApplicationChangedEvent>(OnForegroundChanged);
            }
        }
    }
}
