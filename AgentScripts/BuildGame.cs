using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Builds every scene in the game (Main Menu, Level 1-3) with one shared art direction:
/// dark neutral tiles, stone islands, a stylised low-poly forest and the same dark UI shell.
/// </summary>
public class BuildGame
{
    static Material tileMat, tileRimMat, stoneMat, stoneDarkMat, startGrassMat, endGrassMat, grassMat, grassDarkMat;
    static Material leafMat, leafLightMat, leafDeepMat, pineMat, pineLightMat, trunkMat, hillMat, mountainMat, mountainDarkMat, cloudMat, rockMat, goldMat;
    static Material hoodieMat, jeansMat, skinMat, hairMat, shoeMat, eyeMat, pupilMat, hoodMat;

    static TMP_FontAsset font;
    static Sprite rounded;
    static Sprite dot;

    public static void Run() => RunAll();

    public static void RunAll()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Meshes");
        EnsureFolder("Assets/UI");

        CreateMaterials();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        if (font == null) Debug.LogError("UI font not found at Assets/Fonts/Inter-Regular SDF.asset");
        rounded = CreateRoundedSprite("Assets/UI/PanelRounded.png", 64, 18, false);
        dot = CreateRoundedSprite("Assets/UI/Dot.png", 32, 16, true);

        var level1 = BuildLevel1Data();
        var level2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        var level3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset");
        if (level2 == null || level3 == null)
        {
            Debug.LogError("Level2Data / Level3Data missing - aborting build.");
            return;
        }

        BuildLevelScene(level1, 0, "Assets/Scenes/Level1.unity");
        BuildLevelScene(level2, 1, "Assets/Scenes/Level2.unity");
        BuildLevelScene(level3, 2, "Assets/Scenes/Level3.unity");
        BuildMenuScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level2.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level3.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BuildGame finished: MainMenu + Level1/2/3 rebuilt with the shared forest theme.");
    }

    // =====================================================================
    // LEVEL SCENE
    // =====================================================================
    static void BuildLevelScene(LevelData data, int levelIndex, string savePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        float stride = data.tileSize + data.tileGap;
        float gridDepth = (data.rows - 1) * stride;
        float gridWidth = (data.columns - 1) * stride;

        // ---- GRID -------------------------------------------------------
        var gridRoot = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.useHexTiles = false;
        gridManager.customNeutralColor = new Color(0.105f, 0.112f, 0.122f);
        gridManager.customCompletedColor = new Color(0.155f, 0.168f, 0.183f);
        gridManager.customCompletedEmission = Color.black;
        gridManager.safeMat = tileMat;
        gridManager.unsafeMat = tileMat;
        gridManager.tileEdgeMat = stoneMat;
        gridManager.tileRockMat = stoneDarkMat;
        gridManager.tileRimMat = tileRimMat;

        // ---- START / FINISH ISLANDS -------------------------------------
        float islandW = Mathf.Max(5.2f, gridWidth * 0.46f);
        var startIsland = CreateIsland("StartIsland", null,
            new Vector3(0f, 0.11f, -gridDepth * 0.5f - 3.6f),
            islandW, 4.2f, startGrassMat, stoneMat, stoneDarkMat, true);
        var endIsland = CreateIsland("EndIsland", null,
            new Vector3(0f, 0.11f, gridDepth * 0.5f + 3.6f),
            islandW, 4.2f, endGrassMat, stoneMat, stoneDarkMat, true);

        CreateBeveledObject("GoalPad", endIsland.transform, new Vector3(0f, 0.055f, 0f),
            new Vector3(2.2f, 0.11f, 2.2f), 0.05f, goldMat, false);
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.3f, 0f), new Vector3(0.55f, 0.55f, 0.55f), goldMat, false);
        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size = new Vector3(islandW * 0.85f, 3.2f, 3.4f);
        endBox.isTrigger = true;
        endTrigger.AddComponent<EndPlatformTrigger>();

        gridManager.Build(data);

        // ---- PLAYER -----------------------------------------------------
        var player = BuildPlayer(startIsland.transform.position + Vector3.up * 1.6f);

        // ---- CAMERA -----------------------------------------------------
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam.fieldOfView = 54f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 700f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.transform.position = new Vector3(0f, 12f, -20f);
        cam.transform.rotation = Quaternion.Euler(39f, 0f, 0f);

        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            camData.renderPostProcessing = true;
        }

        var cf = camGo.AddComponent<CameraFollow>();
        cf.target = player.transform;
        cf.distance = 9.4f + Mathf.Max(0, data.rows - 5) * 1.0f + Mathf.Max(0, data.columns - 4) * 0.8f;
        cf.pitch = 39f;
        cf.yaw = 0f;
        cf.targetHeight = 1.35f;
        cf.lookAhead = 3.3f;
        cf.followSmooth = 12f;
        cf.rotationSmooth = 16f;
        cf.minDistance = 6f;
        cf.maxDistance = 24f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        // ---- LIGHTING / POST --------------------------------------------
        ConfigureLighting();
        ConfigurePostProcessing();

        // ---- ENVIRONMENT -------------------------------------------------
        var envRoot = new GameObject("Environment").transform;
        var cloudRoot = BuildEnvironment(envRoot, 20261123 + levelIndex * 977);

        // ---- MANAGERS -----------------------------------------------------
        var lmGo = new GameObject("LevelManager");
        var lm = lmGo.AddComponent<LevelManager>();
        lm.data = data;
        lm.gridManager = gridManager;
        lm.player = player.GetComponent<PlayerController>();
        lm.startPlatform = startIsland.transform;
        lm.endPlatform = endIsland.transform;

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.levels = new LevelData[]
        {
            AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset"),
            AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset"),
            AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset")
        };
        gm.currentLevelIndex = levelIndex;

        // ---- UI -----------------------------------------------------------
        BuildUI(lm, data, levelIndex);

        // ---- EVENT SYSTEM -------------------------------------------------
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ---- AMBIENT MOTION -------------------------------------------------
        var animator = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = cloudRoot;

        EditorSceneManager.SaveScene(scene, savePath);
        Debug.Log("Built " + savePath + " (rows=" + data.rows + ", cols=" + data.columns + ")");
    }

    static LevelData BuildLevel1Data()
    {
        const string path = "Assets/Data/Level1Data.asset";
        var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(levelData, path);
        }
        levelData.levelName = "Level 1";
        levelData.sceneName = "Level1";
        levelData.rows = 5;
        levelData.columns = 4;
        levelData.tileSize = 2.4f;
        levelData.tileGap = 0.6f;
        levelData.timeLimit = 120f;
        levelData.hideMapAfterCountdown = false;
        levelData.mapVisibleDuration = 6f;
        levelData.isUpsideDownPattern = false;
        levelData.objective = "Study the mirrored PATH MAP, then hop across the dark tiles and reach the golden finish island.";
        levelData.instructions = "WASD / Arrows - Move\nSpace - Jump\nRight / Left drag - Orbit camera\nMouse wheel - Zoom\nR - Restart   ESC - Level Select";
        levelData.safeTiles = new List<Vector2Int> {
            new Vector2Int(1,1), new Vector2Int(2,2), new Vector2Int(3,3),
            new Vector2Int(4,2), new Vector2Int(5,4)
        };
        levelData.sequence = new List<Vector2Int>(levelData.safeTiles);
        levelData.displayedPattern = new List<Vector2Int>(levelData.safeTiles);
        EditorUtility.SetDirty(levelData);
        return levelData;
    }

    // =====================================================================
    // MAIN MENU
    // =====================================================================
    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 600f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.transform.position = new Vector3(0f, 8.5f, -20f);
        cam.transform.LookAt(new Vector3(0f, 4.0f, 16f));
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            camData.renderPostProcessing = true;
        }

        ConfigureLighting();
        RenderSettings.fogDensity = 0.0058f;
        ConfigurePostProcessing();
        BuildMenuBackdrop();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var menu = canvasGo.AddComponent<MainMenuManager>();
        menu.levelButtons = new Button[3];

        // Header and footer scrims: darken only the bands behind text so the
        // forest stays vivid through the middle of the screen.
        var gradient = CreateGradientSprite("Assets/UI/MenuHaze.png",
            new Color(0.012f, 0.020f, 0.024f, 0.72f), new Color(0.012f, 0.020f, 0.024f, 0f));

        var topGo = new GameObject("ScrimTop", typeof(RectTransform), typeof(Image));
        topGo.transform.SetParent(canvasGo.transform, false);
        var topRt = topGo.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.sizeDelta = new Vector2(0f, 510f);
        topRt.anchoredPosition = Vector2.zero;
        var topImg = topGo.GetComponent<Image>();
        topImg.sprite = gradient;
        topImg.type = Image.Type.Simple;
        topImg.color = Color.white;
        topImg.raycastTarget = false;

        var botGo = new GameObject("ScrimBottom", typeof(RectTransform), typeof(Image));
        botGo.transform.SetParent(canvasGo.transform, false);
        var botRt = botGo.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0f, 0f);
        botRt.anchorMax = new Vector2(1f, 0f);
        botRt.pivot = new Vector2(0.5f, 0f);
        botRt.sizeDelta = new Vector2(0f, 330f);
        botRt.anchoredPosition = Vector2.zero;
        botRt.localScale = new Vector3(1f, -1f, 1f);
        var botImg = botGo.GetComponent<Image>();
        botImg.sprite = gradient;
        botImg.type = Image.Type.Simple;
        botImg.color = Color.white;
        botImg.raycastTarget = false;

        // Header
        CreateText(canvasGo.transform, "Kicker", "T H E   G R I D   R U N", font, 18f,
            new Vector2(0f, 372f), new Vector2(800f, 26f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.52f, 0.98f, 0.62f, 1f), 7f);
        CreateText(canvasGo.transform, "Title", "PATH FINDER", font, 76f,
            new Vector2(0f, 298f), new Vector2(1000f, 94f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white, 2f);
        CreateText(canvasGo.transform, "Subtitle", "Study the map. Remember the route. Cross the dark tiles.",
            font, 19f, new Vector2(0f, 240f), new Vector2(900f, 32f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.86f, 0.89f, 0.92f, 1f));

        CreateBar(canvasGo.transform, "HeaderRule", new Vector2(0f, 204f), new Vector2(360f, 2f),
            new Color(0.44f, 0.95f, 0.58f, 0.55f), rounded);

        // Level cards
        var cardsRoot = new GameObject("LevelCards", typeof(RectTransform));
        cardsRoot.transform.SetParent(canvasGo.transform, false);
        var crt = cardsRoot.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(1100f, 340f);
        crt.anchoredPosition = new Vector2(0f, 10f);

        string[] cardTitles = { "PATH FINDER", "MEMORY CHALLENGE", "INVERTED BLUEPRINT" };
        string[] cardDescs =
        {
            "Learn the route with a live map.",
            "The map hides after 6 seconds.",
            "Rows are flipped. Read the map carefully."
        };
        string[] cardMeta = { "4 \u00d7 5 GRID   \u00b7   5 TILES", "6 \u00d7 5 GRID   \u00b7   9 TILES", "6 \u00d7 5 GRID   \u00b7   8 TILES" };
        float cardW = 360f;
        float cardH = 300f;
        float gap = 36f;
        float totalW = cardW * 3f + gap * 2f;

        int[][] routeRows =
        {
            new[] { 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 2, 0, 0, 3, 0, 0 },
            new[] { 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0 },
            new[] { 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0 }
        };

        for (int i = 0; i < 3; i++)
        {
            float x = -totalW * 0.5f + cardW * 0.5f + i * (cardW + gap);
            var card = CreatePanel(cardsRoot.transform, "LevelCard" + (i + 1),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(cardW, cardH), new Vector2(x, 0f), rounded,
                new Color(0.042f, 0.052f, 0.050f, 0.95f));

            var btn = card.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.92f, 0.95f, 0.93f, 1f);
            colors.highlightedColor = new Color(1.10f, 1.16f, 1.12f, 1f);
            colors.pressedColor = new Color(0.80f, 0.86f, 0.82f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.10f;
            btn.colors = colors;
            menu.levelButtons[i] = btn;

            // Accent bar along the top edge.
            CreateBar(card.transform, "Accent", new Vector2(0f, cardH * 0.5f - 10f),
                new Vector2(cardW - 56f, 4f), new Color(0.35f, 0.92f, 0.52f, 1f), rounded);

            CreateText(card.transform, "Index", string.Format("{0:00}", i + 1), font, 30f,
                new Vector2(-cardW * 0.5f + 40f, cardH * 0.5f - 48f), new Vector2(60f, 40f),
                TextAlignmentOptions.Left, FontStyles.Bold, new Color(0.36f, 0.90f, 0.52f, 1f));

            CreateMiniRoute(card.transform, new Vector2(cardW * 0.5f - 58f, cardH * 0.5f - 56f),
                routeRows[i], rounded);

            CreateText(card.transform, "CardTitle", cardTitles[i], font, 24f,
                new Vector2(0f, 26f), new Vector2(cardW - 56f, 34f), TextAlignmentOptions.Left,
                FontStyles.Bold, Color.white, 1f);

            CreateText(card.transform, "CardDesc", cardDescs[i], font, 15.5f,
                new Vector2(0f, -30f), new Vector2(cardW - 56f, 50f), TextAlignmentOptions.TopLeft,
                FontStyles.Normal, new Color(0.85f, 0.88f, 0.91f, 1f));

            CreateText(card.transform, "CardMeta", cardMeta[i], font, 12f,
                new Vector2(0f, -78f), new Vector2(cardW - 56f, 20f), TextAlignmentOptions.TopLeft,
                FontStyles.Bold, new Color(0.62f, 0.68f, 0.72f, 1f), 1.6f);

            CreateText(card.transform, "PlayTag", "PLAY  \u2192", font, 13f,
                new Vector2(0f, -cardH * 0.5f + 30f), new Vector2(cardW - 56f, 22f),
                TextAlignmentOptions.Right, FontStyles.Bold, new Color(0.36f, 0.90f, 0.52f, 1f), 2f);
        }

        CreateText(canvasGo.transform, "ControlsHint",
            "WASD  move    \u00b7    SPACE  jump    \u00b7    DRAG  orbit camera    \u00b7    WHEEL  zoom",
            font, 14.5f, new Vector2(0f, -228f), new Vector2(1000f, 26f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.82f, 0.86f, 0.89f, 1f), 1.2f);

        CreateText(canvasGo.transform, "Footer", "More levels unlock as you master the grid.", font, 14f,
            new Vector2(0f, -326f), new Vector2(900f, 26f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.68f, 0.74f, 0.78f, 1f));

        // Toast
        var toast = CreatePanel(canvasGo.transform, "ToastPanel",
            new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.5f),
            new Vector2(520f, 76f), Vector2.zero, rounded,
            new Color(0.115f, 0.030f, 0.035f, 0.97f));
        menu.toastText = CreateText(toast.transform, "ToastText", "", font, 16f,
            Vector2.zero, new Vector2(470f, 54f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(1f, 0.46f, 0.42f, 1f));
        menu.toastPanel = toast;
        toast.SetActive(false);

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        SetupAudioManager();

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.levels = new LevelData[]
        {
            AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset"),
            AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset"),
            AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset")
        };
        gm.currentLevelIndex = 0;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    static void CreateMiniRoute(Transform parent, Vector2 pos, int[] cells, Sprite sprite)
    {
        const int cols = 4;
        const int rows = 4;
        const float cell = 14f;
        const float spacing = 4f;

        float totalW = cols * cell + (cols - 1) * spacing;
        float totalH = rows * cell + (rows - 1) * spacing;

        var root = new GameObject("RouteIcon", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(totalW, totalH);
        rt.anchoredPosition = pos;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                bool lit = cells[r * cols + c] > 0;
                float x = -totalW * 0.5f + cell * 0.5f + c * (cell + spacing);
                float y = totalH * 0.5f - cell * 0.5f - r * (cell + spacing);
                CreateBar(root.transform, string.Format("Cell{0}_{1}", r, c),
                    new Vector2(x, y), new Vector2(cell, cell),
                    lit ? new Color(0.24f, 0.90f, 0.44f, 1f) : new Color(0.145f, 0.160f, 0.175f, 1f),
                    sprite);
            }
        }
    }

    static void BuildMenuBackdrop()
    {
        var root = new GameObject("MenuBackdrop").transform;

        var ground = CreatePrimitive(PrimitiveType.Plane, "MenuGround", root,
            new Vector3(0f, -9f, 22f), new Vector3(30f, 1f, 30f), hillMat, false);
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Rolling hills break up the horizon behind the islands.
        for (int i = 0; i < 7; i++)
        {
            float x = -70f + i * 23f;
            float z = 72f + (i % 3) * 12f;
            var hill = CreatePrimitive(PrimitiveType.Sphere, "MenuHill" + i, root,
                new Vector3(x, -9f + 3.5f, z),
                new Vector3(38f + (i % 3) * 10f, 12f + (i % 2) * 4f, 34f),
                i % 2 == 0 ? hillMat : grassDarkMat, false);
            hill.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        CreateIsland("MenuIslandLeft", root, new Vector3(-21f, -2f, 26f),
            12f, 9f, startGrassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("MenuIslandRight", root, new Vector3(22f, 0f, 27f),
            11f, 8f, endGrassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("MenuIslandFar", root, new Vector3(0f, -5f, 50f),
            24f, 15f, grassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("MenuIslandFloat", root, new Vector3(-10f, 10f, 44f),
            4f, 3.4f, startGrassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("MenuIslandFloatB", root, new Vector3(15f, 12f, 50f),
            3.5f, 3f, endGrassMat, stoneMat, stoneDarkMat, false);

        CreateTree(root, new Vector3(-25f, -2f, 24f), 1.5f, 0);
        CreateTree(root, new Vector3(-19f, -2f, 27f), 1.2f, 2);
        CreateTree(root, new Vector3(-26f, -2f, 29f), 1.1f, 1);
        CreateTree(root, new Vector3(19f, 0f, 25f), 1.4f, 3);
        CreateTree(root, new Vector3(25f, 0f, 28f), 1.15f, 0);
        CreateTree(root, new Vector3(27f, 0f, 31f), 1.25f, 2);
        CreateTree(root, new Vector3(-9.5f, 10f, 44f), 0.55f, 1);

        CreateBeveledObject("MenuGoalPad", root, new Vector3(0f, -4.95f, 50f),
            new Vector3(2.2f, 0.11f, 2.2f), 0.05f, goldMat, false);

        for (int i = 0; i < 4; i++)
        {
            CreateBeveledObject("MenuTile" + i, root,
                new Vector3(-23.5f + i * 2.1f, -1.86f, 23.5f - i * 1.4f),
                new Vector3(2.0f, 0.20f, 2.0f), 0.07f, tileMat, false);
        }

        for (int i = 0; i < 9; i++)
        {
            float x = -62f + i * 15.5f;
            float z = 82f + (i % 3) * 14f;
            float height = 24f + (i % 4) * 5f;
            float diameter = 44f + (i % 3) * 14f;
            CreateCone("MenuMountain" + i, root,
                new Vector3(x, -9f + height * 0.5f, z), diameter, height,
                i % 2 == 0 ? mountainMat : mountainDarkMat, false);
        }

        var clouds = new GameObject("MenuClouds");
        clouds.transform.SetParent(root, false);
        for (int i = 0; i < 7; i++)
        {
            float x = -38f + i * 12.5f;
            float y = 25f + (i % 3) * 4.5f;
            float z = 58f + (i % 4) * 9f;
            for (int k = 0; k < 3; k++)
            {
                var puff = CreatePrimitive(PrimitiveType.Sphere, "MenuPuff" + i + "_" + k, clouds.transform,
                    new Vector3(x + (k - 1) * 2.8f, y + (k % 2) * 0.7f, z + (k % 3) * 0.7f),
                    new Vector3(5.8f - k * 0.6f, 2.5f, 4.2f), cloudMat, false);
                puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }

    static void SetupAudioManager()
    {
        var audioGo = new GameObject("AudioManager");
        var am = audioGo.AddComponent<AudioManager>();
        am.level1ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level1Click.wav");
        am.level2ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level2Click.wav");
        am.level3ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level3Click.wav");
        am.levelLockedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelLocked.wav");
    }

    // =====================================================================
    // LEVEL UI
    // =====================================================================
    static void BuildUI(LevelManager lm, LevelData data, int levelIndex)
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var ui = canvasGo.AddComponent<UIManager>();
        lm.uiManager = ui;
        ui.font = font;
        ui.roundedSprite = rounded;
        ui.dotSprite = dot;

        // ================= LEFT PANEL : MAP =================
        var mapPanel = CreatePanel(canvasGo.transform, "MapPanel",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(360f, 800f), new Vector2(28f, 0f), rounded,
            new Color(0.038f, 0.047f, 0.055f, 0.93f));

        CreateText(mapPanel.transform, "MapTitle", "PATH MAP", font, 30f,
            new Vector2(0f, 340f), new Vector2(312f, 40f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        ui.mapStatusText = CreateText(mapPanel.transform, "MapSub",
            data.hideMapAfterCountdown ? "MEMORIZE PATH" : "LIVE PATH GUIDANCE",
            font, 12.5f, new Vector2(0f, 306f), new Vector2(312f, 24f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.40f, 0.92f, 0.55f, 1f), 1.6f);

        CreateBar(mapPanel.transform, "MapDivider", new Vector2(0f, 282f), new Vector2(312f, 2f),
            new Color(1f, 1f, 1f, 0.11f), rounded);

        CreateText(mapPanel.transform, "FinishTag", "FINISH", font, 13.5f,
            new Vector2(0f, 178f), new Vector2(312f, 18f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.96f, 0.78f, 0.36f, 1f), 3f);

        var mapContainerGo = new GameObject("MapContainer", typeof(RectTransform));
        mapContainerGo.transform.SetParent(mapPanel.transform, false);
        var mapRect = mapContainerGo.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(312f, 390f);
        mapRect.anchoredPosition = Vector2.zero;
        ui.mapContainer = mapContainerGo.transform;

        var markerGo = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        markerGo.transform.SetParent(mapContainerGo.transform, false);
        var markerRect = markerGo.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(18f, 18f);
        var markerImg = markerGo.GetComponent<Image>();
        markerImg.sprite = dot;
        markerImg.color = new Color(0.62f, 1f, 0.70f, 1f);
        markerImg.raycastTarget = false;
        markerGo.SetActive(false);
        ui.playerMarker = markerImg;

        CreateText(mapPanel.transform, "StartTag", "START", font, 13.5f,
            new Vector2(0f, -178f), new Vector2(312f, 18f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.72f, 0.78f, 0.84f, 1f), 3f);

        CreateSwatch(mapPanel.transform, "SafeSwatch", new Vector2(-92f, -242f),
            new Vector2(17f, 17f), new Color(0.24f, 0.90f, 0.44f), rounded);
        CreateText(mapPanel.transform, "SafeLabel", "SAFE", font, 13.5f,
            new Vector2(-45f, -242f), new Vector2(70f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.85f, 0.89f, 1f), 1.5f);
        CreateSwatch(mapPanel.transform, "UnsafeSwatch", new Vector2(24f, -242f),
            new Vector2(17f, 17f), new Color(0.155f, 0.165f, 0.185f), rounded);
        CreateText(mapPanel.transform, "UnsafeLabel", "UNSAFE", font, 13.5f,
            new Vector2(80f, -242f), new Vector2(90f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.85f, 0.89f, 1f), 1.5f);

        ui.mapHintText = CreateText(mapPanel.transform, "MapHint",
            data.hideMapAfterCountdown ? "Memorize before the path conceals!" : "The map mirrors what you see in the course.",
            font, 12.5f, new Vector2(0f, -302f), new Vector2(312f, 46f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.66f, 0.71f, 0.76f, 1f));

        // ================= RIGHT PANEL : LEVEL INFO =================
        var infoPanel = CreatePanel(canvasGo.transform, "InfoPanel",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(440f, 860f), new Vector2(-28f, 0f), rounded,
            new Color(0.038f, 0.047f, 0.055f, 0.93f));

        ui.levelTitle = CreateText(infoPanel.transform, "LevelTitle",
            string.Format("LEVEL {0:00}", levelIndex + 1), font, 44f,
            new Vector2(0f, 372f), new Vector2(384f, 60f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        ui.subtitle = CreateText(infoPanel.transform, "Subtitle",
            data.isUpsideDownPattern ? "INVERTED BLUEPRINT" : data.hideMapAfterCountdown ? "MEMORY CHALLENGE" : "PATH FINDER",
            font, 18f, new Vector2(0f, 330f), new Vector2(384f, 26f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.33f, 0.92f, 0.50f, 1f), 6f);

        CreateBar(infoPanel.transform, "InfoDivider", new Vector2(0f, 300f), new Vector2(384f, 2f),
            new Color(1f, 1f, 1f, 0.11f), rounded);

        CreateText(infoPanel.transform, "ObjectiveLabel", "OBJECTIVE", font, 13.5f,
            new Vector2(0f, 272f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.70f, 0.75f, 0.79f, 1f), 2.5f);
        ui.objectiveText = CreateText(infoPanel.transform, "Objective", data.objective, font, 17.5f,
            new Vector2(0f, 224f), new Vector2(384f, 76f), TextAlignmentOptions.TopLeft,
            FontStyles.Normal, new Color(0.86f, 0.89f, 0.92f, 1f));

        CreateText(infoPanel.transform, "ProgressLabel", "PROGRESS", font, 13.5f,
            new Vector2(0f, 150f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.70f, 0.75f, 0.79f, 1f), 2.5f);
        ui.progressText = CreateText(infoPanel.transform, "Progress",
            string.Format("0 <size=22><color=#7C8794>/ {0}</color></size>", data.sequence.Count),
            font, 40f, new Vector2(0f, 110f), new Vector2(384f, 48f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.40f, 1f, 0.55f, 1f));

        var progressBg = CreateBar(infoPanel.transform, "ProgressBg", new Vector2(0f, 78f),
            new Vector2(384f, 10f), new Color(1f, 1f, 1f, 0.10f), rounded);
        var fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(progressBg.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg = fillGo.GetComponent<Image>();
        fillImg.sprite = rounded;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 0f;
        fillImg.color = new Color(0.32f, 0.94f, 0.50f, 1f);
        fillImg.raycastTarget = false;
        ui.progressFill = fillImg;

        CreateText(infoPanel.transform, "TimeLabel", "TIME", font, 13.5f,
            new Vector2(0f, 44f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.70f, 0.75f, 0.79f, 1f), 2.5f);
        ui.timerText = CreateText(infoPanel.transform, "Timer", "120.0s", font, 32f,
            new Vector2(0f, 10f), new Vector2(384f, 40f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        CreateBar(infoPanel.transform, "InfoDivider2", new Vector2(0f, -28f), new Vector2(384f, 2f),
            new Color(1f, 1f, 1f, 0.11f), rounded);

        CreateText(infoPanel.transform, "ControlsLabel", "CONTROLS", font, 13.5f,
            new Vector2(0f, -54f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.70f, 0.75f, 0.79f, 1f), 2.5f);
        ui.instructionsText = CreateText(infoPanel.transform, "Controls",
            "<b>WASD / ARROWS</b>     Move\n<b>SPACE</b>     Jump\n<b>DRAG / Q E</b>     Orbit camera\n<b>MOUSE WHEEL</b>     Zoom\n<b>R</b>     Restart level",
            font, 15.5f, new Vector2(0f, -138f), new Vector2(384f, 150f),
            TextAlignmentOptions.TopLeft, FontStyles.Normal, new Color(0.72f, 0.77f, 0.81f, 1f));
        ui.instructionsText.lineSpacing = 12f;

        var restartBtn = CreateButton(infoPanel.transform, "RestartButton", "RESTART  (R)", font,
            new Vector2(-98f, -262f), new Vector2(188f, 52f), rounded,
            new Color(0.30f, 0.86f, 0.45f, 1f), new Color(0.03f, 0.09f, 0.05f, 1f));
        restartBtn.onClick.AddListener(() => lm.RestartLevel());
        ui.restartButton = restartBtn;

        var menuBtn = CreateButton(infoPanel.transform, "MenuButton", "LEVELS", font,
            new Vector2(98f, -262f), new Vector2(188f, 52f), rounded,
            new Color(0.20f, 0.24f, 0.28f, 1f), new Color(0.88f, 0.92f, 0.95f, 1f));
        menuBtn.onClick.AddListener(() => lm.LoadMainMenu());
        ui.menuButton = menuBtn;

        CreateText(infoPanel.transform, "FooterHint", "Reach the golden finish island with every safe tile cleared.",
            font, 13f, new Vector2(0f, -318f), new Vector2(384f, 34f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.66f, 0.71f, 0.76f, 1f));

        // ================= OVERLAYS =================
        var complete = CreatePanel(canvasGo.transform, "LevelCompletePanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(560f, 360f), Vector2.zero, rounded,
            new Color(0.035f, 0.075f, 0.055f, 0.96f));
        CreateText(complete.transform, "CompleteTitle", "LEVEL COMPLETE", font, 40f,
            new Vector2(0f, 108f), new Vector2(500f, 52f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.42f, 1f, 0.56f, 1f));
        CreateText(complete.transform, "CompleteSub",
            data.isUpsideDownPattern ? "Inverted blueprint mastered." :
            data.hideMapAfterCountdown ? "Brilliant memory run." : "Safe path mastered - beautiful work.",
            font, 16f, new Vector2(0f, 64f), new Vector2(500f, 26f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.74f, 0.80f, 0.84f, 1f));
        ui.levelCompleteTime = CreateText(complete.transform, "CompleteTime", "", font, 20f,
            new Vector2(0f, 24f), new Vector2(500f, 32f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white);

        var nextBtn = CreateButton(complete.transform, "NextLevelButton",
            levelIndex < 2 ? "NEXT LEVEL" : "REPLAY LEVEL", font,
            new Vector2(0f, -54f), new Vector2(300f, 54f), rounded,
            new Color(0.30f, 0.86f, 0.45f, 1f), new Color(0.03f, 0.09f, 0.05f, 1f));
        if (levelIndex < 2) nextBtn.onClick.AddListener(() => lm.LoadNextLevel());
        else nextBtn.onClick.AddListener(() => lm.RestartLevel());
        ui.nextLevelButton = nextBtn;

        var compMenuBtn = CreateButton(complete.transform, "CompleteMenuButton", "LEVEL SELECT", font,
            new Vector2(0f, -120f), new Vector2(300f, 48f), rounded,
            new Color(0.18f, 0.22f, 0.26f, 1f), new Color(0.88f, 0.92f, 0.95f, 1f));
        compMenuBtn.onClick.AddListener(() => lm.LoadMainMenu());
        ui.completeMenuButton = compMenuBtn;

        complete.SetActive(false);
        ui.levelCompletePanel = complete;

        var fail = CreatePanel(canvasGo.transform, "FailPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(480f, 210f), Vector2.zero, rounded,
            new Color(0.105f, 0.025f, 0.030f, 0.97f));
        CreateText(fail.transform, "FailTitle", "WRONG TILE", font, 34f,
            new Vector2(0f, 34f), new Vector2(430f, 46f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(1f, 0.32f, 0.30f, 1f));
        CreateText(fail.transform, "FailSub", "Restarting the course...", font, 15f,
            new Vector2(0f, -14f), new Vector2(430f, 24f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.82f, 0.80f, 0.82f, 1f));
        fail.SetActive(false);
        ui.failPanel = fail;
    }

    // =====================================================================
    // PLAYER
    // =====================================================================
    static GameObject BuildPlayer(Vector3 position)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;

        var model = new GameObject("Model");
        model.transform.SetParent(player.transform, false);
        model.transform.localPosition = new Vector3(0f, -0.09f, 0f);

        AddPart(PrimitiveType.Capsule, "Torso", model.transform, new Vector3(0f, 1.34f, 0f),
            new Vector3(0.70f, 0.62f, 0.48f), hoodieMat);
        AddPart(PrimitiveType.Sphere, "ShoulderL", model.transform, new Vector3(-0.405f, 1.68f, 0f),
            new Vector3(0.30f, 0.26f, 0.30f), hoodieMat);
        AddPart(PrimitiveType.Sphere, "ShoulderR", model.transform, new Vector3(0.405f, 1.68f, 0f),
            new Vector3(0.30f, 0.26f, 0.30f), hoodieMat);
        AddPart(PrimitiveType.Sphere, "Hood", model.transform, new Vector3(0f, 1.90f, -0.14f),
            new Vector3(0.52f, 0.30f, 0.40f), hoodMat);
        AddPart(PrimitiveType.Cylinder, "Neck", model.transform, new Vector3(0f, 1.90f, 0f),
            new Vector3(0.20f, 0.10f, 0.20f), skinMat);

        var head = AddPart(PrimitiveType.Sphere, "Head", model.transform, new Vector3(0f, 2.06f, 0.015f),
            new Vector3(0.56f, 0.58f, 0.55f), skinMat);
        AddPart(PrimitiveType.Sphere, "Hair", model.transform, new Vector3(0f, 2.16f, -0.025f),
            new Vector3(0.60f, 0.42f, 0.59f), hairMat);
        AddPart(PrimitiveType.Cube, "Fringe", model.transform, new Vector3(0f, 2.24f, 0.24f),
            new Vector3(0.44f, 0.10f, 0.13f), hairMat);

        foreach (var side in new[] { -1f, 1f })
        {
            AddPart(PrimitiveType.Sphere, side < 0 ? "EyeL" : "EyeR", head.transform,
                new Vector3(side * 0.24f, 0.08f, 0.44f), new Vector3(0.18f, 0.20f, 0.10f), eyeMat);
            AddPart(PrimitiveType.Sphere, side < 0 ? "PupilL" : "PupilR", head.transform,
                new Vector3(side * 0.24f, 0.08f, 0.50f), new Vector3(0.08f, 0.09f, 0.05f), pupilMat);
        }

        AddPart(PrimitiveType.Cube, "Zip", model.transform, new Vector3(0f, 1.36f, 0.252f),
            new Vector3(0.035f, 0.72f, 0.02f), shoeMat);

        var armL = CreateLimb("ArmL", model.transform, new Vector3(-0.44f, 1.70f, 0f),
            new Vector3(0.175f, 0.31f, 0.175f), hoodieMat, new Vector3(0f, -0.33f, 0f), 0.07f);
        var armR = CreateLimb("ArmR", model.transform, new Vector3(0.44f, 1.70f, 0f),
            new Vector3(0.175f, 0.31f, 0.175f), hoodieMat, new Vector3(0f, -0.33f, 0f), -0.07f);
        AddPart(PrimitiveType.Sphere, "HandL", armL, new Vector3(0f, -0.64f, 0f),
            new Vector3(0.16f, 0.18f, 0.16f), skinMat);
        AddPart(PrimitiveType.Sphere, "HandR", armR, new Vector3(0f, -0.64f, 0f),
            new Vector3(0.16f, 0.18f, 0.16f), skinMat);

        var legL = CreateLimb("LegL", model.transform, new Vector3(-0.17f, 0.92f, 0f),
            new Vector3(0.24f, 0.36f, 0.24f), jeansMat, new Vector3(0f, -0.36f, 0f), 0f);
        var legR = CreateLimb("LegR", model.transform, new Vector3(0.17f, 0.92f, 0f),
            new Vector3(0.24f, 0.36f, 0.24f), jeansMat, new Vector3(0f, -0.36f, 0f), 0f);

        AddPart(PrimitiveType.Cube, "ShoeL", legL, new Vector3(0f, -0.74f, 0.06f),
            new Vector3(0.28f, 0.17f, 0.44f), shoeMat);
        AddPart(PrimitiveType.Cube, "ShoeR", legR, new Vector3(0f, -0.74f, 0.06f),
            new Vector3(0.28f, 0.17f, 0.44f), shoeMat);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2.35f;
        cc.radius = 0.38f;
        cc.center = new Vector3(0f, 1.175f, 0f);
        cc.stepOffset = 0.35f;
        cc.skinWidth = 0.03f;

        var pc = player.AddComponent<PlayerController>();
        pc.moveSpeed = 6.5f;
        pc.jumpHeight = 2.2f;
        pc.gravity = -22f;

        var visual = player.AddComponent<PlayerVisual>();
        visual.model = model.transform;
        visual.torso = model.transform.Find("Torso");
        visual.head = head.transform;
        visual.armL = armL;
        visual.armR = armR;
        visual.legL = legL;
        visual.legR = legR;

        return player;
    }

    static Transform CreateLimb(string name, Transform parent, Vector3 localPos, Vector3 scale,
        Material mat, Vector3 partOffset, float zTilt)
    {
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;
        pivot.transform.localRotation = Quaternion.Euler(0f, 0f, zTilt);
        AddPart(PrimitiveType.Capsule, name + "_Mesh", pivot.transform, partOffset, scale, mat);
        return pivot.transform;
    }

    static GameObject AddPart(PrimitiveType type, string name, Transform parent, Vector3 localPos,
        Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
        var c = go.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
        return go;
    }

    // =====================================================================
    // ENVIRONMENT
    // =====================================================================
    static Transform BuildEnvironment(Transform root, int seed)
    {
        const float groundY = -30f;

        var ground = CreatePrimitive(PrimitiveType.Plane, "Ground", root,
            new Vector3(0f, groundY, 0f), new Vector3(30f, 1f, 30f), grassMat, false);
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Rolling hills.
        for (int i = 0; i < 14; i++)
        {
            float angle = i * (360f / 14f) + 8f;
            float radius = 55f + (i % 5) * 14f;
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY + 3.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            var hill = CreatePrimitive(PrimitiveType.Sphere, "Hill" + i, root, pos,
                new Vector3(34f + (i % 4) * 10f, 10f + (i % 3) * 4f, 34f + (i % 4) * 8f),
                i % 2 == 0 ? hillMat : grassDarkMat, false);
            hill.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // Faceted low-poly mountains, layered into the haze.
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f + 12f;
            float radius = 105f + (i % 4) * 26f;
            float height = 20f + (i % 4) * 4.5f;
            float diameter = 62f + (i % 4) * 22f;
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + height * 0.5f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateCone("Mountain" + i, root, pos, diameter, height,
                i % 2 == 0 ? mountainMat : mountainDarkMat, false);
        }
        for (int i = 0; i < 7; i++)
        {
            float angle = i * 51f + 30f;
            float radius = 175f + (i % 3) * 30f;
            float height = 38f + (i % 3) * 8f;
            float diameter = 120f + (i % 3) * 40f;
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + height * 0.5f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateCone("FarMountain" + i, root, pos, diameter, height, mountainMat, false);
        }

        // Broad fields of slightly different greens break up the valley floor.
        UnityEngine.Random.InitState(seed + 17);
        for (int i = 0; i < 18; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float radius = 26f + UnityEngine.Random.Range(0f, 90f);
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY + 0.05f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float patch = 10f + UnityEngine.Random.Range(0f, 22f);
            var field = CreatePrimitive(PrimitiveType.Cylinder, "Field" + i, root, pos,
                new Vector3(patch, 0.045f, patch), i % 2 == 0 ? grassDarkMat : hillMat, false);
            field.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // Forest rings, kept clear of the play space so the grid stays readable.
        UnityEngine.Random.InitState(seed);
        for (int i = 0; i < 22; i++)
        {
            float angle = i * (360f / 22f) + UnityEngine.Random.Range(-5f, 5f);
            float radius = 42f + UnityEngine.Random.Range(0f, 15f);
            Vector3 basePos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateTree(root, basePos, UnityEngine.Random.Range(1.7f, 2.5f), UnityEngine.Random.Range(0, 4));
        }
        for (int i = 0; i < 38; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float radius = 62f + UnityEngine.Random.Range(0f, 46f);
            Vector3 basePos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateTree(root, basePos, UnityEngine.Random.Range(1.3f, 2.0f), UnityEngine.Random.Range(0, 4));
        }

        // A handful of taller trees for a layered skyline.
        int hero = 0;
        foreach (float angle in new[] { 38f, 104f, 148f, 216f, 262f, 322f })
        {
            float radius = 48f + (hero % 3) * 6f;
            Vector3 basePos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateTree(root, basePos, 2.9f + (hero % 2) * 0.35f, hero % 2 == 0 ? 2 : 3);
            hero++;
        }

        // Bushes.
        for (int i = 0; i < 32; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float radius = 24f + UnityEngine.Random.Range(0f, 30f);
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY + 0.7f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreatePrimitive(PrimitiveType.Sphere, "Bush" + i, root, pos,
                new Vector3(1.9f, 1.1f, 1.9f) * UnityEngine.Random.Range(0.7f, 1.3f),
                i % 2 == 0 ? grassDarkMat : leafDeepMat, false);
        }

        // Decorative floating islands, pushed out into the background haze.
        CreateIsland("FloatIslandA", root, new Vector3(-44f, 12f, 22f), 5.0f, 4.0f, startGrassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("FloatIslandB", root, new Vector3(47f, 9f, 14f), 4.2f, 3.4f, endGrassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("FloatIslandC", root, new Vector3(-36f, 6f, 44f), 3.6f, 3.0f, startGrassMat, stoneMat, stoneDarkMat, false);
        CreateIsland("FloatIslandD", root, new Vector3(38f, 16f, 42f), 4.6f, 3.6f, endGrassMat, stoneMat, stoneDarkMat, false);

        // A few floating rocks, kept outside the play space.
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 61f + 20f;
            float radius = 34f + (i % 3) * 6f;
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                4f + (i % 4) * 2.2f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateBeveledObject("FloatRock" + i, root, pos,
                new Vector3(1.6f + (i % 3) * 0.6f, 1.2f, 1.4f + (i % 2) * 0.6f), 0.18f, rockMat, false);
        }

        // Clouds around the sky.
        var clouds = new GameObject("Clouds");
        clouds.transform.SetParent(root, false);
        for (int i = 0; i < 14; i++)
        {
            float angle = i * (360f / 14f) + 10f;
            float radius = 52f + (i % 5) * 11f;
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                22f + (i % 4) * 4.5f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            var cluster = new GameObject("Cloud" + i);
            cluster.transform.SetParent(clouds.transform, false);
            cluster.transform.localPosition = pos;

            for (int k = 0; k < 4; k++)
            {
                var puff = CreatePrimitive(PrimitiveType.Sphere, "Puff" + k, cluster.transform,
                    new Vector3((k - 1.5f) * 3.4f, (k % 2) * 0.9f, (k % 3) * 0.8f),
                    new Vector3(7.5f - k * 0.6f, 3.2f - k * 0.2f, 5.4f - k * 0.3f), cloudMat, false);
                puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        return clouds.transform;
    }

    static void CreateTree(Transform parent, Vector3 basePos, float scale, int variant)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = basePos;
        tree.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        if (variant >= 2)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform,
                new Vector3(0f, 2f * scale, 0f),
                new Vector3(0.30f * scale, 2f * scale, 0.30f * scale), trunkMat, false);

            Material pineMain = variant == 3 ? pineLightMat : pineMat;
            Material pineAlt = variant == 3 ? pineMat : pineLightMat;

            CreateCone("PineLow", tree.transform, new Vector3(0f, 4.9f * scale, 0f),
                6.4f * scale, 4.6f * scale, pineMain, true);
            CreateCone("PineMid", tree.transform, new Vector3(0f, 7.0f * scale, 0f),
                5.0f * scale, 4.0f * scale, pineAlt, true);
            CreateCone("PineTop", tree.transform, new Vector3(0f, 8.9f * scale, 0f),
                3.6f * scale, 3.4f * scale, pineMain, true);
        }
        else
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform,
                new Vector3(0f, 2.4f * scale, 0f),
                new Vector3(0.34f * scale, 2.4f * scale, 0.34f * scale), trunkMat, false);

            Material main = variant == 0 ? leafMat : leafLightMat;
            Material second = variant == 0 ? leafLightMat : leafDeepMat;

            CreatePrimitive(PrimitiveType.Sphere, "CanopyLow", tree.transform,
                new Vector3(0f, 4.8f * scale, 0f),
                new Vector3(3.9f, 1.8f, 3.9f) * scale, main, false);
            CreatePrimitive(PrimitiveType.Sphere, "CanopyMid", tree.transform,
                new Vector3(0.2f * scale, 6.3f * scale, -0.15f * scale),
                new Vector3(3.2f, 1.6f, 3.2f) * scale, second, false);
            CreatePrimitive(PrimitiveType.Sphere, "CanopyTop", tree.transform,
                new Vector3(-0.1f * scale, 7.6f * scale, 0.1f * scale),
                new Vector3(2.2f, 1.4f, 2.2f) * scale, main, false);
            CreatePrimitive(PrimitiveType.Sphere, "CanopySide", tree.transform,
                new Vector3(2.0f * scale, 5.0f * scale, 0.3f * scale),
                new Vector3(2.0f, 1.4f, 2.0f) * scale, second, false);
            CreatePrimitive(PrimitiveType.Sphere, "CanopySide2", tree.transform,
                new Vector3(-2.0f * scale, 4.9f * scale, -0.3f * scale),
                new Vector3(1.8f, 1.3f, 1.8f) * scale, main, false);
        }
    }

    static void CreateCone(string name, Transform parent, Vector3 localPos, float diameter,
        float height, Material mat, bool castShadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(diameter, height, diameter);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetConeMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        mr.receiveShadows = castShadows;
    }

    static Mesh coneMesh;
    static Mesh GetConeMesh()
    {
        if (coneMesh != null) return coneMesh;
        const string path = "Assets/Meshes/Cone8.asset";
        coneMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (coneMesh == null)
        {
            coneMesh = ConeMesh.CreateUnit(8);
            coneMesh.name = "Cone8";
            AssetDatabase.CreateAsset(coneMesh, path);
        }
        return coneMesh;
    }

    static GameObject CreateIsland(string name, Transform parent, Vector3 topCenter,
        float width, float depth, Material grass, Material stone, Material rock, bool addCollider)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = topCenter;

        float grassHeight = 0.5f;
        var slab = CreateBeveledObject(name + "_Grass", root.transform,
            new Vector3(0f, -grassHeight * 0.5f, 0f),
            new Vector3(width, grassHeight, depth), 0.11f, grass, addCollider);
        slab.GetComponent<Renderer>().receiveShadows = true;

        float baseHeight = 1.3f;
        CreateBeveledObject(name + "_Base", root.transform,
            new Vector3(0f, -grassHeight - baseHeight * 0.5f, 0f),
            new Vector3(width * 0.9f, baseHeight, depth * 0.9f), 0.16f, stone, false);

        float rockHeight = 0.95f;
        CreateBeveledObject(name + "_Rock", root.transform,
            new Vector3(0f, -grassHeight - baseHeight - rockHeight * 0.5f, 0f),
            new Vector3(width * 0.56f, rockHeight, depth * 0.56f), 0.14f, rock, false);

        return root;
    }

    static GameObject CreateBeveledObject(string name, Transform parent, Vector3 localPos,
        Vector3 size, float bevel, Material mat, bool addCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var mesh = GetMeshAsset(name + "_" + size.x.ToString("0.00") + "_" + size.y.ToString("0.00") + "_" + size.z.ToString("0.00"),
            size.x, size.y, size.z, bevel);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;

        if (addCollider)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.zero;
        }

        return go;
    }

    static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    static Mesh GetMeshAsset(string name, float w, float h, float d, float bevel)
    {
        Mesh cached;
        if (meshCache.TryGetValue(name, out cached)) return cached;

        string safe = name.Replace(' ', '_');
        string path = "Assets/Meshes/" + safe + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = BeveledBoxMesh.Create(w, h, d, bevel);
            mesh.name = safe;
            AssetDatabase.CreateAsset(mesh, path);
        }
        meshCache[name] = mesh;
        return mesh;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 pos,
        Vector3 scale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
        }
        if (!keepCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }
        return go;
    }

    // =====================================================================
    // LIGHTING / POST
    // =====================================================================
    static void ConfigureLighting()
    {
        var light = UnityEngine.Object.FindAnyObjectByType<Light>();
        if (light == null)
        {
            var go = new GameObject("Sun");
            light = go.AddComponent<Light>();
        }
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.97f, 0.90f);
        light.intensity = 1.42f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.85f;
        light.shadowBias = 0.03f;
        light.shadowNormalBias = 0.35f;
        light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        var sky = CreateSkyboxMaterial();
        RenderSettings.skybox = sky;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.71f, 0.84f, 0.96f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0045f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        DynamicGI.UpdateEnvironment();
    }

    static Material CreateSkyboxMaterial()
    {
        const string path = "Assets/Materials/Skybox.mat";
        var shader = Shader.Find("Skybox/Procedural");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetFloat("_SunDisk", 2f);
        mat.SetFloat("_SunSize", 0.045f);
        mat.SetFloat("_SunSizeConvergence", 5f);
        mat.SetFloat("_AtmosphereThickness", 0.72f);
        mat.SetColor("_SkyTint", new Color(0.40f, 0.60f, 1.00f));
        mat.SetColor("_GroundColor", new Color(0.34f, 0.50f, 0.34f));
        mat.SetFloat("_Exposure", 1.12f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigurePostProcessing()
    {
        const string profilePath = "Assets/Settings/GameVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        Tonemapping tone;
        if (!profile.TryGet(out tone)) tone = profile.Add<Tonemapping>();
        tone.active = true;
        tone.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments color;
        if (!profile.TryGet(out color)) color = profile.Add<ColorAdjustments>();
        color.active = true;
        color.postExposure.Override(0.06f);
        color.contrast.Override(10f);
        color.saturation.Override(-8f);

        var go = new GameObject("GlobalVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile = profile;

        EditorUtility.SetDirty(profile);
    }

    // =====================================================================
    // MATERIALS
    // =====================================================================
    static void CreateMaterials()
    {
        tileMat = CreateMaterial("Assets/Materials/Tile.mat", new Color(0.105f, 0.112f, 0.122f), 0.05f, 0.16f, 0f);
        tileRimMat = CreateMaterial("Assets/Materials/TileRim.mat", new Color(0.045f, 0.050f, 0.056f), 0f, 0.10f, 0f);
        stoneMat = CreateMaterial("Assets/Materials/Stone.mat", new Color(0.50f, 0.49f, 0.47f), 0f, 0.16f, 0f);
        stoneDarkMat = CreateMaterial("Assets/Materials/StoneDark.mat", new Color(0.32f, 0.32f, 0.31f), 0f, 0.12f, 0f);
        startGrassMat = CreateMaterial("Assets/Materials/GrassStart.mat", new Color(0.38f, 0.73f, 0.27f), 0f, 0.18f, 0f);
        endGrassMat = CreateMaterial("Assets/Materials/GrassEnd.mat", new Color(0.41f, 0.76f, 0.29f), 0f, 0.18f, 0f);
        grassMat = CreateMaterial("Assets/Materials/Grass.mat", new Color(0.29f, 0.58f, 0.22f), 0f, 0.10f, 0f);
        grassDarkMat = CreateMaterial("Assets/Materials/GrassDark.mat", new Color(0.21f, 0.46f, 0.17f), 0f, 0.08f, 0f);
        leafMat = CreateMaterial("Assets/Materials/Leaf.mat", new Color(0.30f, 0.66f, 0.24f), 0f, 0.12f, 0f);
        leafLightMat = CreateMaterial("Assets/Materials/LeafLight.mat", new Color(0.45f, 0.78f, 0.30f), 0f, 0.12f, 0f);
        leafDeepMat = CreateMaterial("Assets/Materials/LeafDeep.mat", new Color(0.19f, 0.46f, 0.20f), 0f, 0.10f, 0f);
        pineMat = CreateMaterial("Assets/Materials/Pine.mat", new Color(0.20f, 0.48f, 0.21f), 0f, 0.10f, 0f);
        pineLightMat = CreateMaterial("Assets/Materials/PineLight.mat", new Color(0.28f, 0.58f, 0.25f), 0f, 0.10f, 0f);
        trunkMat = CreateMaterial("Assets/Materials/Trunk.mat", new Color(0.38f, 0.26f, 0.16f), 0f, 0.12f, 0f);
        hillMat = CreateMaterial("Assets/Materials/Hill.mat", new Color(0.23f, 0.49f, 0.18f), 0f, 0.08f, 0f);
        mountainMat = CreateMaterial("Assets/Materials/Mountain.mat", new Color(0.43f, 0.52f, 0.57f), 0f, 0.08f, 0f);
        mountainDarkMat = CreateMaterial("Assets/Materials/MountainDark.mat", new Color(0.33f, 0.43f, 0.49f), 0f, 0.08f, 0f);
        cloudMat = CreateMaterial("Assets/Materials/Cloud.mat", new Color(0.98f, 0.99f, 1f), 0f, 0.05f, 0f);
        rockMat = CreateMaterial("Assets/Materials/Rock.mat", new Color(0.43f, 0.43f, 0.42f), 0f, 0.12f, 0f);
        goldMat = CreateMaterial("Assets/Materials/Gold.mat", new Color(1f, 0.72f, 0.22f), 0.25f, 0.55f, 0.55f);

        hoodieMat = CreateMaterial("Assets/Materials/Hoodie.mat", new Color(0.74f, 0.13f, 0.14f), 0f, 0.22f, 0.04f);
        hoodMat = CreateMaterial("Assets/Materials/Hood.mat", new Color(0.60f, 0.10f, 0.11f), 0f, 0.2f, 0.02f);
        jeansMat = CreateMaterial("Assets/Materials/Jeans.mat", new Color(0.16f, 0.33f, 0.68f), 0f, 0.16f, 0f);
        skinMat = CreateMaterial("Assets/Materials/Skin.mat", new Color(0.93f, 0.74f, 0.59f), 0f, 0.2f, 0f);
        hairMat = CreateMaterial("Assets/Materials/Hair.mat", new Color(0.20f, 0.12f, 0.07f), 0f, 0.24f, 0f);
        shoeMat = CreateMaterial("Assets/Materials/Shoe.mat", new Color(0.93f, 0.94f, 0.95f), 0f, 0.35f, 0f);
        eyeMat = CreateMaterial("Assets/Materials/Eye.mat", Color.white, 0f, 0.4f, 0f);
        pupilMat = CreateMaterial("Assets/Materials/Pupil.mat", new Color(0.06f, 0.06f, 0.08f), 0f, 0.4f, 0f);
    }

    static Material CreateMaterial(string path, Color color, float metallic, float smoothness, float emission)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission > 0f ? color * emission : Color.black);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // =====================================================================
    // UI PRIMITIVES
    // =====================================================================
    static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 size, Vector2 pos, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;

        // Soft shadow baked into the panel so hidden overlays hide their shadow too.
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        shadow.effectDistance = new Vector2(0f, -8f);
        shadow.useGraphicAlpha = true;
        return go;
    }

    static TMP_Text CreateText(Transform parent, string name, string content, TMP_FontAsset font,
        float size, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align,
        FontStyles style, Color color, float characterSpacing = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        t.characterSpacing = characterSpacing;
        t.raycastTarget = false;
        return t;
    }

    static Image CreateBar(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image CreateSwatch(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        return CreateBar(parent, name, pos, size, color, sprite);
    }

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font,
        Vector2 pos, Vector2 size, Sprite sprite, Color bg, Color fg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = bg;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.84f, 0.88f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.font = font;
        t.fontSize = 16f;
        t.color = fg;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 1.5f;
        t.raycastTarget = false;
        return btn;
    }

    // =====================================================================
    // SPRITES
    // =====================================================================
    static Sprite CreateRoundedSprite(string path, int size, int radius, bool circle)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha;
                if (circle)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - center;
                    alpha = Mathf.Clamp01(0.5f - dist);
                }
                else
                {
                    float px = Mathf.Abs(x - center) - (center - radius);
                    float py = Mathf.Abs(y - center) - (center - radius);
                    float ox = Mathf.Max(px, 0f);
                    float oy = Mathf.Max(py, 0f);
                    float outside = Mathf.Sqrt(ox * ox + oy * oy);
                    float inside = Mathf.Min(Mathf.Max(px, py), 0f);
                    float sdf = outside + inside - radius;
                    alpha = Mathf.Clamp01(0.5f - sdf);
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        WriteTexture(path, tex, circle ? Vector4.zero : new Vector4(radius, radius, radius, radius));
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite CreateGradientSprite(string path, Color top, Color bottom)
    {
        const int w = 8;
        const int h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(h - 1));
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = c;
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        WriteTexture(path, tex, Vector4.zero);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void WriteTexture(string path, Texture2D tex, Vector4 border)
    {
        string fullPath = System.IO.Path.Combine(
            System.IO.Directory.GetParent(Application.dataPath).FullName, path);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
        System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
