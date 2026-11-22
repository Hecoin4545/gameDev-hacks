using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public LevelData[] levels;
    public int currentLevelIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLevel(int levelNumber)
    {
        currentLevelIndex = levelNumber - 1;
        SceneManager.LoadScene("Level" + levelNumber);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadNextLevel()
    {
        int next = currentLevelIndex + 1;
        if (levels != null && next < levels.Length && levels[next] != null && !string.IsNullOrEmpty(levels[next].sceneName))
        {
            currentLevelIndex = next;
            SceneManager.LoadScene(levels[next].sceneName);
        }
        else
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "Level1")
            {
                currentLevelIndex = 1;
                SceneManager.LoadScene("Level2");
            }
            else if (currentScene == "Level2")
            {
                currentLevelIndex = 2;
                SceneManager.LoadScene("Level3");
            }
            else
            {
                currentLevelIndex = 0;
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
