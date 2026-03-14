using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSetting : MonoBehaviour
{
    public GameObject failPanel;

    // 退出游戏
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 重新开始游戏
    public void RestartGame()
    {
        Debug.Log("Restart Button Clicked!");

        // 重置 GameManager 的状态
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
            Debug.Log("GameManager state reset successfully.");
        }
        else
        {
            Debug.LogError("WARNING: GameManager Instance is NULL! Cannot reset game state.");
        }

        // 隐藏失败面板
        if (failPanel != null)
        {
            failPanel.SetActive(false);
        }

        // 重新加载当前场景
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene);

        Debug.Log($"Loading scene index: {currentScene}");
    }
}