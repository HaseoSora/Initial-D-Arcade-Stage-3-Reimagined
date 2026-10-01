using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public static class Idas3AndroidBuild
{
    private const string RuntimeFolder = "Assets/StreamingAssets/IDAS3-Android";
    private const string RuntimeZip = RuntimeFolder + "/runtime.zip";
    private const string RuntimeHash = RuntimeFolder + "/runtime.sha256";

    [MenuItem("Initial D/Android/Configure")]
    public static void Configure()
    {
        Idas3Build.Configure();
        PlayerSettings.applicationIdentifier = "com.haseosora.initiald3.reimagined";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.fullScreen = true;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        // Direct LAN/Steam transports and packaged/network media use sockets.
        // Force this into the generated Android manifest instead of relying on
        // Unity's feature scan to infer it from managed code.
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = 2;

        var plugin = AssetImporter.GetAtPath("Assets/Plugins/Android/arm64-v8a/libIdas3Unity.so") as PluginImporter;
        if (plugin != null)
        {
            plugin.SetCompatibleWithAnyPlatform(false);
            plugin.SetCompatibleWithEditor(false);
            plugin.SetCompatibleWithPlatform(BuildTarget.Android, true);
            plugin.SetPlatformData(BuildTarget.Android, "CPU", "ARM64");
            plugin.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Initial D: Android ARM64 / Vulkan configured.");
    }

    [MenuItem("Initial D/Android/Prepare Runtime Package")]
    public static void PrepareRuntimePackage()
    {
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string staging = Path.Combine(project, "Library", "IDAS3AndroidRuntime");
        string manifest = Path.Combine(project, "Library", "IDAS3AndroidRuntime-manifest.json");
        if (Directory.Exists(staging)) Directory.Delete(staging, true);
        Directory.CreateDirectory(staging);

        string script = Path.Combine(project, "Tools", "Stage-GameData.ps1");
        var process = new Process();
        process.StartInfo = new ProcessStartInfo("powershell.exe")
        {
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -DestinationRoot \"" + staging +
                        "\" -ManifestPath \"" + manifest + "\"",
            WorkingDirectory = project,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("Android runtime staging failed.\n" + stderr + "\n" + stdout);

        Directory.CreateDirectory(Path.Combine(project, RuntimeFolder));
        string zip = Path.Combine(project, RuntimeZip);
        string temporary = zip + ".tmp";
        if (File.Exists(temporary)) File.Delete(temporary);
        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(staging, temporary, CompressionLevel.Optimal, false);
        File.Move(temporary, zip);

        string digest;
        using (var sha = SHA256.Create())
        using (var input = File.OpenRead(zip))
            digest = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        File.WriteAllText(Path.Combine(project, RuntimeHash), digest + "\n", new System.Text.UTF8Encoding(false));
        Directory.Delete(staging, true);
        AssetDatabase.Refresh();
        Debug.Log("Android runtime package ready: " + new FileInfo(zip).Length + " bytes, SHA256 " + digest);
    }

    [MenuItem("Initial D/Android/Build APK")]
    public static void BuildApk()
    {
        Configure();
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string native = Path.Combine(project, "Assets", "Plugins", "Android", "arm64-v8a", "libIdas3Unity.so");
        if (!File.Exists(native)) throw new FileNotFoundException("Build the Android ARM64 native plugin first.", native);
        if (!File.Exists(Path.Combine(project, RuntimeZip)) || !File.Exists(Path.Combine(project, RuntimeHash)))
            PrepareRuntimePackage();

        Directory.CreateDirectory(Path.Combine(project, "Builds", "Android"));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/InitialDUnityScene.unity" },
            locationPathName = "Builds/Android/InitialDArcadeStage3-Reimagined.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Android APK build failed: " + report.summary.result);
        Debug.Log("Android APK: " + report.summary.outputPath);
    }
}
