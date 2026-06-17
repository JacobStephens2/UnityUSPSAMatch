using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Shooter;

/// <summary>
/// Builds the entire USPSA-style stage (bay, targets, player rig, HUD) from
/// code — no binary scene/prefab/art dependencies. Run via Tools ▸ Shooter ▸
/// Build Scene, or headless: -executeMethod SceneBuilder.Build
/// </summary>
public static class SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    // Paper targets: x, z, facing-yaw, isNoShoot. y is fixed (board centre 1.15).
    static readonly float[][] Papers =
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

    // Steel poppers: x, z.
    static readonly float[][] Steels =
    {
        new[]{-7f, 2f},
        new[]{ 7f, 2f},
        new[]{-3f, 8f},
        new[]{ 3f, 8f},
    };

    [MenuItem("Tools/Shooter/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildBay();
        BuildLighting();
        var input = new GameObject("GameInput").AddComponent<GameInput>();
        var player = BuildPlayer(input);

        foreach (var p in Papers) BuildPaperTarget(p[0], p[1], p[2], p[3] > 0.5f);
        foreach (var s in Steels) BuildSteel(s[0], s[1]);

        var hud = BuildHud(out Text timeText, out Text ammoText, out Text statusText,
                           out Text remainingText, out GameObject resultsPanel,
                           out Text resultsText, out Button restartButton);

        var mm = new GameObject("MatchManager").AddComponent<MatchManager>();
        mm.player = player.GetComponent<PlayerController>();
        mm.gun = player.GetComponent<Gun>();
        mm.timeText = timeText;
        mm.ammoText = ammoText;
        mm.statusText = statusText;
        mm.remainingText = remainingText;
        mm.resultsPanel = resultsPanel;
        mm.resultsText = resultsText;

        UnityEventTools.AddPersistentListener(restartButton.onClick, mm.Restart);
        resultsPanel.SetActive(false);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        Debug.Log("[SceneBuilder] USPSA stage built and saved to " + ScenePath);
    }

    // ---------------- environment ----------------

    static void BuildBay()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5f, 1f, 4f); // 50 x 40
        ground.transform.position = new Vector3(0, 0, 2f);
        Paint(ground, new Color(0.42f, 0.40f, 0.36f)); // gravel/concrete

        // Berms around the bay.
        float h = 3.5f;
        MakeWall("Berm_N", new Vector3(0, h / 2, 16f), new Vector3(44, h, 1f), 0.30f);
        MakeWall("Berm_S", new Vector3(0, h / 2, -14f), new Vector3(44, h, 1f), 0.30f);
        MakeWall("Berm_E", new Vector3(22, h / 2, 1f), new Vector3(1f, h, 32f), 0.30f);
        MakeWall("Berm_W", new Vector3(-22, h / 2, 1f), new Vector3(1f, h, 32f), 0.30f);

        // A couple of waist-high vision barriers (props; they don't fully block any target).
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
        Paint(plate, new Color(0.78f, 0.80f, 0.83f)); // steel

        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Post";
        post.transform.SetParent(root.transform, false);
        post.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
        post.transform.localPosition = new Vector3(0f, 0.33f, 0f);
        DestroyCollider(post);
        Paint(post, new Color(0.25f, 0.27f, 0.30f));
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

        return player;
    }

    // ---------------- HUD ----------------

    static GameObject BuildHud(out Text timeText, out Text ammoText, out Text statusText,
                               out Text remainingText, out GameObject resultsPanel,
                               out Text resultsText, out Button restartButton)
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

        var cross = MakeImage("Crosshair", canvasGo.transform, new Color(1f, 1f, 1f, 0.85f));
        Anchor(cross.rectTransform, C(0.5f), C(0.5f), Vector2.zero, new Vector2(8, 8));

        timeText = MakeText("TimeText", canvasGo.transform, "0.00", 64, TextAnchor.UpperCenter);
        Anchor(timeText.rectTransform, C(0.5f, 1), C(0.5f, 1), new Vector2(0, -30), new Vector2(420, 84));

        remainingText = MakeText("RemainingText", canvasGo.transform, "TARGETS 12", 34, TextAnchor.UpperLeft);
        Anchor(remainingText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -46), new Vector2(420, 50));

        ammoText = MakeText("AmmoText", canvasGo.transform, "10/10", 44, TextAnchor.UpperRight);
        Anchor(ammoText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -46), new Vector2(360, 60));

        statusText = MakeText("StatusText", canvasGo.transform, "MAKE READY", 96, TextAnchor.MiddleCenter);
        statusText.color = new Color(1f, 0.85f, 0.3f);
        Anchor(statusText.rectTransform, C(0.5f), C(0.5f), new Vector2(0, 180), new Vector2(1200, 140));

        // Fire (bottom-right) + Reload (left of it).
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
        resultsPanel = MakeImage("ResultsPanel", canvasGo.transform, new Color(0f, 0f, 0f, 0.86f)).gameObject;
        Stretch(resultsPanel.GetComponent<RectTransform>());

        var title = MakeText("Title", resultsPanel.transform, "STAGE COMPLETE", 76, TextAnchor.MiddleCenter);
        title.color = new Color(0.4f, 0.9f, 0.5f);
        Anchor(title.rectTransform, C(0.5f), C(0.5f), new Vector2(0, 250), new Vector2(1200, 110));

        resultsText = MakeText("ResultsText", resultsPanel.transform, "", 44, TextAnchor.MiddleCenter);
        Anchor(resultsText.rectTransform, C(0.5f), C(0.5f), new Vector2(0, -10), new Vector2(1200, 420));

        var btnImg = MakeImage("RestartButton", resultsPanel.transform, new Color(0.25f, 0.55f, 0.95f, 1f));
        Anchor(btnImg.rectTransform, C(0.5f), C(0.5f), new Vector2(0, -300), new Vector2(360, 110));
        restartButton = btnImg.gameObject.AddComponent<Button>();
        Stretch(MakeText("Label", btnImg.transform, "RUN AGAIN", 44, TextAnchor.MiddleCenter).rectTransform);

        return canvasGo;
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

    static void AddSceneToBuildSettings(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
