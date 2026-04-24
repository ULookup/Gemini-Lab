#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.UI.ViewModels
{
    /// <summary>
    /// Tracks pet state transitions and work status for status panel rendering.
    /// </summary>
    public sealed class PetStatusViewModel : IDisposable
    {
        private readonly IDisposable _stateChangedSub;
        private readonly IDisposable _workStartedSub;
        private readonly IDisposable _workCompletedSub;
        private readonly IDisposable _workFailedSub;

        public PetStatusViewModel(EventBus eventBus)
        {
            _stateChangedSub = eventBus.Subscribe<PetStateChangedEvent>(OnStateChanged);
            _workStartedSub = eventBus.Subscribe<PetWorkStartedEvent>(OnWorkStarted);
            _workCompletedSub = eventBus.Subscribe<PetWorkCompletedEvent>(OnWorkCompleted);
            _workFailedSub = eventBus.Subscribe<PetWorkFailedEvent>(OnWorkFailed);
        }

        public string CurrentState { get; private set; } = "Unknown";

        public string WorkStatus { get; private set; } = "Idle";

        public string LastWorkMessage { get; private set; } = string.Empty;

        public event Action? Changed;

        public void Dispose()
        {
            _stateChangedSub.Dispose();
            _workStartedSub.Dispose();
            _workCompletedSub.Dispose();
            _workFailedSub.Dispose();
        }

        private void OnStateChanged(PetStateChangedEvent payload)
        {
            CurrentState = payload.ToState;
            Changed?.Invoke();
        }

        private void OnWorkStarted(PetWorkStartedEvent payload)
        {
            WorkStatus = "Working";
            LastWorkMessage = $"At {payload.FurnitureId}";
            Changed?.Invoke();
        }

        private void OnWorkCompleted(PetWorkCompletedEvent payload)
        {
            WorkStatus = "Completed";
            LastWorkMessage = payload.Message;
            Changed?.Invoke();
        }

        private void OnWorkFailed(PetWorkFailedEvent payload)
        {
            WorkStatus = "Failed";
            LastWorkMessage = payload.Reason;
            Changed?.Invoke();
        }
    }
}
