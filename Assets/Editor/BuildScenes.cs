using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class BuildScenes
{
    static Material tileMat, stoneMat, stoneDarkMat, startGrassMat, endGrassMat, grassMat, grassDarkMat;
    static Material leafMat, leafLightMat, leafDeepMat, pineMat, pineLightMat, trunkMat, hillMat, mountainMat, mountainDarkMat, cloudMat, rockMat, goldMat;
    static Material hoodieMat, jeansMat, skinMat, hairMat, shoeMat, eyeMat, pupilMat, hoodMat;

    public static void Run()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Meshes");
        EnsureFolder("Assets/UI");
        EnsureFolder("Assets/UI/LevelSelect");

        ConfigureButtonTextures();
        CreateMaterials();

        var l1Data = BuildLevel1Data();
        var l2Data = BuildLevel2Data();

        BuildMainMenuScene();
        BuildLevel1Scene(l1Data);
        BuildLevel2Scene(l2Data);

        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BuildScenes finished: MainMenu, Level1, and Level2 generated and configured in Build Settings.");
    }

    static void ConfigureButtonTextures()
    {
        for (int i = 1; i <= 10; i++)
        {
            string path = string.Format("Assets/UI/LevelSelect/Btn_Level{0}.png", i);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        string fullPath = "Assets/UI/LevelSelect/LevelMenuFull.png";
        var fullImp = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (fullImp != null)
        {
            fullImp.textureType = TextureImporterType.Sprite;
            fullImp.spriteImportMode = SpriteImportMode.Single;
            fullImp.alphaIsTransparency = true;
            fullImp.SaveAndReimport();
        }
    }

    static void SetupAudioManager()
    {
        var go = new GameObject("AudioManager");
        var am = go.AddComponent<AudioManager>();
        am.rightJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/RightJump.wav");
        am.wrongJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/WrongJump.wav");
        am.level1ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level1Click.wav");
        am.level2ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level2Click.wav");
        am.levelLockedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelLocked.wav");
        am.levelCompleteClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelComplete.wav");
        am.buttonClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ButtonClick.wav");
    }

    static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level2.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
    }

    // =========================================================================
    // LEVEL DATA GENERATION
    // =========================================================================
    static LevelData BuildLevel1Data()
    {
        string path = "Assets/Data/Level1Data.asset";
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
        levelData.mapVisibleDuration = 6.0f;
        levelData.objective = "Study the mirrored PATH MAP, then hop across the dark tiles and reach the golden finish island.";
        levelData.instructions = "WASD / Arrows - Move\nSpace - Jump\nMouse Drag / Q E - Rotate camera\nMouse wheel - Zoom\nR - Restart\nESC - Level Select";
        levelData.safeTiles = new List<Vector2Int> {
            new Vector2Int(1,1), new Vector2Int(2,2), new Vector2Int(3,3),
            new Vector2Int(4,2), new Vector2Int(5,4)
        };
        levelData.sequence = new List<Vector2Int>(levelData.safeTiles);
        EditorUtility.SetDirty(levelData);
        return levelData;
    }

    static LevelData BuildLevel2Data()
    {
        string path = "Assets/Data/Level2Data.asset";
        var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(levelData, path);
        }
        levelData.levelName = "Level 2";
        levelData.sceneName = "Level2";
        levelData.rows = 6;
        levelData.columns = 5;
        levelData.tileSize = 2.4f;
        levelData.tileGap = 0.6f;
        levelData.timeLimit = 120f;
        levelData.hideMapAfterCountdown = true;
        levelData.mapVisibleDuration = 6.0f;
        levelData.objective = "MEMORIZE THE PATH! The map is visible for 6 seconds, then conceals. Walk the exact path from memory to clear the level.";
        levelData.instructions = "WASD / Arrows - Move\nSpace - Jump\nMouse Drag / Q E - Rotate camera\nMouse wheel - Zoom\nR - Restart\nESC - Level Select";
        
        // 9-step winding path across 6x5 grid
        levelData.safeTiles = new List<Vector2Int> {
            new Vector2Int(1, 2),
            new Vector2Int(2, 2),
            new Vector2Int(2, 4),
            new Vector2Int(3, 4),
            new Vector2Int(4, 4),
            new Vector2Int(4, 2),
            new Vector2Int(5, 2),
            new Vector2Int(5, 3),
            new Vector2Int(6, 3)
        };
        levelData.sequence = new List<Vector2Int>(levelData.safeTiles);
        EditorUtility.SetDirty(levelData);
        return levelData;
    }

    // =========================================================================
    // MAIN MENU SCENE
    // =========================================================================
    static void BuildMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        var rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PanelRounded.png");
        var dot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dot.png");

        // Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);

        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var menuManager = canvasGo.AddComponent<MainMenuManager>();
        menuManager.levelButtons = new Button[10];

        // Background subtle vignette/card
        var bgPanel = CreatePanel(canvasGo.transform, "MenuCard",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(860f, 920f), Vector2.zero, rounded,
            new Color(0.09f, 0.11f, 0.14f, 0.94f));

        // Header Title
        CreateText(bgPanel.transform, "Title", "SELECT LEVEL", font, 48f,
            new Vector2(0f, 380f), new Vector2(760f, 60f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white, 3f);

        CreateText(bgPanel.transform, "Subtitle", "CHOOSE A STAGE TO BEGIN YOUR MEMORY RUN", font, 14f,
            new Vector2(0f, 332f), new Vector2(760f, 28f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.40f, 0.90f, 0.55f, 1f), 2.5f);

        CreateBar(bgPanel.transform, "Divider", new Vector2(0f, 305f), new Vector2(740f, 2f),
            new Color(1f, 1f, 1f, 0.12f), rounded);

        // Button Grid Container
        var gridGo = new GameObject("ButtonGrid", typeof(RectTransform));
        gridGo.transform.SetParent(bgPanel.transform, false);
        var grt = gridGo.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.5f, 0.5f);
        grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(680f, 520f);
        grt.anchoredPosition = new Vector2(0f, 20f);

        float colWidth = 300f;
        float btnHeight = 84f;
        float gapX = 40f;
        float gapY = 18f;

        float startX = -((colWidth * 2f + gapX) * 0.5f) + (colWidth * 0.5f);
        float startY = 180f;

        for (int i = 0; i < 10; i++)
        {
            int col = i % 2;
            int row = i / 2;
            int levelNum = i + 1;

            float x = startX + col * (colWidth + gapX);
            float y = startY - row * (btnHeight + gapY);

            string spritePath = string.Format("Assets/UI/LevelSelect/Btn_Level{0}.png", levelNum);
            var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var btnGo = new GameObject(string.Format("Btn_Level{0}", levelNum), typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(gridGo.transform, false);
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(colWidth, btnHeight);
            brt.anchoredPosition = new Vector2(x, y);

            var img = btnGo.GetComponent<Image>();
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            else
            {
                img.sprite = rounded;
                img.color = new Color(0.85f, 0.45f, 0.15f);
            }

            var btn = btnGo.GetComponent<Button>();
            var colors = btn.colors;
            if (levelNum <= 2)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
            }
            else
            {
                // Locked levels visually dimmed
                colors.normalColor = new Color(0.65f, 0.65f, 0.65f, 0.60f);
                colors.highlightedColor = new Color(0.80f, 0.80f, 0.80f, 0.85f);
                colors.pressedColor = new Color(0.50f, 0.50f, 0.50f, 0.80f);
                colors.selectedColor = colors.normalColor;

                // Lock icon text tag
                var lockGo = new GameObject("LockTag");
                lockGo.transform.SetParent(btnGo.transform, false);
                var lrt = lockGo.AddComponent<RectTransform>();
                lrt.anchorMin = new Vector2(1f, 0.5f);
                lrt.anchorMax = new Vector2(1f, 0.5f);
                lrt.pivot = new Vector2(1f, 0.5f);
                lrt.sizeDelta = new Vector2(80f, 26f);
                lrt.anchoredPosition = new Vector2(-15f, 0f);
                var lt = lockGo.AddComponent<TextMeshProUGUI>();
                lt.text = "LOCKED";
                lt.font = font;
                lt.fontSize = 11f;
                lt.fontStyle = FontStyles.Bold;
                lt.color = new Color(1f, 0.82f, 0.40f, 0.85f);
                lt.alignment = TextAlignmentOptions.Right;
                lt.raycastTarget = false;
            }
            btn.colors = colors;
            menuManager.levelButtons[i] = btn;
        }

        // Footer Instructions / Info
        CreateText(bgPanel.transform, "Footer", "LEVEL 1: Path Finder   |   LEVEL 2: 6s Memory Conceal Challenge", font, 13f,
            new Vector2(0f, -370f), new Vector2(740f, 32f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.70f, 0.75f, 0.80f, 1f));

        // Toast Popup for locked levels
        var toast = CreatePanel(canvasGo.transform, "ToastPanel",
            new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.5f),
            new Vector2(460f, 70f), Vector2.zero, rounded,
            new Color(0.12f, 0.05f, 0.06f, 0.96f));
        var toastTxt = CreateText(toast.transform, "ToastText", "", font, 16f,
            Vector2.zero, new Vector2(420f, 50f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(1f, 0.45f, 0.40f, 1f));
        menuManager.toastPanel = toast;
        menuManager.toastText = toastTxt;
        toast.SetActive(false);

        // Event System
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // GameManager persistent object in MainMenu
        SetupAudioManager();
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var l1 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset");
        var l2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        gm.levels = new LevelData[] { l1, l2 };

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // =========================================================================
    // LEVEL 1 SCENE
    // =========================================================================
    static void BuildLevel1Scene(LevelData levelData)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var gridRoot = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.safeMat = tileMat;
        gridManager.unsafeMat = tileMat;
        gridManager.tileEdgeMat = stoneMat;
        gridManager.tileRockMat = stoneDarkMat;

        float stride = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;

        var startIsland = CreateIsland("StartIsland", null, new Vector3(0f, 0.11f, -gridDepth * 0.5f - 3.5f),
            5.2f, 4.2f, startGrassMat, stoneMat, stoneDarkMat, true);
        var endIsland = CreateIsland("EndIsland", null, new Vector3(0f, 0.11f, gridDepth * 0.5f + 3.5f),
            5.2f, 4.2f, endGrassMat, stoneMat, stoneDarkMat, true);

        var pad = CreateBeveledObject("GoalPad", endIsland.transform,
            new Vector3(0f, 0.055f, 0f),
            new Vector3(2.2f, 0.11f, 2.2f), 0.05f, goldMat, false);
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.3f, 0f), new Vector3(0.55f, 0.55f, 0.55f), goldMat, false);
        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size = new Vector3(4.4f, 3.2f, 3.4f);
        endBox.isTrigger = true;
        endTrigger.AddComponent<EndPlatformTrigger>();

        gridManager.Build(levelData);

        var player = BuildPlayer(startIsland.transform.position + Vector3.up * 1.6f);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam.fieldOfView = 52f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 600f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.transform.position = new Vector3(0f, 11.5f, -19f);

        var cf = camGo.AddComponent<CameraFollow>();
        cf.target = player.transform;
        cf.distance = 13f;
        cf.pitch = 40f;
        cf.yaw = 0f;
        cf.targetHeight = 1.35f;
        cf.lookAhead = 4.0f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        ConfigureLighting();
        ConfigurePostProcessing();

        var envRoot = new GameObject("Environment").transform;
        var cloudRoot = BuildEnvironment(envRoot);

        var lmGo = new GameObject("LevelManager");
        var lm = lmGo.AddComponent<LevelManager>();
        lm.data = levelData;
        lm.gridManager = gridManager;
        lm.player = player.GetComponent<PlayerController>();
        lm.startPlatform = startIsland.transform;
        lm.endPlatform = endIsland.transform;

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var l1 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset");
        var l2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        gm.levels = new LevelData[] { l1, l2 };
        gm.currentLevelIndex = 0;

        BuildUI(lm, levelData);
        SetupAudioManager();

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var animator = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = cloudRoot;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Level1.unity");
    }

    // =========================================================================
    // LEVEL 2 SCENE
    // =========================================================================
    static void BuildLevel2Scene(LevelData levelData)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var gridRoot = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.safeMat = tileMat;
        gridManager.unsafeMat = tileMat;
        gridManager.tileEdgeMat = stoneMat;
        gridManager.tileRockMat = stoneDarkMat;

        float stride = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;

        // Start and finish platforms scaled to 6x5 grid
        var startIsland = CreateIsland("StartIsland", null, new Vector3(0f, 0.11f, -gridDepth * 0.5f - 3.8f),
            6.2f, 4.4f, startGrassMat, stoneMat, stoneDarkMat, true);
        var endIsland = CreateIsland("EndIsland", null, new Vector3(0f, 0.11f, gridDepth * 0.5f + 3.8f),
            6.2f, 4.4f, endGrassMat, stoneMat, stoneDarkMat, true);

        var pad = CreateBeveledObject("GoalPad", endIsland.transform,
            new Vector3(0f, 0.055f, 0f),
            new Vector3(2.4f, 0.11f, 2.4f), 0.05f, goldMat, false);
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.4f, 0f), new Vector3(0.6f, 0.6f, 0.6f), goldMat, false);
        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size = new Vector3(5.2f, 3.4f, 3.6f);
        endBox.isTrigger = true;
        endTrigger.AddComponent<EndPlatformTrigger>();

        gridManager.Build(levelData);

        var player = BuildPlayer(startIsland.transform.position + Vector3.up * 1.6f);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam.fieldOfView = 54f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 600f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.transform.position = new Vector3(0f, 13f, -22f);

        var cf = camGo.AddComponent<CameraFollow>();
        cf.target = player.transform;
        cf.distance = 15f;
        cf.pitch = 42f;
        cf.yaw = 0f;
        cf.targetHeight = 1.35f;
        cf.lookAhead = 4.2f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        ConfigureLighting();
        ConfigurePostProcessing();

        var envRoot = new GameObject("Environment").transform;
        var cloudRoot = BuildEnvironment(envRoot);

        var lmGo = new GameObject("LevelManager");
        var lm = lmGo.AddComponent<LevelManager>();
        lm.data = levelData;
        lm.gridManager = gridManager;
        lm.player = player.GetComponent<PlayerController>();
        lm.startPlatform = startIsland.transform;
        lm.endPlatform = endIsland.transform;

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var l1 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset");
        var l2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        gm.levels = new LevelData[] { l1, l2 };
        gm.currentLevelIndex = 1;

        BuildUI(lm, levelData);
        SetupAudioManager();

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var animator = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = cloudRoot;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Level2.unity");
    }

    // =========================================================================
    // UI CONSTRUCTION FOR GAME LEVELS
    // =========================================================================
    static void BuildUI(LevelManager lm, LevelData data)
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

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        ui.font = font;

        var rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PanelRounded.png");
        var dot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Dot.png");
        ui.roundedSprite = rounded;
        ui.dotSprite = dot;

        // ================= LEFT PANEL : MAP =================
        var mapPanel = CreatePanel(canvasGo.transform, "MapPanel",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(360f, 840f), new Vector2(28f, 0f), rounded,
            new Color(0.045f, 0.055f, 0.065f, 0.90f));

        CreateText(mapPanel.transform, "MapTitle", "PATH MAP", font, 30f,
            new Vector2(0f, 360f), new Vector2(312f, 40f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        // Map Countdown / Status Header
        ui.mapStatusText = CreateText(mapPanel.transform, "MapStatus",
            data.hideMapAfterCountdown ? "MEMORIZE PATH: 6.0s" : "LIVE PATH GUIDANCE",
            font, 13f, new Vector2(0f, 324f), new Vector2(312f, 24f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.40f, 0.95f, 0.55f, 1f), 1.5f);

        CreateBar(mapPanel.transform, "MapDivider", new Vector2(0f, 298f), new Vector2(312f, 2f),
            new Color(1f, 1f, 1f, 0.11f), rounded);

        CreateText(mapPanel.transform, "FinishTag", "FINISH", font, 12f,
            new Vector2(0f, 226f), new Vector2(312f, 18f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.96f, 0.78f, 0.36f, 1f), 3f);

        var mapContainerGo = new GameObject("MapContainer", typeof(RectTransform));
        mapContainerGo.transform.SetParent(mapPanel.transform, false);
        var mapRect = mapContainerGo.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(312f, 420f);
        mapRect.anchoredPosition = new Vector2(0f, 10f);
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

        CreateText(mapPanel.transform, "StartTag", "START", font, 12f,
            new Vector2(0f, -204f), new Vector2(312f, 18f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.72f, 0.78f, 0.84f, 1f), 3f);

        var safeSwatch = CreateSwatch(mapPanel.transform, "SafeSwatch", new Vector2(-92f, -250f),
            new Vector2(17f, 17f), new Color(0.24f, 0.90f, 0.44f), rounded);
        CreateText(mapPanel.transform, "SafeLabel", "SAFE", font, 12.5f,
            new Vector2(-45f, -250f), new Vector2(70f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.85f, 0.89f, 1f), 1.5f);
        CreateSwatch(mapPanel.transform, "UnsafeSwatch", new Vector2(24f, -250f),
            new Vector2(17f, 17f), new Color(0.155f, 0.165f, 0.185f), rounded);
        CreateText(mapPanel.transform, "UnsafeLabel", "UNSAFE", font, 12.5f,
            new Vector2(80f, -250f), new Vector2(90f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.85f, 0.89f, 1f), 1.5f);

        ui.mapHintText = CreateText(mapPanel.transform, "MapHint",
            data.hideMapAfterCountdown ? "Memorize before the path conceals!" : "The map mirrors what you see in the course.",
            font, 12f, new Vector2(0f, -300f), new Vector2(312f, 34f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.64f, 0.69f, 0.74f, 1f));

        // ================= RIGHT PANEL : LEVEL INFO =================
        var infoPanel = CreatePanel(canvasGo.transform, "InfoPanel",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(440f, 880f), new Vector2(-28f, 0f), rounded,
            new Color(0.045f, 0.055f, 0.065f, 0.90f));

        ui.levelTitle = CreateText(infoPanel.transform, "LevelTitle", data.levelName.ToUpper(), font, 44f,
            new Vector2(0f, 384f), new Vector2(384f, 54f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);
        ui.subtitle = CreateText(infoPanel.transform, "Subtitle",
            data.hideMapAfterCountdown ? "MEMORY CHALLENGE" : "PATH FINDER", font, 18f,
            new Vector2(0f, 344f), new Vector2(384f, 26f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.33f, 0.92f, 0.50f, 1f), 5f);
        CreateBar(infoPanel.transform, "InfoDivider", new Vector2(0f, 316f), new Vector2(384f, 2f),
            new Color(1f, 1f, 1f, 0.11f), rounded);

        CreateText(infoPanel.transform, "ObjectiveLabel", "OBJECTIVE", font, 12f,
            new Vector2(0f, 290f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.66f, 0.71f, 0.76f, 1f), 2.5f);
        ui.objectiveText = CreateText(infoPanel.transform, "Objective", data.objective, font, 15f,
            new Vector2(0f, 240f), new Vector2(384f, 76f), TextAlignmentOptions.TopLeft,
            FontStyles.Normal, new Color(0.86f, 0.89f, 0.92f, 1f));

        CreateText(infoPanel.transform, "ProgressLabel", "PROGRESS", font, 12f,
            new Vector2(0f, 166f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.66f, 0.71f, 0.76f, 1f), 2.5f);
        ui.progressText = CreateText(infoPanel.transform, "Progress",
            string.Format("0 <size=22><color=#7C8794>/ {0}</color></size>", data.sequence.Count),
            font, 40f, new Vector2(0f, 126f), new Vector2(384f, 48f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.40f, 1f, 0.55f, 1f));

        var progressBg = CreateBar(infoPanel.transform, "ProgressBg", new Vector2(0f, 96f),
            new Vector2(384f, 10f), new Color(1f, 1f, 1f, 0.10f), rounded);
        var fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(progressBg.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
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

        CreateText(infoPanel.transform, "TimeLabel", "TIME REMAINING", font, 12f,
            new Vector2(0f, 62f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.66f, 0.71f, 0.76f, 1f), 2.5f);
        ui.timerText = CreateText(infoPanel.transform, "Timer", "120.0s", font, 32f,
            new Vector2(0f, 28f), new Vector2(384f, 40f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        CreateBar(infoPanel.transform, "InfoDivider2", new Vector2(0f, -12f), new Vector2(384f, 2f),
            new Color(1f, 1f, 1f, 0.11f), rounded);

        CreateText(infoPanel.transform, "ControlsLabel", "CONTROLS", font, 12f,
            new Vector2(0f, -38f), new Vector2(384f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.66f, 0.71f, 0.76f, 1f), 2.5f);
        ui.instructionsText = CreateText(infoPanel.transform, "Controls",
            "<b>WASD / ARROWS</b>     Move\n<b>SPACE</b>     Jump\n<b>MOUSE DRAG / Q E</b>     Rotate camera\n<b>MOUSE WHEEL</b>     Zoom\n<b>R</b>     Restart level",
            font, 14f, new Vector2(0f, -112f), new Vector2(384f, 125f),
            TextAlignmentOptions.TopLeft, FontStyles.Normal, new Color(0.68f, 0.73f, 0.78f, 1f));
        ui.instructionsText.lineSpacing = 10f;

        // Action Buttons: Restart & Menu
        var restartBtn = CreateButton(infoPanel.transform, "RestartButton", "RESTART LEVEL (R)",
            font, new Vector2(0f, -225f), new Vector2(384f, 50f), rounded,
            new Color(0.30f, 0.86f, 0.45f, 1f), new Color(0.03f, 0.09f, 0.05f, 1f));
        restartBtn.onClick.AddListener(() => lm.RestartLevel());
        ui.restartButton = restartBtn;

        var menuBtn = CreateButton(infoPanel.transform, "MenuButton", "LEVEL SELECT MENU",
            font, new Vector2(0f, -285f), new Vector2(384f, 50f), rounded,
            new Color(0.20f, 0.24f, 0.28f, 1f), new Color(0.85f, 0.90f, 0.95f, 1f));
        menuBtn.onClick.AddListener(() => lm.LoadMainMenu());
        ui.menuButton = menuBtn;

        CreateText(infoPanel.transform, "FooterHint", "Reach the golden finish island with every safe tile cleared.", font, 12f,
            new Vector2(0f, -345f), new Vector2(384f, 30f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.64f, 0.69f, 0.74f, 1f));

        // ================= OVERLAYS =================
        var complete = CreatePanel(canvasGo.transform, "LevelCompletePanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(580f, 380f), Vector2.zero, rounded,
            new Color(0.035f, 0.075f, 0.055f, 0.96f));
        CreateText(complete.transform, "CompleteTitle", "LEVEL COMPLETE!", font, 40f,
            new Vector2(0f, 115f), new Vector2(500f, 52f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.42f, 1f, 0.56f, 1f));
        CreateText(complete.transform, "CompleteSub",
            data.sceneName == "Level2" ? "Brilliant memory run! You cleared the challenge." : "Safe path mastered — beautiful work.",
            font, 16f, new Vector2(0f, 70f), new Vector2(500f, 26f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.74f, 0.80f, 0.84f, 1f));
        ui.levelCompleteTime = CreateText(complete.transform, "CompleteTime", "", font, 20f,
            new Vector2(0f, 28f), new Vector2(500f, 32f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white);

        string nextBtnLabel = data.sceneName == "Level1" ? "NEXT LEVEL (LEVEL 2)" : "REPLAY LEVEL";
        var nextBtn = CreateButton(complete.transform, "NextLevelButton", nextBtnLabel,
            font, new Vector2(0f, -50f), new Vector2(340f, 54f), rounded,
            new Color(0.30f, 0.86f, 0.45f, 1f), new Color(0.03f, 0.09f, 0.05f, 1f));
        if (data.sceneName == "Level1")
            nextBtn.onClick.AddListener(() => lm.LoadNextLevel());
        else
            nextBtn.onClick.AddListener(() => lm.RestartLevel());
        ui.nextLevelButton = nextBtn;

        var compMenuBtn = CreateButton(complete.transform, "CompleteMenuButton", "LEVEL SELECT MENU",
            font, new Vector2(0f, -114f), new Vector2(340f, 50f), rounded,
            new Color(0.18f, 0.22f, 0.26f, 1f), new Color(0.85f, 0.90f, 0.95f, 1f));
        compMenuBtn.onClick.AddListener(() => lm.LoadMainMenu());
        ui.completeMenuButton = compMenuBtn;

        complete.SetActive(false);
        ui.levelCompletePanel = complete;

        var fail = CreatePanel(canvasGo.transform, "FailPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(480f, 210f), Vector2.zero, rounded,
            new Color(0.105f, 0.025f, 0.030f, 0.97f));
        CreateText(fail.transform, "FailTitle", "WRONG TILE!", font, 34f,
            new Vector2(0f, 34f), new Vector2(430f, 46f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(1f, 0.32f, 0.30f, 1f));
        CreateText(fail.transform, "FailSub", "Restarting course...", font, 15f,
            new Vector2(0f, -14f), new Vector2(430f, 24f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.82f, 0.80f, 0.82f, 1f));
        fail.SetActive(false);
        ui.failPanel = fail;
    }

    // =========================================================================
    // 3D OBJECT HELPERS
    // =========================================================================
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
                new Vector3(side * 0.28f, 0.08f, 0.43f), new Vector3(0.20f, 0.22f, 0.10f), eyeMat);
            AddPart(PrimitiveType.Sphere, side < 0 ? "PupilL" : "PupilR", head.transform,
                new Vector3(side * 0.28f, 0.08f, 0.50f), new Vector3(0.085f, 0.095f, 0.05f), pupilMat);
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

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Material mat, bool addCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
        if (!addCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }
        return go;
    }

    static Transform BuildEnvironment(Transform root)
    {
        const float groundY = -30f;

        var ground = CreatePrimitive(PrimitiveType.Plane, "Ground", root,
            new Vector3(0f, groundY, 0f), new Vector3(30f, 1f, 30f), grassMat, false);
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

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
        light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Skybox.mat");
        if (sky != null) RenderSettings.skybox = sky;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.71f, 0.84f, 0.96f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0045f;
        DynamicGI.UpdateEnvironment();
    }

    static void ConfigurePostProcessing()
    {
        const string profilePath = "Assets/Settings/GameVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile != null)
        {
            var go = new GameObject("GlobalVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = profile;
        }
    }

    // =========================================================================
    // UI PRIMITIVES
    // =========================================================================
    static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 size, Vector2 pos, Sprite sprite, Color color)
    {
        var shadow = new GameObject(name + "_Shadow", typeof(RectTransform), typeof(Image));
        shadow.transform.SetParent(parent, false);
        var srt = shadow.GetComponent<RectTransform>();
        srt.anchorMin = anchor;
        srt.anchorMax = anchor;
        srt.pivot = pivot;
        srt.sizeDelta = size;
        srt.anchoredPosition = pos + new Vector2(0f, -10f);
        var simg = shadow.GetComponent<Image>();
        simg.sprite = sprite;
        simg.type = Image.Type.Sliced;
        simg.color = new Color(0f, 0f, 0f, 0.35f);
        simg.raycastTarget = false;

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

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font,
        Vector2 pos, Vector2 size, Sprite sprite, Color bgColor, Color textColor)
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
        img.color = bgColor;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
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
        t.color = textColor;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 1.5f;
        t.raycastTarget = false;
        return btn;
    }

    static void CreateMaterials()
    {
        tileMat = LoadOrCreateMaterial("Assets/Materials/Tile.mat", new Color(0.105f, 0.112f, 0.122f), 0.05f, 0.16f, 0f);
        stoneMat = LoadOrCreateMaterial("Assets/Materials/Stone.mat", new Color(0.50f, 0.49f, 0.47f), 0f, 0.16f, 0f);
        stoneDarkMat = LoadOrCreateMaterial("Assets/Materials/StoneDark.mat", new Color(0.32f, 0.32f, 0.31f), 0f, 0.12f, 0f);
        startGrassMat = LoadOrCreateMaterial("Assets/Materials/GrassStart.mat", new Color(0.38f, 0.73f, 0.27f), 0f, 0.18f, 0f);
        endGrassMat = LoadOrCreateMaterial("Assets/Materials/GrassEnd.mat", new Color(0.41f, 0.76f, 0.29f), 0f, 0.18f, 0f);
        grassMat = LoadOrCreateMaterial("Assets/Materials/Grass.mat", new Color(0.29f, 0.58f, 0.22f), 0f, 0.10f, 0f);
        grassDarkMat = LoadOrCreateMaterial("Assets/Materials/GrassDark.mat", new Color(0.21f, 0.46f, 0.17f), 0f, 0.08f, 0f);
        leafMat = LoadOrCreateMaterial("Assets/Materials/Leaf.mat", new Color(0.30f, 0.66f, 0.24f), 0f, 0.12f, 0f);
        leafLightMat = LoadOrCreateMaterial("Assets/Materials/LeafLight.mat", new Color(0.45f, 0.78f, 0.30f), 0f, 0.12f, 0f);
        leafDeepMat = LoadOrCreateMaterial("Assets/Materials/LeafDeep.mat", new Color(0.19f, 0.46f, 0.20f), 0f, 0.10f, 0f);
        pineMat = LoadOrCreateMaterial("Assets/Materials/Pine.mat", new Color(0.20f, 0.48f, 0.21f), 0f, 0.10f, 0f);
        pineLightMat = LoadOrCreateMaterial("Assets/Materials/PineLight.mat", new Color(0.28f, 0.58f, 0.25f), 0f, 0.10f, 0f);
        trunkMat = LoadOrCreateMaterial("Assets/Materials/Trunk.mat", new Color(0.38f, 0.26f, 0.16f), 0f, 0.12f, 0f);
        hillMat = LoadOrCreateMaterial("Assets/Materials/Hill.mat", new Color(0.23f, 0.49f, 0.18f), 0f, 0.08f, 0f);
        mountainMat = LoadOrCreateMaterial("Assets/Materials/Mountain.mat", new Color(0.43f, 0.52f, 0.57f), 0f, 0.08f, 0f);
        mountainDarkMat = LoadOrCreateMaterial("Assets/Materials/MountainDark.mat", new Color(0.33f, 0.43f, 0.49f), 0f, 0.08f, 0f);
        cloudMat = LoadOrCreateMaterial("Assets/Materials/Cloud.mat", new Color(0.98f, 0.99f, 1f), 0f, 0.05f, 0f);
        rockMat = LoadOrCreateMaterial("Assets/Materials/Rock.mat", new Color(0.43f, 0.43f, 0.42f), 0f, 0.12f, 0f);
        goldMat = LoadOrCreateMaterial("Assets/Materials/Gold.mat", new Color(1f, 0.72f, 0.22f), 0.25f, 0.55f, 0.55f);

        hoodieMat = LoadOrCreateMaterial("Assets/Materials/Hoodie.mat", new Color(0.74f, 0.13f, 0.14f), 0f, 0.22f, 0.04f);
        hoodMat = LoadOrCreateMaterial("Assets/Materials/Hood.mat", new Color(0.60f, 0.10f, 0.11f), 0f, 0.2f, 0.02f);
        jeansMat = LoadOrCreateMaterial("Assets/Materials/Jeans.mat", new Color(0.16f, 0.33f, 0.68f), 0f, 0.16f, 0f);
        skinMat = LoadOrCreateMaterial("Assets/Materials/Skin.mat", new Color(0.93f, 0.74f, 0.59f), 0f, 0.2f, 0f);
        hairMat = LoadOrCreateMaterial("Assets/Materials/Hair.mat", new Color(0.20f, 0.12f, 0.07f), 0f, 0.24f, 0f);
        shoeMat = LoadOrCreateMaterial("Assets/Materials/Shoe.mat", new Color(0.93f, 0.94f, 0.95f), 0f, 0.35f, 0f);
        eyeMat = LoadOrCreateMaterial("Assets/Materials/Eye.mat", Color.white, 0f, 0.4f, 0f);
        pupilMat = LoadOrCreateMaterial("Assets/Materials/Pupil.mat", new Color(0.06f, 0.06f, 0.08f), 0f, 0.4f, 0f);
    }

    static Material LoadOrCreateMaterial(string path, Color baseColor, float metallic, float smoothness, float emission)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
