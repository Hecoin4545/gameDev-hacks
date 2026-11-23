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
/// Builds Level 4 – Highly Polished Lava Theme with Number Logic puzzle.
/// Features a breathtaking volcanic caldera:
/// - Deep, glowing molten magma lake with volcanic basalt crusts
/// - Majestic volcanic mountain cones and crags ringing the horizon
/// - Obsidian start and end platforms with glowing molten lava borders and flame braziers
/// - Heavy volcanic basalt tiles with glowing magma edges and radiant molten gold completion
/// - Rising fiery ember spark particle system drifting upward into the dark volcanic sky
/// - Highly legible, enlarged UI text:
///   * Spacious Clue Cards (18.5pt bold white text on high-contrast obsidian cards)
///   * Large Guide Panel (32pt title, 36pt counter, 42pt bold column guide boxes)
///   * Prominent 3D floating column badges elevated at y = 3.2f above column entrances
///   * Clear etched glowing 3D numbers directly on the face of every tile in the arena
/// </summary>
public class BuildLevel4
{
    // ── Dedicated Lava Palette Materials (Assets/Materials/Lava_*.mat) ────────
    static Material lavaLakeMat;       // deep glowing molten magma sea
    static Material lavaCrustMat;      // dark floating basalt crust
    static Material obsidianMat;       // polished dark volcanic obsidian
    static Material basaltMat;         // porous volcanic rock
    static Material lavaVeinMat;       // glowing magma seam / accent
    static Material torchFlameMat;     // burning brazier flame crystal
    static Material mountainMat;       // volcanic mountain peak
    static Material mountainDarkMat;   // caldera shadow rock

    static Material tileFaceMat;       // volcanic obsidian puzzle tile face
    static Material tileEdgeLavaMat;   // glowing magma tile edge
    static Material tileRockBaseMat;   // volcanic basalt column base
    static Material tileRimMat;        // dark soot under-rim

    static Material goalPadLavaMat;    // molten gold & obsidian ceremonial altar
    static Material goalCrystalMat;    // radiant heart-of-volcano crystal

    // Player materials (shared)
    static Material hoodieMat, hoodMat, jeansMat, skinMat, hairMat, shoeMat, eyeMat, pupilMat;

    // ── Clue data (Row → answer column, 1-indexed) ───────────────────────────
    // Row 1: smallest prime = 2            → Col 2
    // Row 2: only even prime = 2           → Col 2
    // Row 3: sides of a triangle = 3       → Col 3
    // Row 4: square root of 16 = 4         → Col 4
    // Row 5: cardinal directions (N,S,E,W) = 4 → Col 4
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
        "Jump onto the tile representing the smallest prime number.",
        "Jump onto the tile representing the only even prime number.",
        "Jump onto the tile representing the number of sides in a triangle.",
        "Jump onto the tile representing the square root of 16.",
        "Jump onto the tile representing the number of cardinal directions (North, South, East, West).",
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
        gridManager.safeMat     = tileFaceMat;
        gridManager.unsafeMat   = tileFaceMat;
        gridManager.tileEdgeMat = tileEdgeLavaMat;
        gridManager.tileRockMat = tileRockBaseMat;
        gridManager.tileRimMat  = tileRimMat;
        gridManager.useHexTiles = false;
        
        // Polished lava tile colors: dark obsidian neutral, radiant molten gold completed
        gridManager.customNeutralColor      = new Color(0.12f, 0.08f, 0.08f);
        gridManager.customCompletedColor    = new Color(1.00f, 0.68f, 0.16f);
        gridManager.customCompletedEmission = new Color(1.00f, 0.44f, 0.05f) * 2.6f;

        float stride    = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;
        float halfDepth = gridDepth * 0.5f;
        float islandZOffset = halfDepth + 3.6f;

        // ── START / END PLATFORMS ─────────────────────────────────────────────
        var startIsland = CreateVolcanicPlatform("StartPlatform", null,
            new Vector3(0f, 0.11f, -islandZOffset), 5.8f, 4.8f, true);

        var endIsland = CreateVolcanicPlatform("EndPlatform", null,
            new Vector3(0f, 0.11f, islandZOffset), 5.8f, 4.8f, true);

        // Goal pad – Molten gold & obsidian altar
        CreateBeveledObject("GoalPad", endIsland.transform,
            new Vector3(0f, 0.06f, 0f),
            new Vector3(2.6f, 0.12f, 2.6f), 0.06f, goalPadLavaMat, false);

        // 4 Magma accent pillars around goal pad with flame braziers
        for (int i = 0; i < 4; i++)
        {
            float ox = (i % 2 == 0 ? -1f : 1f) * 1.55f;
            float oz = (i < 2 ? -1f : 1f) * 1.45f;
            CreateBeveledObject("GoalPillar" + i, endIsland.transform,
                new Vector3(ox, 1.1f, oz),
                new Vector3(0.22f, 2.2f, 0.22f), 0.05f, lavaVeinMat, false);

            CreatePrimitive(PrimitiveType.Sphere, "PillarFlame" + i, endIsland.transform,
                new Vector3(ox, 2.3f, oz), new Vector3(0.28f, 0.38f, 0.28f), torchFlameMat, false);
        }

        // Floating Heart of Volcano crystal
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.6f, 0f), new Vector3(0.68f, 0.68f, 0.68f), goalCrystalMat, false);
        marker.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

        // Goal point light for warm illumination
        var goalLightGo = new GameObject("GoalLight");
        goalLightGo.transform.SetParent(endIsland.transform, false);
        goalLightGo.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        var goalPl = goalLightGo.AddComponent<Light>();
        goalPl.type = LightType.Point;
        goalPl.color = new Color(1.0f, 0.58f, 0.18f);
        goalPl.intensity = 3.6f;
        goalPl.range = 8.5f;

        // End trigger
        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size      = new Vector3(5.2f, 3.2f, 4.2f);
        endBox.isTrigger = true;
        endTrigger.AddComponent<EndPlatformTrigger>();

        // Build grid
        gridManager.Build(levelData);

        // ── ETCHED COLUMN LABELS ON EVERY TILE & 3D FLOATING HEADERS ──────────
        AddColumnLabelsAndTileNumbers(levelData, stride, halfDepth);

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
        cam.farClipPlane  = 1000f;
        cam.allowHDR      = true;
        cam.allowMSAA     = true;
        cam.clearFlags    = CameraClearFlags.Skybox;
        
        // Position camera behind player facing forward
        cam.transform.position = player.transform.position + new Vector3(0f, 8.5f, -11.0f);
        cam.transform.rotation = Quaternion.Euler(34f, 0f, 0f);

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
        cf.pitch          = 35f;
        cf.yaw            = 0f;
        cf.targetHeight   = 1.35f;
        cf.lookAhead      = 4.0f;
        cf.followSmooth   = 12f;
        cf.rotationSmooth = 16f;
        cf.minDistance    = 7f;
        cf.maxDistance    = 22f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        // ── VOLCANIC LIGHTING & POST-PROCESSING ──────────────────────────────
        ConfigureLighting();
        ConfigurePostProcessing();

        // ── POLISHED LAVA ENVIRONMENT ─────────────────────────────────────────
        var envRoot = new GameObject("Environment").transform;
        BuildLavaEnvironment(envRoot);

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
            "Welcome to the Molten Caldera! Solve the mathematical clue for each row to find the only safe column. One wrong step plunges you into the inferno!"
        };

        var gmGo = new GameObject("GameManager");
        var gm   = gmGo.AddComponent<GameManager>();
        var l1   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset");
        var l2   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        var l3   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level3Data.asset");
        gm.levels            = new LevelData[] { l1, l2, l3, levelData };
        gm.currentLevelIndex = 3;

        // ── SETUP AUDIO MANAGER IN SCENE ─────────────────────────────────────
        var audioGo = new GameObject("AudioManager");
        var am = audioGo.AddComponent<AudioManager>();
        am.bgMusicClips = new AudioClip[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic2.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic3.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGMusic4.wav")
        };
        am.ambientClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambient.wav");
        am.rightJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/RightJump.wav");
        am.wrongJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/WrongJump.wav");
        am.levelCompleteClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelComplete.wav");
        am.buttonClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ButtonClick.wav");

        // ── UI (POLISHED LAVA THEME & LARGE READABLE TEXT) ───────────────────
        BuildUI(lm, levelData);

        // ── EVENT SYSTEM ─────────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── ENVIRONMENT ANIMATOR ──────────────────────────────────────────────
        envRoot.gameObject.AddComponent<EnvironmentAnimator>();

        // ── SAVE SCENE & ADD TO BUILD SETTINGS ───────────────────────────────
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AddSceneToBuildSettings(scenePath);

        Debug.Log("Level 4 (Polished Lava Theme & Number Logic) built successfully!");
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
        d.timeLimit  = 300f;
        d.objective  = "Solve the mathematical clue for each row to find the only safe column (1, 2, 3, or 4). One wrong move drops you into the magma!";
        d.instructions = "WASD / Arrows – Move\nSpace – Jump\nRight drag / Q E – Rotate camera\nMouse wheel – Zoom\nR – Restart\nF – Dance";
        d.hideMapAfterCountdown = false;
        d.mapVisibleDuration    = 0f;
        d.isUpsideDownPattern   = false;

        d.safeTiles = new List<Vector2Int>(safeTileArray);
        d.sequence  = new List<Vector2Int>(safeTileArray);
        d.displayedPattern = new List<Vector2Int>();

        EditorUtility.SetDirty(d);
        return d;
    }

    // ── MATERIALS ────────────────────────────────────────────────────────────
    static void CreateMaterials()
    {
        // Molten lava sea – deep radiant incandescent magma (calm and rich, not blown out)
        lavaLakeMat     = CreateEmissiveMaterial("Assets/Materials/Lava_Lake.mat",
            new Color(0.80f, 0.16f, 0.02f), 0.05f, 0.85f,
            new Color(0.90f, 0.25f, 0.03f) * 0.65f);

        // Volcanic basalt crust – dark volcanic slag
        lavaCrustMat    = CreateMaterial("Assets/Materials/Lava_Crust.mat",
            new Color(0.12f, 0.08f, 0.07f), 0.10f, 0.25f, 0f);

        // Polished obsidian platform slab
        obsidianMat     = CreateMaterial("Assets/Materials/Lava_Obsidian.mat",
            new Color(0.09f, 0.08f, 0.11f), 0.18f, 0.72f, 0.02f);

        // Porous basalt stone for crags and spires
        basaltMat       = CreateMaterial("Assets/Materials/Lava_Basalt.mat",
            new Color(0.15f, 0.11f, 0.10f), 0.08f, 0.22f, 0f);

        // Glowing magma veins / seams
        lavaVeinMat     = CreateEmissiveMaterial("Assets/Materials/Lava_Vein.mat",
            new Color(1.00f, 0.45f, 0.05f), 0.10f, 0.65f,
            new Color(1.00f, 0.46f, 0.07f) * 2.2f);

        // Brazier flame crystal
        torchFlameMat   = CreateEmissiveMaterial("Assets/Materials/Lava_Flame.mat",
            new Color(1.00f, 0.75f, 0.10f), 0.0f, 0.90f,
            new Color(1.00f, 0.60f, 0.10f) * 3.5f);

        // Mountain cones
        mountainMat     = CreateMaterial("Assets/Materials/Lava_Mountain.mat",
            new Color(0.16f, 0.10f, 0.09f), 0.05f, 0.15f, 0f);

        mountainDarkMat = CreateMaterial("Assets/Materials/Lava_MountainDark.mat",
            new Color(0.10f, 0.06f, 0.05f), 0.05f, 0.12f, 0f);

        // Puzzle Tile materials
        tileFaceMat     = CreateMaterial("Assets/Materials/Lava_TileFace.mat",
            new Color(0.13f, 0.10f, 0.11f), 0.15f, 0.58f, 0f);

        tileEdgeLavaMat = CreateEmissiveMaterial("Assets/Materials/Lava_TileEdge.mat",
            new Color(0.95f, 0.35f, 0.05f), 0.12f, 0.65f,
            new Color(1.00f, 0.35f, 0.05f) * 1.8f);

        tileRockBaseMat = CreateMaterial("Assets/Materials/Lava_TileRock.mat",
            new Color(0.10f, 0.07f, 0.07f), 0.08f, 0.20f, 0f);

        tileRimMat      = CreateMaterial("Assets/Materials/Lava_TileRim.mat",
            new Color(0.06f, 0.04f, 0.04f), 0f, 0.15f, 0f);

        // Goal Altar & Crystal
        goalPadLavaMat  = CreateEmissiveMaterial("Assets/Materials/Lava_GoalPad.mat",
            new Color(1.00f, 0.70f, 0.15f), 0.25f, 0.75f,
            new Color(1.00f, 0.55f, 0.10f) * 1.8f);

        goalCrystalMat  = CreateEmissiveMaterial("Assets/Materials/Lava_GoalCrystal.mat",
            new Color(1.00f, 0.20f, 0.05f), 0.05f, 0.95f,
            new Color(1.00f, 0.25f, 0.05f) * 4.2f);

        // Player (shared)
        hoodieMat  = CreateMaterial("Assets/Materials/Hoodie.mat", new Color(0.74f, 0.13f, 0.14f), 0f, 0.22f, 0.04f);
        hoodMat    = CreateMaterial("Assets/Materials/Hood.mat",   new Color(0.60f, 0.10f, 0.11f), 0f, 0.20f, 0.02f);
        jeansMat   = CreateMaterial("Assets/Materials/Jeans.mat",  new Color(0.16f, 0.33f, 0.68f), 0f, 0.16f, 0f);
        skinMat    = CreateMaterial("Assets/Materials/Skin.mat",   new Color(0.93f, 0.74f, 0.59f), 0f, 0.20f, 0f);
        hairMat    = CreateMaterial("Assets/Materials/Hair.mat",   new Color(0.20f, 0.12f, 0.07f), 0f, 0.24f, 0f);
        shoeMat    = CreateMaterial("Assets/Materials/Shoe.mat",   new Color(0.93f, 0.94f, 0.95f), 0f, 0.35f, 0f);
        eyeMat     = CreateMaterial("Assets/Materials/Eye.mat",    Color.white,                    0f, 0.40f, 0f);
        pupilMat   = CreateMaterial("Assets/Materials/Pupil.mat",  new Color(0.06f, 0.06f, 0.08f), 0f, 0.40f, 0f);
    }

    // ── COLUMN NUMBER LABELS & ON-TILE ETCHED NUMBERS ────────────────────────
    static void AddColumnLabelsAndTileNumbers(LevelData data, float stride, float halfDepth)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        if (font == null) return;

        float halfCols = (data.columns - 1) * stride * 0.5f;
        float row1Z    = -(data.rows - 1) * stride * 0.5f;

        // 1. Large 3D Floating Column Badges facing player (elevated at y = 3.2f above the entrance)
        for (int col = 0; col < data.columns; col++)
        {
            float xPos = -halfCols + col * stride;

            var badge = new GameObject("ColBadge_" + (col + 1));
            // Position elevated above entrance and tilted slightly back to face camera
            badge.transform.position = new Vector3(xPos, 3.2f, row1Z - 0.4f);
            badge.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

            // Backing stone shield
            CreateBeveledObject("BadgePlate", badge.transform,
                new Vector3(0f, 0f, 0.06f), new Vector3(1.35f, 0.95f, 0.10f), 0.08f, obsidianMat, false);
            // Glowing magma border
            CreateBeveledObject("BadgeBorder", badge.transform,
                new Vector3(0f, 0f, 0.04f), new Vector3(1.45f, 1.05f, 0.05f), 0.04f, lavaVeinMat, false);

            // 3D TextMeshPro for column number
            var numGo = new GameObject("NumText");
            numGo.transform.SetParent(badge.transform, false);
            numGo.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            var numTmp = numGo.AddComponent<TextMeshPro>();
            numTmp.text      = (col + 1).ToString();
            numTmp.font      = font;
            numTmp.fontSize  = 11f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color     = new Color(1.0f, 0.88f, 0.35f);
            numTmp.alignment = TextAlignmentOptions.Center;

            // "COL X" sub-label
            var subGo = new GameObject("SubText");
            subGo.transform.SetParent(badge.transform, false);
            subGo.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            var subTmp = subGo.AddComponent<TextMeshPro>();
            subTmp.text      = "COL " + (col + 1);
            subTmp.font      = font;
            subTmp.fontSize  = 3.2f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.color     = new Color(1.0f, 0.65f, 0.20f);
            subTmp.alignment = TextAlignmentOptions.Center;
        }

        // 2. Etched glowing 3D numbers directly on EVERY tile face
        for (int r = 0; r < data.rows; r++)
        {
            for (int c = 0; c < data.columns; c++)
            {
                var tileGo = GameObject.Find(string.Format("Tile_R{0}_C{1}", r + 1, c + 1));
                if (tileGo == null) continue;

                var labelGo = new GameObject("TileNum3D");
                labelGo.transform.SetParent(tileGo.transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 0.125f, 0f);
                labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text      = (c + 1).ToString();
                tmp.font      = font;
                tmp.fontSize  = 9.5f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color     = new Color(1.0f, 0.82f, 0.38f, 0.88f);
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    // ── VOLCANIC PLATFORM BUILDER ─────────────────────────────────────────────
    static GameObject CreateVolcanicPlatform(string name, Transform parent, Vector3 topCenter,
        float width, float depth, bool addCollider)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = topCenter;

        // Top slab – Polished obsidian
        var slab = CreateBeveledObject(name + "_Top", root.transform,
            new Vector3(0f, -0.25f, 0f),
            new Vector3(width, 0.50f, depth), 0.12f, obsidianMat, addCollider);
        slab.GetComponent<Renderer>().receiveShadows = true;

        // Lava vein trim – glowing border under the top slab
        CreateBeveledObject(name + "_VeinRim", root.transform,
            new Vector3(0f, -0.52f, 0f),
            new Vector3(width * 1.02f, 0.08f, depth * 1.02f), 0.04f, lavaVeinMat, false);

        // Mid bevel – Dark basalt rock
        CreateBeveledObject(name + "_Mid", root.transform,
            new Vector3(0f, -1.05f, 0f),
            new Vector3(width * 0.90f, 1.10f, depth * 0.90f), 0.15f, basaltMat, false);

        // Bottom volcanic root dipping down toward the magma
        CreateBeveledObject(name + "_Bot", root.transform,
            new Vector3(0f, -1.85f, 0f),
            new Vector3(width * 0.55f, 1.00f, depth * 0.55f), 0.14f, lavaCrustMat, false);

        // 4 Stone fire braziers with warm point lights at the 4 corners
        float hx = (width * 0.5f) - 0.45f;
        float hz = (depth * 0.5f) - 0.45f;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-hx, 0f, -hz),
            new Vector3( hx, 0f, -hz),
            new Vector3(-hx, 0f,  hz),
            new Vector3( hx, 0f,  hz),
        };

        for (int i = 0; i < corners.Length; i++)
        {
            var p = corners[i];
            CreateBeveledObject(name + "_BrazierPedestal" + i, root.transform,
                p + new Vector3(0f, 0.45f, 0f),
                new Vector3(0.42f, 0.90f, 0.42f), 0.08f, basaltMat, false);

            CreateBeveledObject(name + "_BrazierBowl" + i, root.transform,
                p + new Vector3(0f, 0.95f, 0f),
                new Vector3(0.60f, 0.20f, 0.60f), 0.06f, obsidianMat, false);

            CreatePrimitive(PrimitiveType.Sphere, name + "_Flame" + i, root.transform,
                p + new Vector3(0f, 1.20f, 0f),
                new Vector3(0.36f, 0.50f, 0.36f), torchFlameMat, false);

            var bLightGo = new GameObject(name + "_Light" + i);
            bLightGo.transform.SetParent(root.transform, false);
            bLightGo.transform.localPosition = p + new Vector3(0f, 1.4f, 0f);
            var bLight = bLightGo.AddComponent<Light>();
            bLight.type = LightType.Point;
            bLight.color = new Color(1.0f, 0.55f, 0.15f);
            bLight.intensity = 2.4f;
            bLight.range = 7.0f;
        }

        return root;
    }

    // ── POLISHED LAVA ENVIRONMENT ─────────────────────────────────────────────
    static void BuildLavaEnvironment(Transform root)
    {
        const float lavaY = -28f;

        // 1. Vast molten lava lake (covers entire field of view to the horizon)
        var lavaPlane = CreatePrimitive(PrimitiveType.Plane, "LavaLake", root,
            new Vector3(0f, lavaY, 0f), new Vector3(150f, 1f, 150f), lavaLakeMat, false);
        lavaPlane.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // 2. 16 Jagged volcanic basalt pillars with glowing molten rock bands
        UnityEngine.Random.InitState(54321);
        for (int i = 0; i < 16; i++)
        {
            float angle  = i * (360f / 16f) + 11f;
            float radius = 16f + (i % 4) * 5.0f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                lavaY + 12f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

            float thick = 0.8f + (i % 3) * 0.45f;
            CreateBeveledObject("LavaPillar" + i, root, pos,
                new Vector3(thick, 28f, thick), 0.10f, basaltMat, false);

            CreateBeveledObject("PillarLavaSeam" + i, root,
                new Vector3(pos.x, lavaY + 1.2f, pos.z),
                new Vector3(thick * 1.25f, 0.60f, thick * 1.25f), 0.05f, lavaVeinMat, false);
        }

        // 3. Faceted volcanic mountain cones & caldera peaks ringing the distant horizon
        for (int i = 0; i < 14; i++)
        {
            float angle    = i * (360f / 14f) + 12f;
            float radius   = 95f + (i % 4) * 22f;
            float height   = 45f + (i % 3) * 20f;
            float diameter = 48f + (i % 4) * 20f;
            // Base submerged 5m below lava surface for seamless natural emergence
            Vector3 pos    = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                lavaY + height * 0.5f - 5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

            CreateCone("VolcanoCone" + i, root, pos, diameter, height,
                i % 2 == 0 ? mountainMat : mountainDarkMat, false);
        }

        // 4. 14 Floating volcanic basalt crust rafts on the lava surface
        for (int i = 0; i < 14; i++)
        {
            float angle  = i * (360f / 14f) + 7f;
            float radius = 24f + (i % 3) * 12f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                lavaY + 0.35f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

            float raftW = 4.5f + (i % 3) * 2.5f;
            float raftD = 4.0f + (i % 4) * 2.0f;
            CreateBeveledObject("LavaRaft" + i, root, pos,
                new Vector3(raftW, 0.6f, raftD), 0.12f, lavaCrustMat, false);
        }

        // 5. Rising Ember Spark Particle System
        BuildVolcanicEmbers(root);
    }

    static void BuildVolcanicEmbers(Transform root)
    {
        var psGo = new GameObject("VolcanicEmbers");
        psGo.transform.SetParent(root, false);
        psGo.transform.position = new Vector3(0f, -6f, 0f);

        var ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop            = true;
        main.startLifetime   = 6.5f;
        main.startSpeed      = 2.2f;
        main.startSize       = new ParticleSystem.MinMaxCurve(0.14f, 0.30f);
        main.maxParticles    = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1.0f, 0.85f, 0.3f), 0.0f),
                new GradientColorKey(new Color(1.0f, 0.45f, 0.05f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.15f, 0.02f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.85f, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = ps.emission;
        emission.rateOverTime = 50f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(36f, 2f, 36f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        var rend = psGo.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = torchFlameMat;
        rend.renderMode     = ParticleSystemRenderMode.Billboard;
    }

    // ── LIGHTING ─────────────────────────────────────────────────────────────
    static void ConfigureLighting()
    {
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo == null) lightGo = new GameObject("Directional Light");
        var l = lightGo.GetComponent<Light>();
        if (l == null) l = lightGo.AddComponent<Light>();

        l.type      = LightType.Directional;
        l.color     = new Color(1.0f, 0.74f, 0.52f); // Warm incandescent volcanic sun
        l.intensity = 1.15f;
        l.shadows   = LightShadows.Soft;
        l.shadowStrength   = 0.70f;
        l.shadowBias       = 0.04f;
        l.shadowNormalBias = 0.35f;
        
        // Light shines forward across the arena so all faces facing player are well-lit
        lightGo.transform.rotation = Quaternion.Euler(42f, 25f, 0f);

        // Ambient – Warm volcanic trilight (dark ash sky, dark equator, glowing lava ground bounce)
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.16f, 0.09f, 0.07f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.06f, 0.04f);
        RenderSettings.ambientGroundColor  = new Color(0.35f, 0.10f, 0.03f);
        RenderSettings.ambientIntensity    = 1.0f;

        // Procedural volcanic skybox
        var skyboxMat = CreateLavaSkybox();
        RenderSettings.skybox = skyboxMat;

        // Warm smoky ember fog
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = new Color(0.18f, 0.06f, 0.03f);
        RenderSettings.fogDensity = 0.0022f;
    }

    static Material CreateLavaSkybox()
    {
        string path   = "Assets/Materials/Lava_Skybox.mat";
        var shader = Shader.Find("Skybox/Procedural");
        var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetFloat("_SunSize", 0.02f);
        mat.SetFloat("_SunSizeConvergence", 5f);
        mat.SetFloat("_AtmosphereThickness", 0.40f);
        mat.SetColor("_SkyTint",    new Color(0.08f, 0.04f, 0.04f));
        mat.SetColor("_GroundColor",new Color(0.20f, 0.06f, 0.02f));
        mat.SetFloat("_Exposure", 0.70f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigurePostProcessing()
    {
        string path = "Assets/Settings/Lava_Profile.asset";
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
        bloom.intensity.Override(1.15f);
        bloom.scatter.Override(0.70f);
        bloom.tint.Override(new Color(1.00f, 0.65f, 0.35f));

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.05f);
        ca.contrast.Override(5f);         // Reduced from 12 — was making midtones too dark
        ca.saturation.Override(8f);
        ca.colorFilter.Override(new Color(1.00f, 0.94f, 0.88f));

        // Very subtle vignette — just a soft edge darkening, not centre shadow
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.18f);    // Reduced from 0.30 — was too dark
        vig.smoothness.Override(0.65f);   // Softer falloff
        vig.color.Override(new Color(0.06f, 0.01f, 0.01f));

        var go     = new GameObject("GlobalVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile  = profile;

        EditorUtility.SetDirty(profile);
    }

    // ── UI BUILDER (POLISHED LAVA THEME & LARGE READABLE TEXT) ────────────────
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

        // Rich obsidian dark backing with warm amber headers
        Color panelBg    = new Color(0.06f, 0.03f, 0.02f, 0.96f);
        Color amberHdr   = new Color(1.00f, 0.70f, 0.22f);
        Color goldAccent = new Color(1.00f, 0.85f, 0.30f);

        // ── LEFT PANEL: Puzzle Guide (400px wide, large clear text) ──────────
        var leftPanel = CreatePanel(canvasGo.transform, "PuzzleGuidePanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(36f, -36f), new Vector2(400f, 570f),
            panelBg, rounded);

        CreatePanelBorder(leftPanel.transform, "BorderGlow", new Color(1.0f, 0.50f, 0.15f, 0.85f), rounded);

        // Level title: 32pt Bold
        var ltTitle = CreateLabelTL(leftPanel.transform, "LTitle", "LEVEL 04",
            new Vector2(24f, -20f), new Vector2(350f, 40f), font, 32f, FontStyles.Bold, amberHdr);
        ltTitle.alignment = TextAlignmentOptions.Left;
        ui.levelTitle = ltTitle;

        // Subtitle: 16pt Bold
        var ltSub = CreateLabelTL(leftPanel.transform, "LSub", "MOLTEN LOGIC CALDERA",
            new Vector2(24f, -62f), new Vector2(350f, 24f), font, 16f, FontStyles.Bold, goldAccent);
        ltSub.alignment = TextAlignmentOptions.Left;
        ui.subtitle = ltSub;

        CreateColorBar(leftPanel.transform, "Divider1",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -92f), new Vector2(0f, 2.5f),
            new Color(1.00f, 0.50f, 0.15f, 0.65f));

        // Rows completed header: 15pt Bold
        CreateLabelTL(leftPanel.transform, "RowsHdr", "ROWS COMPLETED",
            new Vector2(24f, -104f), new Vector2(350f, 22f), font, 15f, FontStyles.Bold,
            new Color(0.92f, 0.78f, 0.68f));

        // Rows completed value: 36pt Bold
        var progressLbl = CreateLabelTL(leftPanel.transform, "ProgressVal", "0 / 5",
            new Vector2(24f, -130f), new Vector2(350f, 42f), font, 36f, FontStyles.Bold, amberHdr);
        progressLbl.alignment = TextAlignmentOptions.Left;
        ui.progressText = progressLbl;

        // Progress bar background
        var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(leftPanel.transform, false);
        var barBgRt = barBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0f, 1f);
        barBgRt.anchorMax = new Vector2(1f, 1f);
        barBgRt.pivot     = new Vector2(0.5f, 1f);
        barBgRt.sizeDelta = new Vector2(-48f, 12f);
        barBgRt.anchoredPosition = new Vector2(0f, -180f);
        var barBgImg = barBg.GetComponent<Image>();
        barBgImg.color = new Color(0.18f, 0.08f, 0.06f);
        if (rounded != null) { barBgImg.sprite = rounded; barBgImg.type = Image.Type.Sliced; }

        // Progress bar fill: radiant molten gold
        var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        var barFillRt = barFill.GetComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.sizeDelta = Vector2.zero;
        var barFillImg = barFill.GetComponent<Image>();
        barFillImg.color = new Color(1.00f, 0.72f, 0.15f);
        barFillImg.type  = Image.Type.Filled;
        barFillImg.fillMethod  = Image.FillMethod.Horizontal;
        barFillImg.fillAmount  = 0f;
        ui.progressFill = barFillImg;

        // Note: 15pt readable text
        var noteLabel = CreateLabelTL(leftPanel.transform, "NoteLabel",
            "Solve the clue on the right for each row.\nJump onto the matching column (1–4).\nSafe tiles lock in molten gold!",
            new Vector2(24f, -204f), new Vector2(352f, 54f), font, 15f, FontStyles.Normal,
            new Color(1.00f, 0.94f, 0.85f));
        noteLabel.textWrappingMode = TextWrappingModes.Normal;

        CreateColorBar(leftPanel.transform, "Divider2",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -268f), new Vector2(0f, 2.5f),
            new Color(1.00f, 0.50f, 0.15f, 0.65f));

        // Column guide header: 15pt Bold
        CreateLabelTL(leftPanel.transform, "ColHdr", "COLUMN GUIDE (1 – 4)",
            new Vector2(24f, -280f), new Vector2(350f, 22f), font, 15f, FontStyles.Bold,
            new Color(0.92f, 0.78f, 0.68f));

        // 4 enlarged column boxes (76x76px) with numbers 1–4: 42pt Bold
        for (int col = 0; col < 4; col++)
        {
            float boxX = 24f + col * 88f;

            var boxGo = new GameObject("ColBox" + (col + 1), typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(leftPanel.transform, false);
            var boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0f, 1f);
            boxRt.anchorMax = new Vector2(0f, 1f);
            boxRt.pivot     = new Vector2(0f, 1f);
            boxRt.anchoredPosition = new Vector2(boxX, -310f);
            boxRt.sizeDelta = new Vector2(76f, 76f);
            var boxImg = boxGo.GetComponent<Image>();
            boxImg.color = new Color(0.20f, 0.09f, 0.07f);
            if (rounded != null) { boxImg.sprite = rounded; boxImg.type = Image.Type.Sliced; }

            // Number: 42pt Bold
            var numGo = new GameObject("ColNum" + (col + 1), typeof(RectTransform), typeof(TextMeshProUGUI));
            numGo.transform.SetParent(boxGo.transform, false);
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = Vector2.zero;
            numRt.anchorMax = Vector2.one;
            numRt.sizeDelta = Vector2.zero;
            var numTmp = numGo.GetComponent<TextMeshProUGUI>();
            numTmp.text      = (col + 1).ToString();
            numTmp.font      = font;
            numTmp.fontSize  = 42f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color     = goldAccent;
            numTmp.alignment = TextAlignmentOptions.Center;

            // "COL X" sub-label: 15pt Bold
            var subGo = new GameObject("ColSub" + (col + 1), typeof(RectTransform), typeof(TextMeshProUGUI));
            subGo.transform.SetParent(leftPanel.transform, false);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(0f, 1f);
            subRt.pivot     = new Vector2(0f, 1f);
            subRt.anchoredPosition = new Vector2(boxX, -394f);
            subRt.sizeDelta = new Vector2(76f, 24f);
            var subTmp = subGo.GetComponent<TextMeshProUGUI>();
            subTmp.text      = "COL " + (col + 1);
            subTmp.font      = font;
            subTmp.fontSize  = 15f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.color     = new Color(1.0f, 0.70f, 0.35f);
            subTmp.alignment = TextAlignmentOptions.Center;
        }

        // Safety warning note: 15pt Bold in warm gold
        CreateLabelTL(leftPanel.transform, "SafetyNote",
            "[!] Tiles do NOT show safe/unsafe.\nUse the clues on the right only!",
            new Vector2(24f, -430f), new Vector2(350f, 50f), font, 15f, FontStyles.Bold,
            new Color(1.00f, 0.75f, 0.25f));

        // ── RIGHT PANEL: Clue Cards (490px wide, 640px high, 18.5pt bold text) ─
        var rightPanel = CreatePanel(canvasGo.transform, "ClueCardsPanel",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-36f, -36f), new Vector2(490f, 640f),
            panelBg, rounded);

        CreatePanelBorder(rightPanel.transform, "RBorderGlow", new Color(1.0f, 0.50f, 0.15f, 0.85f), rounded);

        // Header: 24pt Bold
        var rtTitle = CreateLabelTL(rightPanel.transform, "RTitle", "INFERNAL LOGIC CLUES",
            new Vector2(24f, -18f), new Vector2(440f, 32f), font, 24f, FontStyles.Bold, amberHdr);
        rtTitle.alignment = TextAlignmentOptions.Left;

        // Objective text: 16.5pt Bold warm white
        var objLabel = CreateLabelTL(rightPanel.transform, "Objective",
            "Solve the clue for each row to find the only safe column (1, 2, 3, or 4). One wrong step drops you into the magma!",
            new Vector2(24f, -54f), new Vector2(440f, 58f), font, 16.5f, FontStyles.Normal,
            new Color(1.00f, 0.94f, 0.85f));
        objLabel.textWrappingMode = TextWrappingModes.Normal;

        CreateColorBar(rightPanel.transform, "RDivider",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -118f), new Vector2(0f, 2.5f),
            new Color(1.00f, 0.50f, 0.15f, 0.65f));

        // 5 Spacious Clue Cards (98px height each, 18.5pt Bold font)
        Color[] cardAccents = new Color[]
        {
            new Color(1.00f, 0.78f, 0.22f), // Row 1: radiant gold
            new Color(1.00f, 0.52f, 0.15f), // Row 2: flame orange
            new Color(1.00f, 0.36f, 0.18f), // Row 3: warm crimson
            new Color(0.96f, 0.72f, 0.25f), // Row 4: amber gold
            new Color(1.00f, 0.60f, 0.20f), // Row 5: sunburst
        };

        float cardY = -128f;
        for (int row = 0; row < 5; row++)
        {
            var cardGo = new GameObject("ClueCard_Row" + (row + 1), typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(rightPanel.transform, false);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 1f);
            cardRt.anchorMax = new Vector2(1f, 1f);
            cardRt.pivot     = new Vector2(0.5f, 1f);
            cardRt.sizeDelta = new Vector2(-28f, 96f);
            cardRt.anchoredPosition = new Vector2(0f, cardY);
            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.12f, 0.06f, 0.05f, 0.98f);
            if (rounded != null) { cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced; }

            // Left accent stripe
            var stripeGo = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripeGo.transform.SetParent(cardGo.transform, false);
            var stripeRt = stripeGo.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0f);
            stripeRt.anchorMax = new Vector2(0f, 1f);
            stripeRt.pivot     = new Vector2(0f, 0.5f);
            stripeRt.sizeDelta = new Vector2(6f, -10f);
            stripeRt.anchoredPosition = new Vector2(8f, 0f);
            var stripeImg = stripeGo.GetComponent<Image>();
            stripeImg.color = cardAccents[row];
            if (rounded != null) { stripeImg.sprite = rounded; stripeImg.type = Image.Type.Sliced; }

            // "ROW X" header: 16pt Bold
            var rowHdrGo = new GameObject("RowHdr", typeof(RectTransform), typeof(TextMeshProUGUI));
            rowHdrGo.transform.SetParent(cardGo.transform, false);
            var rowHdrRt = rowHdrGo.GetComponent<RectTransform>();
            rowHdrRt.anchorMin = new Vector2(0f, 1f);
            rowHdrRt.anchorMax = new Vector2(1f, 1f);
            rowHdrRt.pivot     = new Vector2(0.5f, 1f);
            rowHdrRt.sizeDelta = new Vector2(-36f, 22f);
            rowHdrRt.anchoredPosition = new Vector2(14f, -8f);
            var rowHdrTmp = rowHdrGo.GetComponent<TextMeshProUGUI>();
            rowHdrTmp.text      = "ROW " + (row + 1);
            rowHdrTmp.font      = font;
            rowHdrTmp.fontSize  = 16f;
            rowHdrTmp.fontStyle = FontStyles.Bold;
            rowHdrTmp.color     = cardAccents[row];
            rowHdrTmp.alignment = TextAlignmentOptions.Left;

            // Clue text: LARGE 18.5pt Bold Crisp Pure White
            var clueTxtGo = new GameObject("ClueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            clueTxtGo.transform.SetParent(cardGo.transform, false);
            var clueTxtRt = clueTxtGo.GetComponent<RectTransform>();
            clueTxtRt.anchorMin = new Vector2(0f, 0f);
            clueTxtRt.anchorMax = new Vector2(1f, 1f);
            clueTxtRt.sizeDelta = new Vector2(-36f, -34f);
            clueTxtRt.anchoredPosition = new Vector2(14f, -18f);
            var clueTmp = clueTxtGo.GetComponent<TextMeshProUGUI>();
            clueTmp.text      = rowClues[row];
            clueTmp.font      = font;
            clueTmp.fontSize  = 18.5f;
            clueTmp.fontStyle = FontStyles.Bold;
            clueTmp.color     = Color.white;
            clueTmp.alignment = TextAlignmentOptions.TopLeft;
            clueTmp.textWrappingMode = TextWrappingModes.Normal;

            cardY -= 100f;
        }

        // ── HUD PANEL: Top Center (Timer + Progress): 34pt Bold ───────────────
        var hudPanel = CreatePanel(canvasGo.transform, "HudPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(360f, 92f),
            panelBg, rounded);
        CreatePanelBorder(hudPanel.transform, "HudBorder", new Color(1.0f, 0.50f, 0.15f, 0.85f), rounded);

        var hudProg = CreateLabel(hudPanel.transform, "ProgText", "0 / 5",
            new Vector2(-85f, 0f), new Vector2(150f, 50f), font, 34f, FontStyles.Bold, amberHdr);
        hudProg.alignment = TextAlignmentOptions.Center;

        var hudTimer = CreateLabel(hudPanel.transform, "TimerText", "300.0s",
            new Vector2(85f, 0f), new Vector2(150f, 50f), font, 34f, FontStyles.Bold, Color.white);
        hudTimer.alignment = TextAlignmentOptions.Center;
        ui.timerText = hudTimer;

        // ── VICTORY PANEL ─────────────────────────────────────────────────────
        var vicPanel = CreatePanel(canvasGo.transform, "VictoryPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520f, 360f),
            new Color(0.12f, 0.05f, 0.04f, 0.98f), rounded);
        CreatePanelBorder(vicPanel.transform, "VicBorder", new Color(1.0f, 0.70f, 0.20f, 0.8f), rounded);
        vicPanel.SetActive(false);
        ui.levelCompletePanel = vicPanel;

        var vicTitle = CreateLabel(vicPanel.transform, "VicTitle", "CALDERA CONQUERED!",
            new Vector2(0f, 105f), new Vector2(480f, 54f), font, 34f, FontStyles.Bold, goldAccent);
        vicTitle.alignment = TextAlignmentOptions.Center;

        var vicSub = CreateLabel(vicPanel.transform, "VicSub", "All 5 infernal rows mastered!",
            new Vector2(0f, 60f), new Vector2(480f, 32f), font, 19f, FontStyles.Normal,
            new Color(1.0f, 0.92f, 0.82f));
        vicSub.alignment = TextAlignmentOptions.Center;

        var vicTime = CreateLabel(vicPanel.transform, "VicTime", "Time: 142.0s",
            new Vector2(0f, 20f), new Vector2(480f, 32f), font, 20f, FontStyles.Normal,
            new Color(1.0f, 0.85f, 0.45f));
        vicTime.alignment = TextAlignmentOptions.Center;
        ui.levelCompleteTime = vicTime;

        var nextBtn = CreateButton(vicPanel.transform, "NextBtn", "MAIN MENU",
            new Vector2(0f, -48f), new Vector2(250f, 54f), rounded, font,
            new Color(0.95f, 0.55f, 0.12f));
        ui.nextLevelButton = nextBtn.GetComponent<Button>();

        var replayBtn = CreateButton(vicPanel.transform, "ReplayBtn", "REPLAY LEVEL",
            new Vector2(0f, -118f), new Vector2(250f, 46f), rounded, font,
            new Color(0.25f, 0.10f, 0.08f));
        ui.completeMenuButton = replayBtn.GetComponent<Button>();

        // ── FAIL PANEL ────────────────────────────────────────────────────────
        var failPanel = CreatePanel(canvasGo.transform, "FailPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(480f, 260f),
            new Color(0.18f, 0.04f, 0.04f, 0.98f), rounded);
        CreatePanelBorder(failPanel.transform, "FailBorder", new Color(1.0f, 0.25f, 0.15f, 0.8f), rounded);
        failPanel.SetActive(false);
        ui.failPanel = failPanel;

        var failTitle = CreateLabel(failPanel.transform, "FailTitle", "PLUNGED INTO MAGMA!",
            new Vector2(0f, 68f), new Vector2(440f, 52f), font, 32f, FontStyles.Bold,
            new Color(1f, 0.35f, 0.25f));
        failTitle.alignment = TextAlignmentOptions.Center;

        var failSub = CreateLabel(failPanel.transform, "FailSub",
            "Incorrect tile! Read the math clues carefully and jump again.",
            new Vector2(0f, 15f), new Vector2(420f, 50f), font, 16.5f, FontStyles.Normal,
            new Color(1.0f, 0.88f, 0.85f));
        failSub.alignment = TextAlignmentOptions.Center;
        failSub.textWrappingMode = TextWrappingModes.Normal;
    }

    static void CreatePanelBorder(Transform parent, string name, Color color, Sprite sprite)
    {
        var borderGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(parent, false);
        var rt = borderGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = new Vector2(4f, 4f);
        var img = borderGo.GetComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        borderGo.transform.SetAsFirstSibling();
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
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateEmissiveMaterial(string path, Color baseColor, float metallic, float smoothness, Color emissionColor)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Material mat, bool addCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        if (!addCollider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
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
        mf.sharedMesh = BeveledBoxMesh.Create(size.x, size.y, size.z, bevel);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        if (addCollider)
        {
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
        }
        return go;
    }

    static void CreateCone(string name, Transform parent, Vector3 localPos, float diameter, float height,
        Material mat, bool castShadows)
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

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        return go;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
        Vector2 anchoredPos, Vector2 sizeDelta, TMP_FontAsset font, float fontSize,
        FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = font;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        return tmp;
    }

    static TextMeshProUGUI CreateLabelTL(Transform parent, string name, string text,
        Vector2 anchoredPos, Vector2 sizeDelta, TMP_FontAsset font, float fontSize,
        FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = font;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 sizeDelta, Sprite sprite, TMP_FontAsset font, Color btnColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var img = go.GetComponent<Image>();
        img.color = btnColor;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = btnColor;
        colors.highlightedColor = btnColor * 1.2f;
        colors.pressedColor     = btnColor * 0.8f;
        btn.colors = colors;

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.font      = font;
        tmp.fontSize  = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    static GameObject CreateColorBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Sprite CreateRoundedSprite(string assetPath, int size, int radius, bool isCircle)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sp != null) return sp;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float rSq = radius * radius;
        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = 1f;
                if (isCircle)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distSq = dx * dx + dy * dy;
                    a = distSq <= center * center ? 1f : 0f;
                }
                else
                {
                    int cx = x < radius ? radius : (x >= size - radius ? size - radius - 1 : x);
                    int cy = y < radius ? radius : (y >= size - radius ? size - radius - 1 : y);
                    int ddx = x - cx;
                    int ddy = y - cy;
                    if (ddx * ddx + ddy * ddy > rSq) a = 0f;
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(assetPath, png);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            if (!isCircle) ti.spriteBorder = new Vector4(radius, radius, radius, radius);
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
