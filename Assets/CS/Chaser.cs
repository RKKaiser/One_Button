using UnityEngine;

public class Chaser : MonoBehaviour
{
    [Header("追逐速度参数")]
    public float minChaseSpeed = 2f;      // 距离为 minSpeedDistance 时的最低移动速度
    public float maxChaseSpeed = 10f;     // 距离为 maxSpeedDistance 时的最高移动速度

    [Header("速度距离映射")]
    public float minSpeedDistance = 5f;   // 当与玩家距离小于此值时，速度为 minChaseSpeed
    public float maxSpeedDistance = 20f;  // 当与玩家距离大于等于此值时，速度为 maxChaseSpeed

    [Header("激活范围")]
    // 激活和休眠的范围基于速度距离参数，留出安全边界，防止频繁切换
    public float activationBuffer = 2f;   // 激活距离缓冲区
    public float deactivationBuffer = 2f; // 休眠距离缓冲区

    private float activationDistance;     // 实际激活距离: maxSpeedDistance + buffer
    private float deactivationDistance;   // 实际休眠距离: activationDistance + buffer

    [Header("内部组件")]
    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isActive = false;

    private System.Action<Chaser> onReturnToPool;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
    }

    public void Init(Transform player, System.Action<Chaser> returnCallback)
    {
        playerTransform = player;
        onReturnToPool = returnCallback;
        isActive = false;
        rb.simulated = false;
        rb.velocity = Vector2.zero;

        // 在初始化时计算实际的激活和休眠距离
        activationDistance = maxSpeedDistance + activationBuffer;
        deactivationDistance = activationDistance + deactivationBuffer;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer < activationDistance && !isActive)
        {
            Activate();
        }
        else if (distanceToPlayer > deactivationDistance && isActive)
        {
            Deactivate();
        }

        if (isActive)
        {
            ChasePlayer();
        }
    }

    void Activate()
    {
        isActive = true;
        rb.simulated = true;
    }

    void Deactivate()
    {
        isActive = false;
        rb.simulated = false;
        rb.velocity = Vector2.zero;
    }

    void ChasePlayer()
    {
        if (playerTransform == null) return;

        float distanceY = playerTransform.position.y - transform.position.y;

        float currentSpeed;
        if (distanceY <= minSpeedDistance)
        {
            // 距离小于等于最小速度距离时，使用最小速度
            currentSpeed = minChaseSpeed;
        }
        else if (distanceY >= maxSpeedDistance)
        {
            // 距离大于等于最大速度距离时，使用最大速度
            currentSpeed = maxChaseSpeed;
        }
        else
        {
            // 在两者之间时，使用线性插值公式
            // (当前距离 - minSpeedDistance) / (maxSpeedDistance - minSpeedDistance) 得到一个 [0, 1] 的比例
            // 再乘以速度差 (maxChaseSpeed - minChaseSpeed)，最后加上最小速度
            float ratio = (distanceY - minSpeedDistance) / (maxSpeedDistance - minSpeedDistance);
            currentSpeed = minChaseSpeed + ratio * (maxChaseSpeed - minChaseSpeed);
        }

        // 向上移动
        rb.velocity = new Vector2(0, currentSpeed);
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