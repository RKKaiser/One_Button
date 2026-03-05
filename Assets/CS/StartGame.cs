using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    private bool hasTriggered = false;

    void Update()
    {
        // 检测任意按键（键盘、手柄）按下，但排除鼠标按钮
        if (Input.anyKeyDown
            && !Input.GetMouseButtonDown(0)
            && !Input.GetMouseButtonDown(1)
            && !Input.GetMouseButtonDown(2)
            && !hasTriggered)
        {
            hasTriggered = true;
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        // 获取当前场景的索引
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 计算下一个场景索引（如果当前是最后一个场景，则循环到第一个，或退出）
        int nextSceneIndex = currentSceneIndex + 1;

        // 如果下一个索引超出总场景数，可以回到第一个（或做其他处理）
        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            // 例如：回到第一个场景
            nextSceneIndex = 0;
            // 或者直接退出游戏
            // Application.Quit();
            // return;
        }

        // 加载场景
        SceneManager.LoadScene(nextSceneIndex);
    }
}