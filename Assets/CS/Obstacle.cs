using UnityEngine;
using System;

public class Obstacle : MonoBehaviour
{
    [Header("基础配置")]
    public float moveSpeed = 5f;
    public float waitTime = 1.5f;
    public float activationDistance = 18f;
    public float screenPadding = 2f;

    [Header("内部状态")]
    public bool isMovingRight = true;

    private bool isActive = false;
    private float waitTimer = 0f;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Transform playerTransform;
    private float screenWidthHalf;

    // 缓存初始缩放，防止多次翻转出错
    private Vector3 originalScale;

    public Action<Obstacle> onReturnToPool;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        // 记录初始缩放 (假设预制体导入时是朝右的，Scale.x 为正)
        originalScale = transform.localScale;

        // 初始关闭物理
        rb.simulated = false;
        rb.velocity = Vector2.zero;
    }

    public void Init(Vector3 startPos, bool startRight, Transform player, Action<Obstacle> returnCallback)
    {
        transform.position = startPos;
        isMovingRight = startRight;
        playerTransform = player;
        onReturnToPool = returnCallback;

        isActive = false;
        waitTimer = 0f;
        rb.simulated = false;
        rb.velocity = Vector2.zero;

        // 【重要】初始化时立即设置正确的朝向
        UpdateFacingDirection();

        if (mainCamera != null)
        {
            screenWidthHalf = mainCamera.orthographicSize * mainCamera.aspect + screenPadding;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. 激活/休眠 检测
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist < activationDistance)
        {
            if (!isActive) Activate();
        }
        else
        {
            if (isActive) Deactivate();
        }

        // 2. 移动逻辑
        if (isActive)
        {
            HandleMovement();
        }
    }

    void Activate()
    {
        isActive = true;
        rb.simulated = true;
        UpdateVelocity();
    }

    void Deactivate()
    {
        isActive = false;
        rb.simulated = false;
        rb.velocity = Vector2.zero;
    }

    void HandleMovement()
    {
        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                UpdateVelocity(); // 等待结束，开始移动并更新朝向
            }
            return;
        }

        float camX = mainCamera.transform.position.x;
        float leftBound = camX - screenWidthHalf;
        float rightBound = camX + screenWidthHalf;

        // 检查边界并切换方向
        if (isMovingRight)
        {
            if (transform.position.x > rightBound)
            {
                isMovingRight = false;
                waitTimer = waitTime;
                rb.velocity = Vector2.zero;
                UpdateFacingDirection(); // 【关键】立即翻转外观
            }
        }
        else
        {
            if (transform.position.x < leftBound)
            {
                isMovingRight = true;
                waitTimer = waitTime;
                rb.velocity = Vector2.zero;
                UpdateFacingDirection(); // 【关键】立即翻转外观
            }
        }
    }

    void UpdateVelocity()
    {
        if (waitTimer > 0) return;

        float dir = isMovingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * moveSpeed, 0);

        // 确保移动时朝向也是对的 (防止初始化状态不对)
        UpdateFacingDirection();
    }

    // 【核心方法】根据移动方向翻转物体
    void UpdateFacingDirection()
    {
        // 如果向右游，Scale.x 应该是正的 (原始值)
        // 如果向左游，Scale.x 应该是负的 (翻转)

        float targetScaleX = isMovingRight ? Mathf.Abs(originalScale.x) : -Mathf.Abs(originalScale.x);

        // 只有当当前缩放与目标不一致时才设置，避免每帧修改 Transform (虽然这里只在转向时调用，但做个保护更好)
        if (Mathf.Sign(transform.localScale.x) != Mathf.Sign(targetScaleX))
        {
            transform.localScale = new Vector3(targetScaleX, originalScale.y, originalScale.z);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SealController playerScript = collision.gameObject.GetComponent<SealController>();
            if (playerScript != null)
            {
                playerScript.OnHitObstacle();
            }
        }
    }

    public void ForceReturn()
    {
        if (onReturnToPool != null) onReturnToPool(this);
    }
}