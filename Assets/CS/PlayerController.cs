using UnityEngine;
using UnityEngine.UI;
using System;

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

    // --- 私有变量 --- 
    private Rigidbody2D rb;
    private Vector3 originalScale;
    private bool hasDied = false;

    private enum SealState { Idle, Floating, Charging, Dashing }
    private SealState currentState = SealState.Idle;

    // 输入与计时 
    private bool isSpaceHeld = false;
    private float currentHoldDuration = 0f;
    private float floatTimer = 0f;
    private float floatMaxTime = 0.4f;

    // --- 死亡事件接口 --- 
    public event Action<Vector3, Vector2> OnCharacterDiedEvent;
    public enum SoundType { Float, Dash, ChargeStart, ChargeLoop, ChargeStop };
    public static event Action<SoundType> OnPlaySoundEvent;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        rb.gravityScale = idleGravityScale;
        rb.drag = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (chargeBarGameObject != null)
            chargeBarGameObject.SetActive(false);
    }

    void Update()
    {
        if (hasDied) return;

        // 1. 检测按下 
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSpaceHeld = true;

            // [修改] 优化逻辑：允许在 Dashing 末期或 Floating 末期直接打断进入蓄力
            // 只要不是已经在蓄力中，都允许重新开启蓄力流程
            if (currentState != SealState.Charging)
            {
                // 如果当前正在 Dash 但速度已经很慢了，或者正在 Float，直接打断
                // 这样消除了“等待状态自动复位”的冷却时间
                EnterChargeState();
            }
        }

        // 2. 检测松开 
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (isSpaceHeld)
            {
                isSpaceHeld = false;

                // 只有在蓄力状态下松开才执行跳跃逻辑
                if (currentState == SealState.Charging)
                {
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
                // [新增] 如果是在 Dashing 或 Floating 过程中松开了按键（虽然通常不会在这时候松，因为那是自动的）
                // 这里不需要额外操作，让物理自然衰减即可，或者根据需要重置
            }
        }

        // 防御性编程：如果按键状态不一致，强制修正
        if (!isSpaceHeld && currentState == SealState.Charging)
        {
            OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);
            // [修改] 如果意外中断蓄力（比如切出窗口），不要直接变 Idle，而是根据当前速度判断
            // 但为了简单起见，这里保持原逻辑归零，或者可以改为保持当前速度
            currentState = SealState.Idle;
            rb.velocity = Vector2.zero; // 防止蓄力中断后速度残留
        }
    }

    // [新增] 提取进入蓄力状态的逻辑，方便复用和打断
    void EnterChargeState()
    {
        currentState = SealState.Charging;
        currentHoldDuration = 0f;
        floatTimer = 0f;

        // [关键修改] 进入蓄力时，立即水平归零，垂直速度可以根据需求选择是否立即归零
        // 为了手感更连贯，如果是在 Dash 末尾，保留一点点惯性可能更自然，但为了精准控制，这里选择立即刹停垂直速度
        rb.velocity = new Vector2(0, 0);

        if (chargeBarGameObject != null)
            chargeBarGameObject.SetActive(true);

        OnPlaySoundEvent?.Invoke(SoundType.ChargeStart);
        OnPlaySoundEvent?.Invoke(SoundType.ChargeLoop);
    }

    void FixedUpdate()
    {
        if (hasDied) return;

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

    void PerformFloat()
    {
        currentState = SealState.Floating;
        floatTimer = 0f;
        rb.velocity = new Vector2(0, floatSpeed);
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
        OnPlaySoundEvent?.Invoke(SoundType.Dash);
    }

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

                // [修改] 移除了自动切换回 Idle 的逻辑！
                // 之前：if (rb.velocity.y < 0.5f) { currentState = Idle; ... }
                // 现在：让 Dash 状态一直保持，直到玩家再次按下空格键触发 EnterChargeState()
                // 这样玩家在 Dash 结束后的滑行期间，随时按下空格都能立刻响应，没有“冷却期”

                rb.drag = dashDrag;

                // 可选：如果速度几乎为0且长时间没操作，可以自动归为Idle以节省逻辑，
                // 但为了手感，我们允许它在 Dashing 状态下“滑行”直到下一次操作
                if (rb.velocity.y < 0.1f && !isSpaceHeld)
                {
                    // 仅仅在视觉上或逻辑上视为空闲，但状态机保持 Dashing 以便随时打断
                    // 或者你可以这里强制设为 Idle，但必须在 Update 的 GetKeyDown 里允许从 Dashing 直接跳转
                    // 鉴于我们在 Update 里已经做了允许从 Dashing 跳转，这里可以安全地自动复位，
                    // 但为了防止那一瞬间的输入丢失，建议还是不要在这里频繁切换状态。
                    // 最稳妥的方式：不切换状态，让速度自然为0，下次按键直接覆盖。
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
            // [修改] 防止除以零或速度极低时的形变闪烁
            if (maxDashSpeed > 0)
            {
                float speedRatio = Mathf.Clamp01(rb.velocity.y / maxDashSpeed);
                // 如果速度很低，形变应该恢复，而不是保持拉长
                float scaleY = Mathf.Lerp(1f, 1.3f, speedRatio);
                float scaleX = Mathf.Lerp(1f, 0.8f, speedRatio);
                targetScale = new Vector3(scaleX * originalScale.x, scaleY * originalScale.y, originalScale.z);
            }
            else
            {
                targetScale = originalScale;
            }
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

        hasDied = false;
        enabled = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        rb.gravityScale = idleGravityScale;
    }

    public void OnHitObstacle()
    {
        if (hasDied) return;

        hasDied = true;
        Vector3 deathPosition = transform.position;
        Vector2 deathVelocity = rb.velocity;

        if (deathVelocity.magnitude < 0.1f)
        {
            deathVelocity = Vector2.up;
        }

        OnCharacterDiedEvent?.Invoke(deathPosition, deathVelocity);
        gameObject.SetActive(false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }

        OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);
    }
}