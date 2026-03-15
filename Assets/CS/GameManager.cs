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
        if (SoundController.Instance != null)
        {
            SoundController.Instance.StopAllSounds();
        }

        // 恢复时间流速
        Time.timeScale = 1.0f;

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
            return;
        }

        isGameActive = false;

        // 暂停时间
        Time.timeScale = 0f;

        Debug.Log("Game Over! Time Paused.");

        if (failPanel) failPanel.SetActive(true);
        if (gameHUD) gameHUD.SetActive(false);

        if (SoundController.Instance != null)
        {
            SoundController.Instance.PlayGameSound(false); // false 表示失败
        }
    }

    // 调用此方法表示游戏胜利 
    public void TriggerGameWin()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Debug.Log("Level Complete!");

        Time.timeScale = 0f;

        if (winPanel) winPanel.SetActive(true);
        if (gameHUD) gameHUD.SetActive(false);

        if (SoundController.Instance != null)
        {
            SoundController.Instance.PlayGameSound(true); // true 表示胜利
        }

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

        // 先隐藏当前面板
        if (failPanel) failPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        // 重置游戏状态，为下一关做准备
        ResetGameUI();

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;

        if (nextScene >= SceneManager.sceneCountInBuildSettings)
        {
            nextScene = 0;
            ResetGameUI();
        }

        Debug.Log($"Loading Scene {nextScene}");
        SceneManager.LoadScene(nextScene);
    }

    // 供外部查询游戏是否结束 
    public bool IsGameActive() => isGameActive;
}