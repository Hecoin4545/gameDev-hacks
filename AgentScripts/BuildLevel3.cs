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
/// Builds Level 3 – Ice and Snowfall theme.
/// Uses completely isolated Ice_*.mat materials so Level 1 (Forest) and Level 2 (Desert) remain untouched.
/// Features a rich arctic landscape: glaciers, ice mesas, snow drifts, ice sheets, alpine mountain cones,
/// frost rocks, crystalline ice spires, snow-dusted pine trees, floating ice floes, drifting polar clouds,
/// and a live falling snowfall particle system.
/// </summary>
public class BuildLevel3
{
    // ── Dedicated Ice & Snow palette materials (isolated from Level 1 & 2) ───
    static Material tileMat;           // icy frost tile surface
    static Material tileEdgeMat;       // glacial edge bevel
    static Material tileRockMat;       // deep frozen glacial base
    static Material tileRimMat;        // polar crevasse rim

    static Material snowMat;           // crisp white powder snow
    static Material snowDarkMat;       // shadowed drift snow
    static Material glacierMat;        // radiant turquoise / cyan glacier ice
    static Material glacierDarkMat;    // deep crevasse ice
    static Material iceSpireMat;       // translucent glossy ice spire
    static Material frostStoneMatA;    // frost-crusted arctic rock
    static Material frostStoneMatB;    // dark arctic basalt crag
    static Material pineTrunkMat;      // dark frosted conifer bark
    static Material pineNeedlesMat;    // frosted spruce needle canopy
    static Material pineSnowCapMat;    // pure white snow cap on pine branches
    static Material crystalMat;        // glowing aurora crystal / goal marker
    static Material goalPadMat;        // luminous aurora ice sanctuary pad
    static Material skyboxMat;         // polar procedural skybox
    static Material snowflakeMat;      // soft particle snowflake

    // Player materials (shared character appearance)
    static Material hoodieMat, hoodMat, jeansMat, skinMat, hairMat, shoeMat, eyeMat, pupilMat;

    [MenuItem("Tools/Build Level 3 (Ice & Snowfall)")]
    public static void Run()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Meshes");
        EnsureFolder("Assets/Textures");
        EnsureFolder("Assets/UI");

        string scenePath = "Assets/Scenes/Level3.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        CreateMaterials();
        var levelData = BuildLevelData();

        foreach (var root in scene.GetRootGameObjects())
            UnityEngine.Object.DestroyImmediate(root);

        // ── GRID ──────────────────────────────────────────────────────────────
        var gridRoot    = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.safeMat                = tileMat;
        gridManager.unsafeMat              = tileMat;
        gridManager.tileEdgeMat            = tileEdgeMat;
        gridManager.tileRockMat            = tileRockMat;
        gridManager.tileRimMat             = tileRimMat;
        gridManager.useHexTiles            = false;
        gridManager.customNeutralColor     = new Color(0.62f, 0.78f, 0.90f);
        gridManager.customCompletedColor   = new Color(0.35f, 0.95f, 0.88f);
        gridManager.customCompletedEmission= new Color(0.18f, 0.65f, 0.60f);

        float stride    = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;
        float halfDepth = gridDepth * 0.5f;

        // Island spacing: offset outside the first and last row of tiles with a comfortable jump gap
        float islandZOffset = halfDepth + 3.5f;

        // ── START / END ISLANDS (properly spaced outside grid) ─────────────────
        var startIsland = CreateIceIsland("StartIsland", null,
            new Vector3(0f, 0.11f, -islandZOffset),
            5.4f, 4.4f, snowMat, glacierMat, frostStoneMatA, true);

        var endIsland = CreateIceIsland("EndIsland", null,
            new Vector3(0f, 0.11f, islandZOffset),
            5.4f, 4.4f, snowMat, glacierMat, frostStoneMatA, true);

        // Goal pad (luminous aurora ice sanctuary)
        CreateBeveledObject("GoalPad", endIsland.transform,
            new Vector3(0f, 0.055f, 0f),
            new Vector3(2.4f, 0.11f, 2.4f), 0.05f, goalPadMat, false);

        // 4 Ice crystal obelisks around the sanctuary
        for (int i = 0; i < 4; i++)
        {
            float ox = (i % 2 == 0 ? -1f : 1f) * 1.8f;
            float oz = (i < 2 ? -1f : 1f) * 1.6f;
            var spire = CreateHexObject("Obelisk" + i, endIsland.transform,
                new Vector3(ox, 0.9f, oz), 0.32f, 1.8f, 0.06f, iceSpireMat, false);
            spire.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f), 45f * i, UnityEngine.Random.Range(-5f, 5f));
        }

        // Floating aurora crystal gem marker
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.3f, 0f), new Vector3(0.58f, 0.58f, 0.58f), crystalMat, false);
        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

        // End trigger
        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size      = new Vector3(4.8f, 3.2f, 3.6f);
        endBox.isTrigger = true;
        endTrigger.AddComponent<EndPlatformTrigger>();

        gridManager.Build(levelData);

        // ── PLAYER (spawns safely in the center of start island) ───────────────
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
        cam.farClipPlane  = 700f;
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
        cf.target          = player.transform;
        cf.distance        = 13.5f;
        cf.pitch           = 40f;
        cf.yaw             = 0f;
        cf.targetHeight    = 1.35f;
        cf.lookAhead       = 4.0f;
        cf.followSmooth    = 12f;
        cf.rotationSmooth  = 16f;
        cf.minDistance     = 7f;
        cf.maxDistance     = 22f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        // ── LIGHTING / POLAR SKY ──────────────────────────────────────────────
        ConfigureLighting();
        ConfigurePostProcessing();

        // ── ENVIRONMENT ───────────────────────────────────────────────────────
        var envRoot  = new GameObject("Environment").transform;
        var clouds   = BuildEnvironment(envRoot);

        // ── LIVE SNOWFALL PARTICLE SYSTEM ─────────────────────────────────────
        CreateSnowfall(envRoot, new Vector3(0f, 0f, 0f));

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
            "Welcome to the Frozen Crags! The icy tiles are slick and perilous through the snowfall. Remember the blueprint in reverse to reach the summit!"
        };

        var gmGo = new GameObject("GameManager");
        var gm   = gmGo.AddComponent<GameManager>();
        var l1   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level1Data.asset");
        var l2   = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level2Data.asset");
        gm.levels            = new LevelData[] { l1, l2, levelData };
        gm.currentLevelIndex = 2;

        // ── UI ────────────────────────────────────────────────────────────────
        BuildUI(lm, levelData);

        // ── EVENT SYSTEM ──────────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── ANIMATION (polar clouds drift) ────────────────────────────────────
        var animator       = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = clouds;

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Level 3 (Ice & Snowfall) built with dedicated high-detail materials, environment, and snowfall.");
    }

    // ── ISOLATED ICE & SNOW MATERIALS ─────────────────────────────────────────
    static void CreateMaterials()
    {
        // Dedicated ice tile materials (isolated from Level 1 & Level 2)
        tileMat       = CreateMaterial("Assets/Materials/IceTile.mat",        new Color(0.68f, 0.86f, 0.96f), 0.12f, 0.40f, 0f);
        tileEdgeMat   = CreateMaterial("Assets/Materials/IceTileEdge.mat",    new Color(0.48f, 0.72f, 0.88f), 0.08f, 0.28f, 0f);
        tileRockMat   = CreateMaterial("Assets/Materials/IceTileRock.mat",    new Color(0.24f, 0.42f, 0.60f), 0.05f, 0.18f, 0f);
        tileRimMat    = CreateMaterial("Assets/Materials/IceTileRim.mat",     new Color(0.12f, 0.24f, 0.40f), 0f,    0.10f, 0f);

        // Dedicated ice landscape materials
        snowMat       = CreateMaterial("Assets/Materials/IceSnow.mat",        new Color(0.96f, 0.98f, 1.0f),  0f,    0.08f, 0f);
        snowDarkMat   = CreateMaterial("Assets/Materials/IceSnowDark.mat",    new Color(0.78f, 0.86f, 0.95f), 0f,    0.06f, 0f);
        glacierMat    = CreateMaterial("Assets/Materials/IceGlacier.mat",     new Color(0.38f, 0.80f, 0.88f), 0.18f, 0.52f, 0.05f);
        glacierDarkMat= CreateMaterial("Assets/Materials/IceGlacierDark.mat", new Color(0.16f, 0.46f, 0.60f), 0.10f, 0.32f, 0f);

        iceSpireMat   = CreateMaterial("Assets/Materials/IceSpire.mat",        new Color(0.62f, 0.90f, 0.98f), 0.22f, 0.68f, 0.15f);
        frostStoneMatA= CreateMaterial("Assets/Materials/IceFrostStone.mat",   new Color(0.50f, 0.58f, 0.68f), 0.05f, 0.14f, 0f);
        frostStoneMatB= CreateMaterial("Assets/Materials/IceFrostStoneDark.mat",new Color(0.28f, 0.35f, 0.44f),0.05f, 0.12f, 0f);

        pineTrunkMat  = CreateMaterial("Assets/Materials/IcePineTrunk.mat",   new Color(0.24f, 0.20f, 0.18f), 0f,    0.10f, 0f);
        pineNeedlesMat= CreateMaterial("Assets/Materials/IcePineNeedles.mat", new Color(0.20f, 0.40f, 0.36f), 0f,    0.16f, 0f);
        pineSnowCapMat= CreateMaterial("Assets/Materials/IcePineSnowCap.mat", new Color(0.98f, 0.99f, 1.0f),  0f,    0.08f, 0f);

        crystalMat    = CreateMaterial("Assets/Materials/IceCrystalGem.mat",  new Color(0.25f, 0.95f, 1.0f),  0.35f, 0.70f, 0.85f);
        goalPadMat    = CreateMaterial("Assets/Materials/IceGoalPad.mat",     new Color(0.35f, 0.92f, 0.88f), 0.20f, 0.60f, 0.45f);

        // Skybox & Snowflake
        skyboxMat     = CreateIceSkyboxMaterial();
        snowflakeMat  = CreateSnowflakeMaterial();

        // Player materials
        hoodieMat  = CreateMaterial("Assets/Materials/Hoodie.mat", new Color(0.74f, 0.13f, 0.14f), 0f, 0.22f, 0.04f);
        hoodMat    = CreateMaterial("Assets/Materials/Hood.mat",   new Color(0.60f, 0.10f, 0.11f), 0f, 0.20f, 0.02f);
        jeansMat   = CreateMaterial("Assets/Materials/Jeans.mat",  new Color(0.16f, 0.33f, 0.68f), 0f, 0.16f, 0f);
        skinMat    = CreateMaterial("Assets/Materials/Skin.mat",   new Color(0.93f, 0.74f, 0.59f), 0f, 0.20f, 0f);
        hairMat    = CreateMaterial("Assets/Materials/Hair.mat",   new Color(0.20f, 0.12f, 0.07f), 0f, 0.24f, 0f);
        shoeMat    = CreateMaterial("Assets/Materials/Shoe.mat",   new Color(0.93f, 0.94f, 0.95f), 0f, 0.35f, 0f);
        eyeMat     = CreateMaterial("Assets/Materials/Eye.mat",    Color.white,                    0f, 0.40f, 0f);
        pupilMat   = CreateMaterial("Assets/Materials/Pupil.mat",  new Color(0.06f, 0.06f, 0.08f), 0f, 0.40f, 0f);
    }

    static LevelData BuildLevelData()
    {
        string path = "Assets/Data/Level3Data.asset";
        var d = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(d, path);
        }
        d.levelName  = "Level 3";
        d.sceneName  = "Level3";
        d.rows       = 6;
        d.columns    = 5;
        d.tileSize   = 2.4f;
        d.tileGap    = 0.65f;
        d.timeLimit  = 150f;
        d.objective  = "GLACIAL REVERSE TRIAL! Study the frost blueprint for 8s through the snowfall. The frozen rows are INVERTED: the top blueprint row is your 1st jump on the glacier!";
        d.instructions = "WASD / Arrows – Move\nSpace – Jump\nRight drag / Q E – Rotate camera\nMouse wheel – Zoom\nR – Restart\nF – Dance\n<b>RULE:</b> Blueprint Top Row = Arena 1st Jump!";
        d.hideMapAfterCountdown = true;
        d.mapVisibleDuration    = 8.0f;
        d.isUpsideDownPattern   = true;

        d.displayedPattern = new List<Vector2Int>
        {
            new Vector2Int(6, 4),
            new Vector2Int(5, 4),
            new Vector2Int(5, 3),
            new Vector2Int(4, 3),
            new Vector2Int(3, 3),
            new Vector2Int(3, 2),
            new Vector2Int(2, 2),
            new Vector2Int(1, 2)
        };

        d.safeTiles = new List<Vector2Int>
        {
            new Vector2Int(1, 4),
            new Vector2Int(2, 4),
            new Vector2Int(2, 3),
            new Vector2Int(3, 3),
            new Vector2Int(4, 3),
            new Vector2Int(4, 2),
            new Vector2Int(5, 2),
            new Vector2Int(6, 2)
        };
        d.sequence = new List<Vector2Int>(d.safeTiles);
        EditorUtility.SetDirty(d);
        return d;
    }

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

    static void ConfigureLighting()
    {
        // Crisp cool arctic directional sunlight
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo == null) lightGo = new GameObject("Directional Light");
        var l = lightGo.GetComponent<Light>();
        if (l == null) l = lightGo.AddComponent<Light>();

        l.type      = LightType.Directional;
        l.color     = new Color(0.88f, 0.94f, 1.0f); // crisp pale arctic sunlight
        l.intensity = 1.30f;
        l.shadows   = LightShadows.Soft;
        l.shadowStrength = 0.65f;
        l.shadowBias     = 0.04f;
        l.shadowNormalBias = 0.35f;

        lightGo.transform.rotation = Quaternion.Euler(46f, -38f, 0f);

        // Ambient lighting: cool trilight (icy blue sky, lavender equator, deep navy/blue ground)
        RenderSettings.ambientMode          = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor      = new Color(0.78f, 0.90f, 1.0f);
        RenderSettings.ambientEquatorColor  = new Color(0.55f, 0.72f, 0.88f);
        RenderSettings.ambientGroundColor   = new Color(0.38f, 0.52f, 0.70f);
        RenderSettings.ambientIntensity     = 1.05f;

        // Polar skybox
        RenderSettings.skybox = skyboxMat;

        // Atmosphere / Fog
        RenderSettings.fog         = true;
        RenderSettings.fogMode     = FogMode.ExponentialSquared;
        RenderSettings.fogColor    = new Color(0.78f, 0.88f, 0.97f);
        RenderSettings.fogDensity  = 0.0022f;
    }

    static Material CreateIceSkyboxMaterial()
    {
        var path   = "Assets/Materials/IceSkybox.mat";
        var shader = Shader.Find("Skybox/Procedural");
        var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetFloat("_SunSize", 0.04f);
        mat.SetFloat("_SunSizeConvergence", 6f);
        mat.SetFloat("_AtmosphereThickness", 0.72f);
        mat.SetColor("_SkyTint",    new Color(0.68f, 0.84f, 0.98f)); // crisp pale cyan
        mat.SetColor("_GroundColor",new Color(0.75f, 0.85f, 0.95f)); // snow ground tint
        mat.SetFloat("_Exposure", 1.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigurePostProcessing()
    {
        var path    = "Assets/Settings/Ice_Profile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.components.Clear();

        // Tonemapping: ACES
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        // Bloom: crystal glimmer
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.80f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(0.85f, 0.94f, 1.0f));

        // Color adjustments: cool temperature and fresh contrast
        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.18f);
        ca.contrast.Override(12f);
        ca.saturation.Override(6f);
        ca.colorFilter.Override(new Color(0.94f, 0.97f, 1.0f));

        // White balance: crisp arctic cooling
        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(-16f);
        wb.tint.Override(-4f);

        // Vignette
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.20f);
        vig.smoothness.Override(0.35f);
        vig.color.Override(new Color(0.10f, 0.16f, 0.28f));

        var go     = new GameObject("GlobalVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile  = profile;

        EditorUtility.SetDirty(profile);
    }

    // ── ENVIRONMENT BUILDER (Matches Desert & Forest detail level) ───────────
    static Transform BuildEnvironment(Transform root)
    {
        const float groundY = -30f;

        // Ground: massive snowy tundra plane
        var ground = CreatePrimitive(PrimitiveType.Plane, "Ground", root,
            new Vector3(0f, groundY, 0f), new Vector3(32f, 1f, 32f), snowMat, false);
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // 16 Billowing Snow Drifts surrounding the valley
        for (int i = 0; i < 16; i++)
        {
            float angle  = i * (360f / 16f) + 6f;
            float radius = 50f + (i % 5) * 16f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 3.0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            var drift = CreatePrimitive(PrimitiveType.Sphere, "SnowDrift" + i, root, pos,
                new Vector3(48f + (i % 4) * 12f, 8f + (i % 3) * 2.5f, 42f + (i % 4) * 10f),
                i % 2 == 0 ? snowMat : snowDarkMat, false);
            drift.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // 20 Snow Sastrugi / Wind-carved ridges
        UnityEngine.Random.InitState(20261123);
        for (int i = 0; i < 20; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 22f + UnityEngine.Random.Range(0f, 72f);
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 0.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float patch = 8f + UnityEngine.Random.Range(0f, 18f);
            var ridge = CreatePrimitive(PrimitiveType.Sphere, "Sastrugi" + i, root, pos,
                new Vector3(patch, 1.6f, patch * 0.55f), i % 2 == 0 ? snowMat : snowDarkMat, false);
            ridge.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // 18 Frozen Ice Sheets / Glacial Lake Patches on the tundra floor
        for (int i = 0; i < 18; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 26f + UnityEngine.Random.Range(0f, 85f);
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 0.08f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float sz = 8f + UnityEngine.Random.Range(0f, 16f);
            var iceSheet = CreatePrimitive(PrimitiveType.Cylinder, "IceSheet" + i, root, pos,
                new Vector3(sz, 0.06f, sz * 0.8f), glacierMat, false);
            iceSheet.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // 6 Monumental Glacial Icebergs / Mesas (Middle Distance)
        CreateGlacierMesa("GlacierA", root, new Vector3(-85f, groundY,  45f), 26f, 24f, 34f, 22f);
        CreateGlacierMesa("GlacierB", root, new Vector3( 90f, groundY,  28f), 22f, 18f, 28f, 16f);
        CreateGlacierMesa("GlacierC", root, new Vector3(-55f, groundY, 115f), 34f, 30f, 40f, 26f);
        CreateGlacierMesa("GlacierD", root, new Vector3( 65f, groundY, 105f), 24f, 20f, 30f, 18f);
        CreateGlacierMesa("GlacierE", root, new Vector3(-125f, groundY, 18f), 40f, 36f, 50f, 32f);
        CreateGlacierMesa("GlacierF", root, new Vector3( 125f, groundY, 55f), 30f, 26f, 42f, 22f);

        // 8 Far Monumental Glacial Peaks (Horizon Ring)
        for (int i = 0; i < 8; i++)
        {
            float angle  = i * 45f + 16f;
            float radius = 160f + (i % 3) * 35f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateGlacierMesa("FarGlacier" + i, root, pos,
                46f + (i % 3) * 18f, 38f + (i % 3) * 14f,
                58f + (i % 3) * 22f, 34f + (i % 3) * 10f);
        }

        // 12 Jagged Alpine Peaks (faceted mountain cones with snowy caps)
        for (int i = 0; i < 12; i++)
        {
            float angle    = i * 30f + 10f;
            float radius   = 100f + (i % 4) * 24f;
            float height   = 22f + (i % 4) * 5f;
            float diameter = 58f + (i % 4) * 20f;
            Vector3 pos    = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + height * 0.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateAlpinePeak("AlpinePeak" + i, root, pos, diameter, height, frostStoneMatA, snowMat);
        }

        // 7 Far Alpine Peaks (distant horizon skyline)
        for (int i = 0; i < 7; i++)
        {
            float angle    = i * 51f + 25f;
            float radius   = 175f + (i % 3) * 28f;
            float height   = 40f + (i % 3) * 9f;
            float diameter = 115f + (i % 3) * 35f;
            Vector3 pos    = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + height * 0.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateAlpinePeak("FarPeak" + i, root, pos, diameter, height, frostStoneMatB, snowMat);
        }

        // 28 Frost Boulders & Arctic Crags
        UnityEngine.Random.InitState(77123);
        for (int i = 0; i < 28; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 24f + UnityEngine.Random.Range(0f, 55f);
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 0.4f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float sz = 1.6f + UnityEngine.Random.Range(0f, 3.4f);
            CreateBeveledObject("FrostRock" + i, root, pos,
                new Vector3(sz, sz * 0.72f, sz * 0.90f), 0.18f,
                i % 3 == 0 ? glacierMat : (i % 2 == 0 ? frostStoneMatA : frostStoneMatB), false);
        }

        // 24 Ice Spire Formations (clusters of faceted crystalline ice shards)
        UnityEngine.Random.InitState(99881);
        for (int i = 0; i < 24; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 22f + UnityEngine.Random.Range(0f, 55f);
            Vector3 basePos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            if (Mathf.Abs(basePos.x) < 7.5f) continue;
            CreateIceSpireCluster(root, basePos, UnityEngine.Random.Range(1.0f, 2.0f), i % 3);
        }

        // 60 Snowy Alpine Pine Trees (evergreens with frosted bark and snowy branch caps)
        UnityEngine.Random.InitState(44332);
        // Inner forest ring framing the view (kept clear of the central grid corridor)
        for (int i = 0; i < 24; i++)
        {
            float angle = i * (360f / 24f) + UnityEngine.Random.Range(-4f, 4f);
            float radius = 32f + UnityEngine.Random.Range(0f, 15f);
            Vector3 basePos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            if (Mathf.Abs(basePos.x) < 7.5f) continue;
            CreateSnowyPineTree(root, basePos, UnityEngine.Random.Range(1.8f, 2.6f), UnityEngine.Random.Range(0, 3));
        }
        // Outer forest slopes
        for (int i = 0; i < 40; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float radius = 50f + UnityEngine.Random.Range(0f, 45f);
            Vector3 basePos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            if (Mathf.Abs(basePos.x) < 6.5f) continue;
            CreateSnowyPineTree(root, basePos, UnityEngine.Random.Range(1.6f, 2.4f), UnityEngine.Random.Range(0, 3));
        }
        // Tall hero pines framing the skyline
        int hero = 0;
        foreach (float ang in new[] { 32f, 146f, 214f, 320f })
        {
            float radius = 40f + (hero % 2) * 6f;
            Vector3 basePos = new Vector3(
                Mathf.Cos(ang * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(ang * Mathf.Deg2Rad) * radius);
            CreateSnowyPineTree(root, basePos, 3.2f + (hero % 2) * 0.45f, 2);
            hero++;
        }

        // 4 Floating Glacial Islands (drifting ice floes in the arctic sky)
        CreateIceIsland("FloatIslandA", root, new Vector3(-45f, 11f, 20f),
            4.6f, 3.8f, snowMat, glacierMat, frostStoneMatA, false);
        CreateIceIsland("FloatIslandB", root, new Vector3( 48f, 8f,  18f),
            3.8f, 3.2f, snowMat, glacierMat, frostStoneMatA, false);
        CreateIceIsland("FloatIslandC", root, new Vector3(-36f, 5f,  44f),
            3.4f, 2.8f, snowMat, glacierMat, frostStoneMatA, false);
        CreateIceIsland("FloatIslandD", root, new Vector3( 40f, 15f, 42f),
            4.8f, 3.8f, snowMat, glacierMat, frostStoneMatA, false);

        // 7 Floating Frost Rocks / Ice Shards
        for (int i = 0; i < 7; i++)
        {
            float angle  = i * 52f + 14f;
            float radius = 35f + (i % 3) * 7f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                3.5f + (i % 4) * 2.4f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateBeveledObject("FloatIce" + i, root, pos,
                new Vector3(1.8f + (i % 3) * 0.6f, 1.3f, 1.5f + (i % 2) * 0.6f),
                0.20f, i % 2 == 0 ? glacierMat : frostStoneMatA, false);
        }

        // 12 Polar Clouds (soft drifting snow clouds)
        var clouds = new GameObject("PolarClouds");
        clouds.transform.SetParent(root, false);
        for (int i = 0; i < 12; i++)
        {
            float angle  = i * (360f / 12f) + 8f;
            float radius = 52f + (i % 5) * 12f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                19f + (i % 4) * 4.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

            var cluster = new GameObject("Cloud" + i);
            cluster.transform.SetParent(clouds.transform, false);
            cluster.transform.localPosition = pos;

            for (int k = 0; k < 4; k++)
            {
                var puff = CreatePrimitive(PrimitiveType.Sphere, "Puff" + k, cluster.transform,
                    new Vector3((k - 1.5f) * 3.6f, (k % 2) * 1.1f, (k % 3) * 0.85f),
                    new Vector3(7.8f - k * 0.65f, 2.8f - k * 0.2f, 5.0f - k * 0.3f),
                    snowMat, false);
                puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        return clouds.transform;
    }

    // ── LIVE SNOWFALL PARTICLE SYSTEM ─────────────────────────────────────────
    static GameObject CreateSnowfall(Transform parent, Vector3 center)
    {
        var snowGo = new GameObject("Snowfall");
        snowGo.transform.SetParent(parent, false);
        snowGo.transform.position = center + new Vector3(0f, 22f, 0f);

        var ps = snowGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7.5f, 11f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.30f);
        main.startColor = new Color(0.96f, 0.98f, 1.0f, 0.92f);
        main.maxParticles = 3200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 240f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(90f, 2f, 90f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f);
        vel.y = new ParticleSystem.MinMaxCurve(-3.8f, -2.2f);
        vel.z = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.6f;

        var psr = snowGo.GetComponent<ParticleSystemRenderer>();
        psr.material = snowflakeMat;
        psr.minParticleSize = 0.003f;
        psr.maxParticleSize = 0.045f;

        return snowGo;
    }

    static Material CreateSnowflakeMaterial()
    {
        string path = "Assets/Materials/SnowflakeParticle.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        var tex = CreateSnowflakeTexture("Assets/Textures/Snowflake.png", 64);
        mat.mainTexture = tex;
        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.95f));
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetFloat("_Blend", 0f);   // Alpha blend
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D CreateSnowflakeTexture(string path, int size)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;

        EnsureFolder("Assets/Textures");
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float radius = center * 0.85f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = alpha * alpha * (3f - 2f * alpha); // smoothstep
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
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ── GLACIER MESA / ICEBERG ────────────────────────────────────────────────
    static void CreateGlacierMesa(string name, Transform parent, Vector3 basePos,
        float topW, float topD, float baseW, float baseD)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = basePos;

        float groundY = 0f;
        float h1 = 8f, h2 = 12f, h3 = 6f;

        // Base layer: deep crevasse blue
        CreateBeveledObject(name + "_Base", root.transform,
            new Vector3(0f, groundY + h1 * 0.5f, 0f),
            new Vector3(baseW, h1, baseD), 0.40f, glacierDarkMat, false);

        // Mid layer: radiant turquoise glacier ice
        CreateBeveledObject(name + "_Mid", root.transform,
            new Vector3(0f, groundY + h1 + h2 * 0.5f, 0f),
            new Vector3(baseW * 0.82f, h2, baseD * 0.82f), 0.32f, glacierMat, false);

        // Top tier: frosty arctic crag
        CreateBeveledObject(name + "_Top", root.transform,
            new Vector3(0f, groundY + h1 + h2 + h3 * 0.5f, 0f),
            new Vector3(topW, h3, topD), 0.25f, frostStoneMatA, false);

        // Cap: smooth powder snow
        CreateBeveledObject(name + "_Cap", root.transform,
            new Vector3(0f, groundY + h1 + h2 + h3, 0f),
            new Vector3(topW * 0.92f, 0.9f, topD * 0.92f), 0.12f, snowMat, false);
    }

    // ── ALPINE MOUNTAIN PEAK ──────────────────────────────────────────────────
    static void CreateAlpinePeak(string name, Transform parent, Vector3 basePos,
        float diameter, float height, Material rockMat, Material snowCapMat)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = basePos;

        // Main rock cone
        CreateCone(name + "_Cone", root.transform, Vector3.zero, diameter, height, rockMat, false);

        // Snow-capped peak (smaller cone resting on top)
        CreateCone(name + "_SnowCap", root.transform,
            new Vector3(0f, height * 0.22f, 0f),
            diameter * 0.45f, height * 0.55f, snowCapMat, false);
    }

    // ── SNOWY ALPINE PINE TREE ────────────────────────────────────────────────
    static void CreateSnowyPineTree(Transform parent, Vector3 basePos, float scale, int tiers)
    {
        var tree = new GameObject("SnowyPine");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = basePos;
        tree.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        // Trunk
        float trunkH = 2.4f * scale;
        float trunkR = 0.35f * scale;
        CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform,
            new Vector3(0f, trunkH * 0.5f, 0f),
            new Vector3(trunkR, trunkH * 0.5f, trunkR), pineTrunkMat, false);

        // 3 Layered pine canopies with snow caps
        float[] layerY = new[] { 5.2f * scale, 7.5f * scale, 9.6f * scale };
        float[] layerD = new[] { 6.6f * scale, 5.2f * scale, 3.8f * scale };
        float[] layerH = new[] { 4.8f * scale, 4.2f * scale, 3.6f * scale };

        for (int i = 0; i < 3; i++)
        {
            float y = layerY[i];
            float d = layerD[i];
            float h = layerH[i];

            // Needles
            CreateCone("Needles_" + i, tree.transform, new Vector3(0f, y, 0f), d, h, pineNeedlesMat, false);

            // Pure white snow cap layer over the needles
            CreateCone("SnowCap_" + i, tree.transform, new Vector3(0f, y + h * 0.18f, 0f), d * 0.72f, h * 0.65f, pineSnowCapMat, false);
        }
    }

    // ── ICE SPIRE / CRYSTAL CLUSTER ───────────────────────────────────────────
    static void CreateIceSpireCluster(Transform parent, Vector3 basePos, float scale, int variant)
    {
        var cluster = new GameObject("IceSpireCluster");
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = basePos;
        cluster.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        // Central towering spire
        float mainH = (7.5f + variant * 2.5f) * scale;
        float mainR = (0.75f + variant * 0.15f) * scale;
        var centerSpire = CreateHexObject("MainSpire", cluster.transform,
            new Vector3(0f, mainH * 0.5f, 0f), mainR, mainH, 0.10f * scale, iceSpireMat, false);
        centerSpire.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-6f, 6f), 0f, UnityEngine.Random.Range(-6f, 6f));

        // 3-5 satellite crystal shards angled outward
        int shards = 3 + variant;
        for (int i = 0; i < shards; i++)
        {
            float angle = i * (360f / shards) + UnityEngine.Random.Range(-15f, 15f);
            float dist  = (1.2f + (i % 2) * 0.45f) * scale;
            float shH   = (3.6f + (i % 3) * 1.4f) * scale;
            float shR   = 0.45f * scale;

            Vector3 shPos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * dist,
                shH * 0.48f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * dist);

            var shard = CreateHexObject("Shard" + i, cluster.transform, shPos, shR, shH, 0.06f * scale, iceSpireMat, false);
            shard.transform.localRotation = Quaternion.Euler(
                Mathf.Sin(angle * Mathf.Deg2Rad) * 18f,
                angle,
                -Mathf.Cos(angle * Mathf.Deg2Rad) * 18f);
        }
    }

    // ── ICE ISLAND ────────────────────────────────────────────────────────────
    static GameObject CreateIceIsland(string name, Transform parent, Vector3 topCenter,
        float width, float depth, Material top, Material mid, Material bot, bool addCollider)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = topCenter;

        float topHeight  = 0.5f;
        var slab = CreateBeveledObject(name + "_Top", root.transform,
            new Vector3(0f, -topHeight * 0.5f, 0f),
            new Vector3(width, topHeight, depth), 0.11f, top, addCollider);
        slab.GetComponent<Renderer>().receiveShadows = true;

        float midHeight = 1.3f;
        CreateBeveledObject(name + "_Mid", root.transform,
            new Vector3(0f, -topHeight - midHeight * 0.5f, 0f),
            new Vector3(width * 0.88f, midHeight, depth * 0.88f), 0.16f, mid, false);

        float botHeight = 0.95f;
        CreateBeveledObject(name + "_Bot", root.transform,
            new Vector3(0f, -topHeight - midHeight - botHeight * 0.5f, 0f),
            new Vector3(width * 0.54f, botHeight, depth * 0.54f), 0.14f, bot, false);

        return root;
    }

    // ── UI BUILDER (Matches Desert & Forest theme UI) ─────────────────────────
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
        if (font == null) Debug.LogError("UI font not found");
        ui.font = font;

        var rounded = CreateRoundedSprite("Assets/UI/PanelRounded.png", 64, 18, false);
        var dot     = CreateRoundedSprite("Assets/UI/Dot.png",          32, 16, true);
        ui.roundedSprite = rounded;
        ui.dotSprite     = dot;

        // MAP panel (top right)
        var mapPanel = CreatePanel(canvasGo.transform, "MapPanel",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-36f, -36f), new Vector2(360f, 490f),
            new Color(0.06f, 0.10f, 0.18f, 0.92f), rounded);

        var mapHeader = CreateLabel(mapPanel.transform, "MapHeader", "PATH BLUEPRINT",
            new Vector2(0f, 215f), new Vector2(320f, 32f), font, 18f, FontStyles.Bold,
            new Color(0.55f, 0.88f, 1.0f));
        mapHeader.alignment = TextAlignmentOptions.Center;

        var statusLabel = CreateLabel(mapPanel.transform, "MapStatus", "MEMORIZE BLUEPRINT: 8.0s",
            new Vector2(0f, 182f), new Vector2(320f, 28f), font, 18f, FontStyles.Bold,
            new Color(0.35f, 0.95f, 0.88f));
        statusLabel.alignment = TextAlignmentOptions.Center;
        ui.mapStatusText = statusLabel;

        var mapContainer = new GameObject("MapContainer", typeof(RectTransform));
        mapContainer.transform.SetParent(mapPanel.transform, false);
        var mapContainerRt = mapContainer.GetComponent<RectTransform>();
        mapContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
        mapContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
        mapContainerRt.pivot     = new Vector2(0.5f, 0.5f);
        mapContainerRt.sizeDelta = new Vector2(320f, 290f);
        mapContainerRt.anchoredPosition = new Vector2(0f, 15f);
        ui.mapContainer = mapContainer.transform;

        // Player marker
        var markerGo = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        markerGo.transform.SetParent(mapContainer.transform, false);
        var markerRt = markerGo.GetComponent<RectTransform>();
        markerRt.sizeDelta = new Vector2(26f, 26f);
        var markerImg = markerGo.GetComponent<Image>();
        markerImg.sprite = dot;
        markerImg.color  = new Color(1f, 0.85f, 0.20f);
        markerGo.SetActive(false);
        ui.playerMarker = markerImg;

        // Map Legend
        var legendGo = new GameObject("Legend", typeof(RectTransform));
        legendGo.transform.SetParent(mapPanel.transform, false);
        var legRt = legendGo.GetComponent<RectTransform>();
        legRt.anchorMin = new Vector2(0.5f, 0f);
        legRt.anchorMax = new Vector2(0.5f, 0f);
        legRt.pivot     = new Vector2(0.5f, 0f);
        legRt.sizeDelta = new Vector2(320f, 38f);
        legRt.anchoredPosition = new Vector2(0f, 44f);

        CreateSwatch(legendGo.transform, "SafeSwatch", new Vector2(-110f, 0f),
            new Color(0.35f, 0.95f, 0.88f), rounded);
        var safeLbl = CreateLabel(legendGo.transform, "SafeLbl", "Safe Path",
            new Vector2(-60f, 0f), new Vector2(75f, 24f), font, 14f, FontStyles.Normal,
            new Color(0.85f, 0.92f, 1.0f));
        safeLbl.alignment = TextAlignmentOptions.Left;

        CreateSwatch(legendGo.transform, "PlayerSwatch", new Vector2(20f, 0f),
            new Color(1f, 0.85f, 0.20f), dot);
        var youLbl = CreateLabel(legendGo.transform, "YouLbl", "You",
            new Vector2(65f, 0f), new Vector2(65f, 24f), font, 14f, FontStyles.Normal,
            new Color(0.85f, 0.92f, 1.0f));
        youLbl.alignment = TextAlignmentOptions.Left;

        var hintLabel = CreateLabel(mapPanel.transform, "MapHint",
            "Top blueprint row = 1st jump in the arena.",
            new Vector2(0f, 14f), new Vector2(320f, 32f), font, 14f, FontStyles.Normal,
            new Color(0.75f, 0.88f, 1.0f));
        var hintRt = hintLabel.rectTransform;
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot     = new Vector2(0.5f, 0f);
        hintLabel.alignment = TextAlignmentOptions.Center;
        ui.mapHintText = hintLabel;

        // INFO Panel (top left)
        var infoPanel = CreatePanel(canvasGo.transform, "InfoPanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(36f, -36f), new Vector2(420f, 250f),
            new Color(0.06f, 0.10f, 0.18f, 0.92f), rounded);

        var titleLabel = CreateLabelTopLeft(infoPanel.transform, "Title", "LEVEL 03",
            new Vector2(24f, -20f), new Vector2(372f, 34f), font, 26f, FontStyles.Bold,
            new Color(0.55f, 0.88f, 1.0f));
        titleLabel.alignment = TextAlignmentOptions.Left;
        ui.levelTitle = titleLabel;

        var subLabel = CreateLabelTopLeft(infoPanel.transform, "Subtitle", "UPSIDE-DOWN MEMORY",
            new Vector2(24f, -56f), new Vector2(372f, 22f), font, 14f, FontStyles.Bold,
            new Color(0.95f, 0.80f, 0.35f));
        subLabel.alignment = TextAlignmentOptions.Left;
        ui.subtitle = subLabel;

        var objLabel = CreateLabelTopLeft(infoPanel.transform, "Objective",
            levelData.objective,
            new Vector2(24f, -84f), new Vector2(372f, 115f), font, 13.5f, FontStyles.Normal,
            new Color(0.85f, 0.92f, 1.0f));
        objLabel.alignment = TextAlignmentOptions.TopLeft;
        objLabel.textWrappingMode = TextWrappingModes.Normal;
        objLabel.overflowMode = TextOverflowModes.Truncate;
        objLabel.lineSpacing = 3f;
        ui.objectiveText = objLabel;

        // Progress bar
        var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(infoPanel.transform, false);
        var barBgRt = barBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0f, 0f);
        barBgRt.anchorMax = new Vector2(1f, 0f);
        barBgRt.pivot     = new Vector2(0.5f, 0f);
        barBgRt.sizeDelta = new Vector2(-48f, 8f);
        barBgRt.anchoredPosition = new Vector2(0f, 18f);
        var barBgImg = barBg.GetComponent<Image>();
        barBgImg.color = new Color(0.12f, 0.18f, 0.28f);
        if (rounded != null) { barBgImg.sprite = rounded; barBgImg.type = Image.Type.Sliced; }

        var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        var barFillRt = barFill.GetComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.sizeDelta = Vector2.zero;
        var barFillImg = barFill.GetComponent<Image>();
        barFillImg.color = new Color(0.35f, 0.95f, 0.88f);
        barFillImg.type  = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = 0f;
        ui.progressFill = barFillImg;

        // HUD panel (top center: Progress & Timer)
        var hudPanel = CreatePanel(canvasGo.transform, "HudPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(340f, 88f),
            new Color(0.06f, 0.10f, 0.18f, 0.92f), rounded);

        var progLbl = CreateLabel(hudPanel.transform, "ProgText", "0 / 8",
            new Vector2(-80f, 0f), new Vector2(140f, 50f), font, 28f, FontStyles.Bold,
            new Color(0.35f, 0.95f, 0.88f));
        progLbl.alignment = TextAlignmentOptions.Center;
        ui.progressText = progLbl;

        var timerLbl = CreateLabel(hudPanel.transform, "TimerText", "150.0s",
            new Vector2(80f, 0f), new Vector2(140f, 50f), font, 28f, FontStyles.Bold,
            Color.white);
        timerLbl.alignment = TextAlignmentOptions.Center;
        ui.timerText = timerLbl;

        // Victory Panel
        var vicPanel = CreatePanel(canvasGo.transform, "VictoryPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(480f, 320f),
            new Color(0.06f, 0.10f, 0.20f, 0.98f), rounded);
        vicPanel.SetActive(false);
        ui.levelCompletePanel = vicPanel;

        var vicTitle = CreateLabel(vicPanel.transform, "VicTitle", "SUMMIT CONQUERED!",
            new Vector2(0f, 85f), new Vector2(420f, 50f), font, 32f, FontStyles.Bold,
            new Color(0.35f, 0.95f, 0.88f));
        vicTitle.alignment = TextAlignmentOptions.Center;

        var vicTime = CreateLabel(vicPanel.transform, "VicTime", "Time: 42.5s",
            new Vector2(0f, 35f), new Vector2(420f, 35f), font, 22f, FontStyles.Normal,
            new Color(0.85f, 0.92f, 1.0f));
        vicTime.alignment = TextAlignmentOptions.Center;
        ui.levelCompleteTime = vicTime;

        var nextBtn = CreateButton(vicPanel.transform, "NextBtn", "MAIN MENU",
            new Vector2(0f, -45f), new Vector2(240f, 52f), rounded, font,
            new Color(0.25f, 0.85f, 0.80f));
        ui.nextLevelButton = nextBtn.GetComponent<Button>();

        var menuBtn = CreateButton(vicPanel.transform, "MenuBtn", "REPLAY LEVEL",
            new Vector2(0f, -110f), new Vector2(240f, 44f), rounded, font,
            new Color(0.18f, 0.28f, 0.44f));
        ui.completeMenuButton = menuBtn.GetComponent<Button>();

        // Fail Panel
        var failPanel = CreatePanel(canvasGo.transform, "FailPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(440f, 240f),
            new Color(0.18f, 0.05f, 0.06f, 0.98f), rounded);
        failPanel.SetActive(false);
        ui.failPanel = failPanel;

        var failTitle = CreateLabel(failPanel.transform, "FailTitle", "FROZEN CRAG COLLAPSE!",
            new Vector2(0f, 55f), new Vector2(400f, 50f), font, 28f, FontStyles.Bold,
            new Color(1f, 0.35f, 0.32f));
        failTitle.alignment = TextAlignmentOptions.Center;

        var failSub = CreateLabel(failPanel.transform, "FailSub", "The icy tile shattered. Memorize the path and try again!",
            new Vector2(0f, 5f), new Vector2(380f, 40f), font, 16f, FontStyles.Normal,
            new Color(0.95f, 0.80f, 0.80f));
        failSub.alignment = TextAlignmentOptions.Center;
    }

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

    static TMP_Text CreateLabelTopLeft(Transform parent, string name, string text,
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

    static GameObject CreateSwatch(Transform parent, string name, Vector2 pos,
        Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(18f, 18f);
        var img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        return go;
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

    // ── MESH / OBJECT UTILITIES ───────────────────────────────────────────────
    static Material CreateMaterial(string path, Color color, float metallic,
        float smoothness, float emission)
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

        var mesh = GetMeshAsset(name + "_" + size.x.ToString("0.00") + "_"
            + size.y.ToString("0.00") + "_" + size.z.ToString("0.00"), size.x, size.y, size.z, bevel);
        var mf = go.AddComponent<MeshFilter>();   mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;

        if (addCollider)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
        }
        return go;
    }

    static GameObject CreateHexObject(string name, Transform parent, Vector3 localPos,
        float radius, float height, float bevel, Material mat, bool addCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var mesh = GetHexMeshAsset(name + "_" + radius.ToString("0.00") + "_" + height.ToString("0.00"),
            radius, height, bevel);
        var mf = go.AddComponent<MeshFilter>();   mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;

        if (addCollider)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(radius * 2f, height, radius * 2f);
        }
        return go;
    }

    static GameObject CreateCone(string name, Transform parent, Vector3 localPos,
        float diameter, float height, Material mat, bool addCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var mesh = GetConeMeshAsset(name + "_" + diameter.ToString("0.00") + "_" + height.ToString("0.00"),
            diameter, height, 8);
        var mf = go.AddComponent<MeshFilter>();   mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;

        if (addCollider)
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = diameter * 0.45f;
            col.height = height;
        }
        return go;
    }

    static Mesh GetMeshAsset(string name, float x, float y, float z, float b)
    {
        string p = "Assets/Meshes/" + name + ".asset";
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if (m == null)
        {
            m = BeveledBoxMesh.Create(x, y, z, b);
            AssetDatabase.CreateAsset(m, p);
        }
        return m;
    }

    static Mesh GetHexMeshAsset(string name, float radius, float height, float bevel)
    {
        string p = "Assets/Meshes/" + name + ".asset";
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if (m == null)
        {
            m = HexPrismMesh.Create(radius, height, bevel);
            AssetDatabase.CreateAsset(m, p);
        }
        return m;
    }

    static Mesh GetConeMeshAsset(string name, float diameter, float height, int sides)
    {
        string p = "Assets/Meshes/" + name + ".asset";
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if (m == null)
        {
            var unit  = ConeMesh.CreateUnit(sides);
            var verts = unit.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].x *= diameter;
                verts[i].y *= height;
                verts[i].z *= diameter;
            }
            m = new Mesh();
            m.name = name;
            m.vertices = verts;
            m.triangles = unit.triangles;
            m.RecalculateNormals();
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, p);
        }
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
