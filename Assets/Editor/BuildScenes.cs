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
        TuneFontMaterial();

        var l1Data = BuildLevel1Data();
        var l2Data = BuildLevel2Data();
        var l3Data = BuildLevel3Data();

        BuildMainMenuScene();
        BuildLevel1Scene(l1Data);
        BuildLevel2.Run();
        BuildLevel3.Run();

        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BuildScenes finished: MainMenu, Level1, Level2, and Level3 generated with matching stylized nature theme.");
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
    }

    static void TuneFontMaterial()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        if (font == null) return;
        var mat = font.material;
        if (mat == null) return;

        var shader = Shader.Find("TextMeshPro/Distance Field");
        if (shader != null) mat.shader = shader;

        if (font.atlasTexture != null)
        {
            mat.SetTexture("_MainTex", font.atlasTexture);
            mat.SetFloat("_TextureWidth", font.atlasTexture.width);
            mat.SetFloat("_TextureHeight", font.atlasTexture.height);
        }

        mat.SetFloat("_GradientScale", 10f);
        mat.SetFloat("_ScaleRatioA", 1f);
        mat.SetFloat("_Sharpness", 0.25f);
        mat.SetFloat("_FaceDilate", 0.06f);
        mat.SetFloat("_OutlineWidth", 0.08f);
        mat.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0.85f));

        EditorUtility.SetDirty(mat);
        EditorUtility.SetDirty(font);
    }

    static void SetupAudioManager()
    {
        var go = new GameObject("AudioManager");
        var am = go.AddComponent<AudioManager>();
        am.rightJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/RightJump.wav");
        am.wrongJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/WrongJump.wav");
        am.level1ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level1Click.wav");
        am.level2ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level2Click.wav");
        am.level3ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level3Click.wav");
        am.levelLockedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelLocked.wav");
        am.levelCompleteClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelComplete.wav");
        am.buttonClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ButtonClick.wav");
        am.bgMusicClips = new AudioClip[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic2.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic3.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic4.wav")
        };
        am.ambientClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambient.wav");
    }

    static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level2.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level3.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level4.unity", true)
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
        levelData.isUpsideDownPattern = false;
        levelData.objective = "Study the mirrored PATH MAP, then hop across the safe stone tiles to reach the golden finish island.";
        levelData.instructions = "WASD / Arrows - Move\nSpace - Jump\nMouse Drag / Q E - Rotate camera\nMouse Wheel - Zoom\nR - Restart\nESC - Level Select Menu";
        levelData.safeTiles = new List<Vector2Int> {
            new Vector2Int(1,1), new Vector2Int(2,2), new Vector2Int(3,3),
            new Vector2Int(4,2), new Vector2Int(5,4)
        };
        levelData.sequence = new List<Vector2Int>(levelData.safeTiles);
        levelData.displayedPattern = new List<Vector2Int>(levelData.safeTiles);
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
        levelData.isUpsideDownPattern = false;
        levelData.objective = "MEMORIZE THE PATH! The map is visible for 6 seconds, then conceals. Walk the exact path from memory to clear the level.";
        levelData.instructions = "WASD / Arrows - Move\nSpace - Jump\nMouse Drag / Q E - Rotate camera\nMouse Wheel - Zoom\nR - Restart\nESC - Level Select Menu";
        
        // 6-step path across 6x5 grid (matches Level2Data.asset)
        levelData.safeTiles = new List<Vector2Int> {
            new Vector2Int(1, 2),
            new Vector2Int(2, 4),
            new Vector2Int(3, 4),
            new Vector2Int(4, 4),
            new Vector2Int(5, 3),
            new Vector2Int(6, 3)
        };
        levelData.sequence = new List<Vector2Int>(levelData.safeTiles);
        levelData.displayedPattern = new List<Vector2Int>(levelData.safeTiles);
        EditorUtility.SetDirty(levelData);
        return levelData;
    }

    static LevelData BuildLevel3Data()
    {
        string path = "Assets/Data/Level3Data.asset";
        var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(levelData, path);
        }
        levelData.levelName = "Level 3";
        levelData.sceneName = "Level3";
        levelData.rows = 6;
        levelData.columns = 5;
        levelData.tileSize = 2.4f;
        levelData.tileGap = 0.6f;
        levelData.timeLimit = 120f;
        levelData.hideMapAfterCountdown = true;
        levelData.mapVisibleDuration = 8.0f;
        levelData.isUpsideDownPattern = true;
        levelData.objective = "UPSIDE-DOWN MEMORY! Study the blueprint for 8s. The physical rows are INVERTED: the LAST row on the blueprint must be jumped FIRST!";
        levelData.instructions = "WASD / Arrows - Move\nSpace - Jump\nMouse Drag / Q E - Rotate camera\nMouse Wheel - Zoom\nR - Restart\nESC - Level Select Menu\n<b>RULE:</b> Blueprint Top Row = Arena 1st Jump!";

        // Blueprint displayed pattern (shown on the map panel during the 8s countdown)
        // Row 6 (Top, nearest Goal on blueprint): (6, 4)
        // Row 5: (5, 4), (5, 3)
        // Row 4: (4, 3)
        // Row 3: (3, 3), (3, 2)
        // Row 2: (2, 2)
        // Row 1 (Bottom, nearest Start on blueprint): (1, 2)
        levelData.displayedPattern = new List<Vector2Int> {
            new Vector2Int(6, 4),
            new Vector2Int(5, 4),
            new Vector2Int(5, 3),
            new Vector2Int(4, 3),
            new Vector2Int(3, 3),
            new Vector2Int(3, 2),
            new Vector2Int(2, 2),
            new Vector2Int(1, 2)
        };

        // Physical course safe tiles & jump sequence:
        // Inverted rows: Arena Row = 7 - Blueprint Row!
        // Arena Row 1 (JUMP 1ST): Takes Blueprint Row 6 -> (1, 4)
        // Arena Row 2 (JUMP 2ND): Takes Blueprint Row 5 -> (2, 4) -> (2, 3)
        // Arena Row 3 (JUMP 3RD): Takes Blueprint Row 4 -> (3, 3)
        // Arena Row 4 (JUMP 4TH): Takes Blueprint Row 3 -> (4, 3) -> (4, 2)
        // Arena Row 5 (JUMP 5TH): Takes Blueprint Row 2 -> (5, 2)
        // Arena Row 6 (JUMP 6TH / LAST): Takes Blueprint Row 1 -> (6, 2)
        levelData.safeTiles = new List<Vector2Int> {
            new Vector2Int(1, 4),
            new Vector2Int(2, 4),
            new Vector2Int(2, 3),
            new Vector2Int(3, 3),
            new Vector2Int(4, 3),
            new Vector2Int(4, 2),
            new Vector2Int(5, 2),
            new Vector2Int(6, 2)
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

        // Camera with Skybox and view of the floating island paradise
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.transform.position = new Vector3(0f, 6.5f, -14f);
        cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

        ConfigureLighting();
        ConfigurePostProcessing();

        var envRoot = new GameObject("Environment").transform;
        var cloudRoot = BuildEnvironment(envRoot);
        var animator = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = cloudRoot;

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

        // Background subtle card letting the floating islands show through
        var bgPanel = CreatePanel(canvasGo.transform, "MenuCard",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(880f, 900f), Vector2.zero, rounded,
            new Color(0.04f, 0.065f, 0.09f, 0.90f));

        CreateText(bgPanel.transform, "Title", "SELECT LEVEL", font, 48f,
            new Vector2(0f, 390f), new Vector2(780f, 60f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white);

        CreateText(bgPanel.transform, "Subtitle", "CHOOSE A STAGE TO BEGIN YOUR MEMORY RUN", font, 16f,
            new Vector2(0f, 342f), new Vector2(780f, 28f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.35f, 0.92f, 0.55f, 1f));

        CreateBar(bgPanel.transform, "Divider", new Vector2(0f, 315f), new Vector2(780f, 2f),
            new Color(1f, 1f, 1f, 0.14f), rounded);

        // Button Grid Container
        var gridGo = new GameObject("ButtonGrid", typeof(RectTransform));
        gridGo.transform.SetParent(bgPanel.transform, false);
        var grt = gridGo.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.5f, 0.5f);
        grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(740f, 560f);
        grt.anchoredPosition = new Vector2(0f, 12f);

        float colWidth = 330f;
        float btnHeight = 92f;
        float gapX = 36f;
        float gapY = 16f;

        float startX = -((colWidth * 2f + gapX) * 0.5f) + (colWidth * 0.5f);
        float startY = 195f;

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
                img.color = new Color(0.18f, 0.75f, 0.35f);
            }

            var btn = btnGo.GetComponent<Button>();
            var colors = btn.colors;
            if (levelNum <= 5)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
                colors.selectedColor = Color.white;
            }
            else
            {
                colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 0.85f);
                colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 0.90f);
                colors.pressedColor = new Color(0.70f, 0.70f, 0.70f, 0.85f);
                colors.selectedColor = colors.normalColor;
            }
            btn.colors = colors;
            menuManager.levelButtons[i] = btn;
        }

        // Footer Instructions / Info
        CreateText(bgPanel.transform, "Footer", "LEVEL 1: Path Finder   |   LEVEL 2: 6s Memory Conceal   |   LEVEL 3: Upside-Down Memory Run", font, 14f,
            new Vector2(0f, -380f), new Vector2(780f, 34f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.82f, 0.88f, 0.94f, 1f));

        // Toast Popup for locked levels
        var toast = CreatePanel(canvasGo.transform, "ToastPanel",
            new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.5f),
            new Vector2(500f, 76f), Vector2.zero, rounded,
            new Color(0.12f, 0.05f, 0.06f, 0.97f));
        var toastTxt = CreateText(toast.transform, "ToastText", "", font, 18f,
            Vector2.zero, new Vector2(460f, 54f), TextAlignmentOptions.Center,
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
        var l3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset");
        var l4 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level4Data.asset");
        gm.levels = new LevelData[] { l1, l2, l3, l4 };
        gm.currentLevelIndex = 0;

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
        gridManager.useHexTiles = false;
        gridManager.customNeutralColor = new Color(0.055f, 0.062f, 0.068f);
        gridManager.customCompletedColor = new Color(0.085f, 0.092f, 0.098f);
        gridManager.customCompletedEmission = Color.black;

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
        var l3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset");
        var l4 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level4Data.asset");
        gm.levels = new LevelData[] { l1, l2, l3, l4 };
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
        gridManager.useHexTiles = false;
        gridManager.customNeutralColor = new Color(0.055f, 0.062f, 0.068f);
        gridManager.customCompletedColor = new Color(0.085f, 0.092f, 0.098f);
        gridManager.customCompletedEmission = Color.black;

        float stride = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;

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
        var l3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset");
        var l4 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level4Data.asset");
        gm.levels = new LevelData[] { l1, l2, l3, l4 };
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
    // LEVEL 3 SCENE (STYLIZED ISLAND THEME MATCHING LEVEL 1 & 2)
    // =========================================================================
    static void BuildLevel3Scene(LevelData levelData)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var gridRoot = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.safeMat = tileMat;
        gridManager.unsafeMat = tileMat;
        gridManager.tileEdgeMat = stoneMat;
        gridManager.tileRockMat = stoneDarkMat;
        gridManager.useHexTiles = false;
        gridManager.customNeutralColor = new Color(0.055f, 0.062f, 0.068f);
        gridManager.customCompletedColor = new Color(0.085f, 0.092f, 0.098f);
        gridManager.customCompletedEmission = Color.black;

        float stride = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;

        // Floating grass islands matching Level 1 & 2
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
        var l3 = levelData;
        var l4 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level4Data.asset");
        gm.levels = new LevelData[] { l1, l2, l3, l4 };
        gm.currentLevelIndex = 2;

        BuildUI(lm, levelData);
        SetupAudioManager();

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var animator = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = cloudRoot;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Level3.unity");
    }

    // =========================================================================
    // UI CONSTRUCTION FOR GAME LEVELS (HIGH-CONTRAST & CRISP LEGIBILITY)
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
            new Vector2(380f, 860f), new Vector2(30f, 0f), rounded,
            new Color(0.04f, 0.06f, 0.09f, 0.94f));

        CreateText(mapPanel.transform, "MapTitle", "PATH MAP", font, 32f,
            new Vector2(0f, 380f), new Vector2(330f, 42f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        string initialStatus = data.isUpsideDownPattern 
            ? "UPSIDE-DOWN BLUEPRINT: 8.0s"
            : (data.hideMapAfterCountdown ? "MEMORIZE PATH: 6.0s" : "LIVE PATH GUIDANCE");
        Color statusColor = new Color(0.35f, 0.92f, 0.55f, 1f);

        ui.mapStatusText = CreateText(mapPanel.transform, "MapStatus",
            initialStatus, font, 15f, new Vector2(0f, 340f), new Vector2(330f, 26f), TextAlignmentOptions.Left,
            FontStyles.Bold, statusColor);

        CreateBar(mapPanel.transform, "MapDivider", new Vector2(0f, 314f), new Vector2(330f, 2f),
            new Color(1f, 1f, 1f, 0.14f), rounded);

        CreateText(mapPanel.transform, "FinishTag", "FINISH", font, 14f,
            new Vector2(0f, 240f), new Vector2(330f, 20f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(1.0f, 0.82f, 0.35f, 1f));

        var mapContainerGo = new GameObject("MapContainer", typeof(RectTransform));
        mapContainerGo.transform.SetParent(mapPanel.transform, false);
        var mapRect = mapContainerGo.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(330f, 440f);
        mapRect.anchoredPosition = new Vector2(0f, 15f);
        ui.mapContainer = mapContainerGo.transform;

        var markerGo = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        markerGo.transform.SetParent(mapContainerGo.transform, false);
        var markerRect = markerGo.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(20f, 20f);
        var markerImg = markerGo.GetComponent<Image>();
        markerImg.sprite = dot;
        markerImg.color = new Color(0.45f, 1f, 0.75f, 1f);
        markerImg.raycastTarget = false;
        markerGo.SetActive(false);
        ui.playerMarker = markerImg;

        CreateText(mapPanel.transform, "StartTag", "START", font, 14f,
            new Vector2(0f, -210f), new Vector2(330f, 20f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.35f, 0.85f, 1.0f, 1f));

        Color legendSafe = new Color(0.24f, 0.90f, 0.44f);
        CreateSwatch(mapPanel.transform, "SafeSwatch", new Vector2(-95f, -255f),
            new Vector2(18f, 18f), legendSafe, rounded);
        CreateText(mapPanel.transform, "SafeLabel", "SAFE", font, 14f,
            new Vector2(-45f, -255f), new Vector2(75f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        CreateSwatch(mapPanel.transform, "UnsafeSwatch", new Vector2(25f, -255f),
            new Vector2(18f, 18f), new Color(0.12f, 0.15f, 0.19f), rounded);
        CreateText(mapPanel.transform, "UnsafeLabel", "UNSAFE", font, 14f,
            new Vector2(82f, -255f), new Vector2(95f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        string hintStr = data.isUpsideDownPattern
            ? "INVERTED: Top blueprint row = 1st jump in the arena."
            : (data.hideMapAfterCountdown ? "Memorize before the path conceals!" : "The map mirrors what you see in the course.");
        ui.mapHintText = CreateText(mapPanel.transform, "MapHint",
            hintStr, font, 13.5f, new Vector2(0f, -305f), new Vector2(330f, 38f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.85f, 0.90f, 0.96f, 1f));

        // ================= RIGHT PANEL : LEVEL INFO =================
        var infoPanel = CreatePanel(canvasGo.transform, "InfoPanel",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(480f, 920f), new Vector2(-30f, 0f), rounded,
            new Color(0.04f, 0.06f, 0.09f, 0.95f));

        ui.levelTitle = CreateText(infoPanel.transform, "LevelTitle", data.levelName.ToUpper(), font, 44f,
            new Vector2(0f, 405f), new Vector2(420f, 52f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        string subText = data.isUpsideDownPattern 
            ? "UPSIDE-DOWN MEMORY"
            : (data.hideMapAfterCountdown ? "MEMORY CHALLENGE" : "PATH FINDER");
        Color subColor = new Color(0.33f, 0.94f, 0.52f, 1f);

        ui.subtitle = CreateText(infoPanel.transform, "Subtitle",
            subText, font, 18f, new Vector2(0f, 365f), new Vector2(420f, 26f), TextAlignmentOptions.Left,
            FontStyles.Bold, subColor);

        CreateBar(infoPanel.transform, "InfoDivider", new Vector2(0f, 335f), new Vector2(420f, 2f),
            new Color(1f, 1f, 1f, 0.14f), rounded);

        // OBJECTIVE
        CreateText(infoPanel.transform, "ObjectiveLabel", "OBJECTIVE", font, 15f,
            new Vector2(0f, 308f), new Vector2(420f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.22f, 0.74f, 0.97f, 1f));

        ui.objectiveText = CreateText(infoPanel.transform, "Objective", data.objective, font, 17f,
            new Vector2(0f, 252f), new Vector2(420f, 84f), TextAlignmentOptions.TopLeft,
            FontStyles.Bold, Color.white);
        ui.objectiveText.lineSpacing = 5f;

        // PROGRESS
        CreateText(infoPanel.transform, "ProgressLabel", "PROGRESS", font, 15f,
            new Vector2(0f, 175f), new Vector2(420f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.22f, 0.74f, 0.97f, 1f));

        ui.progressText = CreateText(infoPanel.transform, "Progress",
            string.Format("0 <size=26><color=#94A3B8>/ {0}</color></size>", data.sequence.Count),
            font, 44f, new Vector2(0f, 132f), new Vector2(420f, 50f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.29f, 0.87f, 0.50f, 1f));

        var progressBg = CreateBar(infoPanel.transform, "ProgressBg", new Vector2(0f, 98f),
            new Vector2(420f, 12f), new Color(1f, 1f, 1f, 0.12f), rounded);
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
        fillImg.color = new Color(0.25f, 0.95f, 0.52f, 1f);
        fillImg.raycastTarget = false;
        ui.progressFill = fillImg;

        // TIME REMAINING
        CreateText(infoPanel.transform, "TimeLabel", "TIME REMAINING", font, 15f,
            new Vector2(0f, 62f), new Vector2(420f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.22f, 0.74f, 0.97f, 1f));

        ui.timerText = CreateText(infoPanel.transform, "Timer", "120.0s", font, 38f,
            new Vector2(0f, 25f), new Vector2(420f, 44f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);

        CreateBar(infoPanel.transform, "InfoDivider2", new Vector2(0f, -16f), new Vector2(420f, 2f),
            new Color(1f, 1f, 1f, 0.14f), rounded);

        // CONTROLS
        CreateText(infoPanel.transform, "ControlsLabel", "CONTROLS", font, 15f,
            new Vector2(0f, -44f), new Vector2(420f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.22f, 0.74f, 0.97f, 1f));

        ui.instructionsText = CreateText(infoPanel.transform, "Controls",
            "<b><color=#FBBF24>WASD / ARROWS</color></b>    Move\n<b><color=#FBBF24>SPACE</color></b>    Jump\n<b><color=#FBBF24>MOUSE DRAG / Q E</color></b>    Rotate camera\n<b><color=#FBBF24>MOUSE WHEEL</color></b>    Zoom\n<b><color=#FBBF24>R</color></b>    Restart level\n<b><color=#FBBF24>ESC</color></b>    Level Select Menu",
            font, 16f, new Vector2(0f, -125f), new Vector2(420f, 136f),
            TextAlignmentOptions.TopLeft, FontStyles.Normal, new Color(0.96f, 0.97f, 0.99f, 1f));
        ui.instructionsText.lineSpacing = 7f;

        // ACTION BUTTONS
        var restartBtn = CreateButton(infoPanel.transform, "RestartButton", "RESTART LEVEL (R)",
            font, new Vector2(0f, -242f), new Vector2(420f, 54f), rounded,
            new Color(0.09f, 0.64f, 0.29f, 1f), Color.white, 20f);
        restartBtn.onClick.AddListener(() => lm.RestartLevel());
        ui.restartButton = restartBtn;

        var menuBtn = CreateButton(infoPanel.transform, "MenuButton", "LEVEL SELECT MENU",
            font, new Vector2(0f, -306f), new Vector2(420f, 54f), rounded,
            new Color(0.20f, 0.25f, 0.33f, 1f), Color.white, 19f);
        menuBtn.onClick.AddListener(() => lm.LoadMainMenu());
        ui.menuButton = menuBtn;

        CreateText(infoPanel.transform, "FooterHint", "Reach the golden finish island once all safe tiles are cleared.", font, 13.5f,
            new Vector2(0f, -368f), new Vector2(420f, 32f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.80f, 0.86f, 0.92f, 1f));

        // ================= LEVEL COMPLETE PANEL =================
        var complete = CreatePanel(canvasGo.transform, "LevelCompletePanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(660f, 460f), Vector2.zero, rounded,
            new Color(0.04f, 0.07f, 0.06f, 0.97f));

        CreateText(complete.transform, "CompleteTitle", "LEVEL COMPLETE!", font, 44f,
            new Vector2(0f, 160f), new Vector2(580f, 54f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.29f, 0.87f, 0.50f, 1f));

        string sub = data.sceneName == "Level3" 
            ? "Brilliant memory run! Inverted challenge cleared."
            : (data.sceneName == "Level2" ? "Brilliant memory run! You cleared the challenge." : "Safe path mastered — beautiful work.");

        CreateText(complete.transform, "CompleteSub", sub, font, 18f,
            new Vector2(0f, 110f), new Vector2(580f, 30f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white);

        ui.levelCompleteTime = CreateText(complete.transform, "CompleteTime", "", font, 24f,
            new Vector2(0f, 62f), new Vector2(580f, 36f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.99f, 0.88f, 0.28f, 1f));

        // Button 1: Next Level (or Return to Menu for Level 3)
        string nextBtnLabel = data.sceneName == "Level1" 
            ? "NEXT LEVEL (LEVEL 2)" 
            : (data.sceneName == "Level2" ? "NEXT LEVEL (LEVEL 3)" : "RETURN TO MENU");

        var nextBtn = CreateButton(complete.transform, "NextLevelButton", nextBtnLabel,
            font, new Vector2(0f, -12f), new Vector2(400f, 56f), rounded,
            new Color(0.09f, 0.64f, 0.29f, 1f), Color.white, 21f);
        if (data.sceneName == "Level3")
            nextBtn.onClick.AddListener(() => lm.LoadMainMenu());
        else
            nextBtn.onClick.AddListener(() => lm.LoadNextLevel());
        ui.nextLevelButton = nextBtn;

        // Button 2: Replay Level
        var replayBtn = CreateButton(complete.transform, "ReplayButton", "REPLAY LEVEL",
            font, new Vector2(0f, -76f), new Vector2(400f, 52f), rounded,
            new Color(0.01f, 0.52f, 0.78f, 1f), Color.white, 20f);
        replayBtn.onClick.AddListener(() => lm.RestartLevel());
        ui.replayButton = replayBtn;

        // Button 3: Main Menu
        var compMenuBtn = CreateButton(complete.transform, "CompleteMenuButton", "LEVEL SELECT MENU",
            font, new Vector2(0f, -138f), new Vector2(400f, 52f), rounded,
            new Color(0.20f, 0.25f, 0.33f, 1f), Color.white, 19f);
        compMenuBtn.onClick.AddListener(() => lm.LoadMainMenu());
        ui.completeMenuButton = compMenuBtn;

        complete.SetActive(false);
        ui.levelCompletePanel = complete;

        // Fail Panel
        var fail = CreatePanel(canvasGo.transform, "FailPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(520f, 230f), Vector2.zero, rounded,
            new Color(0.12f, 0.03f, 0.04f, 0.98f));
        CreateText(fail.transform, "FailTitle", "WRONG TILE!", font, 38f,
            new Vector2(0f, 38f), new Vector2(460f, 48f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(1f, 0.35f, 0.32f, 1f));
        CreateText(fail.transform, "FailSub", "Restarting course...", font, 18f,
            new Vector2(0f, -16f), new Vector2(460f, 28f), TextAlignmentOptions.Center,
            FontStyles.Bold, Color.white);
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

        var torso = AddPart(PrimitiveType.Capsule, "Torso", model.transform, new Vector3(0f, 1.34f, 0f),
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
                new Vector3(side * 0.28f, 0.07f, 0.49f), new Vector3(0.12f, 0.14f, 0.08f), pupilMat);
        }

        var armL = AddPart(PrimitiveType.Capsule, "ArmL", model.transform, new Vector3(-0.46f, 1.25f, 0f),
            new Vector3(0.22f, 0.52f, 0.22f), hoodieMat);
        var armR = AddPart(PrimitiveType.Capsule, "ArmR", model.transform, new Vector3(0.46f, 1.25f, 0f),
            new Vector3(0.22f, 0.52f, 0.22f), hoodieMat);
        AddPart(PrimitiveType.Sphere, "HandL", model.transform, new Vector3(-0.46f, 0.86f, 0.02f),
            new Vector3(0.20f, 0.20f, 0.20f), skinMat);
        AddPart(PrimitiveType.Sphere, "HandR", model.transform, new Vector3(0.46f, 0.86f, 0.02f),
            new Vector3(0.20f, 0.20f, 0.20f), skinMat);

        AddPart(PrimitiveType.Capsule, "Hips", model.transform, new Vector3(0f, 0.88f, 0f),
            new Vector3(0.58f, 0.30f, 0.44f), jeansMat);
        var legL = AddPart(PrimitiveType.Capsule, "LegL", model.transform, new Vector3(-0.19f, 0.52f, 0f),
            new Vector3(0.25f, 0.56f, 0.25f), jeansMat);
        var legR = AddPart(PrimitiveType.Capsule, "LegR", model.transform, new Vector3(0.19f, 0.52f, 0f),
            new Vector3(0.25f, 0.56f, 0.25f), jeansMat);
        AddPart(PrimitiveType.Cube, "FootL", model.transform, new Vector3(-0.19f, 0.10f, 0.05f),
            new Vector3(0.27f, 0.18f, 0.42f), shoeMat);
        AddPart(PrimitiveType.Cube, "FootR", model.transform, new Vector3(0.19f, 0.10f, 0.05f),
            new Vector3(0.27f, 0.18f, 0.42f), shoeMat);

        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1.05f, 0f);
        cc.radius = 0.42f;
        cc.height = 2.1f;

        var controller = player.AddComponent<PlayerController>();
        controller.moveSpeed = 6.5f;
        controller.jumpHeight = 1.7f;

        var visual = player.AddComponent<PlayerVisual>();
        visual.model = model.transform;
        visual.torso = torso.transform;
        visual.head = head.transform;
        visual.armL = armL.transform;
        visual.armR = armR.transform;
        visual.legL = legL.transform;
        visual.legR = legR.transform;

        return player;
    }

    static Transform BuildEnvironment(Transform root)
    {
        var terrainRoot = new GameObject("Terrain").transform;
        terrainRoot.SetParent(root, false);

        CreateIsland("Island_Left1", terrainRoot, new Vector3(-17f, -1.8f, -4f), 11f, 8.5f, hillMat, stoneMat, stoneDarkMat, true);
        CreateIsland("Island_Right1", terrainRoot, new Vector3(18f, -2.4f, 6f), 12f, 9f, hillMat, stoneMat, stoneDarkMat, true);
        CreateIsland("Island_FarCenter", terrainRoot, new Vector3(0f, -4.5f, 32f), 16f, 11f, hillMat, stoneMat, stoneDarkMat, true);

        var bgRoot = new GameObject("BackdropMountains").transform;
        bgRoot.SetParent(root, false);
        CreateIsland("Mountain_L1", bgRoot, new Vector3(-34f, 4f, 42f), 22f, 18f, mountainMat, mountainDarkMat, stoneDarkMat, false);
        CreateIsland("Mountain_L2", bgRoot, new Vector3(-18f, 8f, 54f), 28f, 22f, mountainDarkMat, mountainMat, stoneDarkMat, false);
        CreateIsland("Mountain_R1", bgRoot, new Vector3(32f, 5f, 46f), 24f, 19f, mountainMat, mountainDarkMat, stoneDarkMat, false);

        var vegRoot = new GameObject("Vegetation").transform;
        vegRoot.SetParent(root, false);
        CreatePine("Pine1", vegRoot, new Vector3(-18f, -0.6f, -3f), 1.55f);
        CreatePine("Pine2", vegRoot, new Vector3(-15f, -0.7f, -6f), 1.35f);
        CreateDeciduous("Deciduous1", vegRoot, new Vector3(17f, -1.2f, 7f), 1.45f);
        CreateRockCluster("Rocks_Left", vegRoot, new Vector3(-13f, -1.4f, 0f), 1.2f);
        CreateRockCluster("Rocks_Right", vegRoot, new Vector3(14f, -1.8f, 2f), 1.3f);

        var cloudRoot = new GameObject("Clouds").transform;
        cloudRoot.SetParent(root, false);
        CreateCloud("Cloud_01", cloudRoot, new Vector3(-26f, 15f, 28f), 1.3f);
        CreateCloud("Cloud_02", cloudRoot, new Vector3(22f, 18f, 34f), 1.5f);
        CreateCloud("Cloud_03", cloudRoot, new Vector3(4f, 21f, 44f), 1.7f);

        return cloudRoot;
    }

    static void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.82f, 0.92f);

        var lightGo = new GameObject("Directional Light");
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.96f, 0.88f);
        l.intensity = 1.35f;
        lightGo.transform.rotation = Quaternion.Euler(46f, -38f, 0f);

        var fillGo = new GameObject("Sky Fill Light");
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.65f, 0.78f, 0.92f);
        fill.intensity = 0.45f;
        fillGo.transform.rotation = Quaternion.Euler(30f, 140f, 0f);
    }

    static void ConfigurePostProcessing()
    {
        var volGo = new GameObject("Global Volume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;

        // Load existing or create a fresh profile asset
        string profilePath = "Assets/Settings/SampleScene_Profile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        // Clear old components safely
        foreach (var c in profile.components)
            AssetDatabase.RemoveObjectFromAsset(c);
        profile.components.Clear();

        // Soft bloom — gentle glow, not blown out
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = 0.28f;
        bloom.threshold.value = 0.95f;
        AssetDatabase.AddObjectToAsset(bloom, profile);

        // Neutral ACES tonemapping for natural colours
        var tonemap = profile.Add<Tonemapping>(true);
        tonemap.mode.value = TonemappingMode.ACES;
        AssetDatabase.AddObjectToAsset(tonemap, profile);

        // NOTE: ShadowsMidtonesHighlights deliberately omitted — it caused a
        // dark midtone shadow in the centre of the screen. Keep post-processing clean.

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        vol.sharedProfile = profile;
    }

    static GameObject CreateIsland(string name, Transform parent, Vector3 pos,
        float width, float depth, Material topMat, Material baseMat, Material rockMat, bool addCollider)
    {
        var root = new GameObject(name);
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.position = pos;

        float topH = 0.45f;
        var top = CreateBeveledObject("GrassCap", root.transform,
            Vector3.zero, new Vector3(width, topH, depth), 0.22f, topMat, addCollider);

        float b1H = 2.4f;
        CreateBeveledObject("StoneLayer", root.transform,
            new Vector3(0f, -topH * 0.5f - b1H * 0.5f, 0f),
            new Vector3(width * 0.88f, b1H, depth * 0.88f), 0.35f, baseMat, false);

        float b2H = 2.8f;
        CreateBeveledObject("RockRoot", root.transform,
            new Vector3(0f, -topH * 0.5f - b1H - b2H * 0.5f, 0f),
            new Vector3(width * 0.60f, b2H, depth * 0.60f), 0.30f, rockMat, false);

        return top;
    }

    static void CreatePine(string name, Transform parent, Vector3 pos, float scale)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * scale;

        CreatePrimitive(PrimitiveType.Cylinder, "Trunk", root.transform,
            new Vector3(0f, 0.8f, 0f), new Vector3(0.32f, 0.8f, 0.32f), trunkMat, false);

        CreateCone("Tier0", root.transform, new Vector3(0f, 1.25f, 0f), 2.2f, 1.55f, pineMat);
        CreateCone("Tier1", root.transform, new Vector3(0f, 2.25f, 0f), 1.75f, 1.45f, pineLightMat);
        CreateCone("Tier2", root.transform, new Vector3(0f, 3.20f, 0f), 1.25f, 1.35f, pineMat);
    }

    static void CreateDeciduous(string name, Transform parent, Vector3 pos, float scale)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * scale;

        CreatePrimitive(PrimitiveType.Cylinder, "Trunk", root.transform,
            new Vector3(0f, 1.1f, 0f), new Vector3(0.42f, 1.1f, 0.42f), trunkMat, false);

        CreatePrimitive(PrimitiveType.Sphere, "Foliage_Center", root.transform,
            new Vector3(0f, 3.1f, 0f), new Vector3(2.5f, 2.3f, 2.5f), leafMat, false);
        CreatePrimitive(PrimitiveType.Sphere, "Foliage_Top", root.transform,
            new Vector3(0.1f, 4.0f, -0.1f), new Vector3(1.8f, 1.7f, 1.8f), leafLightMat, false);
        CreatePrimitive(PrimitiveType.Sphere, "Foliage_SideL", root.transform,
            new Vector3(-0.9f, 2.7f, 0.3f), new Vector3(1.5f, 1.4f, 1.5f), leafDeepMat, false);
        CreatePrimitive(PrimitiveType.Sphere, "Foliage_SideR", root.transform,
            new Vector3(0.85f, 2.6f, -0.4f), new Vector3(1.6f, 1.5f, 1.6f), leafMat, false);
    }

    static void CreateRockCluster(string name, Transform parent, Vector3 pos, float scale)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * scale;

        CreateBeveledObject("RockA", root.transform,
            new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.8f, 1.1f), 0.15f, rockMat, false);
        CreateBeveledObject("RockB", root.transform,
            new Vector3(0.85f, 0.22f, 0.2f), new Vector3(0.9f, 0.55f, 0.8f), 0.12f, stoneDarkMat, false);
        CreateBeveledObject("RockC", root.transform,
            new Vector3(-0.65f, 0.18f, -0.3f), new Vector3(0.75f, 0.45f, 0.7f), 0.10f, stoneMat, false);
    }

    static void CreateCloud(string name, Transform parent, Vector3 pos, float scale)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * scale;

        CreatePrimitive(PrimitiveType.Sphere, "PuffCenter", root.transform,
            Vector3.zero, new Vector3(4.8f, 2.4f, 3.2f), cloudMat, false);
        CreatePrimitive(PrimitiveType.Sphere, "PuffTop", root.transform,
            new Vector3(0.4f, 0.9f, 0f), new Vector3(3.2f, 2.2f, 2.6f), cloudMat, false);
        CreatePrimitive(PrimitiveType.Sphere, "PuffL", root.transform,
            new Vector3(-2.2f, -0.2f, 0.3f), new Vector3(2.6f, 1.8f, 2.2f), cloudMat, false);
        CreatePrimitive(PrimitiveType.Sphere, "PuffR", root.transform,
            new Vector3(2.1f, -0.1f, -0.2f), new Vector3(2.8f, 1.9f, 2.4f), cloudMat, false);
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

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font,
        Vector2 pos, Vector2 size, Sprite sprite, Color bgColor, Color textColor, float fontSize = 18f)
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
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
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
        t.fontSize = fontSize;
        t.color = textColor;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 0.2f;
        t.raycastTarget = false;
        return btn;
    }

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
        return go;
    }

    static GameObject CreateBar(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite)
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
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static GameObject CreateSwatch(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite)
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
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Material mat, bool addCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null) mr.sharedMaterial = mat;
        if (!addCollider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }
        return go;
    }

    static GameObject CreateBeveledObject(string name, Transform parent,
        Vector3 localPos, Vector3 size, float bevel, Material mat, bool addBoxCollider)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = BeveledBoxMesh.Create(size.x, size.y, size.z, bevel);
        var mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;
        if (addBoxCollider)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.zero;
        }
        return go;
    }

    static GameObject CreateCone(string name, Transform parent, Vector3 localPos, float radius, float height, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = ConeMesh.CreateUnit(12);
        var mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;
        return go;
    }

    static GameObject AddPart(PrimitiveType type, string name, Transform parent,
        Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null) mr.sharedMaterial = mat;
        return go;
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
        if (emission > 0f && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * emission);
        }
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
