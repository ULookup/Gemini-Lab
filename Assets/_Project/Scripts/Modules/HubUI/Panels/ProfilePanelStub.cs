#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class ProfilePanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.PetStatus;

        [Header("标题")]
        [SerializeField] private Image? _titleIcon;

        [Header("Angel 侧")]
        [SerializeField] private Image? _angelBg;
        [SerializeField] private PersonalityRadarGraphic? _angelRadar;
        [SerializeField] private Image? _angelMoodIcon;
        [SerializeField] private TMP_Text? _angelMoodText;
        [SerializeField] private Image? _angelEnergyIcon;
        [SerializeField] private TMP_Text? _angelEnergyText;
        [SerializeField] private Image? _angelEnergyFill;
        [SerializeField] private Image? _angelRelationIcon;
        [SerializeField] private TMP_Text? _angelRelationText;

        [Header("Evil 侧")]
        [SerializeField] private Image? _evilBg;
        [SerializeField] private PersonalityRadarGraphic? _evilRadar;
        [SerializeField] private Image? _evilMoodIcon;
        [SerializeField] private TMP_Text? _evilMoodText;
        [SerializeField] private Image? _evilEnergyIcon;
        [SerializeField] private TMP_Text? _evilEnergyText;
        [SerializeField] private Image? _evilEnergyFill;
        [SerializeField] private Image? _evilRelationIcon;
        [SerializeField] private TMP_Text? _evilRelationText;

        [Header("宠物立绘（中心）")]
        [SerializeField] private Image? _angelPetImage;
        [SerializeField] private Image? _evilPetImage;

        private IPetRoster? _roster;
        private IDisposable? _snapshotSub;
        private IDisposable? _matrixSub;

        protected override void OnDestroy()
        {
            _snapshotSub?.Dispose();
            _matrixSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            ResolveServicesIfNeeded();
            SubscribeSnapshotIfNeeded();
            RefreshAll();
        }

        public override void OnClose()
        {
            base.OnClose();
            _snapshotSub?.Dispose();
            _snapshotSub = null;
            _matrixSub?.Dispose();
            _matrixSub = null;
        }

        private void ResolveServicesIfNeeded()
        {
            if (_roster == null) ServiceLocator.TryResolve(out _roster);
        }

        private void SubscribeSnapshotIfNeeded()
        {
            if (_snapshotSub != null) return;
            if (ServiceLocator.TryResolve(out EventBus? bus) && bus is not null)
            {
                _snapshotSub = bus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnSnapshotChanged);
            }

            // Subscribe to personality evolution changes (re-sub on every open)
            if (_matrixSub == null &&
                ServiceLocator.TryResolve(out IPersonalityEvolutionService? evolution) &&
                evolution != null)
            {
                evolution.MatrixChanged += OnMatrixChanged;
                _matrixSub = new ActionDisposable(() => evolution.MatrixChanged -= OnMatrixChanged);
            }
        }

        private void OnMatrixChanged(PetId _, PersonalityVector __)
        {
            RefreshAll();
        }

        private void OnSnapshotChanged(PetRuntimeSnapshotChangedEvent _)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshPet(PetId.Angel, _angelMoodText, _angelEnergyText, _angelRelationText, _angelRadar, _angelEnergyFill);
            RefreshPet(PetId.Devil, _evilMoodText, _evilEnergyText, _evilRelationText, _evilRadar, _evilEnergyFill);
        }

        private void RefreshPet(PetId id, TMP_Text? moodText, TMP_Text? energyText,
            TMP_Text? relationText, PersonalityRadarGraphic? radar, Image? energyFill)
        {
            if (_roster != null)
            {
                var data = _roster.TryGet(id);
                if (data != null)
                {
                    if (moodText != null) moodText.text = Mathf.RoundToInt(data.Mood).ToString();
                    if (energyText != null) energyText.text = Mathf.RoundToInt(data.Energy).ToString();
                    if (energyFill != null) energyFill.fillAmount = data.Energy / 100f;
                }
                else
                {
                    SetUnavailableText(moodText, energyText);
                }
            }
            else
            {
                SetUnavailableText(moodText, energyText);
            }

            if (relationText != null) relationText.text = "--";

            if (radar != null && ServiceLocator.TryResolve(out IPersonalityEvolutionService? evolution) && evolution != null)
            {
                var matrix = evolution.GetMatrix(id);
                var values = new List<float>(7)
                {
                    matrix.Kindness, matrix.Evilness, matrix.Calmness,
                    matrix.Bravery, matrix.Shyness, matrix.Integrity, matrix.Curiosity
                };
                radar.SetValues(values);
            }
            else if (radar != null)
            {
                radar.SetValues(new List<float> { 0f, 0f, 0f, 0f, 0f, 0f, 0f });
            }
        }

        private static void SetUnavailableText(TMP_Text? moodText, TMP_Text? energyText)
        {
            if (moodText != null) moodText.text = "--";
            if (energyText != null) energyText.text = "--";
        }
    }
}
