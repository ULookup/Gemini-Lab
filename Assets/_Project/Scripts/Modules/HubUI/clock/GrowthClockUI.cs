using System;
using GeminiLab.Core;
using GeminiLab.Core.Time;
using GeminiLab.Modules.EmotionGarden;
using UnityEngine;
using UnityEngine.UI;


public sealed class GrowthClockUI : MonoBehaviour
{
    [Header("UI References")]

    [SerializeField]
    private Image _sproutZone;

    [SerializeField]
    private RectTransform _clockHand;


    [Header("Display")]

    [Tooltip("成熟后是否隐藏整个闹钟")]
    [SerializeField]
    private bool _hideWhenFinished = true;

    [Tooltip("如果你的指针 Rotation Z = 0 时不是朝12点，可在这里补角度")]
    [SerializeField]
    private float _handStartAngle = 0f;


    // 当前正在显示哪一朵花的创建时间
    private long _createdAtUtcTicks;

    // 当前是否有花需要显示
    private bool _hasTarget;

    // 游戏自己的时钟
    private IGameClock _clock;


    private void Awake()
    {
        ApplySproutZone();
        TryResolveClock();

        _hasTarget = false;

        if (_clockHand != null)
        {
            _clockHand.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    _handStartAngle
                );
        }
    }



    /// <summary>
    /// 显示某一朵正在生长的花。
    /// </summary>
    public void Show(long createdAtUtcTicks)
    {
        if (createdAtUtcTicks <= 0)
        {
            Hide();
            return;
        }

        _createdAtUtcTicks = createdAtUtcTicks;
        _hasTarget = true;

        gameObject.SetActive(true);

        ApplySproutZone();

        RefreshImmediately();
    }


    /// <summary>
    /// 隐藏生长时钟。
    /// </summary>
    public void Hide()
    {
        _hasTarget = false;

        // 不关闭整个闹钟
        // 只把指针恢复到起点
        if (_clockHand != null)
        {
            _clockHand.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    _handStartAngle
                );
        }

        // 保证闹钟仍然显示
        gameObject.SetActive(true);
    }



    private void Update()
    {
        if (!_hasTarget)
            return;

        RefreshImmediately();
    }


    /// <summary>
    /// 根据发芽时间 / 成熟时间，
    /// 自动设置绿色 Zone 的大小。
    /// </summary>
    private void ApplySproutZone()
    {
        if (_sproutZone == null)
            return;

        float bloomSeconds =
            Mathf.Max(
                0.01f,
                EmotionGardenGrowthTiming.BloomSeconds
            );

        float sproutRatio =
            EmotionGardenGrowthTiming.SproutSeconds
            / bloomSeconds;

        _sproutZone.fillAmount =
            Mathf.Clamp01(sproutRatio);
    }


    /// <summary>
    /// 刷新指针。
    /// </summary>
    private void RefreshImmediately()
    {
        if (_clockHand == null)
            return;

        if (!TryResolveClock())
            return;

        long elapsedTicks =
            _clock.UtcNow.Ticks -
            _createdAtUtcTicks;

        if (elapsedTicks < 0)
            elapsedTicks = 0;

        double elapsedSeconds =
            TimeSpan
                .FromTicks(elapsedTicks)
                .TotalSeconds;

        float bloomSeconds =
            Mathf.Max(
                0.01f,
                EmotionGardenGrowthTiming.BloomSeconds
            );

        float progress =
            Mathf.Clamp01(
                (float)(
                    elapsedSeconds /
                    bloomSeconds
                )
            );

        // ==========================================
        // 0%   = 12点
        // 25%  = 3点
        // 50%  = 6点
        // 75%  = 9点
        // 100% = 回到12点
        //
        // Unity Z 负角度 = 顺时针
        // ==========================================

        float angle =
            _handStartAngle -
            progress * 360f;

        _clockHand.localEulerAngles =
            new Vector3(
                0f,
                0f,
                angle
            );


        // // 完全成熟
        // if (progress >= 1f &&
        //     _hideWhenFinished)
        // {
        //     Hide();
        // }
    }


    /// <summary>
    /// 找项目当前使用的 IGameClock。
    /// </summary>
    private bool TryResolveClock()
    {
        if (_clock != null)
            return true;

        if (ServiceLocator.TryResolve(
                out IGameClock clock) &&
            clock != null)
        {
            _clock = clock;
            return true;
        }

        return false;
    }
}
