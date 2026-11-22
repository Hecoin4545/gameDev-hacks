using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelData data;
    public GridManager gridManager;
    public UIManager uiManager;
    public PlayerController player;
    public Transform startPlatform;
    public Transform endPlatform;
    public float restartDelay = 1.2f;

    private int progress = 0;
    private float timer;
    private float mapCountdownTimer;
    private bool mapCountdownActive = false;
    private bool running = false;
    private bool completed = false;
    private Tile currentTile;

    private static Material fxMaterial;

    void Awake() { Instance = this; }

    void Start()
    {
        if (data == null) { Debug.LogError("LevelManager: no LevelData"); return; }
        timer = data.timeLimit;

        if (data.hideMapAfterCountdown)
        {
            mapCountdownTimer = data.mapVisibleDuration;
            mapCountdownActive = true;
        }

        if (gridManager != null) gridManager.Build(data);
        if (uiManager != null)
        {
            uiManager.BuildMap(data);
            uiManager.SetLevelInfo(data);
            uiManager.UpdateProgress(0, data.sequence.Count);
            uiManager.UpdateTimer(timer);
            uiManager.ClearPlayerTile();
            if (data.hideMapAfterCountdown)
            {
                uiManager.UpdateMapCountdown(mapCountdownTimer);
            }
        }
        running = true;
        if (player != null && startPlatform != null)
        {
            player.transform.position = startPlatform.position + Vector3.up * 1.5f;
        }
    }

    void Update()
    {
        if (!running || completed || data == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            Fail("Time's up!");
            return;
        }

        if (mapCountdownActive)
        {
            mapCountdownTimer -= Time.deltaTime;
            if (mapCountdownTimer <= 0f)
            {
                mapCountdownTimer = 0f;
                mapCountdownActive = false;
                if (uiManager != null) uiManager.ConcealMap();
            }
            else
            {
                if (uiManager != null) uiManager.UpdateMapCountdown(mapCountdownTimer);
            }
        }

        if (uiManager != null)
        {
            uiManager.UpdateTimer(timer);
            uiManager.UpdateProgress(progress, data.sequence.Count);
        }
    }

    public void OnTileLanded(Tile tile)
    {
        if (!running || completed || data == null) return;
        if (tile == currentTile) return;
        currentTile = tile;

        bool ok = false;
        if (tile.isSafe && progress < data.sequence.Count)
        {
            var expected = data.sequence[progress];
            if (tile.row == expected.x && tile.column == expected.y) ok = true;
        }

        if (ok)
        {
            progress++;
            tile.PlaySuccessFeedback();
            AudioManager.Instance?.PlayRightJump();
            SpawnParticles(tile.transform.position + Vector3.up * 0.22f, new Color(0.92f, 0.98f, 1f), 14, 0.55f);
            if (uiManager != null)
            {
                uiManager.UpdateProgress(progress, data.sequence.Count);
                uiManager.SetPlayerTile(tile.row, tile.column);
                uiManager.MarkCompleted(tile.row, tile.column);
            }
        }
        else
        {
            tile.FlashDanger();
            AudioManager.Instance?.PlayWrongJump();
            Fail("Wrong tile!");
        }
    }

    public void OnTileExited(Tile tile)
    {
        if (currentTile == tile) currentTile = null;
    }

    public void Fail(string reason)
    {
        if (completed) return;
        running = false;
        mapCountdownActive = false;
        var visual = player != null ? player.GetComponent<PlayerVisual>() : null;
        if (visual != null) visual.PlayDeath();
        SpawnParticles(player != null ? player.transform.position + Vector3.up * 1.2f : Vector3.zero,
            new Color(1f, 0.22f, 0.18f), 48, 0.9f);
        if (uiManager != null) uiManager.ShowFail();
        StartCoroutine(RestartRoutine());
    }

    public void OnReachEnd()
    {
        if (completed || data == null) return;
        if (progress < data.sequence.Count) return;
        completed = true;
        running = false;
        mapCountdownActive = false;
        AudioManager.Instance?.PlayLevelComplete();
        if (endPlatform != null)
            SpawnParticles(endPlatform.position + Vector3.up * 1.1f, new Color(0.35f, 1f, 0.55f), 60, 1f);
        if (uiManager != null) uiManager.ShowLevelComplete(timer);
    }

    private void SpawnParticles(Vector3 pos, Color color, int count, float lifetime)
    {
        var go = new GameObject("FX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor = color;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startLifetime = lifetime;
        main.maxParticles = count + 10;
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetFxMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        ps.Play();
        Destroy(go, lifetime + 1f);
    }

    private static Material GetFxMaterial()
    {
        if (fxMaterial != null) return fxMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        fxMaterial = new Material(shader);
        fxMaterial.color = Color.white;
        return fxMaterial;
    }

    IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartLevel()
    {
        AudioManager.Instance?.PlayButtonClick();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextLevel();
        }
        else
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "Level1") SceneManager.LoadScene("Level2");
            else SceneManager.LoadScene("MainMenu");
        }
    }

    public void LoadMainMenu()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
