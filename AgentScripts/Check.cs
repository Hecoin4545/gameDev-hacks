using UnityEditor;
using UnityEngine;

public class Check
{
    public static void Run()
    {
        Debug.Log("isPlaying=" + EditorApplication.isPlaying + " paused=" + EditorApplication.isPaused
            + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public static void Stop()
    {
        Debug.Log("Forcing exit play mode. wasPlaying=" + EditorApplication.isPlaying);
        EditorApplication.ExitPlaymode();
    }
}
