using UnityEngine;
using UnityEngine.SceneManagement;

// Cambiar de escena desde cualquier script.
public static class SceneLoader
{
    public static void Load(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public static void ReloadCurrent()
    {
        Load(SceneManager.GetActiveScene().name);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
