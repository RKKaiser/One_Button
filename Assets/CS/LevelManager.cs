using UnityEngine;
using System.Collections.Generic;
public class LevelManager : MonoBehaviour
{
    [Header("障碍物预制体")]
    public GameObject eelPrefab;
    public GameObject sharkPrefab;
    [Header("追逐者预制体")] 
    public GameObject chaserPrefab;

    [System.Serializable]
    public class ObstacleSpawnPoint
    {
        public float yPos; // 生成高度

        public enum ObstacleType { Eel, Shark, Random } // [移除] Chaser 类型已不在障碍物列表中
        public ObstacleType type; // 指定类型

        public bool forceDirectionRight = false; // 是否强制向右？(false=随机或向左)
        public float xOffset = 0f; // 相对于摄像机中心的额外X偏移

        // [新增] 为障碍物添加参数覆盖设置
        [Header("障碍物参数 (勾选启用覆盖)")]
        public ObstacleParameters parametersToOverride = new ObstacleParameters();
    }

    // [新增] 专门用于存放障碍物参数的结构体
    [System.Serializable]
    public struct ObstacleParameters // 使用 struct 也是一个常见实践
    {
        public bool enabled; // 总开关
        
        [Header("--- 覆盖参数 ---")]
        public float speed;
        public float activationDistance;
        public float waitTime;

        // 构造函数设置默认值
        public ObstacleParameters(bool isEnabled, float spd, float actDist, float wTime)
        {
            enabled = isEnabled;
            speed = spd;
            activationDistance = actDist;
            waitTime = wTime;
        }
    }

    [Header("关卡配置 (在此处设计障碍物)")]
    public List<ObstacleSpawnPoint> spawnPoints = new List<ObstacleSpawnPoint>();

    [Header("追逐者配置 (在此处设计追逐者)")]
    public List<ChaserSpawnConfig> chaserSpawnConfigs = new List<ChaserSpawnConfig>();

    [System.Serializable]
    public class ChaserSpawnConfig
    {
        [Tooltip("追逐者生成时与玩家的初始Y轴距离。正值表示在下方，负值表示在上方。")]
        public float initialYDistanceFromPlayer = 15f;

        [Header("参数覆盖 (勾选以启用)")]
        public ChaserParameters parametersToOverride = new ChaserParameters(); // 将所有参数封装在一个子对象中
    }

    // [新增] 专门用于存放追逐者参数的结构体
    [System.Serializable]
    public struct ChaserParameters // 使用 struct 也是一个常见实践
    {
        public bool enabled; // 总开关
        
        [Header("--- 覆盖参数 ---")]
        public float minChaseSpeed;
        public float maxChaseSpeed;
        public float minSpeedDistance;
        public float maxSpeedDistance;

        // 构造函数设置默认值
        public ChaserParameters(bool isEnabled, float minSpd, float maxSpd, float minDist, float maxDist)
        {
            enabled = isEnabled;
            minChaseSpeed = minSpd;
            maxChaseSpeed = maxSpd;
            minSpeedDistance = minDist;
            maxSpeedDistance = maxDist;
        }
    }

    [Header("通用设置")]
    public float baseSpawnXRange = 8f;
    public float seaSurfaceY = 20f;
    public bool showGizmos = true;

    [Header("内部池子")]
    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    private Transform player;
    private Camera mainCamera;
    public Transform obstacleParent;

    void Start()
    {
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

        if (eelPrefab != null) pools[eelPrefab] = new List<GameObject>();
        if (sharkPrefab != null) pools[sharkPrefab] = new List<GameObject>();
        if (chaserPrefab != null) pools[chaserPrefab] = new List<GameObject>();

        SpawnObstacles();
        SpawnChasers();
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
                // [修改] 随机列表中不再包含追逐者
                int randomNum = Random.Range(0, 2); // 0, 1
                if(randomNum == 0) prefabToSpawn = eelPrefab;
                else prefabToSpawn = sharkPrefab;
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
                Debug.LogWarning($"跳过障碍物生成点 Y={point.yPos}，因为对应的预制体未分配。");
                continue;
            }

            // 2. 确定方向
            bool moveRight = point.forceDirectionRight || Random.value > 0.5f;

            // 3. 计算最终位置
            // 基础随机范围 + 用户自定义的偏移
            float randomOffset = Random.Range(-baseSpawnXRange, baseSpawnXRange);
            float finalX = camX + randomOffset + point.xOffset;
            Vector3 spawnPos = new Vector3(finalX, point.yPos, 0);

            // 4. 从池子获取并生成，传递配置参数
            GetFromPool(prefabToSpawn, spawnPos, moveRight, point.parametersToOverride); // [修改] 传递参数配置
        }
        Debug.Log($"✅ 障碍物生成完毕：共生成 {spawnPoints.Count} 个障碍物点。");
    }

    // [新增] 专门用于生成追逐者的方法
    void SpawnChasers()
    {
        if (chaserPrefab == null)
        {
            Debug.LogWarning("⚠️ Chaser Prefab 未分配，无法生成追逐者。");
            return;
        }
        if (chaserSpawnConfigs.Count == 0)
        {
            Debug.LogWarning("⚠️ Chaser Spawn Configs 列表为空，没有生成任何追逐者。请在 Inspector 中添加配置。");
            return;
        }

        foreach (var config in chaserSpawnConfigs)
        {
            // [修改] 计算生成位置：玩家当前位置 + 指定的 Y 轴距离
            float spawnY = player.position.y - config.initialYDistanceFromPlayer; // 减去距离，表示在下方
            Vector3 spawnPos = new Vector3(mainCamera.transform.position.x, spawnY, 0); // X 轴固定为相机中心

            // [修改] 从池子获取并生成，传递配置参数
            GetFromPoolForChaser(spawnPos, config); // [修正] 调用新方法
        }
        Debug.Log($"✅ 追逐者生成完毕：共生成 {chaserSpawnConfigs.Count} 个追逐者。");
    }

    // [新增] 专门为 Chaser 获取对象池实例的方法
    GameObject GetFromPoolForChaser(Vector3 pos, ChaserSpawnConfig config)
    {
        GameObject obj = null;
        List<GameObject> pool = pools[chaserPrefab]; // 直接指定 Chaser 池

        if (pool != null && pool.Count > 0)
        {
            obj = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            obj.transform.position = pos;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(chaserPrefab, obstacleParent);
            obj.transform.position = pos;
        }

        // 为 Chaser 脚本初始化，并应用参数覆盖
        Chaser chaserScript = obj.GetComponent<Chaser>();
        if (chaserScript != null)
        {
            // 应用参数覆盖
            var p = config.parametersToOverride;
            if (p.enabled)
            {
                chaserScript.minChaseSpeed = p.minChaseSpeed;
                chaserScript.maxChaseSpeed = p.maxChaseSpeed;
                chaserScript.minSpeedDistance = p.minSpeedDistance;
                chaserScript.maxSpeedDistance = p.maxSpeedDistance;
            }
            
            chaserScript.Init(player, (c) => ReturnToPool(c, chaserPrefab));
        }
        return obj;
    }


    // --- 对象池逻辑 (针对 Obstacle) ---
    // [修改] 添加参数覆盖参数
    GameObject GetFromPool(GameObject prefab, Vector3 pos, bool moveRight, ObstacleParameters paramConfig)
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

        // 为 Obstacle 脚本初始化的方法
        Obstacle obs = obj.GetComponent<Obstacle>();
        if (obs != null)
        {
            // [新增] 应用参数覆盖
            if (paramConfig.enabled)
            {
                obs.moveSpeed = paramConfig.speed;
                obs.activationDistance = paramConfig.activationDistance;
                obs.waitTime = paramConfig.waitTime;
            }
            obs.Init(pos, moveRight, player, (o) => ReturnToPool(o, prefab));
        }
        return obj;
    }

    // [修改] 回收方法也需要能处理 Chaser 类型
    void ReturnToPool(object obj, GameObject prefabType)
    {
        // 使用 object 类型接收，然后根据预制体类型进行转换和处理
        GameObject go = null;
        if (obj is Obstacle)
        {
            go = ((Obstacle)obj).gameObject;
        }
        else if (obj is Chaser)
        {
            go = ((Chaser)obj).gameObject;
        }
        else
        {
            Debug.LogError("未知的对象类型尝试返回池子！");
            return;
        }

        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(obstacleParent);

        if (!pools.ContainsKey(prefabType))
            pools[prefabType] = new List<GameObject>();
        pools[prefabType].Add(go);
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

        // 绘制障碍物生成点
        foreach (var point in spawnPoints)
        {
            Vector3 pos = new Vector3(camX + point.xOffset, point.yPos, 0);

            // 绘制位置点
            Gizmos.color = point.type == ObstacleSpawnPoint.ObstacleType.Eel ? Color.yellow :
                           point.type == ObstacleSpawnPoint.ObstacleType.Shark ? Color.red : Color.white; // Random
            Gizmos.DrawSphere(pos, 0.3f);

            // 绘制方向箭头
            Vector3 arrowEnd = pos + Vector3.right * (point.forceDirectionRight ? 1 : -1);
            Gizmos.DrawLine(pos, pos + Vector3.right * 1.5f);
        }

        // [新增] 绘制追逐者生成点
        if (chaserSpawnConfigs.Count > 0 && player != null)
        {
            Gizmos.color = Color.magenta; // 为追逐者设置颜色
            foreach (var config in chaserSpawnConfigs)
            {
                float spawnY = player.position.y - config.initialYDistanceFromPlayer;
                Vector3 pos = new Vector3(camX, spawnY, 0); // X 固定为相机中心
                Gizmos.DrawWireCube(pos, new Vector3(0.5f, 0.5f, 0.5f)); // 用方块表示，区别于圆形障碍物
            }
        }

        // 绘制海面线
        Gizmos.color = Color.cyan;
        Vector3 lineStart = new Vector3(camX - 20, seaSurfaceY, 0);
        Vector3 lineEnd = new Vector3(camX + 20, seaSurfaceY, 0);
        Gizmos.DrawLine(lineStart, lineEnd);
    }
}