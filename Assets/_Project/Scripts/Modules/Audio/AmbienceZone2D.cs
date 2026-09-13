using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class AmbienceZone2D : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip ambienceClip;

    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 0.6f;

    [Header("Fade")]
    [SerializeField] private float fadeInSeconds = 0.8f;
    [SerializeField] private float fadeOutSeconds = 1.0f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private AudioSource audioSource;
    private Coroutine fadeRoutine;

    // 防止 Player 有多个 Collider 时，
    // 一个 Collider 先出去导致声音误停
    private readonly HashSet<Collider2D> playerCollidersInside = new();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // 这是“进入区域就能听见”的环境声，
        // 暂时使用纯 2D。
        audioSource.spatialBlend = 0f;

        audioSource.clip = ambienceClip;
        audioSource.volume = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside.Add(other);

        // 已经有 Player 的其他 Collider 在区域内了，
        // 不重复启动声音。
        if (playerCollidersInside.Count > 1)
            return;

        StartFade(targetVolume, fadeInSeconds);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside.Remove(other);

        // Player 还有其他 Collider 留在区域内
        if (playerCollidersInside.Count > 0)
            return;

        StartFade(0f, fadeOutSeconds);
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            return true;

        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.CompareTag(playerTag))
        {
            return true;
        }

        return other.transform.root.CompareTag(playerTag);
    }

    private void StartFade(float target, float duration)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(target, duration));
    }

    private IEnumerator FadeRoutine(float target, float duration)
    {
        if (ambienceClip == null)
            yield break;

        if (audioSource.clip != ambienceClip)
        {
            audioSource.clip = ambienceClip;
        }

        // 淡入前确保开始播放
        if (target > 0f && !audioSource.isPlaying)
        {
            audioSource.volume = 0f;
            audioSource.Play();
        }

        float startVolume = audioSource.volume;

        if (duration <= 0f)
        {
            audioSource.volume = target;

            if (target <= 0f)
                audioSource.Stop();

            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            audioSource.volume =
                Mathf.Lerp(startVolume, target, t);

            yield return null;
        }

        audioSource.volume = target;

        // 淡出完成后真正停止播放
        if (target <= 0f)
        {
            audioSource.Stop();
        }

        fadeRoutine = null;
    }

    private void OnDisable()
    {
        playerCollidersInside.Clear();

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }
}
