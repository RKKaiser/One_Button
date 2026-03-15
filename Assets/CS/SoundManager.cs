using UnityEngine;
using System;

public class SoundController : MonoBehaviour
{
    public static SoundController Instance { get; private set; }

    [Header("玩家音效")]
    public AudioClip floatSound;
    public AudioClip dashSound;
    public AudioClip chargeStartSound;
    public AudioClip chargeLoopSound;

    [Header("游戏状态音效")]
    public AudioClip winSound;
    public AudioClip failSound;

    private AudioSource loopAudioSource;
    private AudioSource oneShotAudioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化循环音效源
        GameObject loopAudioObject = new GameObject("Charge Loop Source");
        loopAudioSource = loopAudioObject.AddComponent<AudioSource>();
        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;
        loopAudioSource.clip = chargeLoopSound;
        loopAudioSource.transform.SetParent(transform);

        // 初始化一次性音效源
        oneShotAudioSource = gameObject.AddComponent<AudioSource>();
        oneShotAudioSource.playOnAwake = false;
        oneShotAudioSource.spatialBlend = 0f;
        // 【关键设置】确保不会因重叠播放导致声音停不下来
        oneShotAudioSource.pitch = 1.0f;
    }

    void OnEnable()
    {
            SealController.OnPlaySoundEvent += HandlePlaySound;
    }

    void OnDisable()
    {
            SealController.OnPlaySoundEvent -= HandlePlaySound;
    }

    private void HandlePlaySound(SealController.SoundType soundType)
    {
        switch (soundType)
        {
            case SealController.SoundType.Float:
                PlayOneShot(floatSound);
                break;
            case SealController.SoundType.Dash:
                PlayOneShot(dashSound);
                break;
            case SealController.SoundType.ChargeStart:
                PlayOneShot(chargeStartSound);
                break;
            case SealController.SoundType.ChargeLoop:
                StartChargeLoop();
                break;
            case SealController.SoundType.ChargeStop:
                StopChargeLoop();
                break;
        }
    }

    public void PlayGameSound(bool isWin)
    {
        if (isWin)
        {
            PlayOneShot(winSound);
        }
        else
        {
            PlayOneShot(failSound);
        }
    }

    /// <summary>
    /// 【修改点】播放前先停止当前声音，防止重叠或无限播放
    /// </summary>
    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || oneShotAudioSource == null) return;

        // 如果正在播放任何声音，先强制停止
        if (oneShotAudioSource.isPlaying)
        {
            oneShotAudioSource.Stop();
        }

        oneShotAudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 【新增】公开方法：强制停止所有音效（包括循环音效和一次性音效）
    /// 供 GameManager 在重启时调用
    /// </summary>
    public void StopAllSounds()
    {
        if (oneShotAudioSource != null && oneShotAudioSource.isPlaying)
        {
            oneShotAudioSource.Stop();
        }

        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }
    }

    private void StartChargeLoop()
    {
        if (chargeLoopSound != null && !loopAudioSource.isPlaying)
        {
            loopAudioSource.Play();
        }
    }

    private void StopChargeLoop()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }
    }
}