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
    public Button replayButton;
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
    private bool isUpsideDown = false;
    private bool mapConcealed = false;

    private static readonly Color SafeColor = new Color(0.18f, 0.95f, 0.50f, 1f);
    private static readonly Color SafeCyberColor = new Color(0.00f, 0.92f, 1.00f, 1f);
    private static readonly Color UnsafeColor = new Color(0.12f, 0.15f, 0.19f, 1f);
    private static readonly Color CompletedColor = new Color(0.45f, 1.00f, 0.70f, 1f);
    private static readonly Color ConcealedColor = new Color(0.10f, 0.12f, 0.15f, 1f);

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

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => LevelManager.Instance?.RestartLevel());
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
            float s = 1f + Mathf.Sin(markerPulse * 6f) * 0.18f;
            playerMarker.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }

    public void SetLevelInfo(LevelData data)
    {
        if (levelTitle != null)
            levelTitle.text = !string.IsNullOrEmpty(data.levelName) ? data.levelName.ToUpper() : "LEVEL 01";

        if (subtitle != null)
        {
            if (data.isUpsideDownPattern)
                subtitle.text = "UPSIDE-DOWN BLUEPRINT";
            else if (data.hideMapAfterCountdown)
                subtitle.text = "MEMORY CHALLENGE";
            else
                subtitle.text = "PATH FINDER";
        }

        if (objectiveText != null) objectiveText.text = data.objective;
        if (instructionsText != null) instructionsText.text = data.instructions;
        if (mapPlayerLabel != null) mapPlayerLabel.text = "You are here";

        if (data.hideMapAfterCountdown)
        {
            if (mapHintText != null)
            {
                if (data.isUpsideDownPattern)
                    mapHintText.text = "Memorize rows in reverse: Top blueprint row = 1st jump in arena!";
                else
                    mapHintText.text = "Memorize before the path conceals!";
            }
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
        isUpsideDown = data.isUpsideDownPattern;

        var patternList = (data.displayedPattern != null && data.displayedPattern.Count > 0)
            ? data.displayedPattern
            : data.safeTiles;

        foreach (var safe in patternList)
        {
            safeCellKeys.Add(safe.x * 100 + safe.y);
        }

        var containerRect = mapContainer as RectTransform;
        float containerW = containerRect != null ? containerRect.sizeDelta.x : 320f;
        float containerH = containerRect != null ? containerRect.sizeDelta.y : 420f;
        float spacing = 8f;
        float cellW = Mathf.Floor(Mathf.Min(56f, (containerW - 74f - (mapCols - 1) * spacing) / mapCols));
        float cellH = Mathf.Floor(Mathf.Min(56f, (containerH - 74f - (mapRows - 1) * spacing) / mapRows));
        float cell = Mathf.Max(Mathf.Min(cellW, cellH), 32f);

        float totalW = mapCols * cell + (mapCols - 1) * spacing;
        float totalH = mapRows * cell + (mapRows - 1) * spacing;

        Color activeSafeColor = isUpsideDown ? SafeCyberColor : SafeColor;

        for (int dr = 0; dr < mapRows; dr++)
        {
            // dr = 0 is the top visual row on map (Row = mapRows)
            int row = mapRows - dr;
            for (int c = 0; c < mapCols; c++)
            {
                int col = c + 1;
                float x = -totalW * 0.5f + cell * 0.5f + c * (cell + spacing) + 16f;
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
                img.color = safe ? activeSafeColor : UnsafeColor;
                img.raycastTarget = false;
                if (roundedSprite != null)
                {
                    img.sprite = roundedSprite;
                    img.type = Image.Type.Sliced;
                }

                cellRects[key] = rt;
                cellImages[key] = img;

                // Column labels above top row
                if (dr == 0 && font != null)
                {
                    var label = CreateLabel(mapContainer, "Col" + col, "C" + col,
                        new Vector2(x, totalH * 0.5f + 18f), new Vector2(cell, 24f));
                    label.color = new Color(0.35f, 0.85f, 1f, 1f);
                    label.fontSize = 14f;
                    label.fontStyle = FontStyles.Bold;
                }
            }

            // Row labels on left side
            if (font != null)
            {
                float y = totalH * 0.5f - cell * 0.5f - dr * (cell + spacing) - 10f;
                string rowText = "R" + row;
                if (isUpsideDown)
                {
                    // Annotate inverted order
                    int jumpStep = dr + 1;
                    if (jumpStep == 1) rowText = "R" + row + " (1st)";
                    else if (jumpStep == 2) rowText = "R" + row + " (2nd)";
                    else if (jumpStep == mapRows) rowText = "R" + row + " (Last)";
                }

                var rowLabel = CreateLabel(mapContainer, "Row" + row, rowText,
                    new Vector2(-totalW * 0.5f - 24f, y), new Vector2(62f, 22f));
                rowLabel.color = isUpsideDown ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.70f, 0.85f, 0.95f, 1f);
                rowLabel.fontSize = isUpsideDown ? 11.5f : 13f;
                rowLabel.fontStyle = FontStyles.Bold;
                rowLabel.alignment = TextAlignmentOptions.Right;
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
                string label = isUpsideDown ? "UPSIDE-DOWN BLUEPRINT" : "MEMORIZE PATH";
                mapStatusText.text = string.Format("{0}: <color=#FFE600>{1:0.0}s</color>", label, timeLeft);
                mapStatusText.color = timeLeft <= 2.5f ? new Color(1f, 0.40f, 0.35f) : new Color(0.35f, 0.95f, 0.55f);
            }
            else
            {
                mapStatusText.text = isUpsideDown
                    ? "BLUEPRINT CONCEALED - JUMP REVERSE ROWS!"
                    : "PATH CONCEALED - WALK FROM MEMORY!";
                mapStatusText.color = new Color(1f, 0.82f, 0.28f);
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
            mapStatusText.text = isUpsideDown
                ? "BLUEPRINT CONCEALED - JUMP REVERSE ROWS!"
                : "PATH CONCEALED - WALK FROM MEMORY!";
            mapStatusText.color = new Color(1f, 0.82f, 0.28f);
        }

        if (mapHintText != null)
        {
            mapHintText.text = isUpsideDown
                ? "Recall: Top blueprint row was jumped on arena row 1!"
                : "Memory test: recall the safe green path!";
        }
    }

    public void SetPlayerTile(int physRow, int physCol)
    {
        if (playerMarker == null) return;
        int displayRow = isUpsideDown ? (mapRows + 1 - physRow) : physRow;
        int key = displayRow * 100 + physCol;
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

    public void MarkCompleted(int physRow, int physCol)
    {
        int displayRow = isUpsideDown ? (mapRows + 1 - physRow) : physRow;
        int key = displayRow * 100 + physCol;
        completedCellKeys.Add(key);
        Image img;
        if (cellImages.TryGetValue(key, out img))
        {
            img.color = isUpsideDown ? SafeCyberColor : CompletedColor;
        }
    }

    public void UpdateTimer(float t)
    {
        if (timerText != null)
        {
            timerText.text = string.Format("{0:0.0}s", Mathf.Max(0f, t));
            timerText.color = t <= 15f ? new Color(1f, 0.35f, 0.32f) : Color.white;
        }
    }

    public void UpdateProgress(int done, int total)
    {
        if (progressText != null)
            progressText.text = string.Format("{0} <size=26><color=#94A3B8>/ {1}</color></size>", done, total);
        if (progressFill != null)
            progressFill.fillAmount = total > 0 ? Mathf.Clamp01((float)done / total) : 0f;
    }

    public void ShowLevelComplete(float timeLeft)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            StartCoroutine(FadeIn(levelCompletePanel, 0.32f));
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
        t.color = new Color(0.82f, 0.88f, 0.95f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
