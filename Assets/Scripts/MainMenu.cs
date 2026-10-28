using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string gameSceneName = "MainScene";   // your game scene's exact name

    void Start()
    {
        Time.timeScale = 1f;   // in case you arrive here from a frozen Game Over
    }

    public void StartGame()
    {
        SceneFader.Load(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}