using UnityEngine;

/// <summary>
/// 全局音效管理器，负责根据 SealController 发出的事件播放对应音效
/// </summary>
public class SoundController : MonoBehaviour
{
    // 将所有音效剪辑拖拽到 Inspector 中进行分配
    [Header("玩家音效")]
    public AudioClip floatSound;         // 单击上浮
    public AudioClip dashSound;          // 冲刺释放
    public AudioClip chargeStartSound;   // 蓄力开始
    public AudioClip chargeLoopSound;    // 蓄力循环

    // 用于播放循环音效的 AudioSource
    private AudioSource loopAudioSource;

    void Awake()
    {
        // 确保实例唯一
        if (FindObjectsOfType<SoundController>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // 创建一个专用的 AudioSource 用于循环播放
        GameObject loopAudioObject = new GameObject("Charge Loop Source");
        loopAudioSource = loopAudioObject.AddComponent<AudioSource>();
        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;
        loopAudioSource.clip = chargeLoopSound; // 预先设置好循环音效
        loopAudioSource.transform.SetParent(transform); // 将其设为 SoundManager 的子物体
    }

    void OnEnable()
    {
        // 订阅 SealController 发出的音效事件
        SealController.OnPlaySoundEvent += HandlePlaySound;
    }

    void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        SealController.OnPlaySoundEvent -= HandlePlaySound;
    }

    /// <summary>
    /// 根据接收到的音效类型参数，执行相应的播放或停止操作
    /// </summary>
    /// <param name="soundType">要播放的音效类型</param>
    private void HandlePlaySound(SealController.SoundType soundType)
    {
        switch (soundType)
        {
            case SealController.SoundType.Float:
                PlaySound(floatSound);
                break;
            case SealController.SoundType.Dash:
                PlaySound(dashSound);
                break;
            case SealController.SoundType.ChargeStart:
                PlaySound(chargeStartSound);
                break;
            case SealController.SoundType.ChargeLoop:
                StartChargeLoop();
                break;
            case SealController.SoundType.ChargeStop:
                StopChargeLoop();
                break;
        }
    }

    /// <summary>
    /// 播放一次性音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// 开始播放蓄力循环音效
    /// </summary>
    private void StartChargeLoop()
    {
        if (chargeLoopSound != null && !loopAudioSource.isPlaying)
        {
            loopAudioSource.Play();
        }
    }

    /// <summary>
    /// 停止播放蓄力循环音效
    /// </summary>
    private void StopChargeLoop()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }
    }
}