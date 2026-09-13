using UnityEngine;
using UnityEngine.EventSystems;

public class SFXTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("Sound")]
    [SerializeField] private AudioClip clip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    [Header("Click Detection")]
    [Tooltip("Canvas里的 Button / Image / UI 元素使用这个")]
    [SerializeField] private bool playOnUIClick = true;

    [Tooltip("场景里的 SpriteRenderer + Collider2D 使用这个")]
    [SerializeField] private bool playOnWorldSpriteClick = false;


    /// <summary>
    /// Canvas UI：
    /// Button、Image 等被点击时自动调用
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playOnUIClick)
            return;

        Play();
    }


    /// <summary>
    /// 世界中的 SpriteRenderer：
    /// 物体需要 Collider2D / Collider
    /// </summary>
    private void OnMouseDown()
    {
        if (!playOnWorldSpriteClick)
            return;

        Play();
    }


    /// <summary>
    /// 也可以被其他脚本、Button OnClick、Animation Event 手动调用
    /// </summary>
    public void Play()
    {
        if (clip == null)
        {
            Debug.LogWarning(
                $"[SFXTrigger] {gameObject.name} 没有设置 AudioClip。",
                this
            );
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                $"[SFXTrigger] 找不到 AudioManager。Object: {gameObject.name}",
                this
            );
            return;
        }

        AudioManager.Instance.PlaySFX(clip, volume);
    }
}
