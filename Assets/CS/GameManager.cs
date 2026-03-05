using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI 引用")]
    public GameObject failPanel;      // 失败界面
    public GameObject winPanel;       // 胜利界面
    public GameObject gameHUD;        // 游戏内HUD (血条、分数等)

    [Header("配置")]
    public float winWaitTime = 2.0f;  // 胜利后停留时间
    public float failWaitTime = 2.5f; // 失败后停留时间

    private bool isGameActive = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 跨场景保留
    }

    void Start()
    {
        ResetGameUI();
    }

    void ResetGameUI()
    {
        isGameActive = true;
        Time.timeScale = 1.0f; // 确保时间正常流动

        if (failPanel) failPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (gameHUD) gameHUD.SetActive(true);
    }

    // 调用此方法表示游戏失败
    public void TriggerGameOver()
    {
        if (!isGameActive) return;
        isGameActive = false;

        Debug.Log("Game Over!");

        if (failPanel) failPanel.SetActive(true);
        if (gameHUD) gameHUD.SetActive(false);

        // 播放失败音效可以在 SealController 或 LevelManager 中触发，也可以在这里统一加一个 AudioSource
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 0) sources[0].Play();

        // 延迟重开当前关卡
        StartCoroutine(ReloadLevelAfterDelay(failWaitTime));
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

    IEnumerator ReloadLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene);
    }

    IEnumerator LoadNextLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;

        // 检查是否有下一关
        if (nextScene >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("恭喜通关所有关卡！返回第一关或显示总通关界面。");
            nextScene = 0; // 循环回第一关，或者你可以加载一个 "EndGame" 场景
        }

        SceneManager.LoadScene(nextScene);
    }

    // 供外部查询游戏是否结束
    public bool IsGameActive() => isGameActive;
}