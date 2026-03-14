using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI 引用")]
    public GameObject failPanel; // 失败界面
    public GameObject winPanel;  // 胜利界面
    public GameObject gameHUD;   // 游戏内HUD (血条、分数等)

    [Header("配置")]
    public float winWaitTime = 2.0f; // 胜利后停留时间
    //public float failWaitTime = 2.5f; // 失败后停留时间

    private bool isGameActive = true;

    void Awake()
    {
        // 单例模式检查
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 跨场景保留，防止场景切换时管理器丢失
    }

    void Start()
    {
        // 仅在第一次启动或手动调用 RestartGame 后初始化 UI
        // 注意：由于 DontDestroyOnLoad，Start 不会在每次场景加载时运行，
        // 所以必须依赖 RestartGame 来重置状态
        ResetGameUI();
    }

    /// <summary>
    /// 重置游戏内部状态和 UI，必须在重新加载场景前调用
    /// </summary>
    public void RestartGame()
    {
        ResetGameUI();
    }

    void ResetGameUI()
    {
        isGameActive = true; // 【关键修复】重新激活游戏状态，允许再次触发 GameOver
        Time.timeScale = 1.0f; // 确保时间正常流动

        if (failPanel) failPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (gameHUD) gameHUD.SetActive(true);

        Debug.Log("GameManager: Game State Reset. Ready to play.");
    }

    // 调用此方法表示游戏失败 
    public void TriggerGameOver()
    {
        if (!isGameActive)
        {
            Debug.LogWarning("GameManager: Game already over, ignoring duplicate TriggerGameOver call.");
            return; // 如果游戏已非活跃状态，直接返回，防止重复弹窗
        }

        isGameActive = false;
        Debug.Log("Game Over!");

        if (failPanel) failPanel.SetActive(true);
        if (gameHUD) gameHUD.SetActive(false);

        // 播放失败音效可以在 SealController 或 LevelManager 中触发，也可以在这里统一加一个 AudioSource 
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 0) sources[0].Play();

        // 延迟重开当前关卡 (目前被注释，需外部控制重启)
        //StartCoroutine(ReloadLevelAfterDelay(failWaitTime));
    }

    // 调用此方法表示游戏胜利 
    public void TriggerGameWin()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Debug.Log("Level Complete!");

        if (winPanel) winPanel.SetActive(true);
        if (gameHUD) gameHUD.SetActive(false);

        // 延迟进入下一关 
        StartCoroutine(LoadNextLevelAfterDelay(winWaitTime));
    }

    //IEnumerator ReloadLevelAfterDelay(float delay) 
    //{
    //    yield return new WaitForSeconds(delay);
    //    int currentScene = SceneManager.GetActiveScene().buildIndex;
    //    SceneManager.LoadScene(currentScene);
    //}

    IEnumerator LoadNextLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (failPanel) failPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;

        // 检查是否有下一关 
        if (nextScene >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("恭喜通关所有关卡！返回第一关或显示总通关界面。");
            nextScene = 0; // 循环回第一关，或者你可以加载一个 "EndGame" 场景
        }

        // 注意：加载下一关前，如果是循环回第一关，建议在加载前调用 RestartGame()
        // 但 LoadScene 会触发新场景的 Start，如果新场景是全新的，Start 会处理重置。
        // 如果是同一个场景重载，则必须由外部调用 RestartGame。
        SceneManager.LoadScene(nextScene);
    }

    // 供外部查询游戏是否结束 
    public bool IsGameActive() => isGameActive;
}