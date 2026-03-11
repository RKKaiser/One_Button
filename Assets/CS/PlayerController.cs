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
    public float dashDrag = 5f;           // 正常冲刺时的阻力
    public float chargeBrakeDrag = 25f;   // [新增] 蓄力刹车时的巨大阻力，用于快速停止
    public float clickThreshold = 0.2f;

    [Header("大跳锁定参数")]
    public float dashLockTime = 0.3f;     // [新增] 大跳开始后多少秒内禁止再次蓄力

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

    // [新增] 大跳锁定计时器
    private float dashLockTimer = 0f;

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

        // 更新大跳锁定计时器
        if (dashLockTimer > 0)
        {
            dashLockTimer -= Time.deltaTime;
        }

        // 1. 检测按下 
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSpaceHeld = true;

            // [修改] 逻辑判断优化
            // 允许进入蓄力的条件：
            // 1. 当前不在蓄力中
            // 2. 如果当前是大跳状态，必须过了锁定时间 (dashLockTimer <= 0)
            bool canEnterCharge = (currentState != SealState.Charging);

            if (currentState == SealState.Dashing && dashLockTimer > 0)
            {
                canEnterCharge = false; // 锁定期间禁止蓄力
            }

            if (canEnterCharge)
            {
                EnterChargeState();
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
            OnPlaySoundEvent?.Invoke(SoundType.ChargeStop);
            // 如果蓄力意外中断，且当前速度很低，则归零，否则保持惯性
            if (rb.velocity.y < 0.5f)
            {
                currentState = SealState.Idle;
                rb.velocity = Vector2.zero;
            }
            else
            {
                // 如果是在高速运动中意外松开（比如被外力影响），保持Dashing状态让阻力减速
                currentState = SealState.Dashing;
            }
        }
    }

    // [修改] 进入蓄力状态
    void EnterChargeState()
    {
        // 如果之前是大跳，现在要蓄力，我们不需要立刻把速度设为0，
        // 而是交给 FixedUpdate 中的高阻力去处理“刹车”效果
        currentState = SealState.Charging;
        currentHoldDuration = 0f;
        floatTimer = 0f;

        // [重要修改] 不再在这里强制 rb.velocity = 0;
        // 保留当前速度，让物理引擎通过 drag 来减速，手感更自然

        // 但是水平速度必须归零，防止横向漂移
        rb.velocity = new Vector2(0, rb.velocity.y);

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
        // [新增] 重置锁定计时器，大跳刚开始不能立刻蓄力
        dashLockTimer = dashLockTime;

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
                rb.velocity = new Vector2(0, 0);
                rb.gravityScale = 0;
                rb.drag = 0f;
                break;

            case SealState.Charging:
                // [修改] 蓄力时的物理逻辑
                rb.gravityScale = 0;

                // 如果是在大跳过程中进入蓄力，此时速度可能还很高
                // 我们施加巨大的阻力 (chargeBrakeDrag) 让它快速停下，而不是瞬间归零
                if (rb.velocity.y > 0.1f)
                {
                    rb.drag = chargeBrakeDrag;
                }
                else
                {
                    // 速度几乎为0时，恢复普通阻力，保持悬停
                    rb.drag = 0f;
                    rb.velocity = new Vector2(0, 0); // 确保完全停稳
                }
                break;

            case SealState.Floating:
                rb.gravityScale = 0;
                rb.drag = 0f;
                break;

            case SealState.Dashing:
                rb.gravityScale = 0;
                rb.drag = dashDrag; // 正常冲刺阻力

                // 不再自动切换状态，由输入控制
                break;
        }
    }

    void HandleVisualDeformation()
    {
        Vector3 targetScale = originalScale;

        if (currentState == SealState.Charging)
        {
            float chargeRatio = Mathf.Clamp01(currentHoldDuration / chargeDuration);

            // [修改] 形变逻辑优化
            // 如果还在高速刹车中，不要完全变扁，而是根据速度和蓄力进度混合计算
            // 这样视觉上能体现出“正在急停”的过程
            float speedRatio = Mathf.Clamp01(rb.velocity.y / maxDashSpeed);

            // 蓄力越久越扁，但速度越快会稍微拉长一点抵消
            float scaleY = Mathf.Lerp(1f, squashAmount, chargeRatio) * Mathf.Lerp(1f, 1.2f, speedRatio);
            float scaleX = Mathf.Lerp(1f, 1f + (1f - squashAmount) * 0.5f, chargeRatio) * Mathf.Lerp(1f, 0.9f, speedRatio);

            targetScale = new Vector3(scaleX * originalScale.x, scaleY * originalScale.y, originalScale.z);
        }
        else if (currentState == SealState.Dashing)
        {
            if (maxDashSpeed > 0)
            {
                float speedRatio = Mathf.Clamp01(rb.velocity.y / maxDashSpeed);
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
        dashLockTimer = 0f;
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