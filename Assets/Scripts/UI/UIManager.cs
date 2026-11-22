using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text levelTitle;
    public TMP_Text subtitle;
    public TMP_Text objectiveText;
    public TMP_Text instructionsText;
    public TMP_Text progressText;
    public TMP_Text timerText;
    public TMP_Text mapPlayerLabel;
    public TMP_Text mapStatusText;
    public TMP_Text mapHintText;
    public Image progressFill;
    public Transform mapContainer;
    public Image playerMarker;
    public Sprite roundedSprite;
    public Sprite dotSprite;
    public TMP_FontAsset font;

    public GameObject levelCompletePanel;
    public TMP_Text levelCompleteTime;
    public Button restartButton;
    public Button nextLevelButton;
    public Button menuButton;
    public Button completeMenuButton;
    public GameObject failPanel;

    private readonly Dictionary<int, RectTransform> cellRects = new Dictionary<int, RectTransform>();
    private readonly Dictionary<int, Image> cellImages = new Dictionary<int, Image>();
    private readonly HashSet<int> safeCellKeys = new HashSet<int>();
    private readonly HashSet<int> completedCellKeys = new HashSet<int>();

    private int mapRows;
    private int mapCols;
    private float markerPulse;
    private bool mapConcealed = false;

    private static readonly Color SafeColor = new Color(0.24f, 0.90f, 0.44f);
    private static readonly Color UnsafeColor = new Color(0.155f, 0.165f, 0.185f);
    private static readonly Color CompletedColor = new Color(0.52f, 1f, 0.66f);
    private static readonly Color ConcealedColor = new Color(0.13f, 0.14f, 0.16f);

    void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => LevelManager.Instance?.RestartLevel());
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() => LevelManager.Instance?.LoadNextLevel());
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() => LevelManager.Instance?.LoadMainMenu());
        }

        if (completeMenuButton != null)
        {
            completeMenuButton.onClick.RemoveAllListeners();
            completeMenuButton.onClick.AddListener(() => LevelManager.Instance?.LoadMainMenu());
        }
    }

    void Update()
    {
        if (playerMarker != null && playerMarker.gameObject.activeSelf)
        {
            markerPulse += Time.deltaTime;
            float s = 1f + Mathf.Sin(markerPulse * 5f) * 0.14f;
            playerMarker.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }

    public void SetLevelInfo(LevelData data)
    {
        if (levelTitle != null)
            levelTitle.text = !string.IsNullOrEmpty(data.levelName) ? data.levelName.ToUpper() : "LEVEL 01";

        if (subtitle != null)
            subtitle.text = data.hideMapAfterCountdown ? "MEMORY CHALLENGE" : "PATH FINDER";

        if (objectiveText != null) objectiveText.text = data.objective;
        if (instructionsText != null) instructionsText.text = data.instructions;
        if (mapPlayerLabel != null) mapPlayerLabel.text = "You are here";

        if (data.hideMapAfterCountdown)
        {
            if (mapHintText != null)
                mapHintText.text = "Memorize before the path conceals!";
            UpdateMapCountdown(data.mapVisibleDuration);
        }
        else
        {
            if (mapStatusText != null)
                mapStatusText.text = "LIVE PATH GUIDANCE";
            if (mapHintText != null)
                mapHintText.text = "The map mirrors what you see in the course.";
        }
    }

    public void BuildMap(LevelData data)
    {
        if (mapContainer == null) return;
        foreach (Transform c in mapContainer)
        {
            if (playerMarker != null && c == playerMarker.transform) continue;
            Destroy(c.gameObject);
        }
        cellRects.Clear();
        cellImages.Clear();
        safeCellKeys.Clear();
        completedCellKeys.Clear();
        mapConcealed = false;

        mapRows = data.rows;
        mapCols = data.columns;

        foreach (var safe in data.safeTiles)
        {
            safeCellKeys.Add(safe.x * 100 + safe.y);
        }

        var containerRect = mapContainer as RectTransform;
        float containerW = containerRect != null ? containerRect.sizeDelta.x : 312f;
        float containerH = containerRect != null ? containerRect.sizeDelta.y : 400f;
        float spacing = 8f;
        float cellW = Mathf.Floor(Mathf.Min(58f, (containerW - 64f - (mapCols - 1) * spacing) / mapCols));
        float cellH = Mathf.Floor(Mathf.Min(58f, (containerH - 64f - (mapRows - 1) * spacing) / mapRows));
        float cell = Mathf.Max(Mathf.Min(cellW, cellH), 28f);

        float totalW = mapCols * cell + (mapCols - 1) * spacing;
        float totalH = mapRows * cell + (mapRows - 1) * spacing;

        for (int dr = 0; dr < mapRows; dr++)
        {
            // Mirror the course: the row nearest the player (row 1) sits at the bottom of the map.
            int row = mapRows - dr;
            for (int c = 0; c < mapCols; c++)
            {
                int col = c + 1;
                float x = -totalW * 0.5f + cell * 0.5f + c * (cell + spacing) + 8f;
                float y = totalH * 0.5f - cell * 0.5f - dr * (cell + spacing) - 10f;

                var go = new GameObject(string.Format("MapCell_R{0}_C{1}", row, col), typeof(RectTransform), typeof(Image));
                go.transform.SetParent(mapContainer, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cell - 2f, cell - 2f);
                rt.anchoredPosition = new Vector2(x, y);

                var img = go.GetComponent<Image>();
                int key = row * 100 + col;
                bool safe = safeCellKeys.Contains(key);
                img.color = safe ? SafeColor : UnsafeColor;
                img.raycastTarget = false;
                if (roundedSprite != null)
                {
                    img.sprite = roundedSprite;
                    img.type = Image.Type.Sliced;
                }

                cellRects[key] = rt;
                cellImages[key] = img;

                // Column labels above the first displayed row.
                if (dr == 0 && font != null)
                {
                    var label = CreateLabel(mapContainer, "Col" + col, col.ToString(),
                        new Vector2(x, totalH * 0.5f + 14f), new Vector2(cell, 20f));
                    label.color = new Color(0.62f, 0.66f, 0.70f, 1f);
                    label.fontSize = Mathf.Clamp(cell * 0.38f, 11f, 15f);
                }
            }

            if (font != null)
            {
                float y = totalH * 0.5f - cell * 0.5f - dr * (cell + spacing) - 10f;
                var rowLabel = CreateLabel(mapContainer, "Row" + row, row.ToString(),
                    new Vector2(-totalW * 0.5f - 16f, y), new Vector2(26f, 20f));
                rowLabel.color = new Color(0.62f, 0.66f, 0.70f, 1f);
                rowLabel.fontSize = Mathf.Clamp(cell * 0.38f, 11f, 15f);
            }
        }

        if (playerMarker != null)
        {
            playerMarker.gameObject.SetActive(false);
            playerMarker.transform.SetAsLastSibling();
        }
    }

    public void UpdateMapCountdown(float timeLeft)
    {
        if (mapStatusText != null)
        {
            if (timeLeft > 0.05f)
            {
                mapStatusText.text = string.Format("MEMORIZE PATH: <color=#FFDD44>{0:0.0}s</color>", timeLeft);
                mapStatusText.color = timeLeft <= 2.0f ? new Color(1f, 0.40f, 0.35f) : new Color(0.35f, 0.95f, 0.55f);
            }
            else
            {
                mapStatusText.text = "PATH CONCEALED - WALK FROM MEMORY!";
                mapStatusText.color = new Color(1f, 0.78f, 0.28f);
            }
        }
    }

    public void ConcealMap()
    {
        mapConcealed = true;
        foreach (var kvp in cellImages)
        {
            if (!completedCellKeys.Contains(kvp.Key))
            {
                kvp.Value.color = ConcealedColor;
            }
        }

        if (mapStatusText != null)
        {
            mapStatusText.text = "PATH CONCEALED - WALK FROM MEMORY!";
            mapStatusText.color = new Color(1f, 0.78f, 0.28f);
        }

        if (mapHintText != null)
        {
            mapHintText.text = "Memory test: recall the safe green path!";
        }
    }

    public void SetPlayerTile(int row, int col)
    {
        if (playerMarker == null) return;
        int key = row * 100 + col;
        RectTransform cell;
        if (cellRects.TryGetValue(key, out cell))
        {
            playerMarker.rectTransform.anchoredPosition = cell.anchoredPosition;
            playerMarker.gameObject.SetActive(true);
            markerPulse = 0f;
        }
        else
        {
            playerMarker.gameObject.SetActive(false);
        }
    }

    public void ClearPlayerTile()
    {
        if (playerMarker != null) playerMarker.gameObject.SetActive(false);
    }

    public void MarkCompleted(int row, int col)
    {
        int key = row * 100 + col;
        completedCellKeys.Add(key);
        Image img;
        if (cellImages.TryGetValue(key, out img))
        {
            img.color = CompletedColor;
        }
    }

    public void UpdateTimer(float t)
    {
        if (timerText != null)
        {
            timerText.text = string.Format("{0:0.0}s", Mathf.Max(0f, t));
            timerText.color = t <= 15f ? new Color(1f, 0.34f, 0.32f) : Color.white;
        }
    }

    public void UpdateProgress(int done, int total)
    {
        if (progressText != null)
            progressText.text = string.Format("{0} <size=24><color=#94A3B8>/ {1}</color></size>", done, total);
        if (progressFill != null)
            progressFill.fillAmount = total > 0 ? Mathf.Clamp01((float)done / total) : 0f;
    }

    public void ShowLevelComplete(float timeLeft)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            StartCoroutine(FadeIn(levelCompletePanel, 0.35f));
        }
        if (levelCompleteTime != null)
            levelCompleteTime.text = string.Format("Cleared with <color=#FFE600>{0:0.0}s</color> remaining", Mathf.Max(0f, timeLeft));
    }

    public void ShowFail()
    {
        if (failPanel == null) return;
        failPanel.SetActive(true);
        StartCoroutine(PopIn(failPanel, 0.22f));
    }

    private IEnumerator FadeIn(GameObject go, float duration)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        float t = 0f;
        cg.alpha = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.SmoothStep(0f, 1f, t / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator PopIn(GameObject go, float duration)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        var t0 = go.transform;
        float t = 0f;
        cg.alpha = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            cg.alpha = k;
            t0.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, k);
            yield return null;
        }
        cg.alpha = 1f;
        t0.localScale = Vector3.one;
    }

    private TMP_Text CreateLabel(Transform parent, string name, string content, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.font = font;
        t.fontSize = 14f;
        t.color = new Color(0.62f, 0.66f, 0.70f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
