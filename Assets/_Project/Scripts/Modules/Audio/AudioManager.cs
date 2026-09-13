using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip music;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource modeBgmSource;

    private bool baseMusicWasPlayingBeforeMode = false;
    private bool isModeMusicActive = false;


    [Header("Scene Music")]
    [SerializeField] private List<SceneMusic> sceneMusic = new();

    private const string DesktopSceneName = "Desktop_Overlay";

    private bool isDesktopMode = false;
    private bool musicWasPlayingBeforeDesktop = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        //SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        //SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Start()
    {
        TryPlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // // Desktop 模式：暂停音乐
        // if (scene.name == DesktopSceneName)
        // {
        //     EnterDesktopMode();
        //     return;
        // }

        // // Desktop 正开着时，不允许其它 Scene 启动音乐
        // if (isDesktopMode)
        //     return;
        

        Debug.Log($"[AudioManager] Scene loaded: {scene.name}");
        TryPlaySceneMusic(scene.name);
    }

    private void TryPlaySceneMusic(string sceneName)
    {
        SceneMusic data =
            sceneMusic.Find(x => x.sceneName == sceneName);

        // 没有为该 Scene 配音乐：
        // 什么也不做，继续保持当前 BGM
        if (data == null || data.music == null)
            return;

        // 当前已经是这首歌，不从头播放
        if (bgmSource.clip == data.music &&
            bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = data.music;
        bgmSource.volume = data.volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }

    public void StopMusic()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // private void OnSceneUnloaded(Scene scene)
    // {
    //     if (scene.name == DesktopSceneName)
    //     {
    //         ExitDesktopMode();
    //     }
    // }

    private void EnterDesktopMode()
    {
        isDesktopMode = true;

        musicWasPlayingBeforeDesktop = bgmSource.isPlaying;

        if (musicWasPlayingBeforeDesktop)
        {
            bgmSource.Pause();
        }
    }

    private void ExitDesktopMode()
    {
        isDesktopMode = false;

        if (musicWasPlayingBeforeDesktop && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }

        musicWasPlayingBeforeDesktop = false;
    }

    public void EnterMusicMode(AudioClip music, float volume = 1f)
    {
        // 防止重复进入
        if (isModeMusicActive)
        {
            modeBgmSource.Stop();
        }
        else
        {
            baseMusicWasPlayingBeforeMode = bgmSource.isPlaying;

            if (baseMusicWasPlayingBeforeMode)
            {
                bgmSource.Pause();
            }

            isModeMusicActive = true;
        }

        if (music == null)
            return;

        modeBgmSource.clip = music;
        modeBgmSource.volume = volume;
        modeBgmSource.loop = true;
        modeBgmSource.Play();
    }

    public void ExitMusicMode()
    {
        if (!isModeMusicActive)
            return;

        modeBgmSource.Stop();
        modeBgmSource.clip = null;

        isModeMusicActive = false;

        if (baseMusicWasPlayingBeforeMode &&
            bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }

        baseMusicWasPlayingBeforeMode = false;
    }



}
