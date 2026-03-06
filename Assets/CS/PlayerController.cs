using UnityEngine;
using UnityEngine.UI;
using System; // 引入 System 命名空间以支持事件

[RequireComponent(typeof(Rigidbody2D))]
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

    // 删除音效引用
    /*
    [Header("音效")]
    public AudioClip floatSound; // 单击上浮
    public AudioClip dashSound; // 冲刺释放
    public AudioClip chargeStartSound;// 蓄力开始的音效
    public AudioClip chargeLoopSound; // 蓄力期间的循环音效
    */

    // --- 私有变量 ---
    private Rigidbody2D rb;
    //  删除 AudioSource 变量
    // private AudioSource audioSource;
    private Vector3 originalScale;
    private bool hasDied = false;

    private enum SealState { Idle, Floating, Charging, Dashing }
    private SealState currentState = SealState.Idle;

    // 输入与计时
    private bool isSpaceHeld = false;
    private float currentHoldDuration = 0f;
    private float floatTimer = 0f;
    private float floatMaxTime = 0.4f;

    //  删除音效状态标记
    // private bool isChargeSoundPlaying = false;

    // --- 死亡事件接口 ---
    // 当角色死亡时触发，传递死亡位置和当前速度方向，供外部死亡动画模块使用
    public event Action<Vector3, Vector2> OnCharacterDiedEvent;

    // --- 音效事件接口 ---
    // 定义音效类型枚举
    public enum SoundType { Float, Dash, ChargeStart, ChargeLoop, ChargeStop };

    // 定义音效触发事件
    public static event Action<SoundType> OnPlaySoundEvent;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 删除获取 AudioSource 组件
        // audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;
        rb.gravityScale = idleGravityScale;
        rb.drag = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (chargeBarGameObject != null)
            chargeBarGameObject.SetActive(false);

        // 删除 PreloadAudio() 调用
        // PreloadAudio();
    }

    // 删除整个 PreloadAudio 函数
    /*
    void PreloadAudio()
    {
        if (audioSource == null) return;

        if (floatSound != null) audioSource.PlayOneShot(floatSound, 0f);
        if (dashSound != null) audioSource.PlayOneShot(dashSound, 0f);
        if (chargeStartSound != null) audioSource.PlayOneShot(chargeStartSound, 0f);
        if (chargeLoopSound != null) audioSource.PlayOneShot(chargeLoopSound, 0f);
        audioSource.Stop();
    }
    */

    void Update()
    {
        if (hasDied) return; // 死亡后停止所有输入响应

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

                if (chargeBarGameObject != null)
                    chargeBarGameObject.SetActive(true);

                //  触发音效事件，而非直接播放
                OnPlaySoundEvent?.Invoke(SoundType.ChargeStart);

                //  触发循环音效播放事件
                OnPlaySoundEvent?.Invoke(SoundType.ChargeLoop);
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
                    //  触发停止循环音效事件
                    OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);

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

        // 防御性编程
        if (!isSpaceHeld && currentState == SealState.Charging)
        {
            //  触发停止循环音效事件
            OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);
            currentState = SealState.Idle;
        }
    }

    void FixedUpdate()
    {
        if (hasDied) return; // 死亡后停止物理更新

        // 1. 蓄力计时
        if (currentState == SealState.Charging && isSpaceHeld)
        {
            currentHoldDuration += Time.fixedDeltaTime;
            if (currentHoldDuration > chargeDuration)
            {
                currentHoldDuration = chargeDuration;
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
        //  触发音效事件，而非直接播放
        OnPlaySoundEvent?.Invoke(SoundType.Float);
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
        //  触发音效事件，而非直接播放
        OnPlaySoundEvent?.Invoke(SoundType.Dash);
    }

    // --- 删除所有音效管理函数 ---
    /*
    void StopChargeSound()
    {
        if (isChargeSoundPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
            isChargeSoundPlaying = false;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    */

    void HandleMovementPhysics()
    {
        switch (currentState)
        {
            case SealState.Idle:
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
                    if (chargeBarGameObject != null)
                        chargeBarGameObject.SetActive(false);
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
            if (chargeBarGameObject != null)
                chargeBarGameObject.SetActive(true);
            if (chargeBarSlider != null)
            {
                float ratio = Mathf.Clamp01(currentHoldDuration / chargeDuration);
                chargeBarSlider.value = ratio;
            }
        }
        else
        {
            if (chargeBarGameObject != null)
                chargeBarGameObject.SetActive(false);
        }
    }

    public void ForceReset()
    {
        //  触发停止循环音效事件
        OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);
        currentState = SealState.Idle;
        isSpaceHeld = false;
        currentHoldDuration = 0f;
        floatTimer = 0f;
        rb.velocity = Vector2.zero;
        rb.drag = 0f;
        transform.localScale = originalScale;

        if (chargeBarGameObject != null)
            chargeBarGameObject.SetActive(false);

        // 重置死亡状态 (如果需要重玩同一场景而不重新加载)
        hasDied = false;
        enabled = true; // 重新启用碰撞箱

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // 恢复重力设置
        rb.gravityScale = idleGravityScale;
    }

    public void OnHitObstacle()
    {
        if (hasDied) return;
        hasDied = true;

        // 通知外部系统
        // 让外部系统有机会生成替身动画
        // 获取当前的位置和速度方向，传递给死亡动画模块
        Vector3 deathPosition = transform.position;
        Vector2 deathVelocity = rb.velocity;

        // 如果速度很小，默认给一个向上的方向，避免替身动画方向错误
        if (deathVelocity.magnitude < 0.1f)
        {
            deathVelocity = Vector2.up;
        }

        // 触发死亡事件
        OnCharacterDiedEvent?.Invoke(deathPosition, deathVelocity);

        // 隐藏本体 (替代之前的 enabled = false 和 物理弹飞)
        gameObject.SetActive(false);

        // 禁用碰撞箱防止干扰
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 通知 GameManager 游戏结束
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }

        //  触发停止循环音效事件
        OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);
        //  在这里可以添加播放死亡音效的事件
        // OnPlaySoundEvent?.Invoke(SoundType.Death);
    }
}