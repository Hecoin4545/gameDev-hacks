using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Builds Level 4 – Number Logic puzzle theme.
/// A "cyber-academy" platform floating in space.
/// Each row has exactly ONE safe tile, identified by solving a math/logic clue.
/// No map panel – replaced by a Puzzle Guide panel (left) and Clue Cards panel (right).
/// </summary>
public class BuildLevel4
{
    // ── Dedicated Level 4 palette materials ──────────────────────────────────
    static Material tileMat;          // dark puzzle tile face
    static Material tileEdgeMat;      // cyan/gold edge bevel
    static Material tileRockMat;      // deep base column
    static Material tileRimMat;       // bottom rim

    static Material platformMat;      // start/end platform – polished dark slate
    static Material platformMidMat;   // platform mid layer
    static Material platformBotMat;   // platform bottom layer
    static Material accentMat;        // cyan neon accent
    static Material goldMat;          // gold goal pad
    static Material glowMat;          // goal crystal emission

    static Material gridFloorMat;     // vast dark floor plane
    static Material pillarMat;        // floating support pillars
    static Material orbitRingMat;     // decorative orbit rings
    static Material starDustMat;      // star-cloud spheres

    // Player materials (shared)
    static Material hoodieMat, hoodMat, jeansMat, skinMat, hairMat, shoeMat, eyeMat, pupilMat;

    // ── Clue data (Row → answer column, 1-indexed) ───────────────────────────
    // Row 1: smallest prime = 2            → Col 2
    // Row 2: only even prime = 2           → Col 2
    // Row 3: sides of a triangle = 3       → Col 3
    // Row 4: square root of 16 = 4         → Col 4
    // Row 5: cardinal directions (N,S,E,W) = 4 → Col 4
    //   NOTE: "letters in English alphabet" = 26, which exceeds the 4 columns,
    //   so Row 5 uses "number of cardinal directions" = 4 instead.
    static readonly Vector2Int[] safeTileArray = new Vector2Int[]
    {
        new Vector2Int(1, 2),
        new Vector2Int(2, 2),
        new Vector2Int(3, 3),
        new Vector2Int(4, 4),
        new Vector2Int(5, 4),
    };

    static readonly string[] rowClues = new string[]
    {
        "Jump onto the tile representing\nthe smallest prime number.",
        "Jump onto the tile representing\nthe only even prime number.",
        "Jump onto the tile representing\nthe number of sides in a triangle.",
        "Jump onto the tile representing\nthe square root of 16.",
        "Jump onto the tile representing\nthe number of cardinal directions\n(North, South, East, West).",
    };

    [MenuItem("Tools/Build Level 4 (Number Logic)")]
    public static void Run()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Meshes");
        EnsureFolder("Assets/Textures");
        EnsureFolder("Assets/UI");

        // Open (or create) the scene
        string scenePath = "Assets/Scenes/Level4.unity";
        bool exists = System.IO.File.Exists(System.IO.Path.Combine(
            Application.dataPath.Replace("Assets", ""), scenePath));
        var scene = exists
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMaterials();
        var levelData = BuildLevelData();

        // Clear any existing scene objects
        foreach (var root in scene.GetRootGameObjects())
            UnityEngine.Object.DestroyImmediate(root);

        // ── GRID ─────────────────────────────────────────────────────────────
        var gridRoot    = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.safeMat     = tileMat;
        gridManager.unsafeMat   = tileMat;
        gridManager.tileEdgeMat = tileEdgeMat;
        gridManager.tileRockMat = tileRockMat;
        gridManager.tileRimMat  = tileRimMat;
        gridManager.useHexTiles = false;
        gridManager.customNeutralColor      = new Color(0.12f, 0.14f, 0.22f);
        gridManager.customCompletedColor    = new Color(0.25f, 0.90f, 0.75f);
        gridManager.customCompletedEmission = new Color(0.08f, 0.55f, 0.45f);

        float stride    = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;
        float halfDepth = gridDepth * 0.5f;
        float islandZOffset = halfDepth + 3.5f;

        // ── START / END PLATFORMS ─────────────────────────────────────────────
        var startIsland = CreatePlatform("StartPlatform", null,
            new Vector3(0f, 0.11f, -islandZOffset), 5.4f, 4.4f, true);

        var endIsland = CreatePlatform("EndPlatform", null,
            new Vector3(0f, 0.11f, islandZOffset), 5.4f, 4.4f, true);

        // Goal pad
        CreateBeveledObject("GoalPad", endIsland.transform,
            new Vector3(0f, 0.055f, 0f),
            new Vector3(2.4f, 0.11f, 2.4f), 0.05f, goldMat, false);

        // 4 Cyan accent pillars around goal pad
        for (int i = 0; i < 4; i++)
        {
            float ox = (i % 2 == 0 ? -1f : 1f) * 1.5f;
            float oz = (i < 2 ? -1f : 1f) * 1.4f;
            CreateBeveledObject("GoalPillar" + i, endIsland.transform,
                new Vector3(ox, 1.0f, oz),
                new Vector3(0.18f, 2.0f, 0.18f), 0.05f, accentMat, false);
        }

        // Floating goal crystal
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.5f, 0f), new Vector3(0.60f, 0.60f, 0.60f), glowMat, false);
        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

        // End trigger
        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size      = new Vector3(5.0f, 3.2f, 3.8f);
        endBox.isTrigger = true;
        endTrigger.AddComponent<EndPlatformTrigger>();

        gridManager.Build(levelData);

        // ── ADD COLUMN NUMBER LABELS ON TILES ────────────────────────────────
        AddColumnLabels(levelData, stride);

        // ── PLAYER ────────────────────────────────────────────────────────────
        var player = BuildPlayer(new Vector3(0f, 1.7f, -islandZOffset));

        // ── CAMERA ───────────────────────────────────────────────────────────
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }
        cam.fieldOfView   = 52f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane  = 800f;
        cam.allowHDR      = true;
        cam.allowMSAA     = true;
        cam.clearFlags    = CameraClearFlags.Skybox;
        cam.transform.position = new Vector3(0f, 12f, -islandZOffset - 9.6f);

        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.antialiasing        = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            camData.renderPostProcessing = true;
        }

        var cf = cam.gameObject.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
        cf.target         = player.transform;
        cf.distance       = 13.5f;
        cf.pitch          = 40f;
        cf.yaw            = 0f;
        cf.targetHeight   = 1.35f;
        cf.lookAhead      = 4.0f;
        cf.followSmooth   = 12f;
        cf.rotationSmooth = 16f;
        cf.minDistance    = 7f;
        cf.maxDistance    = 22f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        // ── LIGHTING / SPACE SKY ─────────────────────────────────────────────
        ConfigureLighting();
        ConfigurePostProcessing();

        // ── ENVIRONMENT ───────────────────────────────────────────────────────
        var envRoot = new GameObject("Environment").transform;
        BuildEnvironment(envRoot);

        // ── MANAGERS ─────────────────────────────────────────────────────────
        var lmGo = new GameObject("LevelManager");
        var lm   = lmGo.AddComponent<LevelManager>();
        lm.data          = levelData;
        lm.gridManager   = gridManager;
        lm.player        = player.GetComponent<PlayerController>();
        lm.startPlatform = startIsland.transform;
        lm.endPlatform   = endIsland.transform;
        lm.levelNarratorTexts = new string[]
        {
            "Welcome, brave adventurer! The ancient path of tiles awaits you. Only the safe tiles will hold your weight — choose wisely!",
            "The desert sands are treacherous! Memorize the path in 5 seconds before the map vanishes, then navigate the dunes from memory!",
            "Welcome to the Frozen Crags! The icy tiles are slick and perilous through the snowfall. Remember the blueprint in reverse to reach the summit!",
            "Welcome to the Number Logic Lab! Solve the mathematical clue for each row and jump onto the correct tile. One wrong step and you restart from the beginning!"
        };

        var gmGo = new GameObject("GameManager");
        var gm   = gmGo.AddComponent<GameManager>();
        var l1   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset");
        var l2   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        var l3   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset");
        gm.levels            = new LevelData[] { l1, l2, l3, levelData };
        gm.currentLevelIndex = 3;

        // ── UI ────────────────────────────────────────────────────────────────
        BuildUI(lm, levelData);

        // ── EVENT SYSTEM ─────────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── ENVIRONMENT ANIMATOR ──────────────────────────────────────────────
        var envAnim = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        // No specific cloud root for Level 4 (uses orbit rings instead)

        // ── SAVE SCENE & ADD TO BUILD SETTINGS ───────────────────────────────
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AddSceneToBuildSettings(scenePath);

        Debug.Log("Level 4 (Number Logic) built successfully!");
    }

    // ── LEVEL DATA ───────────────────────────────────────────────────────────
    static LevelData BuildLevelData()
    {
        string path = "Assets/Data/Level4Data.asset";
        var d = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(d, path);
        }
        d.levelName  = "Level 4";
        d.sceneName  = "Level4";
        d.rows       = 5;
        d.columns    = 4;
        d.tileSize   = 2.4f;
        d.tileGap    = 0.55f;
        d.timeLimit  = 300f;   // 5 minutes – generous, as this is a logic puzzle
        d.objective  = "Solve the clue for each row and jump onto the correct tile. One wrong move and you must start again!";
        d.instructions = "WASD / Arrows – Move\nSpace – Jump\nRight drag / Q E – Rotate camera\nMouse wheel – Zoom\nR – Restart\nF – Dance";
        d.hideMapAfterCountdown = false;
        d.mapVisibleDuration    = 0f;
        d.isUpsideDownPattern   = false;

        d.safeTiles = new List<Vector2Int>(safeTileArray);
        d.sequence  = new List<Vector2Int>(safeTileArray);
        // No displayed pattern — this is a logic puzzle, not a memory one
        d.displayedPattern = new List<Vector2Int>();

        EditorUtility.SetDirty(d);
        return d;
    }

    // ── MATERIALS ────────────────────────────────────────────────────────────
    static void CreateMaterials()
    {
        // Tile materials – dark cyber slate with cyan glow edges
        tileMat       = CreateMaterial("Assets/Materials/PuzzleTile.mat",        new Color(0.08f, 0.10f, 0.18f), 0.15f, 0.55f, 0f);
        tileEdgeMat   = CreateMaterial("Assets/Materials/PuzzleTileEdge.mat",    new Color(0.20f, 0.65f, 0.90f), 0.20f, 0.65f, 0.08f);
        tileRockMat   = CreateMaterial("Assets/Materials/PuzzleTileRock.mat",    new Color(0.06f, 0.08f, 0.14f), 0.10f, 0.30f, 0f);
        tileRimMat    = CreateMaterial("Assets/Materials/PuzzleTileRim.mat",     new Color(0.04f, 0.06f, 0.10f), 0f,    0.20f, 0f);

        // Platform / environment
        platformMat    = CreateMaterial("Assets/Materials/PuzzlePlatform.mat",   new Color(0.10f, 0.13f, 0.22f), 0.20f, 0.60f, 0f);
        platformMidMat = CreateMaterial("Assets/Materials/PuzzlePlatformMid.mat",new Color(0.14f, 0.18f, 0.28f), 0.15f, 0.50f, 0f);
        platformBotMat = CreateMaterial("Assets/Materials/PuzzlePlatformBot.mat",new Color(0.08f, 0.10f, 0.18f), 0.10f, 0.35f, 0f);
        accentMat      = CreateMaterial("Assets/Materials/PuzzleAccent.mat",     new Color(0.20f, 0.80f, 0.95f), 0.25f, 0.80f, 0.30f);
        goldMat        = CreateMaterial("Assets/Materials/PuzzleGold.mat",       new Color(1.0f,  0.75f, 0.20f), 0.30f, 0.65f, 0.55f);
        glowMat        = CreateMaterial("Assets/Materials/PuzzleGlow.mat",       new Color(0.30f, 0.95f, 0.85f), 0.10f, 0.70f, 0.90f);

        gridFloorMat   = CreateMaterial("Assets/Materials/PuzzleFloor.mat",      new Color(0.02f, 0.03f, 0.06f), 0.05f, 0.15f, 0f);
        pillarMat      = CreateMaterial("Assets/Materials/PuzzlePillar.mat",     new Color(0.10f, 0.14f, 0.24f), 0.20f, 0.55f, 0f);
        orbitRingMat   = CreateMaterial("Assets/Materials/PuzzleOrbitRing.mat",  new Color(0.20f, 0.60f, 1.00f), 0.15f, 0.65f, 0.12f);
        starDustMat    = CreateMaterial("Assets/Materials/PuzzleStarDust.mat",   new Color(0.80f, 0.85f, 1.00f), 0f,    0.05f, 0.02f);

        // Player (shared with other levels)
        hoodieMat  = CreateMaterial("Assets/Materials/Hoodie.mat", new Color(0.74f, 0.13f, 0.14f), 0f, 0.22f, 0.04f);
        hoodMat    = CreateMaterial("Assets/Materials/Hood.mat",   new Color(0.60f, 0.10f, 0.11f), 0f, 0.20f, 0.02f);
        jeansMat   = CreateMaterial("Assets/Materials/Jeans.mat",  new Color(0.16f, 0.33f, 0.68f), 0f, 0.16f, 0f);
        skinMat    = CreateMaterial("Assets/Materials/Skin.mat",   new Color(0.93f, 0.74f, 0.59f), 0f, 0.20f, 0f);
        hairMat    = CreateMaterial("Assets/Materials/Hair.mat",   new Color(0.20f, 0.12f, 0.07f), 0f, 0.24f, 0f);
        shoeMat    = CreateMaterial("Assets/Materials/Shoe.mat",   new Color(0.93f, 0.94f, 0.95f), 0f, 0.35f, 0f);
        eyeMat     = CreateMaterial("Assets/Materials/Eye.mat",    Color.white,                    0f, 0.40f, 0f);
        pupilMat   = CreateMaterial("Assets/Materials/Pupil.mat",  new Color(0.06f, 0.06f, 0.08f), 0f, 0.40f, 0f);
    }

    // ── COLUMN NUMBER LABELS ─────────────────────────────────────────────────
    /// <summary>
    /// Adds floating TMP text "1", "2", "3", "4" labels above each column of tiles.
    /// Also adds small inline labels on each tile face so the player always knows the number.
    /// </summary>
    static void AddColumnLabels(LevelData data, float stride)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        if (font == null) return;

        float halfCols = (data.columns - 1) * stride * 0.5f;

        // Floating column-header labels (top row height + extra offset)
        float topRowZ  = -(data.rows - 1) * stride * 0.5f;   // GridManager places row 1 at negative Z
        // We'll put them slightly above that row
        for (int col = 0; col < data.columns; col++)
        {
            float xPos = -halfCols + col * stride;

            // Floating label above the column (world-space canvas)
            var labelGo = new GameObject("ColHeader_" + (col + 1));
            labelGo.transform.position = new Vector3(xPos, 1.5f, topRowZ - 1.0f);

            var canvas = labelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            labelGo.AddComponent<CanvasScaler>();
            labelGo.AddComponent<GraphicRaycaster>();
            var crt = labelGo.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(1.2f, 0.6f);
            labelGo.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(labelGo.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text      = (col + 1).ToString();
            tmp.font      = font;
            tmp.fontSize  = 72f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = new Color(0.90f, 0.95f, 1.0f, 0.95f);
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    // ── PLATFORM BUILDER ─────────────────────────────────────────────────────
    static GameObject CreatePlatform(string name, Transform parent, Vector3 topCenter,
        float width, float depth, bool addCollider)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = topCenter;

        // Top slab
        var slab = CreateBeveledObject(name + "_Top", root.transform,
            new Vector3(0f, -0.25f, 0f),
            new Vector3(width, 0.5f, depth), 0.10f, platformMat, addCollider);
        slab.GetComponent<Renderer>().receiveShadows = true;

        // Mid bevel
        CreateBeveledObject(name + "_Mid", root.transform,
            new Vector3(0f, -1.00f, 0f),
            new Vector3(width * 0.88f, 1.1f, depth * 0.88f), 0.14f, platformMidMat, false);

        // Bottom tip
        CreateBeveledObject(name + "_Bot", root.transform,
            new Vector3(0f, -1.70f, 0f),
            new Vector3(width * 0.52f, 0.85f, depth * 0.52f), 0.12f, platformBotMat, false);

        return root;
    }

    // ── ENVIRONMENT ──────────────────────────────────────────────────────────
    static void BuildEnvironment(Transform root)
    {
        const float groundY = -35f;

        // Vast dark space floor
        var ground = CreatePrimitive(PrimitiveType.Plane, "SpaceFloor", root,
            new Vector3(0f, groundY, 0f), new Vector3(45f, 1f, 45f), gridFloorMat, false);
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // 12 floating support pillars beneath the grid
        UnityEngine.Random.InitState(42001);
        for (int i = 0; i < 12; i++)
        {
            float angle  = i * (360f / 12f) + 8f;
            float radius = 10f + (i % 4) * 4.5f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 15f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateBeveledObject("Pillar" + i, root, pos,
                new Vector3(0.6f, 30f, 0.6f), 0.06f, pillarMat, false);
        }

        // 8 Large floating space rock / platform shards (background)
        for (int i = 0; i < 8; i++)
        {
            float angle  = i * 45f + 22f;
            float radius = 55f + (i % 3) * 15f;
            float height = -5f + (i % 4) * 4f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                height,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float sz = 8f + (i % 3) * 4f;
            CreateBeveledObject("SpaceShard" + i, root, pos,
                new Vector3(sz, sz * 0.4f, sz * 0.7f), 0.18f, platformMat, false);
        }

        // 6 Large orbit ring decorations
        for (int i = 0; i < 6; i++)
        {
            float angle  = i * (360f / 6f) + 14f;
            float radius = 70f + (i % 3) * 20f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                10f + (i % 3) * 8f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            var ring = CreatePrimitive(PrimitiveType.Sphere, "OrbitRing" + i, root,
                pos, new Vector3(18f + (i % 3) * 8f, 1.2f, 18f + (i % 3) * 8f), orbitRingMat, false);
            ring.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // 40 Star-dust background spheres (very large, distant nebulae)
        UnityEngine.Random.InitState(77421);
        for (int i = 0; i < 40; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 150f + UnityEngine.Random.Range(0f, 200f);
            float yPos   = -40f + UnityEngine.Random.Range(0f, 80f);
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                yPos,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float sz = 20f + UnityEngine.Random.Range(0f, 50f);
            var star = CreatePrimitive(PrimitiveType.Sphere, "StarDust" + i, root,
                pos, new Vector3(sz, sz * 0.5f, sz * 0.8f), starDustMat, false);
            star.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // 4 Floating accent beacon pillars (near grid, decorative)
        for (int i = 0; i < 4; i++)
        {
            float ox = (i % 2 == 0 ? -1f : 1f) * 12f;
            float oz = (i < 2 ? -1f : 1f) * 12f;
            CreateBeveledObject("Beacon" + i, root,
                new Vector3(ox, 3f, oz),
                new Vector3(0.25f, 8f, 0.25f), 0.04f, accentMat, false);
            // Beacon top cap
            CreateBeveledObject("BeaconCap" + i, root,
                new Vector3(ox, 7.5f, oz),
                new Vector3(0.65f, 0.65f, 0.65f), 0.08f, glowMat, false);
        }
    }

    // ── LIGHTING ─────────────────────────────────────────────────────────────
    static void ConfigureLighting()
    {
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo == null) lightGo = new GameObject("Directional Light");
        var l = lightGo.GetComponent<Light>();
        if (l == null) l = lightGo.AddComponent<Light>();

        l.type      = LightType.Directional;
        l.color     = new Color(0.75f, 0.85f, 1.0f);
        l.intensity = 1.10f;
        l.shadows   = LightShadows.Soft;
        l.shadowStrength   = 0.65f;
        l.shadowBias       = 0.04f;
        l.shadowNormalBias = 0.35f;
        lightGo.transform.rotation = Quaternion.Euler(44f, -30f, 0f);

        // Ambient – deep space trilight
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.12f, 0.18f, 0.35f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.12f, 0.22f);
        RenderSettings.ambientGroundColor  = new Color(0.04f, 0.06f, 0.10f);
        RenderSettings.ambientIntensity    = 0.90f;

        // Procedural space skybox
        var skyboxMat = CreateSpaceSkybox();
        RenderSettings.skybox = skyboxMat;

        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = new Color(0.04f, 0.06f, 0.14f);
        RenderSettings.fogDensity = 0.0018f;
    }

    static Material CreateSpaceSkybox()
    {
        string path   = "Assets/Materials/PuzzleSkybox.mat";
        var shader = Shader.Find("Skybox/Procedural");
        var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetFloat("_SunSize", 0.02f);
        mat.SetFloat("_SunSizeConvergence", 4f);
        mat.SetFloat("_AtmosphereThickness", 0.10f);
        mat.SetColor("_SkyTint",    new Color(0.05f, 0.07f, 0.18f));
        mat.SetColor("_GroundColor",new Color(0.03f, 0.04f, 0.08f));
        mat.SetFloat("_Exposure", 0.60f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigurePostProcessing()
    {
        string path    = "Assets/Settings/Puzzle_Profile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.components.Clear();

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.80f);
        bloom.intensity.Override(1.20f);
        bloom.scatter.Override(0.70f);
        bloom.tint.Override(new Color(0.70f, 0.88f, 1.0f));

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.10f);
        ca.contrast.Override(10f);
        ca.saturation.Override(8f);
        ca.colorFilter.Override(new Color(0.90f, 0.95f, 1.0f));

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.28f);
        vig.smoothness.Override(0.40f);
        vig.color.Override(new Color(0.02f, 0.04f, 0.10f));

        var go     = new GameObject("GlobalVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile  = profile;

        EditorUtility.SetDirty(profile);
    }

    // ── UI BUILDER ───────────────────────────────────────────────────────────
    static void BuildUI(LevelManager lm, LevelData data)
    {
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var ui       = canvasGo.AddComponent<UIManager>();
        lm.uiManager = ui;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        if (font == null) Debug.LogError("UIManager: font not found at Assets/Fonts/Inter-Regular SDF.asset");
        ui.font = font;

        var rounded = CreateRoundedSprite("Assets/UI/PanelRounded.png", 64, 18, false);
        var dot     = CreateRoundedSprite("Assets/UI/Dot.png",          32, 16, true);
        ui.roundedSprite = rounded;
        ui.dotSprite     = dot;

        Color panelBg   = new Color(0.04f, 0.06f, 0.14f, 0.94f);
        Color cyanHdr   = new Color(0.30f, 0.88f, 1.00f);
        Color goldAccent= new Color(1.00f, 0.82f, 0.30f);
        Color textBody  = new Color(0.82f, 0.90f, 1.00f);

        // ── LEFT PANEL: Puzzle Guide (replaces the map) ──────────────────────
        var leftPanel = CreatePanel(canvasGo.transform, "PuzzleGuidePanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(36f, -36f), new Vector2(340f, 480f),
            panelBg, rounded);

        // Level title
        var ltTitle = CreateLabelTL(leftPanel.transform, "LTitle", "LEVEL 04",
            new Vector2(20f, -18f), new Vector2(300f, 32f), font, 24f, FontStyles.Bold, cyanHdr);
        ltTitle.alignment = TextAlignmentOptions.Left;
        ui.levelTitle = ltTitle;

        // Subtitle
        var ltSub = CreateLabelTL(leftPanel.transform, "LSub", "NUMBER LOGIC",
            new Vector2(20f, -52f), new Vector2(300f, 20f), font, 13f, FontStyles.Bold, goldAccent);
        ltSub.alignment = TextAlignmentOptions.Left;
        ui.subtitle = ltSub;

        // Divider bar
        CreateColorBar(leftPanel.transform, "Divider1",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -78f), new Vector2(0f, 2f),
            new Color(0.20f, 0.65f, 1.00f, 0.5f));

        // Rows completed header
        CreateLabelTL(leftPanel.transform, "RowsHdr", "ROWS COMPLETED",
            new Vector2(20f, -88f), new Vector2(300f, 18f), font, 11f, FontStyles.Bold,
            new Color(0.60f, 0.70f, 0.90f));

        // Rows completed value  — wired to progressText
        var progressLbl = CreateLabelTL(leftPanel.transform, "ProgressVal", "0 / 5",
            new Vector2(20f, -108f), new Vector2(300f, 32f), font, 26f, FontStyles.Bold, cyanHdr);
        progressLbl.alignment = TextAlignmentOptions.Left;
        ui.progressText = progressLbl;

        // Progress bar background
        var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(leftPanel.transform, false);
        var barBgRt = barBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0f, 1f);
        barBgRt.anchorMax = new Vector2(1f, 1f);
        barBgRt.pivot     = new Vector2(0.5f, 1f);
        barBgRt.sizeDelta = new Vector2(-40f, 8f);
        barBgRt.anchoredPosition = new Vector2(0f, -148f);
        var barBgImg = barBg.GetComponent<Image>();
        barBgImg.color = new Color(0.08f, 0.10f, 0.20f);
        if (rounded != null) { barBgImg.sprite = rounded; barBgImg.type = Image.Type.Sliced; }

        // Progress bar fill — wired to progressFill
        var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        var barFillRt = barFill.GetComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.sizeDelta = Vector2.zero;
        var barFillImg = barFill.GetComponent<Image>();
        barFillImg.color = new Color(0.25f, 0.90f, 0.75f);
        barFillImg.type  = Image.Type.Filled;
        barFillImg.fillMethod  = Image.FillMethod.Horizontal;
        barFillImg.fillAmount  = 0f;
        ui.progressFill = barFillImg;

        // Wire mapStatusText to show "Rows Completed: 0 / 5" (not needed but keep reference)
        // We'll repurpose mapStatusText as a small note
        var noteLabel = CreateLabelTL(leftPanel.transform, "NoteLabel",
            "Solve each clue on the right.\nOnly ONE tile per row is correct.",
            new Vector2(20f, -170f), new Vector2(300f, 46f), font, 11.5f, FontStyles.Normal,
            new Color(0.65f, 0.75f, 0.90f));
        noteLabel.textWrappingMode = TextWrappingModes.Normal;
        noteLabel.alignment = TextAlignmentOptions.TopLeft;
        ui.mapStatusText = noteLabel;

        // Divider bar 2
        CreateColorBar(leftPanel.transform, "Divider2",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -224f), new Vector2(0f, 2f),
            new Color(0.20f, 0.65f, 1.00f, 0.5f));

        // Column visual header
        CreateLabelTL(leftPanel.transform, "ColHdr", "COLUMN GUIDE",
            new Vector2(20f, -234f), new Vector2(300f, 18f), font, 11f, FontStyles.Bold,
            new Color(0.60f, 0.70f, 0.90f));

        // 4 column boxes with numbers 1–4
        for (int col = 0; col < 4; col++)
        {
            float boxX = 20f + col * 72f;

            // Box background
            var boxGo = new GameObject("ColBox" + (col + 1), typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(leftPanel.transform, false);
            var boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0f, 1f);
            boxRt.anchorMax = new Vector2(0f, 1f);
            boxRt.pivot     = new Vector2(0f, 1f);
            boxRt.anchoredPosition = new Vector2(boxX, -260f);
            boxRt.sizeDelta = new Vector2(60f, 60f);
            var boxImg = boxGo.GetComponent<Image>();
            boxImg.color = new Color(0.10f, 0.14f, 0.26f);
            if (rounded != null) { boxImg.sprite = rounded; boxImg.type = Image.Type.Sliced; }

            // Number label inside box
            var numGo = new GameObject("ColNum" + (col + 1), typeof(RectTransform), typeof(TextMeshProUGUI));
            numGo.transform.SetParent(boxGo.transform, false);
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = Vector2.zero;
            numRt.anchorMax = Vector2.one;
            numRt.sizeDelta = Vector2.zero;
            var numTmp = numGo.GetComponent<TextMeshProUGUI>();
            numTmp.text      = (col + 1).ToString();
            numTmp.font      = font;
            numTmp.fontSize  = 26f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color     = cyanHdr;
            numTmp.alignment = TextAlignmentOptions.Center;

            // "COL X" sub-label below box
            var subGo = new GameObject("ColSub" + (col + 1), typeof(RectTransform), typeof(TextMeshProUGUI));
            subGo.transform.SetParent(leftPanel.transform, false);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(0f, 1f);
            subRt.pivot     = new Vector2(0f, 1f);
            subRt.anchoredPosition = new Vector2(boxX, -324f);
            subRt.sizeDelta = new Vector2(60f, 16f);
            var subTmp = subGo.GetComponent<TextMeshProUGUI>();
            subTmp.text      = "COL\n" + (col + 1);
            subTmp.font      = font;
            subTmp.fontSize  = 9f;
            subTmp.fontStyle = FontStyles.Normal;
            subTmp.color     = new Color(0.60f, 0.70f, 0.90f);
            subTmp.alignment = TextAlignmentOptions.Center;
        }

        // Safety note
        CreateLabelTL(leftPanel.transform, "SafetyNote",
            "⚠ Tiles do NOT show safe/unsafe.\nUse the clues on the right only!",
            new Vector2(20f, -356f), new Vector2(300f, 40f), font, 10.5f, FontStyles.Normal,
            new Color(1.00f, 0.75f, 0.30f));

        // Timer (repurpose mapHintText)
        var timerHint = CreateLabelTL(leftPanel.transform, "TimerHint", "Time: 300.0s",
            new Vector2(20f, -408f), new Vector2(300f, 24f), font, 13f, FontStyles.Normal,
            new Color(0.70f, 0.80f, 1.00f));
        ui.mapHintText = timerHint;

        // ── RIGHT PANEL: Clue Cards ──────────────────────────────────────────
        var rightPanel = CreatePanel(canvasGo.transform, "ClueCardsPanel",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-36f, -36f), new Vector2(360f, 600f),
            panelBg, rounded);

        // Level header on right panel
        var rtTitle = CreateLabelTL(rightPanel.transform, "RTitle", "LEVEL 04",
            new Vector2(20f, -16f), new Vector2(320f, 28f), font, 20f, FontStyles.Bold, cyanHdr);
        rtTitle.alignment = TextAlignmentOptions.Left;

        var rtSub = CreateLabelTL(rightPanel.transform, "RSub", "NUMBER LOGIC",
            new Vector2(20f, -46f), new Vector2(320f, 18f), font, 11f, FontStyles.Bold, goldAccent);
        rtSub.alignment = TextAlignmentOptions.Left;

        // Objective text
        var objLabel = CreateLabelTL(rightPanel.transform, "Objective",
            "Solve the clue for each row and jump\nonto the correct tile. One wrong move\nand you must start again!",
            new Vector2(20f, -68f), new Vector2(320f, 54f), font, 11f, FontStyles.Normal, textBody);
        objLabel.textWrappingMode = TextWrappingModes.Normal;
        objLabel.alignment = TextAlignmentOptions.TopLeft;
        ui.objectiveText = objLabel;

        CreateColorBar(rightPanel.transform, "RDivider",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -128f), new Vector2(0f, 2f),
            new Color(0.20f, 0.65f, 1.00f, 0.5f));

        // 5 clue cards
        Color[] cardColors = new Color[]
        {
            new Color(0.08f, 0.12f, 0.26f),
            new Color(0.08f, 0.14f, 0.22f),
            new Color(0.10f, 0.12f, 0.26f),
            new Color(0.08f, 0.14f, 0.24f),
            new Color(0.10f, 0.12f, 0.22f),
        };
        Color[] accentColors = new Color[]
        {
            new Color(0.30f, 0.90f, 1.00f),
            new Color(0.30f, 0.88f, 0.78f),
            new Color(0.90f, 0.80f, 0.30f),
            new Color(1.00f, 0.65f, 0.30f),
            new Color(0.80f, 0.50f, 1.00f),
        };

        float cardY = -138f;
        for (int row = 0; row < 5; row++)
        {
            // Card background
            var cardGo = new GameObject("ClueCard_Row" + (row + 1), typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(rightPanel.transform, false);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 1f);
            cardRt.anchorMax = new Vector2(1f, 1f);
            cardRt.pivot     = new Vector2(0.5f, 1f);
            cardRt.sizeDelta = new Vector2(-24f, 82f);
            cardRt.anchoredPosition = new Vector2(0f, cardY);
            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = cardColors[row];
            if (rounded != null) { cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced; }

            // Left accent stripe
            var stripeGo = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripeGo.transform.SetParent(cardGo.transform, false);
            var stripeRt = stripeGo.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0f);
            stripeRt.anchorMax = new Vector2(0f, 1f);
            stripeRt.pivot     = new Vector2(0f, 0.5f);
            stripeRt.sizeDelta = new Vector2(4f, -8f);
            stripeRt.anchoredPosition = new Vector2(6f, 0f);
            var stripeImg = stripeGo.GetComponent<Image>();
            stripeImg.color = accentColors[row];
            if (rounded != null) { stripeImg.sprite = rounded; stripeImg.type = Image.Type.Sliced; }

            // "ROW X" header
            var rowHdrGo = new GameObject("RowHdr", typeof(RectTransform), typeof(TextMeshProUGUI));
            rowHdrGo.transform.SetParent(cardGo.transform, false);
            var rowHdrRt = rowHdrGo.GetComponent<RectTransform>();
            rowHdrRt.anchorMin = new Vector2(0f, 1f);
            rowHdrRt.anchorMax = new Vector2(1f, 1f);
            rowHdrRt.pivot     = new Vector2(0.5f, 1f);
            rowHdrRt.sizeDelta = new Vector2(-18f, 20f);
            rowHdrRt.anchoredPosition = new Vector2(6f, -8f);
            var rowHdrTmp = rowHdrGo.GetComponent<TextMeshProUGUI>();
            rowHdrTmp.text      = "ROW " + (row + 1);
            rowHdrTmp.font      = font;
            rowHdrTmp.fontSize  = 10f;
            rowHdrTmp.fontStyle = FontStyles.Bold;
            rowHdrTmp.color     = accentColors[row];
            rowHdrTmp.alignment = TextAlignmentOptions.Left;

            // Clue text
            var clueTxtGo = new GameObject("ClueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            clueTxtGo.transform.SetParent(cardGo.transform, false);
            var clueTxtRt = clueTxtGo.GetComponent<RectTransform>();
            clueTxtRt.anchorMin = new Vector2(0f, 0f);
            clueTxtRt.anchorMax = new Vector2(1f, 1f);
            clueTxtRt.sizeDelta = new Vector2(-18f, -28f);
            clueTxtRt.anchoredPosition = new Vector2(6f, -14f);
            var clueTmp = clueTxtGo.GetComponent<TextMeshProUGUI>();
            clueTmp.text      = rowClues[row];
            clueTmp.font      = font;
            clueTmp.fontSize  = 12.5f;
            clueTmp.fontStyle = FontStyles.Normal;
            clueTmp.color     = textBody;
            clueTmp.alignment = TextAlignmentOptions.TopLeft;
            clueTmp.textWrappingMode = TextWrappingModes.Normal;

            cardY -= 90f;  // move down for next card
        }

        // ── HUD PANEL: Top center (Timer + Progress) ──────────────────────────
        var hudPanel = CreatePanel(canvasGo.transform, "HudPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(340f, 88f),
            panelBg, rounded);

        var hudProg = CreateLabel(hudPanel.transform, "ProgText", "0 / 5",
            new Vector2(-80f, 0f), new Vector2(140f, 50f), font, 28f, FontStyles.Bold, cyanHdr);
        hudProg.alignment = TextAlignmentOptions.Center;
        // Note: progressText is already wired to the left panel counter.
        // The HUD will just show it as a secondary display (not wired to UIManager; purely visual)

        var hudTimer = CreateLabel(hudPanel.transform, "TimerText", "300.0s",
            new Vector2(80f, 0f), new Vector2(140f, 50f), font, 28f, FontStyles.Bold, Color.white);
        hudTimer.alignment = TextAlignmentOptions.Center;
        ui.timerText = hudTimer;

        // ── VICTORY PANEL ─────────────────────────────────────────────────────
        var vicPanel = CreatePanel(canvasGo.transform, "VictoryPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500f, 340f),
            new Color(0.04f, 0.06f, 0.14f, 0.98f), rounded);
        vicPanel.SetActive(false);
        ui.levelCompletePanel = vicPanel;

        var vicTitle = CreateLabel(vicPanel.transform, "VicTitle", "LOGIC MASTERED!",
            new Vector2(0f, 95f), new Vector2(460f, 50f), font, 32f, FontStyles.Bold,
            new Color(0.30f, 0.95f, 0.80f));
        vicTitle.alignment = TextAlignmentOptions.Center;

        var vicSub = CreateLabel(vicPanel.transform, "VicSub", "All 5 rows solved correctly.",
            new Vector2(0f, 55f), new Vector2(460f, 30f), font, 17f, FontStyles.Normal, textBody);
        vicSub.alignment = TextAlignmentOptions.Center;

        var vicTime = CreateLabel(vicPanel.transform, "VicTime", "Time: 142.0s",
            new Vector2(0f, 20f), new Vector2(460f, 30f), font, 18f, FontStyles.Normal, textBody);
        vicTime.alignment = TextAlignmentOptions.Center;
        ui.levelCompleteTime = vicTime;

        var nextBtn = CreateButton(vicPanel.transform, "NextBtn", "MAIN MENU",
            new Vector2(0f, -45f), new Vector2(240f, 52f), rounded, font,
            new Color(0.20f, 0.80f, 0.70f));
        ui.nextLevelButton = nextBtn.GetComponent<Button>();

        var replayBtn = CreateButton(vicPanel.transform, "ReplayBtn", "REPLAY LEVEL",
            new Vector2(0f, -112f), new Vector2(240f, 44f), rounded, font,
            new Color(0.12f, 0.20f, 0.36f));
        ui.completeMenuButton = replayBtn.GetComponent<Button>();

        // ── FAIL PANEL ────────────────────────────────────────────────────────
        var failPanel = CreatePanel(canvasGo.transform, "FailPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(460f, 250f),
            new Color(0.16f, 0.04f, 0.04f, 0.98f), rounded);
        failPanel.SetActive(false);
        ui.failPanel = failPanel;

        var failTitle = CreateLabel(failPanel.transform, "FailTitle", "WRONG TILE!",
            new Vector2(0f, 65f), new Vector2(420f, 50f), font, 30f, FontStyles.Bold,
            new Color(1f, 0.35f, 0.32f));
        failTitle.alignment = TextAlignmentOptions.Center;

        var failSub = CreateLabel(failPanel.transform, "FailSub",
            "That tile was incorrect. Re-read the clues carefully and try again!",
            new Vector2(0f, 15f), new Vector2(400f, 44f), font, 15f, FontStyles.Normal,
            new Color(0.95f, 0.80f, 0.80f));
        failSub.alignment = TextAlignmentOptions.Center;
        failSub.textWrappingMode = TextWrappingModes.Normal;
    }

    // ── SCENE BUILD SETTINGS ─────────────────────────────────────────────────
    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyAdded = false;
        foreach (var s in scenes)
            if (s.path == scenePath) { alreadyAdded = true; break; }
        if (!alreadyAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Level4 scene added to Build Settings.");
        }
    }

    // ── PLAYER BUILDER ───────────────────────────────────────────────────────
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
        cc.height     = 2.35f;
        cc.radius     = 0.38f;
        cc.center     = new Vector3(0f, 1.175f, 0f);
        cc.stepOffset = 0.35f;
        cc.skinWidth  = 0.03f;

        var pc = player.AddComponent<PlayerController>();
        pc.moveSpeed  = 6.5f;
        pc.jumpHeight = 2.2f;
        pc.gravity    = -22f;

        var visual   = player.AddComponent<PlayerVisual>();
        visual.model = model.transform;
        visual.torso = model.transform.Find("Torso");
        visual.head  = head.transform;
        visual.armL  = armL;
        visual.armR  = armR;
        visual.legL  = legL;
        visual.legR  = legR;

        return player;
    }

    // ── SHARED HELPERS ────────────────────────────────────────────────────────
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

    static GameObject AddPart(PrimitiveType type, string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        var col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material CreateMaterial(string path, Color color, float metallic, float smoothness, float emission)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);
        if (emission > 0.001f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 pos, Vector3 scale, Material mat, bool addCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        if (!addCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject CreateBeveledObject(string name, Transform parent, Vector3 localPos,
        Vector3 size, float bevel, Material mat, bool addCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = BeveledBoxMesh.Create(size.x, size.y, size.z, bevel);
        mr.sharedMaterial = mat;
        mr.receiveShadows = true;
        mr.shadowCastingMode = ShadowCastingMode.On;

        if (addCollider)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }
        return go;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    // ── UI HELPERS ────────────────────────────────────────────────────────────
    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
        Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        return go;
    }

    static TMP_Text CreateLabel(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, TMP_FontAsset font, float fontSize,
        FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = font;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        return tmp;
    }

    // Top-left anchored label
    static TMP_Text CreateLabelTL(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, TMP_FontAsset font, float fontSize,
        FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = font;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Sprite sprite, TMP_FontAsset font, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }

        var lblGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(go.transform, false);
        var lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        var tmp = lblGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.font      = font;
        tmp.fontSize  = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void CreateColorBar(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 delta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = delta;
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
    }

    static Sprite CreateRoundedSprite(string path, int size, int radius, bool isCircle)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = isCircle ? size * 0.5f : radius;
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 1f;
                if (isCircle)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    alpha = Mathf.Clamp01(r - dist + 0.5f);
                }
                else
                {
                    float cx = x < r ? r : (x > size - 1 - r ? size - 1 - r : x);
                    float cy = y < r ? r : (y > size - 1 - r ? size - 1 - r : y);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    alpha = Mathf.Clamp01(r - dist + 0.5f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteBorder = isCircle ? Vector4.zero : new Vector4(radius, radius, radius, radius);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
