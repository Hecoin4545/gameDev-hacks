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
    public float restartDelay = 1.8f;

    [Header("Narrator")]
    [TextArea(2, 4)]
    public string[] levelNarratorTexts = new string[]
    {
        "Welcome, brave adventurer! The ancient path of tiles awaits you. Only the safe tiles will hold your weight — choose wisely!",
        "The desert sands are treacherous! Memorize the path in 5 seconds before the map vanishes, then navigate the dunes from memory!",
        "Welcome to the Frozen Crags! The icy tiles are slick and perilous through the snowfall. Remember the blueprint in reverse to reach the summit!"
    };

    private int progress = 0;
    private float timer;
    private float mapCountdownTimer;
    private bool mapCountdownActive = false;
    private bool running = false;
    private bool completed = false;
    private Tile currentTile;

    // Phase flags
    private bool introPhase = true;    // fly-over + narrator, player locked
    private bool playerFalling = false;

    private Vector3 startPosition;
    private CameraFollow camFollow;

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
                uiManager.UpdateMapCountdown(mapCountdownTimer);
        }

        if (player != null && startPlatform != null)
        {
            startPosition = startPlatform.position + Vector3.up * 1.5f;
            player.transform.position = startPosition;
        }
        else if (player != null)
        {
            startPosition = player.transform.position;
        }

        // Lock player input during intro
        if (player != null) player.SetInputEnabled(false);

        camFollow = FindObjectOfType<CameraFollow>();
        if (camFollow != null && gridManager != null)
            camFollow.gridRoot = gridManager.transform;

        // Determine level index for audio
        int levelIdx = 0;
        string sn = SceneManager.GetActiveScene().name;
        if (sn.Contains("2")) levelIdx = 1;
        else if (sn.Contains("3")) levelIdx = 2;
        else if (sn.Contains("4")) levelIdx = 3;

        // Start background music immediately
        AudioManager.Instance?.PlayBGMusic(levelIdx);
        AudioManager.Instance?.PlayAmbient();

        // Start the intro sequence
        StartCoroutine(IntroSequence(levelIdx));
    }

    private IEnumerator IntroSequence(int levelIdx)
    {
        // 1. Wait for fly-over to complete
        if (camFollow != null && camFollow.doFlyOver)
        {
            yield return new WaitUntil(() => camFollow.IsFlyOverDone);
        }

        // 2. Show narrator subtitle and play voice
        string text = levelIdx < levelNarratorTexts.Length ? levelNarratorTexts[levelIdx] : "";
        if (uiManager != null && !string.IsNullOrEmpty(text))
            uiManager.ShowNarrator(text);

        AudioManager.Instance?.PlayNarrator(levelIdx + 1);

        // Wait while narrator is playing (or a minimum 3s if no clip)
        float waitTime = 0f;
        while (waitTime < 3f || (AudioManager.Instance != null && AudioManager.Instance.IsNarratorPlaying()))
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (uiManager != null) uiManager.HideNarrator();

        // 3. Unlock player
        introPhase = false;
        running = true;
        if (player != null) player.SetInputEnabled(true);
    }

    void Update()
    {
        if (introPhase || !running || completed || data == null) return;
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
        if (introPhase || !running || completed || data == null) return;
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
            // Wrong tile: flash, then tile and character both fall
            tile.FlashDanger();
            AudioManager.Instance?.PlayWrongJump();
            AudioManager.Instance?.PlayFall();
            StartCoroutine(WrongTileFall(tile));
        }
    }

    private IEnumerator WrongTileFall(Tile tile)
    {
        running = false;
        // Trigger player fall visuals
        var visual = player != null ? player.GetComponent<PlayerVisual>() : null;
        if (visual != null) visual.PlayFall();
        if (player != null) player.SetInputEnabled(false);

        // Slight delay so the player sinks a bit before tile falls
        yield return new WaitForSeconds(0.35f);
        tile.FallOff();

        SpawnParticles(player != null ? player.transform.position : Vector3.zero,
            new Color(1f, 0.4f, 0.1f), 32, 0.8f);

        yield return new WaitForSeconds(0.5f);
        if (uiManager != null) uiManager.ShowFail();

        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    /// <summary>Teleport player back to the start tile (used when falling off the starting area).</summary>
    public void RespawnAtStart()
    {
        if (completed) return;
        // Don't count this as a fail, just warp back
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = startPosition;
            if (cc != null) cc.enabled = true;
        }
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
            else if (currentScene == "Level2") SceneManager.LoadScene("Level3");
            else SceneManager.LoadScene("MainMenu");
        }
    }

    public void LoadMainMenu()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
        else
            SceneManager.LoadScene("MainMenu");
    }
}
