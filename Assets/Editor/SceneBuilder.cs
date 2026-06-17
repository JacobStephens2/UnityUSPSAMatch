using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Shooter;

/// <summary>
/// Builds the entire USPSA-style stage (bay, targets, player rig, HUD) from
/// code — no binary scene/prefab/art dependencies. Stage 1 is paper/steel only;
/// Stage 2 adds wild-west outlaws that move and shoot back. Run via
/// Tools ▸ Shooter ▸ Build All Stages, or headless: -executeMethod
/// SceneBuilder.BuildAll
/// </summary>
public static class SceneBuilder
{
    const string Stage1Path = "Assets/Scenes/Main.unity";
    const string Stage2Path = "Assets/Scenes/Stage2.unity";

    // Paper targets: x, z, facing-yaw, isNoShoot. y is fixed (board centre 1.15).
    static readonly float[][] Papers1 =
    {
        new[]{-6f, -2f, 180f, 0f},
        new[]{-3.5f,-2f, 180f, 1f},   // no-shoot
        new[]{-1f, -1f, 180f, 0f},
        new[]{ 2f, -2f, 180f, 0f},
        new[]{ 5f, -1f, 165f, 0f},
        new[]{-5f,  4f, 180f, 0f},
        new[]{-1.5f,5f, 180f, 0f},
        new[]{ 2.5f,5f, 195f, 0f},
        new[]{ 6f,  4f, 180f, 1f},    // no-shoot
        new[]{ 0f,  9f, 180f, 0f},
    };

    static readonly float[][] Steels1 =
    {
        new[]{-7f, 2f},
        new[]{ 7f, 2f},
        new[]{-3f, 8f},
        new[]{ 3f, 8f},
    };

    // Stage 2: a leaner target array, framing the gunfight with the outlaws.
    static readonly float[][] Papers2 =
    {
        new[]{-7f, 3f, 180f, 0f},
        new[]{ 7f, 3f, 180f, 0f},
        new[]{-3f, 12f,180f, 1f},   // no-shoot (a bystander behind the outlaws)
        new[]{ 4f, 1f, 180f, 0f},
    };

    static readonly float[][] Steels2 =
    {
        new[]{-9f, 8f},
        new[]{ 9f, 8f},
    };

    // Outlaws: x, z, patrolHalfWidth, maxHits, hitChance, moveSpeed.
    static readonly float[][] Outlaws2 =
    {
        new[]{ 0f, 7f,  4.5f, 5f, 0.55f, 2.7f},   // the main bad guy: centre, aggressive
        new[]{-6f, 12f, 3.0f, 4f, 0.42f, 2.1f},   // a second outlaw, further back
    };

    [MenuItem("Tools/Shooter/Build All Stages")]
    public static void BuildAll()
    {
        BuildStage(1, Stage1Path);
        BuildStage(2, Stage2Path);
        // Build settings: Stage 1 first (index 0), then Stage 2.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Stage1Path, true),
            new EditorBuildSettingsScene(Stage2Path, true),
        };
        Debug.Log("[SceneBuilder] Built Stage 1 + Stage 2.");
    }

    [MenuItem("Tools/Shooter/Build Scene")]
    public static void Build() => BuildAll();

    static void BuildStage(int stage, string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildBay();
        BuildLighting();
        var input = new GameObject("GameInput").AddComponent<GameInput>();
        var player = BuildPlayer(input);

        var papers = stage == 1 ? Papers1 : Papers2;
        var steels = stage == 1 ? Steels1 : Steels2;
        foreach (var p in papers) BuildPaperTarget(p[0], p[1], p[2], p[3] > 0.5f);
        foreach (var s in steels) BuildSteel(s[0], s[1]);
        if (stage == 2)
            foreach (var e in Outlaws2)
                BuildEnemy(e[0], e[1], e[2], (int)e[3], e[4], e[5]);

        var mm = new GameObject("MatchManager").AddComponent<MatchManager>();
        mm.stageNumber = stage;
        mm.nextScene = stage == 1 ? "Stage2" : "";
        mm.player = player.GetComponent<PlayerController>();
        mm.gun = player.GetComponent<Gun>();
        mm.playerHealth = player.GetComponent<PlayerHealth>();
        BuildHud(mm);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[SceneBuilder] Built stage " + stage + " -> " + path);
    }

    // ---------------- environment ----------------

    static void BuildBay()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5f, 1f, 4f); // 50 x 40
        ground.transform.position = new Vector3(0, 0, 2f);
        Paint(ground, new Color(0.42f, 0.40f, 0.36f)); // gravel/concrete

        float h = 3.5f;
        MakeWall("Berm_N", new Vector3(0, h / 2, 16f), new Vector3(44, h, 1f), 0.30f);
        MakeWall("Berm_S", new Vector3(0, h / 2, -14f), new Vector3(44, h, 1f), 0.30f);
        MakeWall("Berm_E", new Vector3(22, h / 2, 1f), new Vector3(1f, h, 32f), 0.30f);
        MakeWall("Berm_W", new Vector3(-22, h / 2, 1f), new Vector3(1f, h, 32f), 0.30f);

        // Waist-high vision barriers — cover the player can duck behind in Stage 2.
        MakeWall("Barrier_1", new Vector3(-2.5f, 0.9f, 1.5f), new Vector3(2.4f, 1.8f, 0.18f), 0.22f);
        MakeWall("Barrier_2", new Vector3(3.5f, 0.9f, 6.5f), new Vector3(2.4f, 1.8f, 0.18f), 0.22f);
    }

    static void MakeWall(string name, Vector3 pos, Vector3 scale, float grey)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position = pos;
        w.transform.localScale = scale;
        Paint(w, new Color(grey, grey * 0.95f, grey * 0.85f));
    }

    static void BuildLighting()
    {
        var sunGo = new GameObject("Directional Light");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.color = new Color(1f, 0.97f, 0.92f);
        sunGo.transform.rotation = Quaternion.Euler(52f, 20f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.50f);
    }

    // ---------------- targets ----------------

    static void BuildPaperTarget(float x, float z, float yaw, bool noShoot)
    {
        var root = new GameObject(noShoot ? "NoShoot" : "PaperTarget");
        root.transform.position = new Vector3(x, 1.15f, z);
        root.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var pt = root.AddComponent<PaperTarget>();
        pt.isNoShoot = noShoot;

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        board.transform.SetParent(root.transform, false);
        board.transform.localScale = new Vector3(0.46f, 0.76f, 0.06f);
        Paint(board, noShoot ? new Color(0.93f, 0.93f, 0.93f) : new Color(0.80f, 0.67f, 0.46f));

        if (!noShoot)
        {
            ZoneDecal(root.transform, 0.30f, 0.61f, 0.031f, new Color(0.63f, 0.51f, 0.35f)); // C
            ZoneDecal(root.transform, 0.152f, 0.28f, 0.033f, new Color(0.42f, 0.33f, 0.22f)); // A
        }
        else
        {
            ZoneDecal(root.transform, 0.46f, 0.76f, 0.031f, new Color(0.80f, 0.15f, 0.15f)); // red frame
            ZoneDecal(root.transform, 0.40f, 0.70f, 0.033f, new Color(0.95f, 0.95f, 0.95f)); // white field
        }

        var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Stand";
        stand.transform.SetParent(root.transform, false);
        stand.transform.localScale = new Vector3(0.05f, 0.77f, 0.05f);
        stand.transform.localPosition = new Vector3(0f, -0.765f, 0f);
        DestroyCollider(stand);
        Paint(stand, new Color(0.30f, 0.22f, 0.15f));
    }

    static void ZoneDecal(Transform parent, float w, float h, float zOff, Color col)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d.name = "Zone";
        d.transform.SetParent(parent, false);
        d.transform.localPosition = new Vector3(0f, 0f, zOff);
        d.transform.localScale = new Vector3(w, h, 0.006f);
        DestroyCollider(d);
        Paint(d, col);
    }

    static void BuildSteel(float x, float z)
    {
        var root = new GameObject("Steel");
        root.transform.position = new Vector3(x, 0f, z);
        root.transform.rotation = Quaternion.Euler(0, 180f, 0);
        root.AddComponent<SteelTarget>();

        var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Plate";
        plate.transform.SetParent(root.transform, false);
        plate.transform.localScale = new Vector3(0.32f, 0.55f, 0.05f);
        plate.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        Paint(plate, new Color(0.78f, 0.80f, 0.83f));

        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Post";
        post.transform.SetParent(root.transform, false);
        post.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
        post.transform.localPosition = new Vector3(0f, 0.33f, 0f);
        DestroyCollider(post);
        Paint(post, new Color(0.25f, 0.27f, 0.30f));
    }

    // ---------------- enemy (wild-west outlaw / ranchero) ----------------

    public static void BuildEnemy(float x, float z, float patrolHalfWidth, int maxHits, float hitChance, float moveSpeed)
    {
        var root = new GameObject("Outlaw");
        root.transform.position = new Vector3(x, 0f, z);
        root.transform.rotation = Quaternion.Euler(0, 180f, 0); // start facing the player (−z)
        var enemy = root.AddComponent<Enemy>();
        enemy.patrolHalfWidth = patrolHalfWidth;
        enemy.maxHits = maxHits;
        enemy.hitChance = hitChance;
        enemy.moveSpeed = moveSpeed;

        Color skin = new Color(0.74f, 0.56f, 0.43f);
        Color serape = new Color(0.72f, 0.27f, 0.20f);   // terracotta poncho
        Color trousers = new Color(0.28f, 0.24f, 0.20f);
        Color straw = new Color(0.82f, 0.69f, 0.42f);    // sombrero
        Color dark = new Color(0.11f, 0.09f, 0.08f);

        var tints = new List<Renderer>();

        // Torso (poncho) + head are the main hit zones (keep colliders).
        var torso = Prim(PrimitiveType.Cube, root.transform, "Torso",
            new Vector3(0, 1.02f, 0), new Vector3(0.52f, 0.72f, 0.34f), serape, true);
        tints.Add(torso.GetComponent<Renderer>());
        var head = Prim(PrimitiveType.Sphere, root.transform, "Head",
            new Vector3(0, 1.56f, 0), new Vector3(0.32f, 0.34f, 0.32f), skin, true);
        tints.Add(head.GetComponent<Renderer>());

        // Limbs (also shootable).
        Prim(PrimitiveType.Cube, root.transform, "LegL", new Vector3(-0.13f, 0.33f, 0), new Vector3(0.18f, 0.66f, 0.22f), trousers, true);
        Prim(PrimitiveType.Cube, root.transform, "LegR", new Vector3(0.13f, 0.33f, 0), new Vector3(0.18f, 0.66f, 0.22f), trousers, true);
        Prim(PrimitiveType.Cube, root.transform, "ArmL", new Vector3(-0.35f, 1.06f, 0), new Vector3(0.14f, 0.56f, 0.16f), serape, true);
        Prim(PrimitiveType.Cube, root.transform, "ArmR", new Vector3(0.31f, 1.0f, 0.16f), new Vector3(0.14f, 0.5f, 0.16f), serape, true);

        // Decorative bits (no colliders).
        Prim(PrimitiveType.Cube, root.transform, "Belt", new Vector3(0, 0.66f, 0), new Vector3(0.54f, 0.08f, 0.36f), dark, false);
        Prim(PrimitiveType.Cube, root.transform, "Stripe1", new Vector3(0, 1.16f, 0.18f), new Vector3(0.5f, 0.06f, 0.02f), new Color(0.92f, 0.85f, 0.55f), false);
        Prim(PrimitiveType.Cube, root.transform, "Stripe2", new Vector3(0, 1.04f, 0.18f), new Vector3(0.5f, 0.05f, 0.02f), new Color(0.85f, 0.72f, 0.30f), false);
        Prim(PrimitiveType.Cube, root.transform, "Stripe3", new Vector3(0, 0.92f, 0.18f), new Vector3(0.5f, 0.04f, 0.02f), dark, false);
        Prim(PrimitiveType.Cube, root.transform, "BootL", new Vector3(-0.13f, 0.05f, 0.03f), new Vector3(0.20f, 0.12f, 0.30f), dark, false);
        Prim(PrimitiveType.Cube, root.transform, "BootR", new Vector3(0.13f, 0.05f, 0.03f), new Vector3(0.20f, 0.12f, 0.30f), dark, false);
        Prim(PrimitiveType.Cube, root.transform, "Mustache", new Vector3(0, 1.50f, 0.165f), new Vector3(0.18f, 0.04f, 0.04f), dark, false);

        // Sombrero: wide flat brim + crown + band.
        Prim(PrimitiveType.Cylinder, root.transform, "HatBrim", new Vector3(0, 1.73f, 0), new Vector3(0.98f, 0.03f, 0.98f), straw, false);
        Prim(PrimitiveType.Cylinder, root.transform, "HatCrown", new Vector3(0, 1.80f, 0), new Vector3(0.40f, 0.13f, 0.40f), straw, false);
        Prim(PrimitiveType.Cube, root.transform, "HatBand", new Vector3(0, 1.75f, 0), new Vector3(0.43f, 0.04f, 0.43f), new Color(0.5f, 0.2f, 0.15f), false);

        // Revolver in the right hand, pointing forward (+local z → toward the player).
        Prim(PrimitiveType.Cube, root.transform, "Revolver", new Vector3(0.31f, 1.0f, 0.34f), new Vector3(0.06f, 0.10f, 0.26f), new Color(0.20f, 0.18f, 0.16f), false);

        var muzzleGo = new GameObject("Muzzle");
        muzzleGo.transform.SetParent(root.transform, false);
        muzzleGo.transform.localPosition = new Vector3(0.31f, 1.0f, 0.50f);

        var flashGo = new GameObject("EnemyFlash");
        flashGo.transform.SetParent(muzzleGo.transform, false);
        var flash = flashGo.AddComponent<Light>();
        flash.type = LightType.Point; flash.range = 6f; flash.intensity = 3f;
        flash.color = new Color(1f, 0.8f, 0.45f); flash.enabled = false;

        var tracerGo = new GameObject("EnemyTracer");
        tracerGo.transform.SetParent(root.transform, false);
        var tracer = tracerGo.AddComponent<LineRenderer>();
        tracer.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.55f, 0.3f) };
        tracer.startWidth = 0.035f; tracer.endWidth = 0.01f; tracer.numCapVertices = 2;
        tracer.useWorldSpace = true; tracer.enabled = false;

        enemy.muzzle = muzzleGo.transform;
        enemy.muzzleFlash = flash;
        enemy.tracer = tracer;
        enemy.tintRenderers = tints.ToArray();
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name,
                           Vector3 lpos, Vector3 lscale, Color col, bool keepCollider)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lpos;
        g.transform.localScale = lscale;
        if (!keepCollider) DestroyCollider(g);
        Paint(g, col);
        return g;
    }

    // ---------------- player ----------------

    static GameObject BuildPlayer(GameInput input)
    {
        var player = new GameObject("Player") { tag = "Player" };
        player.transform.position = new Vector3(0, 1.0f, -12f);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.4f; cc.center = Vector3.zero;

        var pivot = new GameObject("CameraPivot") { tag = "MainCamera" };
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0, 0.7f, 0);
        var cam = pivot.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.Skybox;
        pivot.AddComponent<AudioListener>();

        var flashGo = new GameObject("MuzzleFlash");
        flashGo.transform.SetParent(pivot.transform, false);
        flashGo.transform.localPosition = new Vector3(0.25f, -0.18f, 0.6f);
        var flash = flashGo.AddComponent<Light>();
        flash.type = LightType.Point; flash.range = 8f; flash.intensity = 3.5f;
        flash.color = new Color(1f, 0.85f, 0.5f); flash.enabled = false;

        var tracerGo = new GameObject("Tracer");
        tracerGo.transform.SetParent(pivot.transform, false);
        var tracer = tracerGo.AddComponent<LineRenderer>();
        var unlit = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.95f, 0.4f) };
        tracer.material = unlit;
        tracer.startWidth = 0.04f; tracer.endWidth = 0.015f; tracer.numCapVertices = 2;
        tracer.enabled = false;

        var gun = player.AddComponent<Gun>();
        gun.aimCamera = cam; gun.muzzleFlash = flash; gun.tracer = tracer;

        var pc = player.AddComponent<PlayerController>();
        pc.cameraPivot = pivot.transform; pc.input = input; pc.gun = gun;

        player.AddComponent<PlayerHealth>();

        return player;
    }

    // ---------------- HUD ----------------

    static void BuildHud(MatchManager mm)
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("HUD Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen damage vignette (red), behind the rest, alpha driven at runtime.
        var flash = MakeImage("DamageFlash", canvasGo.transform, new Color(0.7f, 0.05f, 0.05f, 0f));
        Stretch(flash.rectTransform);
        flash.raycastTarget = false;
        mm.damageFlash = flash;

        var cross = MakeImage("Crosshair", canvasGo.transform, new Color(1f, 1f, 1f, 0.85f));
        Anchor(cross.rectTransform, C(0.5f), C(0.5f), Vector2.zero, new Vector2(8, 8));

        mm.timeText = MakeText("TimeText", canvasGo.transform, "0.00", 64, TextAnchor.UpperCenter);
        Anchor(mm.timeText.rectTransform, C(0.5f, 1), C(0.5f, 1), new Vector2(0, -30), new Vector2(420, 84));

        mm.remainingText = MakeText("RemainingText", canvasGo.transform, "TARGETS 12", 34, TextAnchor.UpperLeft);
        Anchor(mm.remainingText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -46), new Vector2(420, 50));

        mm.ammoText = MakeText("AmmoText", canvasGo.transform, "10/10", 44, TextAnchor.UpperRight);
        Anchor(mm.ammoText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -46), new Vector2(360, 60));

        // Health bar (top-left, under TARGETS) — shown only when the stage has enemies.
        var barBg = MakeImage("HealthBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.5f));
        Anchor(barBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -104), new Vector2(360, 34));
        var fill = MakeImage("HealthFill", barBg.transform, new Color(0.3f, 0.8f, 0.35f, 0.9f));
        fill.rectTransform.anchorMin = new Vector2(0, 0);
        fill.rectTransform.anchorMax = new Vector2(1, 1);
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        fill.rectTransform.offsetMin = new Vector2(3, 3);
        fill.rectTransform.offsetMax = new Vector2(-3, -3);
        fill.raycastTarget = false;
        mm.healthFill = fill;
        mm.healthText = MakeText("HealthText", barBg.transform, "HP 100", 26, TextAnchor.MiddleCenter);
        Stretch(mm.healthText.rectTransform);

        mm.statusText = MakeText("StatusText", canvasGo.transform, "MAKE READY", 96, TextAnchor.MiddleCenter);
        mm.statusText.color = new Color(1f, 0.85f, 0.3f);
        Anchor(mm.statusText.rectTransform, C(0.5f), C(0.5f), new Vector2(0, 180), new Vector2(1400, 140));

        var fireImg = MakeImage("FireButton", canvasGo.transform, new Color(0.9f, 0.3f, 0.3f, 0.55f));
        Anchor(fireImg.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-200, 200), new Vector2(220, 220));
        fireImg.gameObject.AddComponent<TouchFireButton>();
        Stretch(MakeText("Label", fireImg.transform, "FIRE", 44, TextAnchor.MiddleCenter).rectTransform);

        var reloadImg = MakeImage("ReloadButton", canvasGo.transform, new Color(0.3f, 0.5f, 0.9f, 0.55f));
        Anchor(reloadImg.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-440, 170), new Vector2(170, 130));
        reloadImg.gameObject.AddComponent<TouchReloadButton>();
        Stretch(MakeText("Label", reloadImg.transform, "RELOAD", 34, TextAnchor.MiddleCenter).rectTransform);

        var hint = MakeText("Hint", canvasGo.transform,
            "Left: move   Right: look   FIRE: shoot   RELOAD / R", 26, TextAnchor.LowerCenter);
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        Anchor(hint.rectTransform, C(0.5f, 0), C(0.5f, 0), new Vector2(0, 26), new Vector2(1200, 40));

        // Results panel
        mm.resultsPanel = MakeImage("ResultsPanel", canvasGo.transform, new Color(0f, 0f, 0f, 0.86f)).gameObject;
        Stretch(mm.resultsPanel.GetComponent<RectTransform>());

        mm.resultsTitle = MakeText("Title", mm.resultsPanel.transform, "STAGE COMPLETE", 76, TextAnchor.MiddleCenter);
        mm.resultsTitle.color = new Color(0.4f, 0.9f, 0.5f);
        Anchor(mm.resultsTitle.rectTransform, C(0.5f), C(0.5f), new Vector2(0, 250), new Vector2(1400, 110));

        mm.resultsText = MakeText("ResultsText", mm.resultsPanel.transform, "", 44, TextAnchor.MiddleCenter);
        Anchor(mm.resultsText.rectTransform, C(0.5f), C(0.5f), new Vector2(0, -10), new Vector2(1300, 440));

        var btnImg = MakeImage("RestartButton", mm.resultsPanel.transform, new Color(0.25f, 0.55f, 0.95f, 1f));
        Anchor(btnImg.rectTransform, C(0.5f), C(0.5f), new Vector2(0, -300), new Vector2(420, 110));
        var restartButton = btnImg.gameObject.AddComponent<Button>();
        mm.resultsButtonText = MakeText("Label", btnImg.transform, "RUN AGAIN", 44, TextAnchor.MiddleCenter);
        Stretch(mm.resultsButtonText.rectTransform);

        UnityEventTools.AddPersistentListener(restartButton.onClick, mm.OnResultsButton);
        mm.resultsPanel.SetActive(false);
    }

    // ---------------- helpers ----------------

    static Vector2 C(float v) => new Vector2(v, v);
    static Vector2 C(float a, float b) => new Vector2(a, b);

    static Font UiFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static Text MakeText(string name, Transform parent, string content, int size, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.font = UiFont(); t.fontSize = size; t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Paint(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        r.sharedMaterial = new Material(shader) { color = color };
    }

    static void DestroyCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }
}
