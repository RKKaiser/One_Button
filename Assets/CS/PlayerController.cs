using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class SealController : MonoBehaviour
{
    [Header("运动参数")]
    public float idleGravityScale = 0f;
    public float floatSpeed = 3f;
    public float maxDashSpeed = 15f;
    public float minDashSpeed = 6f;
    public float chargeDuration = 1.5f;
    public float dashDrag = 5f;
    public float clickThreshold = 0.2f;

    [Header("形变参数")]
    [Range(0.1f, 0.9f)]
    public float squashAmount = 0.6f;
    public float deformLerpSpeed = 10f;

    [Header("引用")]
    public Slider chargeBarSlider;
    public GameObject chargeBarGameObject;

    [Header("音效")]
    public AudioClip floatSound;      // 单击上浮
    public AudioClip dashSound;       // 冲刺释放
    public AudioClip chargeStartSound;// [新增] 蓄力开始的音效 (可选，如"咻"的一声)
    public AudioClip chargeLoopSound; // [重要] 蓄力期间的循环音效 (如引擎嗡嗡声)

    // --- 私有变量 ---
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Vector3 originalScale;

    private enum SealState { Idle, Floating, Charging, Dashing }
    private SealState currentState = SealState.Idle;

    // 输入与计时
    private bool isSpaceHeld = false;
    private float currentHoldDuration = 0f;
    private float floatTimer = 0f;
    private float floatMaxTime = 0.4f;

    // [新增] 音效状态标记，防止重复播放或漏播
    private bool isChargeSoundPlaying = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        rb.gravityScale = idleGravityScale;
        rb.drag = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (chargeBarGameObject != null) chargeBarGameObject.SetActive(false);

        PreloadAudio();
    }

    void PreloadAudio()
    {
        if (audioSource == null) return;
        if (floatSound != null) audioSource.PlayOneShot(floatSound, 0f);
        if (dashSound != null) audioSource.PlayOneShot(dashSound, 0f);
        if (chargeStartSound != null) audioSource.PlayOneShot(chargeStartSound, 0f);
        if (chargeLoopSound != null) audioSource.PlayOneShot(chargeLoopSound, 0f);
        audioSource.Stop();
    }

    void Update()
    {
        // 1. 检测按下
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSpaceHeld = true;

            if (currentState == SealState.Idle || currentState == SealState.Floating)
            {
                currentState = SealState.Charging;
                currentHoldDuration = 0f;
                floatTimer = 0f;
                rb.velocity = new Vector2(rb.velocity.x, 0);

                if (chargeBarGameObject != null) chargeBarGameObject.SetActive(true);

                // [修复点 1] 在进入蓄力状态的瞬间，播放“开始音效”
                PlaySound(chargeStartSound);

                // [修复点 2] 如果有循环音效，标记为需要播放
                if (chargeLoopSound != null)
                {
                    // 注意：这里不直接播放，交给 FixedUpdate 处理，或者在这里直接播放并循环
                    // 方案 A: 在 Update 里直接播放循环音 (简单，但可能受帧率影响微小延迟)
                    // 方案 B: 在 FixedUpdate 里检测状态变化播放 (更稳)
                    // 我们采用方案 C: 在这里播放，并设置标记防止重复
                    if (!isChargeSoundPlaying)
                    {
                        audioSource.loop = true; // 设置为循环
                        audioSource.clip = chargeLoopSound;
                        audioSource.Play();
                        isChargeSoundPlaying = true;
                    }
                }
            }
        }

        // 2. 检测松开
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (isSpaceHeld)
            {
                isSpaceHeld = false;

                if (currentState == SealState.Charging)
                {
                    // [修复点 3] 松开时，无论单击还是蓄力，都要停止蓄力音效
                    StopChargeSound();

                    if (currentHoldDuration < clickThreshold)
                    {
                        PerformFloat();
                    }
                    else
                    {
                        PerformDash();
                    }
                }
            }
        }

        // 防御性编程：如果意外松开键但状态还在 Charging (比如切屏了)
        if (!isSpaceHeld && currentState == SealState.Charging)
        {
            StopChargeSound();
            currentState = SealState.Idle;
        }
    }

    void FixedUpdate()
    {
        // 1. 蓄力计时
        if (currentState == SealState.Charging && isSpaceHeld)
        {
            currentHoldDuration += Time.fixedDeltaTime;
            if (currentHoldDuration > chargeDuration)
            {
                currentHoldDuration = chargeDuration;
                // 可以在这里添加蓄力满的特殊音效提示
            }
        }

        // 2. 上浮计时
        if (currentState == SealState.Floating)
        {
            floatTimer += Time.fixedDeltaTime;
            if (floatTimer >= floatMaxTime)
            {
                StopFloating();
            }
        }

        // 3. 物理移动
        HandleMovementPhysics();

        // 4. 视觉与 UI
        HandleVisualDeformation();
        UpdateChargeUI();
    }

    // --- 动作执行 ---

    void PerformFloat()
    {
        currentState = SealState.Floating;
        floatTimer = 0f;
        rb.velocity = new Vector2(0, floatSpeed);
        PlaySound(floatSound);
        // 蓄力音已在 Update 的 KeyUp 中停止
    }

    void StopFloating()
    {
        if (currentState == SealState.Floating)
        {
            currentState = SealState.Idle;
            rb.velocity = new Vector2(0, 0);
            floatTimer = 0f;
        }
    }

    void PerformDash()
    {
        currentState = SealState.Dashing;
        float chargeRatio = Mathf.Clamp01(currentHoldDuration / chargeDuration);
        float currentDashSpeed = Mathf.Lerp(minDashSpeed, maxDashSpeed, chargeRatio);

        rb.velocity = new Vector2(0, currentDashSpeed);
        PlaySound(dashSound);
        // 蓄力音已在 Update 的 KeyUp 中停止
    }

    // --- 核心修复：音效管理函数 ---

    void StopChargeSound()
    {
        if (isChargeSoundPlaying)
        {
            audioSource.Stop(); // 停止当前播放的所有声音
            audioSource.loop = false; // 恢复非循环状态，以免影响下一次 PlayOneShot
            isChargeSoundPlaying = false;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            // 如果正在播放循环的蓄力音，PlayOneShot 通常会混合播放，不会打断
            // 但为了保险，如果是 dashSound，我们肯定希望清晰听到
            audioSource.PlayOneShot(clip);
        }
    }

    void HandleMovementPhysics()
    {
        switch (currentState)
        {
            case SealState.Idle:
                rb.velocity = new Vector2(0, 0);
                rb.gravityScale = 0;
                rb.drag = 0f;
                break;
            case SealState.Charging:
                rb.velocity = new Vector2(0, 0);
                rb.gravityScale = 0;
                rb.drag = 0f;
                break;
            case SealState.Floating:
                rb.gravityScale = 0;
                rb.drag = 0f;
                break;
            case SealState.Dashing:
                rb.gravityScale = 0;
                if (rb.velocity.y < 0.5f)
                {
                    currentState = SealState.Idle;
                    rb.velocity = Vector2.zero;
                    if (chargeBarGameObject != null) chargeBarGameObject.SetActive(false);
                }
                else
                {
                    rb.drag = dashDrag;
                }
                break;
        }
    }

    void HandleVisualDeformation()
    {
        Vector3 targetScale = originalScale;

        if (currentState == SealState.Charging)
        {
            float chargeRatio = Mathf.Clamp01(currentHoldDuration / chargeDuration);
            float scaleY = Mathf.Lerp(1f, squashAmount, chargeRatio);
            float scaleX = Mathf.Lerp(1f, 1f + (1f - squashAmount) * 0.5f, chargeRatio);
            targetScale = new Vector3(scaleX * originalScale.x, scaleY * originalScale.y, originalScale.z);
        }
        else if (currentState == SealState.Dashing)
        {
            float speedRatio = Mathf.Clamp01(rb.velocity.y / maxDashSpeed);
            float scaleY = Mathf.Lerp(1f, 1.3f, speedRatio);
            float scaleX = Mathf.Lerp(1f, 0.8f, speedRatio);
            targetScale = new Vector3(scaleX * originalScale.x, scaleY * originalScale.y, originalScale.z);
        }
        else if (currentState == SealState.Floating)
        {
            targetScale = new Vector3(originalScale.x * 0.95f, originalScale.y * 1.05f, originalScale.z);
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.fixedDeltaTime * deformLerpSpeed);
    }

    void UpdateChargeUI()
    {
        if (currentState == SealState.Charging)
        {
            if (chargeBarGameObject != null) chargeBarGameObject.SetActive(true);
            if (chargeBarSlider != null)
            {
                float ratio = Mathf.Clamp01(currentHoldDuration / chargeDuration);
                chargeBarSlider.value = ratio;
            }
        }
        else
        {
            if (chargeBarGameObject != null) chargeBarGameObject.SetActive(false);
        }
    }

    public void ForceReset()
    {
        StopChargeSound(); // 重置时也要确保停止音效
        currentState = SealState.Idle;
        isSpaceHeld = false;
        currentHoldDuration = 0f;
        floatTimer = 0f;
        rb.velocity = Vector2.zero;
        rb.drag = 0f;
        transform.localScale = originalScale;
        if (chargeBarGameObject != null) chargeBarGameObject.SetActive(false);
    }
}