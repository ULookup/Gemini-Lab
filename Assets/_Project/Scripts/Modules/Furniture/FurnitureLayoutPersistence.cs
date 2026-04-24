#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Persists placed furniture layout through SaveSystem.
    /// </summary>
    public sealed class FurnitureLayoutPersistence : MonoBehaviour
    {
        private const string LayoutSlot = "furniture_layout";

        private IFurnitureService? _furnitureService;
        private ISaveSystem? _saveSystem;
        private EventBus? _eventBus;
        private IDisposable? _placedSubscription;
        private IDisposable? _removedSubscription;
        private readonly SemaphoreSlim _saveLock = new(1, 1);
        private bool _saveQueued;

        private async void Start()
        {
            if (!ServiceLocator.TryResolve(out _furnitureService))
            {
                return;
            }

            if (!ServiceLocator.TryResolve(out _saveSystem))
            {
                return;
            }

            FurnitureLayoutSnapshot? snapshot = await _saveSystem.LoadAsync<FurnitureLayoutSnapshot>(LayoutSlot);
            if (snapshot is not null)
            {
                _furnitureService.RestoreLayout(snapshot);
            }

            if (ServiceLocator.TryResolve(out _eventBus))
            {
                _placedSubscription = _eventBus.Subscribe<FurniturePlacedEvent>(_ => QueueSave());
                _removedSubscription = _eventBus.Subscribe<FurnitureRemovedEvent>(_ => QueueSave());
            }
        }

        private void OnApplicationQuit()
        {
            SaveBlocking("application quit");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveBlocking("application pause");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveBlocking("application focus lost");
            }
        }

        private void OnDestroy()
        {
            _placedSubscription?.Dispose();
            _removedSubscription?.Dispose();
        }

        private void QueueSave()
        {
            if (_saveQueued)
            {
                return;
            }

            _saveQueued = true;
            _ = SaveQueuedAsync();
        }

        private async Task SaveQueuedAsync()
        {
            await Task.Delay(200);
            _saveQueued = false;
            await SaveAsyncInternal("event-driven save");
        }

        private void SaveBlocking(string reason)
        {
            try
            {
                if (_furnitureService is null || _saveSystem is null)
                {
                    return;
                }

                FurnitureLayoutSnapshot snapshot = _furnitureService.CaptureLayout();
                _saveSystem.SaveNow(LayoutSlot, snapshot);
                Debug.Log($"[FurnitureLayoutPersistence] Layout saved ({reason}).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FurnitureLayoutPersistence] Failed to save on {reason}: {ex.Message}");
            }
        }

        private async Task SaveAsyncInternal(string reason)
        {
            if (_furnitureService is null || _saveSystem is null)
            {
                return;
            }

            await _saveLock.WaitAsync();
            try
            {
                FurnitureLayoutSnapshot snapshot = _furnitureService.CaptureLayout();
                await _saveSystem.SaveAsync(LayoutSlot, snapshot);
                Debug.Log($"[FurnitureLayoutPersistence] Layout saved ({reason}).");
            }
            finally
            {
                _saveLock.Release();
            }
        }
    }
}
