using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("障碍物预制体")]
    public GameObject eelPrefab;
    public GameObject sharkPrefab;

    [System.Serializable]
    public class ObstacleSpawnPoint
    {
        public float yPos;              // 生成高度
        public enum ObstacleType { Eel, Shark, Random }
        public ObstacleType type;       // 指定类型
        public bool forceDirectionRight = false; // 是否强制向右？(false=随机或向左)
        public float xOffset = 0f;      // 相对于摄像机中心的额外X偏移
    }

    [Header("关卡配置 (在此处设计关卡)")]
    public List<ObstacleSpawnPoint> spawnPoints = new List<ObstacleSpawnPoint>();

    [Header("通用设置")]
    public float baseSpawnXRange = 8f;  // 基础随机范围
    public float seaSurfaceY = 20f;     // 胜利高度
    public bool showGizmos = true;

    [Header("内部池子")]
    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    private Transform player;
    private Camera mainCamera;

    // 父容器引用
    public Transform obstacleParent;

    void Start()
    {
        // --- 初始化检查 ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("❌ 未找到 Player 标签的物体！");
            return;
        }
        player = playerObj.transform;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("❌ 未找到 MainCamera！");
            return;
        }

        if (obstacleParent == null)
        {
            Debug.LogWarning("⚠️ 未设置 Obstacle Parent，将使用 LevelManager 自身作为父节点。");
            obstacleParent = transform;
        }

        // 初始化池子
        if (eelPrefab != null) pools[eelPrefab] = new List<GameObject>();
        if (sharkPrefab != null) pools[sharkPrefab] = new List<GameObject>();

        SpawnObstacles();
    }

    void SpawnObstacles()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ SpawnPoints 列表为空，没有生成任何障碍物。请在 Inspector 中添加配置。");
            return;
        }

        float camX = mainCamera.transform.position.x;

        foreach (var point in spawnPoints)
        {
            // 1. 确定生成什么预制体
            GameObject prefabToSpawn = null;

            if (point.type == ObstacleSpawnPoint.ObstacleType.Random)
            {
                prefabToSpawn = (Random.value > 0.5f) ? eelPrefab : sharkPrefab;
            }
            else if (point.type == ObstacleSpawnPoint.ObstacleType.Eel)
            {
                prefabToSpawn = eelPrefab;
            }
            else if (point.type == ObstacleSpawnPoint.ObstacleType.Shark)
            {
                prefabToSpawn = sharkPrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"跳过生成点 Y={point.yPos}，因为对应的预制体未分配。");
                continue;
            }

            // 2. 确定方向
            bool moveRight;
            if (point.forceDirectionRight)
            {
                moveRight = true;
            }
            else
            {
                // 如果没强制向右，则随机 (或者你可以逻辑改成强制向左)
                moveRight = Random.value > 0.5f;
            }

            // 3. 计算最终位置
            // 基础随机范围 + 用户自定义的偏移
            float randomOffset = Random.Range(-baseSpawnXRange, baseSpawnXRange);
            float finalX = camX + randomOffset + point.xOffset;

            Vector3 spawnPos = new Vector3(finalX, point.yPos, 0);

            // 4. 从池子获取并生成
            GetFromPool(prefabToSpawn, spawnPos, moveRight);
        }

        Debug.Log($"✅ 关卡生成完毕：共生成 {spawnPoints.Count} 个障碍物点。");
    }

    // --- 对象池逻辑 (保持不变) ---

    GameObject GetFromPool(GameObject prefab, Vector3 pos, bool moveRight)
    {
        GameObject obj = null;
        List<GameObject> pool = pools[prefab];

        if (pool != null && pool.Count > 0)
        {
            obj = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            obj.transform.position = pos;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, obstacleParent);
            obj.transform.position = pos;
        }

        Obstacle obs = obj.GetComponent<Obstacle>();
        if (obs != null)
        {
            obs.Init(pos, moveRight, player, (o) => ReturnToPool(o, prefab));
        }

        return obj;
    }

    void ReturnToPool(Obstacle obs, GameObject prefabType)
    {
        if (obs == null) return;
        GameObject obj = obs.gameObject;

        obj.SetActive(false);
        obj.transform.SetParent(obstacleParent);

        if (!pools.ContainsKey(prefabType))
            pools[prefabType] = new List<GameObject>();

        pools[prefabType].Add(obj);
    }

    // --- 胜利检测 ---

    void Update()
    {
        if (!GameManager.Instance || !GameManager.Instance.IsGameActive()) return;
        if (player == null) return;

        if (player.position.y >= seaSurfaceY)
        {
            GameManager.Instance.TriggerGameWin();
        }
    }

    // --- 编辑器可视化 ---

    void OnDrawGizmos()
    {
        if (!showGizmos || spawnPoints == null) return;

        float camX = Application.isPlaying ? Camera.main.transform.position.x : transform.position.x;

        foreach (var point in spawnPoints)
        {
            Vector3 pos = new Vector3(camX + point.xOffset, point.yPos, 0);

            // 绘制位置点
            Gizmos.color = point.type == ObstacleSpawnPoint.ObstacleType.Eel ? Color.yellow :
                           point.type == ObstacleSpawnPoint.ObstacleType.Shark ? Color.red : Color.white;

            Gizmos.DrawSphere(pos, 0.3f);

            // 绘制方向箭头
            Vector3 arrowEnd = pos + Vector3.right * (point.forceDirectionRight ? 1 : -1);
            // 注意：这里只是简单示意，实际运行时会随机，所以编辑器里可能不显示确切方向
            Gizmos.DrawLine(pos, pos + Vector3.right * 1.5f);

            // 绘制文字
            // Unity Gizmos 不支持直接 DrawString，通常用 GUI 或者忽略
        }

        // 绘制海面线
        Gizmos.color = Color.cyan;
        Vector3 lineStart = new Vector3(camX - 20, seaSurfaceY, 0);
        Vector3 lineEnd = new Vector3(camX + 20, seaSurfaceY, 0);
        Gizmos.DrawLine(lineStart, lineEnd);
    }
}