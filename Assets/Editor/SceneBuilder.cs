using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Shooter;

/// <summary>
/// Builds the entire playable scene (arena, player rig, HUD, spawner) from code
/// so the project carries no binary scene/prefab dependencies. Run via the
/// Tools menu or headless: -executeMethod SceneBuilder.Build
/// </summary>
public static class SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Shooter/Build Scene")]
    public static void Build()
    {
        EnsureTag("Enemy");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildEnvironment();
        var input = new GameObject("GameInput").AddComponent<GameInput>();
        var player = BuildPlayer(input);
        BuildLighting();

        var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
        spawner.player = player.transform;

        var hud = BuildHud(out Text healthText, out Text scoreText,
                           out GameObject gameOverPanel, out Text finalScoreText,
                           out Button restartButton);

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.player = player.GetComponent<PlayerController>();
        gm.playerHealth = player.GetComponent<Health>();
        gm.healthText = healthText;
        gm.scoreText = scoreText;
        gm.gameOverPanel = gameOverPanel;
        gm.finalScoreText = finalScoreText;

        // Persistent click handler so it survives scene serialization.
        UnityEventTools.AddPersistentListener(restartButton.onClick, gm.Restart);
        gameOverPanel.SetActive(false);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        Debug.Log("[SceneBuilder] Scene built and saved to " + ScenePath);
    }

    static void BuildEnvironment()
    {
        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60 x 60 units
        Paint(ground, new Color(0.20f, 0.23f, 0.27f));

        // Perimeter walls keep the action contained.
        float h = 3f, half = 30f, th = 1f;
        MakeWall("Wall_N", new Vector3(0, h / 2, half), new Vector3(half * 2, h, th));
        MakeWall("Wall_S", new Vector3(0, h / 2, -half), new Vector3(half * 2, h, th));
        MakeWall("Wall_E", new Vector3(half, h / 2, 0), new Vector3(th, h, half * 2));
        MakeWall("Wall_W", new Vector3(-half, h / 2, 0), new Vector3(th, h, half * 2));

        // A few cover blocks for visual interest.
        var rng = new System.Random(12345);
        for (int i = 0; i < 8; i++)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Cover_" + i;
            float s = 1.5f + (float)rng.NextDouble() * 2f;
            box.transform.localScale = new Vector3(s, s, s);
            float x = (float)(rng.NextDouble() * 40 - 20);
            float z = (float)(rng.NextDouble() * 40 - 20);
            box.transform.position = new Vector3(x, s / 2f, z);
            Paint(box, new Color(0.35f, 0.38f, 0.42f));
        }
    }

    static void MakeWall(string name, Vector3 pos, Vector3 scale)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position = pos;
        w.transform.localScale = scale;
        Paint(w, new Color(0.15f, 0.16f, 0.19f));
    }

    static GameObject BuildPlayer(GameInput input)
    {
        var player = new GameObject("Player") { tag = "Player" };
        player.transform.position = new Vector3(0, 1.0f, 0);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.4f; cc.center = Vector3.zero;

        var health = player.AddComponent<Health>();
        health.maxHealth = 100f;

        // Camera pivot (pitched) holds the camera + audio listener.
        var pivot = new GameObject("CameraPivot") { tag = "MainCamera" };
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0, 0.7f, 0);
        var cam = pivot.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.Skybox;
        pivot.AddComponent<AudioListener>();

        // Muzzle flash light (child of camera).
        var flashGo = new GameObject("MuzzleFlash");
        flashGo.transform.SetParent(pivot.transform, false);
        flashGo.transform.localPosition = new Vector3(0.25f, -0.18f, 0.6f);
        var flash = flashGo.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.range = 8f; flash.intensity = 3.5f;
        flash.color = new Color(1f, 0.85f, 0.5f);
        flash.enabled = false;

        // Tracer line.
        var tracerGo = new GameObject("Tracer");
        tracerGo.transform.SetParent(pivot.transform, false);
        var tracer = tracerGo.AddComponent<LineRenderer>();
        var unlit = new Material(Shader.Find("Unlit/Color"));
        unlit.color = new Color(1f, 0.95f, 0.4f);
        tracer.material = unlit;
        tracer.startWidth = 0.05f; tracer.endWidth = 0.02f;
        tracer.numCapVertices = 2;
        tracer.enabled = false;

        var gun = player.AddComponent<Gun>();
        gun.aimCamera = cam;
        gun.muzzleFlash = flash;
        gun.tracer = tracer;

        var pc = player.AddComponent<PlayerController>();
        pc.cameraPivot = pivot.transform;
        pc.input = input;
        pc.gun = gun;

        return player;
    }

    static void BuildLighting()
    {
        var sunGo = new GameObject("Directional Light");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.color = new Color(1f, 0.96f, 0.9f);
        sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
    }

    // ---------------- HUD ----------------

    static GameObject BuildHud(out Text healthText, out Text scoreText,
                               out GameObject gameOverPanel, out Text finalScoreText,
                               out Button restartButton)
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

        // Crosshair
        var cross = MakeImage("Crosshair", canvasGo.transform, new Color(1f, 1f, 1f, 0.85f));
        Anchor(cross.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));

        healthText = MakeText("HealthText", canvasGo.transform, "HP 100", 40, TextAnchor.UpperLeft);
        Anchor(healthText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -50), new Vector2(360, 60));

        scoreText = MakeText("ScoreText", canvasGo.transform, "SCORE 0", 40, TextAnchor.UpperRight);
        Anchor(scoreText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -50), new Vector2(360, 60));

        // Fire button (bottom-right, large for thumbs).
        var fireImg = MakeImage("FireButton", canvasGo.transform, new Color(0.9f, 0.3f, 0.3f, 0.55f));
        Anchor(fireImg.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-200, 200), new Vector2(220, 220));
        fireImg.gameObject.AddComponent<TouchFireButton>();
        var fireLabel = MakeText("Label", fireImg.transform, "FIRE", 44, TextAnchor.MiddleCenter);
        Stretch(fireLabel.rectTransform);

        // Hint text for movement/look zones.
        var hint = MakeText("Hint", canvasGo.transform,
            "Left side: move    Right side: look    FIRE: shoot", 26, TextAnchor.LowerCenter);
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1100, 40));

        // Game Over panel
        gameOverPanel = MakeImage("GameOverPanel", canvasGo.transform, new Color(0f, 0f, 0f, 0.85f)).gameObject;
        Stretch(gameOverPanel.GetComponent<RectTransform>());

        var goTitle = MakeText("Title", gameOverPanel.transform, "GAME OVER", 90, TextAnchor.MiddleCenter);
        Anchor(goTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(900, 140));
        goTitle.color = new Color(1f, 0.4f, 0.4f);

        finalScoreText = MakeText("FinalScore", gameOverPanel.transform, "Score 0   Kills 0", 48, TextAnchor.MiddleCenter);
        Anchor(finalScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(900, 80));

        var btnImg = MakeImage("RestartButton", gameOverPanel.transform, new Color(0.25f, 0.55f, 0.95f, 1f));
        Anchor(btnImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(360, 110));
        restartButton = btnImg.gameObject.AddComponent<Button>();
        var btnLabel = MakeText("Label", btnImg.transform, "RESTART", 44, TextAnchor.MiddleCenter);
        Stretch(btnLabel.rectTransform);

        return canvasGo;
    }

    static Font UiFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Text MakeText(string name, Transform parent, string content, int size, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = UiFont();
        t.fontSize = size;
        t.alignment = align;
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
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ---------------- helpers ----------------

    static void Paint(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader) { color = color };
        r.sharedMaterial = mat;
    }

    static void EnsureTag(string tag)
    {
        var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    static void AddSceneToBuildSettings(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
