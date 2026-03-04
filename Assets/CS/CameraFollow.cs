using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("跟随的目标（玩家海豹）")]
    public Transform target;

    [Header("跟随参数")]
    [Tooltip("跟随的平滑速度 (越小越慢，越大越瞬移)")]
    [Range(0.1f, 20f)]
    public float followSpeed = 5f;

    [Tooltip("Y轴偏移量 (正值让摄像机在玩家上方，留出上浮空间)")]
    public float yOffset = 2f;

    [Tooltip("X轴偏移量 (通常设为0)")]
    public float xOffset = 0f;

    [Header("边界限制 (可选)")]
    [Tooltip("是否限制摄像机移动范围")]
    public bool limitBounds = false;

    [Tooltip("最小X坐标")]
    public float minX = -10f;
    [Tooltip("最大X坐标")]
    public float maxX = 10f;
    [Tooltip("最小Y坐标 (海底)")]
    public float minY = -5f;
    [Tooltip("最大Y坐标 (海面)")]
    public float maxY = 20f;

    // 3D摄像机的Z轴距离 (2D游戏通常Z=-10)
    private float defaultZ = -10f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: 未设置目标(Transform)，请手动拖入玩家物体！");
            return;
        }

        // 记录初始Z轴，防止意外改变
        defaultZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 计算目标位置
        // 目标位置 = 玩家位置 + 偏移量
        Vector3 targetPosition = new Vector3(
            target.position.x + xOffset,
            target.position.y + yOffset,
            defaultZ
        );

        // 2. 应用边界限制 (如果开启)
        if (limitBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        // 3. 平滑移动摄像机
        // 使用 Vector3.Lerp 实现平滑追赶效果
        // Time.deltaTime 保证不同帧率下速度一致
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // 4. 应用位置
        transform.position = smoothedPosition;

        // 强制锁定Z轴，防止2D变3D视角
        transform.position = new Vector3(transform.position.x, transform.position.y, defaultZ);
    }
}