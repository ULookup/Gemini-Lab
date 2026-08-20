#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using GeminiLab.Modules.Pet.Social;
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
        [SerializeField] private Image? _angelMoodFill;
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
        [SerializeField] private Image? _evilMoodFill;
        [SerializeField] private Image? _evilEnergyIcon;
        [SerializeField] private TMP_Text? _evilEnergyText;
        [SerializeField] private Image? _evilEnergyFill;
        [SerializeField] private Image? _evilRelationIcon;
        [SerializeField] private TMP_Text? _evilRelationText;

        [Header("Relation 共享")]
        [SerializeField] private Image? _relationFill;

        [Header("宠物立绘（中心）")]
        [SerializeField] private Image? _angelPetImage;
        [SerializeField] private Image? _evilPetImage;

        private IPetRoster? _roster;
        private IPetSocialService? _social;
        private IDisposable? _snapshotSub;
        private IDisposable? _matrixSub;
        private IDisposable? _friendshipSub;

        protected override void OnDestroy()
        {
            _snapshotSub?.Dispose();
            _matrixSub?.Dispose();
            _friendshipSub?.Dispose();
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
            _friendshipSub?.Dispose();
            _friendshipSub = null;
        }

        private void ResolveServicesIfNeeded()
        {
            if (_roster == null) ServiceLocator.TryResolve(out _roster);
            if (_social == null) ServiceLocator.TryResolve(out _social);
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

            // 订阅亲密度变化（数值规则文档 §14），与性格订阅同模式。
            if (_friendshipSub == null && _social != null)
            {
                _social.FriendshipChanged += OnFriendshipChanged;
                _friendshipSub = new ActionDisposable(() =>
                {
                    if (_social != null) _social.FriendshipChanged -= OnFriendshipChanged;
                });
            }
        }

        private void OnFriendshipChanged(float _)
        {
            RefreshRelation();
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
            RefreshPet(PetId.Angel, _angelMoodText, _angelMoodFill, _angelEnergyText, _angelEnergyFill, _angelRadar);
            RefreshPet(PetId.Devil, _evilMoodText, _evilMoodFill, _evilEnergyText, _evilEnergyFill, _evilRadar);
            RefreshRelation();
        }

        private void RefreshPet(PetId id,
            TMP_Text? moodText, Image? moodFill,
            TMP_Text? energyText, Image? energyFill,
            PersonalityRadarGraphic? radar)
        {
            if (_roster != null)
            {
                var data = _roster.TryGet(id);
                if (data != null)
                {
                    if (moodText != null) moodText.text = Mathf.RoundToInt(data.Mood).ToString();
                    if (moodFill != null) moodFill.fillAmount = data.Mood / 100f;
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

        /// <summary>
        /// 显示双宠亲密度（数值规则文档 §14）：读 <see cref="IPetSocialService.Friendship"/>。
        /// 旧数据源 PetRuntimeData.Relation 没有写入路径（永远初始值），已废弃显示。
        /// </summary>
        private void RefreshRelation()
        {
            float? friendship = _social?.Friendship;
            string label = friendship.HasValue
                ? $"{Mathf.RoundToInt(friendship.Value)}·{PetSocialService.GetStageLabel(friendship.Value)}"
                : "--";

            if (_angelRelationText != null)
                _angelRelationText.text = label;
            if (_evilRelationText != null)
                _evilRelationText.text = label;
            if (_relationFill != null)
                _relationFill.fillAmount = (friendship ?? 0f) / 100f;
        }

        private static void SetUnavailableText(TMP_Text? moodText, TMP_Text? energyText)
        {
            if (moodText != null) moodText.text = "--";
            if (energyText != null) energyText.text = "--";
        }
    }
}
