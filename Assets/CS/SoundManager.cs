using UnityEngine;
using System;

public class SoundController : MonoBehaviour
{
    public static SoundController Instance { get; private set; }

    [Header("玩家音效 (SFX)")]
    public AudioClip floatSound;
    public AudioClip dashSound;
    public AudioClip chargeStartSound;
    public AudioClip chargeLoopSound;

    [Header("游戏状态音效")]
    public AudioClip winSound;
    public AudioClip failSound;

    [Header("背景音乐 (BGM)")] // 【新增】BGM 配置
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.6f; // BGM 音量通常比音效小

    private AudioSource loopAudioSource;      // 用于蓄力循环
    private AudioSource oneShotAudioSource;   // 用于一次性音效
    private AudioSource bgmAudioSource;       // 【新增】专门用于 BGM

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 确保切换场景时 BGM 不会断

        // 1. 初始化蓄力循环源
        GameObject loopAudioObject = new GameObject("Charge Loop Source");
        loopAudioSource = loopAudioObject.AddComponent<AudioSource>();
        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;
        loopAudioSource.clip = chargeLoopSound;
        loopAudioSource.transform.SetParent(transform);

        // 2. 初始化一次性音效源
        oneShotAudioSource = gameObject.AddComponent<AudioSource>();
        oneShotAudioSource.playOnAwake = false;
        oneShotAudioSource.spatialBlend = 0f;

        // 3. 【新增】初始化 BGM 源
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = true; // BGM 必须循环
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.spatialBlend = 0f;

    }

    void OnEnable()
    {
            SealController.OnPlaySoundEvent += HandlePlaySound;
    }

    void OnDisable()
    {
            SealController.OnPlaySoundEvent -= HandlePlaySound;
    }

    // ... (HandlePlaySound, PlayGameSound, PlayOneShot 等方法保持不变) ...

    private void HandlePlaySound(SealController.SoundType soundType)
    {
        switch (soundType)
        {
            case SealController.SoundType.Float: PlayOneShot(floatSound); break;
            case SealController.SoundType.Dash: PlayOneShot(dashSound); break;
            case SealController.SoundType.ChargeStart: PlayOneShot(chargeStartSound); break;
            case SealController.SoundType.ChargeLoop: StartChargeLoop(); break;
            case SealController.SoundType.ChargeStop: StopChargeLoop(); break;
        }
    }

    public void PlayGameSound(bool isWin)
    {
        if (isWin) PlayOneShot(winSound);
        else PlayOneShot(failSound);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || oneShotAudioSource == null) return;
        if (oneShotAudioSource.isPlaying) oneShotAudioSource.Stop();
        oneShotAudioSource.PlayOneShot(clip);
    }

    // --- BGM 相关新方法 ---

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM()
    {
        if (bgmClip == null || bgmAudioSource == null)
        {
            Debug.LogWarning("BGM Clip or Source is missing!");
            return;
        }

        // 如果当前已经在播放同一个音乐，则不重复播放
        if (bgmAudioSource.clip == bgmClip && bgmAudioSource.isPlaying)
        {
            return;
        }

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.Play();
        Debug.Log("BGM Started: " + bgmClip.name);
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
    }

    /// <summary>
    /// 暂停/恢复 BGM (可用于游戏暂停菜单)
    /// </summary>
    public void PauseBGM(bool isPaused)
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Pause(); 
            if (isPaused) bgmAudioSource.Pause();
            else bgmAudioSource.UnPause(); 
        }
    }

    public void StopAllSounds()
    {
        if (oneShotAudioSource != null && oneShotAudioSource.isPlaying)
            oneShotAudioSource.Stop();

        if (loopAudioSource != null && loopAudioSource.isPlaying)
            loopAudioSource.Stop();

    }

    private void StartChargeLoop()
    {
        if (chargeLoopSound != null && !loopAudioSource.isPlaying)
            loopAudioSource.Play();
    }

    private void StopChargeLoop()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
            loopAudioSource.Stop();
    }
}