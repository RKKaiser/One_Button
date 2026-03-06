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
        public float yPos;
        public enum ObstacleType { Eel, Shark, Random }
        public ObstacleType type;
        public bool forceDirectionRight = false;
        public float xOffset = 0f;
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
            GameObject prefabToSpawn = null;
            if (point.type == ObstacleSpawnPoint.ObstacleType.Random)
            {
                int randomNum = Random.Range(0, 2);
                if (randomNum == 0) prefabToSpawn = eelPrefab;
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

            bool moveRight = point.forceDirectionRight || Random.value > 0.5f;

            float randomOffset = Random.Range(-baseSpawnXRange, baseSpawnXRange);
            float finalX = camX + randomOffset + point.xOffset;
            Vector3 spawnPos = new Vector3(finalX, point.yPos, 0);

            GetFromPool(prefabToSpawn, spawnPos, moveRight);
        }
        Debug.Log($"✅ 障碍物生成完毕：共生成 {spawnPoints.Count} 个障碍物点。");
    }

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
            float spawnY = player.position.y - config.initialYDistanceFromPlayer;
            Vector3 spawnPos = new Vector3(mainCamera.transform.position.x, spawnY, 0);

            GetFromPoolForChaser(spawnPos, config);
        }
        Debug.Log($"✅ 追逐者生成完毕：共生成 {chaserSpawnConfigs.Count} 个追逐者。");
    }

    GameObject GetFromPoolForChaser(Vector3 pos, ChaserSpawnConfig config)
    {
        GameObject obj = null;
        List<GameObject> pool = pools[chaserPrefab];

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

    void ReturnToPool(object obj, GameObject prefabType)
    {
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

    void Update()
    {
        if (!GameManager.Instance || !GameManager.Instance.IsGameActive()) return;
        if (player == null) return;

        if (player.position.y >= seaSurfaceY)
        {
            GameManager.Instance.TriggerGameWin();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || spawnPoints == null) return;
        float camX = Application.isPlaying ? Camera.main.transform.position.x : transform.position.x;

        foreach (var point in spawnPoints)
        {
            Vector3 pos = new Vector3(camX + point.xOffset, point.yPos, 0);
            Gizmos.color = point.type == ObstacleSpawnPoint.ObstacleType.Eel ? Color.yellow :
                           point.type == ObstacleSpawnPoint.ObstacleType.Shark ? Color.red : Color.white;
            Gizmos.DrawSphere(pos, 0.3f);
            Vector3 arrowEnd = pos + Vector3.right * (point.forceDirectionRight ? 1 : -1);
            Gizmos.DrawLine(pos, pos + Vector3.right * 1.5f);
        }

        if (chaserSpawnConfigs.Count > 0 && player != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var config in chaserSpawnConfigs)
            {
                float spawnY = player.position.y - config.initialYDistanceFromPlayer;
                Vector3 pos = new Vector3(camX, spawnY, 0);
                Gizmos.DrawWireCube(pos, new Vector3(0.5f, 0.5f, 0.5f));
            }
        }

        Gizmos.color = Color.cyan;
        Vector3 lineStart = new Vector3(camX - 20, seaSurfaceY, 0);
        Vector3 lineEnd = new Vector3(camX + 20, seaSurfaceY, 0);
        Gizmos.DrawLine(lineStart, lineEnd);
    }
}