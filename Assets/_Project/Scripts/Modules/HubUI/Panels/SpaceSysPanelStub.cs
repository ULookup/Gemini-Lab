#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class SpaceSysPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.SpaceSys;

        [Header("Angel 数值条")]
        [SerializeField] private Image? _angelMoodFill;
        [SerializeField] private Image? _angelEnergyFill;

        [Header("Devil 数值条")]
        [SerializeField] private Image? _devilMoodFill;
        [SerializeField] private Image? _devilEnergyFill;

        private IPetRoster? _roster;
        private IDisposable? _snapshotSub;

        protected override void OnDestroy()
        {
            _snapshotSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();
            SubscribeIfNeeded();
            RefreshAll();
        }

        public override void OnClose()
        {
            base.OnClose();
            _snapshotSub?.Dispose();
            _snapshotSub = null;
        }

        private void EnsureServices()
        {
            if (_roster == null) ServiceLocator.TryResolve(out _roster);
        }

        private void SubscribeIfNeeded()
        {
            if (_snapshotSub != null) return;
            if (ServiceLocator.TryResolve(out EventBus? bus) && bus is not null)
            {
                _snapshotSub = bus.Subscribe<PetRuntimeSnapshotChangedEvent>(_ => RefreshAll());
            }
        }

        private void RefreshAll()
        {
            if (_roster == null) return;

            var angelData = _roster.TryGet(PetId.Angel);
            if (angelData != null)
            {
                if (_angelMoodFill != null) _angelMoodFill.fillAmount = angelData.Mood / 100f;
                if (_angelEnergyFill != null) _angelEnergyFill.fillAmount = angelData.Energy / 100f;
            }

            var devilData = _roster.TryGet(PetId.Devil);
            if (devilData != null)
            {
                if (_devilMoodFill != null) _devilMoodFill.fillAmount = devilData.Mood / 100f;
                if (_devilEnergyFill != null) _devilEnergyFill.fillAmount = devilData.Energy / 100f;
            }
        }
    }
}
