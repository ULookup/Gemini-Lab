using UnityEngine;

public class CanvasMusicMode : MonoBehaviour
{
    [SerializeField] private AudioClip music;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.EnterMusicMode(music, volume);
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ExitMusicMode();
        }
    }
}
