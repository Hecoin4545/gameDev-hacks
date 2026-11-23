using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Level Buttons")]
    public Button[] levelButtons;

    [Header("Toast / Notification")]
    public GameObject toastPanel;
    public TMP_Text toastText;

    private Coroutine toastCoroutine;
    private bool isLoadingScene = false;

    void Start()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNum = i + 1;
            if (levelButtons[i] != null)
            {
                levelButtons[i].onClick.AddListener(() => OnLevelClicked(levelNum));
            }
        }

        if (toastPanel != null)
            toastPanel.SetActive(false);
    }

    public void OnLevelClicked(int levelNumber)
    {
        if (isLoadingScene) return;

        if (levelNumber == 1)
        {
            AudioManager.Instance?.PlayLevel1Click();
            StartCoroutine(LoadSceneDelayed("Level1", 0.16f, 0));
        }
        else if (levelNumber == 2)
        {
            AudioManager.Instance?.PlayLevel2Click();
            StartCoroutine(LoadSceneDelayed("Level2", 0.20f, 1));
        }
        else if (levelNumber == 3)
        {
            AudioManager.Instance?.PlayLevel3Click();
            StartCoroutine(LoadSceneDelayed("Level3", 0.20f, 2));
        }
        else if (levelNumber == 4)
        {
            AudioManager.Instance?.PlayLevel3Click();
            StartCoroutine(LoadSceneDelayed("Level4", 0.20f, 3));
        }
        else
        {
            AudioManager.Instance?.PlayLevelLocked();
            ShowToast(string.Format("Level {0} is locked!\nComplete Level 4 to unlock.", levelNumber));
        }
    }

    private IEnumerator LoadSceneDelayed(string sceneName, float delay, int levelIndex)
    {
        isLoadingScene = true;
        yield return new WaitForSecondsRealtime(delay);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentLevelIndex = levelIndex;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null) return;
        toastText.text = message;
        toastPanel.SetActive(true);

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        var cg = toastPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = toastPanel.AddComponent<CanvasGroup>();

        // Fade in
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / 0.2f);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(2.0f);

        // Fade out
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(1f - (t / 0.3f));
            yield return null;
        }
        cg.alpha = 0f;
        toastPanel.SetActive(false);
    }
}
