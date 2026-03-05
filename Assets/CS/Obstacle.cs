using UnityEngine;
using System;

public class Obstacle : MonoBehaviour
{
    [Header("基础配置")]
    public float moveSpeed = 5f;
    public float waitTime = 1.5f;
    public float activationDistance = 18f; // 超过此距离停止运算
    public float screenPadding = 2f;

    [Header("内部状态")]
    public bool isMovingRight = true;

    private bool isActive = false;
    private float waitTimer = 0f;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Transform playerTransform;
    private float screenWidthHalf;

    // 回调：当需要回收时调用
    public Action<Obstacle> onReturnToPool;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        // 初始关闭物理以节省性能
        rb.simulated = false;
        rb.velocity = Vector2.zero;
    }

    // 由 LevelManager 调用
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
                UpdateVelocity(); // 等待结束，开始移动
            }
            return;
        }

        float camX = mainCamera.transform.position.x;
        float leftBound = camX - screenWidthHalf;
        float rightBound = camX + screenWidthHalf;

        // 检查边界
        if (isMovingRight)
        {
            if (transform.position.x > rightBound)
            {
                isMovingRight = false; // 准备向左
                waitTimer = waitTime;  // 开始等待
                rb.velocity = Vector2.zero;
            }
        }
        else
        {
            if (transform.position.x < leftBound)
            {
                isMovingRight = true;  // 准备向右
                waitTimer = waitTime;  // 开始等待
                rb.velocity = Vector2.zero;
            }
        }
    }

    void UpdateVelocity()
    {
        if (waitTimer > 0) return;
        float dir = isMovingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * moveSpeed, 0);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 通知玩家被撞
            SealController playerScript = collision.gameObject.GetComponent<SealController>();
            if (playerScript != null)
            {
                playerScript.OnHitObstacle();
            }
        }
    }

    // 如果障碍物因为某些原因（如玩家死亡重置）需要手动回收
    public void ForceReturn()
    {
        if (onReturnToPool != null) onReturnToPool(this);
    }
}