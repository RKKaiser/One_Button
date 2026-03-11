using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSetting: MonoBehaviour
{
    public GameObject failPanel;
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 在构建的应用程序中退出
            Application.Quit();
#endif
    }
    public void RestartGame()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        if (failPanel)
        {
            failPanel.SetActive(false);
            Debug.Log("Restart Game");
        }
        SceneManager.LoadScene(currentScene);
    }

}