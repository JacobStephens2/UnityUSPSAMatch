using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Generates the app icon procedurally (a shooting-target emblem) so the
/// project needs no image assets, then assigns it to the Android player icons.
/// </summary>
public static class IconGenerator
{
    const string IconPath = "Assets/AppIcon/AppIcon.png";

    [MenuItem("Tools/Shooter/Generate App Icon")]
    public static void Regenerate()
    {
        WritePng();
        Assign();
    }

    /// <summary>Create the PNG if missing, then assign to Android icons.</summary>
    public static void EnsureAndAssign()
    {
        if (!File.Exists(IconPath)) WritePng();
        Assign();
    }

    static void WritePng()
    {
        const int S = 512;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);

        Color bg   = new Color(0.11f, 0.12f, 0.15f);
        Color tan  = new Color(0.82f, 0.69f, 0.47f);
        Color dark = new Color(0.20f, 0.18f, 0.15f);
        Color red  = new Color(0.80f, 0.20f, 0.20f);
        Color white = new Color(0.95f, 0.95f, 0.95f);

        float cx = S / 2f, cy = S / 2f;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = x - cx, dy = y - cy;
            float r = Mathf.Sqrt(dx * dx + dy * dy);

            // Concentric scoring target (matches the in-game cardboard palette).
            Color c = bg;
            if (r < 210f) c = tan;
            if (r < 165f) c = dark;
            if (r < 150f) c = tan;
            if (r < 95f)  c = dark;
            if (r < 78f)  c = red;

            // Thin white crosshair with a centre gap.
            float thick = 9f, gap = 30f, reach = 238f;
            bool onH = Mathf.Abs(dy) < thick && Mathf.Abs(dx) > gap && Mathf.Abs(dx) < reach;
            bool onV = Mathf.Abs(dx) < thick && Mathf.Abs(dy) > gap && Mathf.Abs(dy) < reach;
            if (onH || onV) c = white;

            px[y * S + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
        File.WriteAllBytes(IconPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);

        var imp = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 512;
            imp.SaveAndReimport();
        }
        Debug.Log("[IconGenerator] wrote " + IconPath);
    }

    static void Assign()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (tex == null) { Debug.LogError("[IconGenerator] icon texture not found"); return; }

        var android = NamedBuildTarget.Android;
        SetKind(android, tex, IconKind.Application);
        Debug.Log("[IconGenerator] assigned Android icons");
    }

    static void SetKind(NamedBuildTarget target, Texture2D tex, IconKind kind)
    {
        int[] sizes = PlayerSettings.GetIconSizes(target, kind);
        if (sizes == null || sizes.Length == 0) return;
        var icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = tex;
        PlayerSettings.SetIcons(target, icons, kind);
    }
}
