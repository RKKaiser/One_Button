using UnityEngine;

public class BackgroundFish : MonoBehaviour
{
    [Header("基础移动设置")]
    public float moveSpeed = 2f;
    [Tooltip("走出屏幕后等待多久再次出现 (秒)")]
    public float waitTime = 2f;
    [Tooltip("水平活动范围的半宽 (基于相机中心)。超过此距离视为离开屏幕。建议与 LevelManager 的 baseSpawnXRange 一致。")]
    public float spawnRangeX = 8f;

    private bool moveRight = false;
    private Transform player;
    private System.Action<object> onReturnCallback;

    // 状态控制
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Camera mainCamera;

    /// <summary>
    /// 初始化背景鱼
    /// </summary>
    public void Init(Vector3 startPos, bool right, System.Action<object> returnCallback)
    {
        transform.position = startPos;
        moveRight = right;
        onReturnCallback = returnCallback;

        mainCamera = Camera.main;

        // 简单的翻转，假设鱼默认朝右
        Vector3 scale = transform.localScale;
        scale.x = right ? 1f : -1f;
        transform.localScale = scale;

        // 重置状态
        isWaiting = false;
        waitTimer = 0f;
    }

    void Update()
    {
        if (isWaiting)
        {
            HandleWaiting();
            return;
        }

        // 正常移动
        float direction = moveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);

        // 检查是否超出设定的水平范围
        CheckHorizontalBounds();
    }

    // 处理等待逻辑
    void HandleWaiting()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            RespawnOnOppositeSide();
        }
    }

    // 检测水平边界 (基于相机中心和配置的范围)
    void CheckHorizontalBounds()
    {
        if (mainCamera == null) return;

        float camX = mainCamera.transform.position.x;
        float limitRight = camX + spawnRangeX;
        float limitLeft = camX - spawnRangeX;

        // 如果向右游且超出了右边界
        if (moveRight && transform.position.x > limitRight)
        {
            StartWaiting();
        }
        // 如果向左游且超出了左边界
        else if (!moveRight && transform.position.x < limitLeft)
        {
            StartWaiting();
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = waitTime;
    }

    // 重生到另一侧
    void RespawnOnOppositeSide()
    {
        isWaiting = false;

        if (mainCamera == null) return;

        float camX = mainCamera.transform.position.x;
        float buffer = 2f; // 重生在范围外一点点，避免刚出来就触发判定

        if (moveRight)
        {
            // 原本向右游，现在从左边范围外出现
            transform.position = new Vector3(camX - spawnRangeX - buffer, transform.position.y, transform.position.z);
        }
        else
        {
            // 原本向左游，现在从右边范围外出现
            transform.position = new Vector3(camX + spawnRangeX + buffer, transform.position.y, transform.position.z);
        }
    }

    void OnDisable()
    {
        isWaiting = false;
        waitTimer = 0f;
    }
}