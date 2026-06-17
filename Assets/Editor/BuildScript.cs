using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Configures Android player settings and packages an installable APK.
/// Headless: -buildTarget android -executeMethod BuildScript.BuildApk
/// </summary>
public static class BuildScript
{
    const string ApkPath = "Builds/AndroidShooter.apk";
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Shooter/Build Android APK")]
    public static void BuildApk()
    {
        // Make sure the scene exists (build it from code if needed).
        if (!File.Exists(ScenePath))
            SceneBuilder.Build();

        ConfigureAndroid();

        Directory.CreateDirectory("Builds");
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] APK built OK: {summary.outputPath} " +
                      $"({summary.totalSize / (1024 * 1024f):0.0} MB)");
        }
        else
        {
            Debug.LogError($"[BuildScript] Build FAILED: {summary.result}, errors={summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }

    static void ConfigureAndroid()
    {
        PlayerSettings.companyName = "Vagabond";
        PlayerSettings.productName = "Unity USPSA Match";   // Android home-screen label

        var android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, "com.vagabond.androidshooter");

        // Procedural app icon (target emblem).
        IconGenerator.EnsureAndAssign();
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // Landscape FPS.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // Plain debug-signed APK (sideloadable), not an app bundle.
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.useCustomKeystore = false;
    }
}
