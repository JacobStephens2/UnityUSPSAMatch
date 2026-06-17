using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-only render captures for visual verification (no emulator needed).
/// Run graphics-enabled: -executeMethod EnemyPreview.Capture (omit -nographics).
/// </summary>
public static class EnemyPreview
{
    public static void Capture()
    {
        CaptureRanchero("/tmp/ranchero.png");
        CaptureStage2View("/tmp/stage2.png");
        Debug.Log("[EnemyPreview] captures written");
    }

    public static void CheckMusic()
    {
        var clip = Shooter.ProcAudio.Music;
        var data = new float[clip.samples];
        clip.GetData(data, 0);
        float peak = 0f; double sq = 0;
        foreach (var x in data) { peak = Mathf.Max(peak, Mathf.Abs(x)); sq += (double)x * x; }
        double rms = System.Math.Sqrt(sq / Mathf.Max(1, data.Length));
        Debug.LogWarning($"MUSICCHECK len={clip.length:0.0}s samples={clip.samples} peak={peak:0.00} rms={rms:0.000}");
    }

    static void CaptureRanchero(string outPath)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.25f;
        sun.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = Vector3.one * 2f;
        ground.GetComponent<Renderer>().sharedMaterial =
            new Material(Shader.Find("Standard")) { color = new Color(0.45f, 0.43f, 0.38f) };

        SceneBuilder.BuildEnemy(0f, 0f, 2f, 5, 0.5f, 2.5f);

        var camGo = new GameObject("PreviewCam");
        var cam = camGo.AddComponent<Camera>();
        cam.transform.position = new Vector3(1.5f, 1.45f, -3.0f);
        cam.transform.LookAt(new Vector3(0f, 1.05f, 0f));
        cam.fieldOfView = 42f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.63f, 0.72f);

        Render(cam, 820, 1040, outPath);
    }

    static void CaptureStage2View(string outPath)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Stage2.unity", OpenSceneMode.Single);
        Camera cam = null;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            cam = c; // the player camera
        if (cam == null) return;
        // Aim slightly downrange so the outlaws are in frame.
        Render(cam, 1280, 720, outPath);
    }

    static void Render(Camera cam, int w, int h, string outPath)
    {
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(rt);
    }
}
