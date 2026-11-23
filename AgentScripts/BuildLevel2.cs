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
/// Builds Level 2 – Desert theme.
/// Uses completely isolated Desert_*.mat materials so it never overwrites Level 1's forest theme.
/// Correctly calculates start and end island positions based on grid dimensions so no overlap occurs.
/// </summary>
public class BuildLevel2
{
    // ── Dedicated Desert palette materials (isolated from Level 1) ───────────
    static Material tileMat;          // desert tile surface
    static Material tileEdgeMat;      // desert edge bevel
    static Material tileRockMat;      // desert stone base

    static Material sandMat;          // sun-bleached sand
    static Material sandDarkMat;      // shadow / damp sand
    static Material duneMat;          // dune highlight
    static Material duneDarkMat;      // dune shadow
    static Material sandstoneMatA;    // warm buff sandstone (islands / rocks)
    static Material sandstoneMatB;    // darker terracotta sandstone
    static Material mesaMat;          // flat-top mesa cap (ochre)
    static Material mesaDarkMat;      // mesa cliff shadow
    static Material boneMat;          // bleached bone / dry salt
    static Material cactusBodyMat;    // succulent green
    static Material cactusSpineMat;   // pale cactus spine accent
    static Material desertRockMat;    // desert rock
    static Material goldMat;          // finish pad gold

    // Player materials (shared character appearance)
    static Material hoodieMat, hoodMat, jeansMat, skinMat, hairMat, shoeMat, eyeMat, pupilMat;

    [MenuItem("Tools/Build Level 2 (Desert)")]
    public static void Run()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Meshes");
        EnsureFolder("Assets/UI");

        string scenePath = "Assets/Scenes/Level2.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        CreateMaterials();
        var levelData = BuildLevelData();

        foreach (var root in scene.GetRootGameObjects())
            UnityEngine.Object.DestroyImmediate(root);

        // ── GRID ──────────────────────────────────────────────────────────────
        var gridRoot    = new GameObject("Grid");
        var gridManager = gridRoot.AddComponent<GridManager>();
        gridManager.safeMat     = tileMat;
        gridManager.unsafeMat   = tileMat;
        gridManager.tileEdgeMat = tileEdgeMat;
        gridManager.tileRockMat = tileRockMat;

        float stride    = levelData.tileSize + levelData.tileGap;
        float gridDepth = (levelData.rows - 1) * stride;
        float halfDepth = gridDepth * 0.5f;

        // Island spacing: offset outside the first and last row of tiles with a comfortable jump gap
        float islandZOffset = halfDepth + 3.5f;

        // ── START / END ISLANDS (properly spaced outside grid) ─────────────────
        var startIsland = CreateDesertIsland("StartIsland", null,
            new Vector3(0f, 0.11f, -islandZOffset),
            5.4f, 4.4f, sandMat, sandstoneMatA, sandstoneMatB, true);

        var endIsland = CreateDesertIsland("EndIsland", null,
            new Vector3(0f, 0.11f, islandZOffset),
            5.4f, 4.4f, mesaMat, sandstoneMatA, sandstoneMatB, true);

        // Goal pad (golden oasis circle)
        CreateBeveledObject("GoalPad", endIsland.transform,
            new Vector3(0f, 0.055f, 0f),
            new Vector3(2.2f, 0.11f, 2.2f), 0.05f, goldMat, false);

        // Floating gem marker
        var marker = CreatePrimitive(PrimitiveType.Cube, "GoalMarker", endIsland.transform,
            new Vector3(0f, 2.3f, 0f), new Vector3(0.55f, 0.55f, 0.55f), goldMat, false);
        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);

        // End trigger
        var endTrigger = new GameObject("EndTrigger");
        endTrigger.transform.SetParent(endIsland.transform, false);
        endTrigger.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var endBox = endTrigger.AddComponent<BoxCollider>();
        endBox.size      = new Vector3(4.4f, 3.2f, 3.4f);
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
        cam.transform.position = new Vector3(0f, 11.5f, -islandZOffset - 9.6f);

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
        cf.distance        = 13f;
        cf.pitch           = 40f;
        cf.yaw             = 0f;
        cf.targetHeight    = 1.35f;
        cf.lookAhead       = 4.0f;
        cf.followSmooth    = 12f;
        cf.rotationSmooth  = 16f;
        cf.minDistance     = 7f;
        cf.maxDistance     = 22f;
        player.GetComponent<PlayerController>().cameraTransform = cam.transform;

        // ── LIGHTING / DESERT SKY ─────────────────────────────────────────────
        ConfigureLighting();
        ConfigurePostProcessing();

        // ── ENVIRONMENT ───────────────────────────────────────────────────────
        var envRoot  = new GameObject("Environment").transform;
        var heatHaze = BuildEnvironment(envRoot);

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
        gm.levels            = new LevelData[] { levelData };
        gm.currentLevelIndex = 1;

        // ── UI ────────────────────────────────────────────────────────────────
        BuildUI(lm, levelData);

        // ── EVENT SYSTEM ──────────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null) es.AddComponent(inputModuleType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── ANIMATION (dust haze drift) ───────────────────────────────────────
        var animator       = envRoot.gameObject.AddComponent<EnvironmentAnimator>();
        animator.cloudRoot = heatHaze;

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Level 2 (Desert) rebuilt with dedicated materials and bug fixed.");
    }

    // ── ISOLATED DESERT MATERIALS ─────────────────────────────────────────────
    static void CreateMaterials()
    {
        // Dedicated desert tile materials (NOT touching Level 1 Tile.mat / Stone.mat)
        tileMat       = CreateMaterial("Assets/Materials/DesertTile.mat",       new Color(0.18f, 0.14f, 0.11f), 0.05f, 0.14f, 0f);
        tileEdgeMat   = CreateMaterial("Assets/Materials/DesertTileEdge.mat",   new Color(0.48f, 0.34f, 0.22f), 0f,    0.14f, 0f);
        tileRockMat   = CreateMaterial("Assets/Materials/DesertTileRock.mat",   new Color(0.33f, 0.22f, 0.13f), 0f,    0.10f, 0f);

        // Dedicated desert landscape materials
        sandMat       = CreateMaterial("Assets/Materials/DesertSand.mat",       new Color(0.89f, 0.74f, 0.48f), 0f, 0.08f, 0f);
        sandDarkMat   = CreateMaterial("Assets/Materials/DesertSandDark.mat",   new Color(0.72f, 0.56f, 0.33f), 0f, 0.07f, 0f);
        duneMat       = CreateMaterial("Assets/Materials/DesertDune.mat",       new Color(0.93f, 0.80f, 0.57f), 0f, 0.09f, 0f);
        duneDarkMat   = CreateMaterial("Assets/Materials/DesertDuneDark.mat",   new Color(0.76f, 0.60f, 0.38f), 0f, 0.07f, 0f);

        sandstoneMatA = CreateMaterial("Assets/Materials/DesertSandstoneA.mat", new Color(0.78f, 0.56f, 0.36f), 0f, 0.12f, 0f);
        sandstoneMatB = CreateMaterial("Assets/Materials/DesertSandstoneB.mat", new Color(0.55f, 0.37f, 0.22f), 0f, 0.10f, 0f);
        mesaMat       = CreateMaterial("Assets/Materials/DesertMesa.mat",       new Color(0.82f, 0.52f, 0.26f), 0f, 0.12f, 0f);
        mesaDarkMat   = CreateMaterial("Assets/Materials/DesertMesaDark.mat",   new Color(0.56f, 0.33f, 0.15f), 0f, 0.10f, 0f);

        boneMat       = CreateMaterial("Assets/Materials/DesertBone.mat",       new Color(0.94f, 0.90f, 0.80f), 0f, 0.08f, 0f);
        cactusBodyMat = CreateMaterial("Assets/Materials/DesertCactus.mat",     new Color(0.24f, 0.52f, 0.22f), 0f, 0.18f, 0f);
        cactusSpineMat= CreateMaterial("Assets/Materials/DesertCactusSpine.mat",new Color(0.84f, 0.82f, 0.70f), 0f, 0.20f, 0f);
        desertRockMat = CreateMaterial("Assets/Materials/DesertRock.mat",        new Color(0.70f, 0.55f, 0.40f), 0f, 0.10f, 0f);
        goldMat       = CreateMaterial("Assets/Materials/Gold.mat",            new Color(1f, 0.72f, 0.22f),    0.25f, 0.55f, 0.55f);

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
        string path = "Assets/Data/Level2Data.asset";
        var d = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(d, path);
        }
        d.levelName  = "Level 2";
        d.sceneName  = "Level2";
        d.rows       = 7;
        d.columns    = 5;
        d.tileSize   = 2.4f;
        d.tileGap    = 0.65f;
        d.timeLimit  = 150f;
        d.objective  = "The desert sands hide deadly traps. Memorize the path in 5 SECONDS before it disappears, then walk the path from memory to reach the golden oasis!";
        d.instructions = "WASD / Arrows – Move\nSpace – Jump\nRight drag / Q E – Rotate camera\nMouse wheel – Zoom\nR – Restart\nF – Dance";
        d.hideMapAfterCountdown = true;
        d.mapVisibleDuration = 5.0f;
        d.isUpsideDownPattern = false;

        d.safeTiles = new List<Vector2Int>
        {
            new Vector2Int(1, 2),
            new Vector2Int(2, 4),
            new Vector2Int(3, 3),
            new Vector2Int(4, 1),
            new Vector2Int(5, 3),
            new Vector2Int(6, 5),
            new Vector2Int(7, 2),
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
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
        var c = go.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
        return go;
    }

    static void ConfigureLighting()
    {
        var light = UnityEngine.Object.FindAnyObjectByType<Light>();
        if (light == null)
        {
            var go = new GameObject("Sun");
            light  = go.AddComponent<Light>();
        }
        light.type           = LightType.Directional;
        light.color          = new Color(1f, 0.90f, 0.65f);
        light.intensity      = 1.65f;
        light.shadows        = LightShadows.Soft;
        light.shadowStrength = 0.90f;
        light.shadowBias     = 0.025f;
        light.shadowNormalBias = 0.30f;
        light.transform.rotation = Quaternion.Euler(62f, -20f, 0f);

        var sky = CreateDesertSkyboxMaterial();
        RenderSettings.skybox           = sky;
        RenderSettings.sun              = light;
        RenderSettings.ambientMode      = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;

        RenderSettings.fog        = true;
        RenderSettings.fogColor   = new Color(0.86f, 0.73f, 0.48f);
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0055f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        DynamicGI.UpdateEnvironment();
    }

    static Material CreateDesertSkyboxMaterial()
    {
        var path   = "Assets/Materials/DesertSkybox.mat";
        var shader = Shader.Find("Skybox/Procedural");
        var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetFloat("_SunDisk",           2f);
        mat.SetFloat("_SunSize",           0.06f);
        mat.SetFloat("_SunSizeConvergence", 4f);
        mat.SetFloat("_AtmosphereThickness", 0.45f);
        mat.SetColor("_SkyTint",     new Color(0.72f, 0.82f, 1.00f));
        mat.SetColor("_GroundColor", new Color(0.76f, 0.60f, 0.36f));
        mat.SetFloat("_Exposure",    1.30f);
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
        color.postExposure.Override(0.14f);
        color.contrast.Override(12f);
        color.saturation.Override(-8f);

        var go     = new GameObject("GlobalVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile  = profile;

        EditorUtility.SetDirty(profile);
    }

    static Transform BuildEnvironment(Transform root)
    {
        const float groundY = -30f;

        var ground = CreatePrimitive(PrimitiveType.Plane, "Ground", root,
            new Vector3(0f, groundY, 0f), new Vector3(30f, 1f, 30f), sandMat, false);
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Sand dunes
        for (int i = 0; i < 16; i++)
        {
            float angle  = i * (360f / 16f) + 5f;
            float radius = 50f + (i % 5) * 16f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 2.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            var dune = CreatePrimitive(PrimitiveType.Sphere, "Dune" + i, root, pos,
                new Vector3(50f + (i % 4) * 14f, 7f + (i % 3) * 2.5f, 44f + (i % 4) * 10f),
                i % 2 == 0 ? duneMat : sandDarkMat, false);
            dune.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // Ripples
        UnityEngine.Random.InitState(20261123);
        for (int i = 0; i < 20; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 22f + UnityEngine.Random.Range(0f, 70f);
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 0.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float patch = 8f + UnityEngine.Random.Range(0f, 18f);
            var ripple = CreatePrimitive(PrimitiveType.Sphere, "Ripple" + i, root, pos,
                new Vector3(patch, 1.8f, patch * 0.6f), i % 2 == 0 ? duneMat : duneDarkMat, false);
            ripple.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // Mesas
        CreateMesa("MesaA", root, new Vector3(-90f, groundY, 50f),  28f, 25f, 36f, 22f);
        CreateMesa("MesaB", root, new Vector3( 95f, groundY, 30f),  22f, 18f, 28f, 16f);
        CreateMesa("MesaC", root, new Vector3(-60f, groundY, 120f), 36f, 32f, 42f, 28f);
        CreateMesa("MesaD", root, new Vector3( 70f, groundY, 110f), 24f, 20f, 32f, 18f);
        CreateMesa("MesaE", root, new Vector3(-130f, groundY, 20f), 42f, 38f, 54f, 34f);
        CreateMesa("MesaF", root, new Vector3( 130f, groundY, 60f), 32f, 28f, 44f, 24f);

        for (int i = 0; i < 8; i++)
        {
            float angle  = i * 45f + 18f;
            float radius = 160f + (i % 3) * 35f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateMesa("FarMesa" + i, root, pos,
                48f + (i % 3) * 20f, 40f + (i % 3) * 14f,
                60f + (i % 3) * 24f, 36f + (i % 3) * 10f);
        }

        // Rocks
        UnityEngine.Random.InitState(77123);
        for (int i = 0; i < 28; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 24f + UnityEngine.Random.Range(0f, 55f);
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                groundY + 0.4f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float sz = 1.5f + UnityEngine.Random.Range(0f, 3.5f);
            CreateBeveledObject("Rock" + i, root, pos,
                new Vector3(sz, sz * 0.7f, sz * 0.9f), 0.18f,
                i % 3 == 0 ? boneMat : (i % 2 == 0 ? sandstoneMatA : sandstoneMatB), false);
        }

        // Bleached bone
        for (int i = 0; i < 8; i++)
        {
            float angle  = i * 44f + 11f;
            float radius = 28f + (i % 4) * 7f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY + 0.3f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateBeveledObject("Bone" + i, root, pos,
                new Vector3(1.1f, 0.8f, 1.3f), 0.22f, boneMat, false);
        }

        // Cacti
        UnityEngine.Random.InitState(42);
        for (int i = 0; i < 20; i++)
        {
            float angle  = UnityEngine.Random.Range(0f, 360f);
            float radius = 26f + UnityEngine.Random.Range(0f, 50f);
            Vector3 basePos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateCactus(root, basePos, UnityEngine.Random.Range(0.8f, 1.6f), i % 3);
        }
        foreach (float ang in new[] { 20f, 85f, 155f, 230f, 300f })
        {
            float radius = 36f;
            Vector3 basePos = new Vector3(
                Mathf.Cos(ang * Mathf.Deg2Rad) * radius, groundY,
                Mathf.Sin(ang * Mathf.Deg2Rad) * radius);
            CreateCactus(root, basePos, 2.2f, 0);
        }

        // Floating desert islands (distant)
        CreateDesertIsland("FloatIslandA", root, new Vector3(-46f, 11f, 20f),
            4.6f, 3.8f, duneMat, sandstoneMatA, sandstoneMatB, false);
        CreateDesertIsland("FloatIslandB", root, new Vector3( 50f, 8f,  18f),
            3.8f, 3.2f, mesaMat, sandstoneMatA, sandstoneMatB, false);
        CreateDesertIsland("FloatIslandC", root, new Vector3(-38f, 5f,  46f),
            3.4f, 2.8f, duneMat, sandstoneMatA, sandstoneMatB, false);
        CreateDesertIsland("FloatIslandD", root, new Vector3( 42f, 15f, 44f),
            5.0f, 4.0f, mesaMat, sandstoneMatA, sandstoneMatB, false);

        // Floating rocks
        for (int i = 0; i < 7; i++)
        {
            float angle  = i * 52f + 15f;
            float radius = 36f + (i % 3) * 7f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                3f + (i % 4) * 2.5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            CreateBeveledObject("FloatRock" + i, root, pos,
                new Vector3(1.8f + (i % 3) * 0.7f, 1.3f, 1.6f + (i % 2) * 0.7f),
                0.20f, i % 2 == 0 ? sandstoneMatA : mesaDarkMat, false);
        }

        // Dust clouds
        var clouds = new GameObject("DustClouds");
        clouds.transform.SetParent(root, false);
        for (int i = 0; i < 12; i++)
        {
            float angle  = i * (360f / 12f) + 7f;
            float radius = 55f + (i % 5) * 12f;
            Vector3 pos  = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                18f + (i % 4) * 5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

            var cluster = new GameObject("DustCloud" + i);
            cluster.transform.SetParent(clouds.transform, false);
            cluster.transform.localPosition = pos;

            for (int k = 0; k < 4; k++)
            {
                var puff = CreatePrimitive(PrimitiveType.Sphere, "Puff" + k, cluster.transform,
                    new Vector3((k - 1.5f) * 3.8f, (k % 2) * 1.2f, (k % 3) * 0.9f),
                    new Vector3(8f - k * 0.7f, 2.6f - k * 0.2f, 5f - k * 0.3f),
                    duneMat, false);
                puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        return clouds.transform;
    }

    static void CreateCactus(Transform parent, Vector3 basePos, float scale, int variant)
    {
        var cactus = new GameObject("Cactus");
        cactus.transform.SetParent(parent, false);
        cactus.transform.localPosition = basePos;
        cactus.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        CreatePrimitive(PrimitiveType.Cylinder, "Trunk", cactus.transform,
            new Vector3(0f, 2.4f * scale, 0f),
            new Vector3(0.38f * scale, 2.4f * scale, 0.38f * scale), cactusBodyMat, false);

        if (variant == 0)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var armPivot = new GameObject("ArmPivot");
                armPivot.transform.SetParent(cactus.transform, false);
                armPivot.transform.localPosition = new Vector3(side * 0.38f * scale, 2.6f * scale, 0f);
                armPivot.transform.localRotation = Quaternion.Euler(0f, 0f, side * 90f);

                CreatePrimitive(PrimitiveType.Cylinder, "ArmH", armPivot.transform,
                    new Vector3(0f, 0.7f * scale, 0f),
                    new Vector3(0.28f * scale, 0.7f * scale, 0.28f * scale), cactusBodyMat, false);

                CreatePrimitive(PrimitiveType.Cylinder, "ArmV", cactus.transform,
                    new Vector3(side * 1.4f * scale, 3.8f * scale, 0f),
                    new Vector3(0.28f * scale, 1.2f * scale, 0.28f * scale), cactusBodyMat, false);

                for (int s = 0; s < 3; s++)
                {
                    CreatePrimitive(PrimitiveType.Sphere, "Spine" + s, cactus.transform,
                        new Vector3(side * 0.42f * scale, (1.2f + s * 0.9f) * scale, 0.38f * scale),
                        new Vector3(0.07f, 0.07f, 0.14f) * scale, cactusSpineMat, false);
                }
            }
        }
        else if (variant == 1)
        {
            CreatePrimitive(PrimitiveType.Sphere, "Barrel", cactus.transform,
                new Vector3(0f, 1.1f * scale, 0f),
                new Vector3(0.9f * scale, 1.1f * scale, 0.9f * scale), cactusBodyMat, false);
            for (int s = 0; s < 6; s++)
            {
                float ang = s * 60f;
                CreatePrimitive(PrimitiveType.Sphere, "Sp" + s, cactus.transform,
                    new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 0.46f * scale,
                                1.1f * scale,
                                Mathf.Sin(ang * Mathf.Deg2Rad) * 0.46f * scale),
                    new Vector3(0.06f, 0.06f, 0.18f) * scale, cactusSpineMat, false);
            }
        }
        else
        {
            for (int p = 0; p < 3; p++)
            {
                float offsetX = (p % 2 == 0 ? 0f : (p == 1 ? -0.4f : 0.4f)) * scale;
                CreatePrimitive(PrimitiveType.Sphere, "Pad" + p, cactus.transform,
                    new Vector3(offsetX, (1.2f + p * 1.1f) * scale, 0f),
                    new Vector3(0.65f * scale, 0.80f * scale, 0.30f * scale), cactusBodyMat, false);
            }
        }
    }

    static void CreateMesa(string name, Transform parent, Vector3 basePos,
        float topW, float topD, float baseW, float baseD)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = basePos;

        float groundY = 0f;
        float h1 = 8f, h2 = 12f, h3 = 6f;

        CreateBeveledObject(name + "_Base", root.transform,
            new Vector3(0f, groundY + h1 * 0.5f, 0f),
            new Vector3(baseW, h1, baseD), 0.4f, mesaDarkMat, false);

        CreateBeveledObject(name + "_Mid", root.transform,
            new Vector3(0f, groundY + h1 + h2 * 0.5f, 0f),
            new Vector3(baseW * 0.78f, h2, baseD * 0.78f), 0.3f, mesaMat, false);

        CreateBeveledObject(name + "_Top", root.transform,
            new Vector3(0f, groundY + h1 + h2 + h3 * 0.5f, 0f),
            new Vector3(topW, h3, topD), 0.25f, sandstoneMatA, false);

        CreateBeveledObject(name + "_Cap", root.transform,
            new Vector3(0f, groundY + h1 + h2 + h3, 0f),
            new Vector3(topW * 0.9f, 0.8f, topD * 0.9f), 0.1f, duneMat, false);
    }

    static GameObject CreateDesertIsland(string name, Transform parent, Vector3 topCenter,
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

        // MAP panel
        var mapPanel = CreatePanel(canvasGo.transform, "MapPanel",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(360f, 800f), new Vector2(28f, 0f), rounded,
            new Color(0.09f, 0.06f, 0.04f, 0.90f));

        CreateText(mapPanel.transform, "MapTitle", "PATH MAP", font, 30f,
            new Vector2(0f, 340f), new Vector2(312f, 40f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);
        CreateText(mapPanel.transform, "MapSub", "DESERT COURSE VIEW", font, 12.5f,
            new Vector2(0f, 306f), new Vector2(312f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.70f, 0.50f, 1f), 2.5f);
        CreateBar(mapPanel.transform, "MapDivider", new Vector2(0f, 282f),
            new Vector2(312f, 2f), new Color(1f, 0.9f, 0.6f, 0.14f), rounded);

        CreateText(mapPanel.transform, "FinishTag", "OASIS", font, 12f,
            new Vector2(0f, 206f), new Vector2(312f, 18f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.96f, 0.78f, 0.36f, 1f), 3f);

        var mapContainerGo = new GameObject("MapContainer", typeof(RectTransform));
        mapContainerGo.transform.SetParent(mapPanel.transform, false);
        var mapRect = mapContainerGo.GetComponent<RectTransform>();
        mapRect.anchorMin      = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax      = new Vector2(0.5f, 0.5f);
        mapRect.pivot          = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta      = new Vector2(312f, 400f);
        mapRect.anchoredPosition = Vector2.zero;
        ui.mapContainer = mapContainerGo.transform;

        var markerGo   = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        markerGo.transform.SetParent(mapContainerGo.transform, false);
        var markerRect = markerGo.GetComponent<RectTransform>();
        markerRect.anchorMin      = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax      = new Vector2(0.5f, 0.5f);
        markerRect.pivot          = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta      = new Vector2(18f, 18f);
        var markerImg             = markerGo.GetComponent<Image>();
        markerImg.sprite          = dot;
        markerImg.color           = new Color(0.62f, 1f, 0.70f, 1f);
        markerImg.raycastTarget   = false;
        markerGo.SetActive(false);
        ui.playerMarker = markerImg;

        CreateText(mapPanel.transform, "StartTag", "START", font, 12f,
            new Vector2(0f, -196f), new Vector2(312f, 18f), TextAlignmentOptions.Center,
            FontStyles.Bold, new Color(0.80f, 0.72f, 0.56f, 1f), 3f);

        CreateSwatch(mapPanel.transform, "SafeSwatch",   new Vector2(-92f, -242f),
            new Vector2(17f, 17f), new Color(0.24f, 0.90f, 0.44f), rounded);
        CreateText(mapPanel.transform, "SafeLabel", "SAFE", font, 12.5f,
            new Vector2(-45f, -242f), new Vector2(70f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.88f, 0.80f, 0.64f, 1f), 1.5f);
        CreateSwatch(mapPanel.transform, "UnsafeSwatch", new Vector2(24f, -242f),
            new Vector2(17f, 17f), new Color(0.20f, 0.14f, 0.09f), rounded);
        CreateText(mapPanel.transform, "UnsafeLabel", "UNSAFE", font, 12.5f,
            new Vector2(80f, -242f), new Vector2(90f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.88f, 0.80f, 0.64f, 1f), 1.5f);

        CreateText(mapPanel.transform, "MapHint", "The map mirrors what you see in the desert.", font, 12f,
            new Vector2(0f, -296f), new Vector2(312f, 34f), TextAlignmentOptions.Center,
            FontStyles.Normal, new Color(0.72f, 0.62f, 0.48f, 1f));

        // INFO panel
        var infoPanel = CreatePanel(canvasGo.transform, "InfoPanel",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(310f, 580f), new Vector2(-28f, 0f), rounded,
            new Color(0.09f, 0.06f, 0.04f, 0.90f));

        CreateText(infoPanel.transform, "LevelTag", "LEVEL 2", font, 13f,
            new Vector2(0f, 258f), new Vector2(260f, 22f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.70f, 0.50f, 1f), 2.5f);
        CreateText(infoPanel.transform, "LevelName", "DESERT TRIAL", font, 32f,
            new Vector2(0f, 220f), new Vector2(260f, 44f), TextAlignmentOptions.Left,
            FontStyles.Bold, Color.white);
        CreateBar(infoPanel.transform, "Divider", new Vector2(0f, 190f),
            new Vector2(260f, 2f), new Color(1f, 0.9f, 0.6f, 0.14f), rounded);

        CreateText(infoPanel.transform, "ObjHead", "OBJECTIVE", font, 12f,
            new Vector2(0f, 158f), new Vector2(260f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.70f, 0.50f, 1f), 2.5f);
        CreateText(infoPanel.transform, "ObjBody", data.objective, font, 14f,
            new Vector2(0f, 90f), new Vector2(260f, 100f), TextAlignmentOptions.Left,
            FontStyles.Normal, new Color(0.90f, 0.84f, 0.72f, 1f));

        CreateBar(infoPanel.transform, "Divider2", new Vector2(0f, 30f),
            new Vector2(260f, 2f), new Color(1f, 0.9f, 0.6f, 0.14f), rounded);

        CreateText(infoPanel.transform, "CtrlHead", "CONTROLS", font, 12f,
            new Vector2(0f, 0f), new Vector2(260f, 20f), TextAlignmentOptions.Left,
            FontStyles.Bold, new Color(0.80f, 0.70f, 0.50f, 1f), 2.5f);
        CreateText(infoPanel.transform, "CtrlBody", data.instructions, font, 13f,
            new Vector2(0f, -100f), new Vector2(260f, 160f), TextAlignmentOptions.Left,
            FontStyles.Normal, new Color(0.90f, 0.84f, 0.72f, 1f));

        // HUD (top center: timer)
        var hud = CreatePanel(canvasGo.transform, "HUD",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(380f, 72f), new Vector2(0f, -20f), rounded,
            new Color(0.09f, 0.06f, 0.04f, 0.86f));

        var timerGo = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerGo.transform.SetParent(hud.transform, false);
        var timerRect = timerGo.GetComponent<RectTransform>();
        timerRect.anchorMin = timerRect.anchorMax = timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.sizeDelta = new Vector2(340f, 50f);
        timerRect.anchoredPosition = Vector2.zero;
        var timerTxt = timerGo.GetComponent<TextMeshProUGUI>();
        timerTxt.text = "2:30";
        timerTxt.font = font;
        timerTxt.fontSize     = 34f;
        timerTxt.color        = Color.white;
        timerTxt.fontStyle    = FontStyles.Bold;
        timerTxt.alignment    = TextAlignmentOptions.Center;
        ui.timerText = timerTxt;

        var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(hud.transform, false);
        var subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = subRect.anchorMax = subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.sizeDelta = new Vector2(340f, 28f);
        subRect.anchoredPosition = new Vector2(0f, -26f);
        var subTxt = subGo.GetComponent<TextMeshProUGUI>();
        subTxt.text = "DESERT TRIAL";
        subTxt.font = font;
        subTxt.fontSize     = 15f;
        subTxt.color        = new Color(0.80f, 0.70f, 0.50f, 1f);
        subTxt.fontStyle    = FontStyles.Bold;
        subTxt.alignment    = TextAlignmentOptions.Center;
        subTxt.characterSpacing = 2.5f;
    }

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 offset, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(anchorMin.x > 0.5f ? 1f : anchorMin.x < 0.5f ? 0f : 0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color  = color;
        img.type   = Image.Type.Sliced;
        img.raycastTarget = false;
        return go;
    }

    static void CreateText(Transform parent, string name, string text, TMP_FontAsset font,
        float size, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align,
        FontStyles style, Color color, float spacing = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.font = font;
        tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.characterSpacing = spacing;
        tmp.raycastTarget = false;
    }

    static void CreateBar(Transform parent, string name, Vector2 pos, Vector2 size,
        Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = color;
        img.type = Image.Type.Sliced; img.raycastTarget = false;
    }

    static GameObject CreateSwatch(Transform parent, string name, Vector2 pos,
        Vector2 size, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = color;
        img.type = Image.Type.Sliced; img.raycastTarget = false;
        return go;
    }

    static Sprite CreateRoundedSprite(string path, int size, int radius, bool circle)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha;
                if (circle)
                {
                    float dx = x - center, dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - center;
                    alpha = Mathf.Clamp01(0.5f - dist);
                }
                else
                {
                    float px = Mathf.Abs(x - center) - (center - radius);
                    float py = Mathf.Abs(y - center) - (center - radius);
                    float ox = Mathf.Max(px, 0f), oy = Mathf.Max(py, 0f);
                    float outside = Mathf.Sqrt(ox * ox + oy * oy);
                    float inside  = Mathf.Min(Mathf.Max(px, py), 0f);
                    float sdf     = outside + inside - radius;
                    alpha = Mathf.Clamp01(0.5f - sdf);
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        string fullPath = System.IO.Path.Combine(
            System.IO.Directory.GetParent(Application.dataPath).FullName, path);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
        System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder     = circle ? Vector4.zero : new Vector4(radius, radius, radius, radius);
            importer.mipmapEnabled    = false;
            importer.alphaIsTransparency = true;
            importer.filterMode       = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

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
        mat.EnableKeyword("_EMISSION");
        if (emission > 0f)
        {
            mat.SetColor("_EmissionColor", color * emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        var r = go.GetComponent<Renderer>();
        if (r != null) { r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        if (!keepCollider) { var c = go.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c); }
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
            box.size = size; box.center = Vector3.zero;
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

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf   = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
