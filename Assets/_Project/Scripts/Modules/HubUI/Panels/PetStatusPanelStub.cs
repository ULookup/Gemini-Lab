#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 宠物状态面板：Angel / Devil 双页签 + 3 条状态值进度 + 7 维性格雷达图。
    /// 数据源：<see cref="IPetRoster"/>（只读）+ <see cref="PetRuntimeSnapshotChangedEvent"/>。
    /// </summary>
    public sealed class PetStatusPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.PetStatus;

        [Header("页签")]
        [SerializeField] private Button? _tabAngel;
        [SerializeField] private Button? _tabDevil;

        [Header("状态值")]
        [SerializeField] private TMP_Text? _petNameText;
        [SerializeField] private TMP_Text? _moodText;
        [SerializeField] private Image? _moodFill;
        [SerializeField] private TMP_Text? _energyText;
        [SerializeField] private Image? _energyFill;
        [SerializeField] private TMP_Text? _satietyText;
        [SerializeField] private Image? _satietyFill;
        [SerializeField] private TMP_Text? _stateText;

        [Header("性格雷达")]
        [SerializeField] private PersonalityRadarGraphic? _radar;
        [SerializeField] private TMP_Text[] _radarAxisLabels = Array.Empty<TMP_Text>();

        [Tooltip("雷达轴标签；7 维，顺序：善良/邪恶/冷静/勇敢/害羞/正直/好奇")]
        [SerializeField] private string[] _radarAxisTexts = { "善良", "邪恶", "冷静", "勇敢", "害羞", "正直", "好奇" };

        private PetId _currentPet = PetId.Angel;
        private IPetRoster? _roster;
        private PersonalityMatrixSO? _angelPersonality;
        private PersonalityMatrixSO? _devilPersonality;
        private IDisposable? _snapshotSub;

        protected override void Awake()
        {
            base.Awake();

            if (_tabAngel != null) _tabAngel.onClick.AddListener(() => SwitchTab(PetId.Angel));
            if (_tabDevil != null) _tabDevil.onClick.AddListener(() => SwitchTab(PetId.Devil));

            WriteRadarAxisLabels();
        }

        protected override void OnDestroy()
        {
            _snapshotSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            ResolveServicesIfNeeded();
            SubscribeSnapshotIfNeeded();
            Refresh();
        }

        public override void OnClose()
        {
            base.OnClose();
            _snapshotSub?.Dispose();
            _snapshotSub = null;
        }

        /// <summary>供 Editor 作者工具注入两只宠物的人格 SO（可选）。</summary>
        public void SetPersonalities(PersonalityMatrixSO? angel, PersonalityMatrixSO? devil)
        {
            _angelPersonality = angel;
            _devilPersonality = devil;
        }

        private void ResolveServicesIfNeeded()
        {
            if (_roster == null)
            {
                ServiceLocator.TryResolve(out _roster);
            }
        }

        private void SubscribeSnapshotIfNeeded()
        {
            if (_snapshotSub != null) return;

            if (ServiceLocator.TryResolve(out EventBus? bus) && bus is not null)
            {
                _snapshotSub = bus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnSnapshotChanged);
            }
        }

        private void OnSnapshotChanged(PetRuntimeSnapshotChangedEvent evt)
        {
            if (evt.PetId == _currentPet)
            {
                RefreshStateValues(evt.Mood, evt.Energy, evt.Satiety, evt.CurrentState);
            }
        }

        private void SwitchTab(PetId id)
        {
            _currentPet = id;
            Refresh();
        }

        private void Refresh()
        {
            if (_petNameText != null)
            {
                _petNameText.text = _currentPet == PetId.Angel ? "天使" : "恶魔";
            }

            if (_roster != null)
            {
                var data = _roster.TryGet(_currentPet);
                if (data != null)
                {
                    RefreshStateValues(data.Mood, data.Energy, data.Satiety, data.CurrentState);
                }
                else
                {
                    RefreshStateValues(0f, 0f, 0f, "未注册");
                }
            }
            else
            {
                RefreshStateValues(0f, 0f, 0f, "未注册");
            }

            RefreshRadar();
        }

        private void RefreshStateValues(float mood, float energy, float satiety, string currentState)
        {
            if (_moodText != null) _moodText.text = $"心情 {Mathf.RoundToInt(mood)}";
            if (_moodFill != null) _moodFill.fillAmount = Mathf.Clamp01(mood / 100f);
            if (_energyText != null) _energyText.text = $"精力 {Mathf.RoundToInt(energy)}";
            if (_energyFill != null) _energyFill.fillAmount = Mathf.Clamp01(energy / 100f);
            if (_satietyText != null) _satietyText.text = $"饱食 {Mathf.RoundToInt(satiety)}";
            if (_satietyFill != null) _satietyFill.fillAmount = Mathf.Clamp01(satiety / 100f);
            if (_stateText != null) _stateText.text = $"状态：{currentState}";
        }

        private void RefreshRadar()
        {
            if (_radar == null) return;

            var values = new List<float>(7);
            if (ServiceLocator.TryResolve(out IPersonalityEvolutionService? evolution) && evolution is not null)
            {
                var v = evolution.GetMatrix(_currentPet);
                values.Add(v.Kindness);
                values.Add(v.Evilness);
                values.Add(v.Calmness);
                values.Add(v.Bravery);
                values.Add(v.Shyness);
                values.Add(v.Integrity);
                values.Add(v.Curiosity);
            }
            else
            {
                // Fallback：Inspector SO
                var p = _currentPet == PetId.Angel ? _angelPersonality : _devilPersonality;
                if (p != null)
                {
                    values.Add(p.Kindness);
                    values.Add(p.Evilness);
                    values.Add(p.Calmness);
                    values.Add(p.Bravery);
                    values.Add(p.Shyness);
                    values.Add(p.Integrity);
                    values.Add(p.Curiosity);
                }
                else
                {
                    for (int i = 0; i < 7; i++) values.Add(0f);
                }
            }
            _radar.SetValues(values);
        }

        private void WriteRadarAxisLabels()
        {
            if (_radarAxisLabels == null) return;
            for (int i = 0; i < _radarAxisLabels.Length && i < _radarAxisTexts.Length; i++)
            {
                if (_radarAxisLabels[i] != null)
                {
                    _radarAxisLabels[i].text = _radarAxisTexts[i];
                }
            }
        }
    }
}
