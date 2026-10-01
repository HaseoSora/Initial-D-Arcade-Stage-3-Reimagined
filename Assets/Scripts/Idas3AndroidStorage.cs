using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Android native code cannot read files directly out of StreamingAssets because
// they live inside the APK. Install one verified runtime-data archive into
// app-private persistent storage, then give C++ an ordinary filesystem path.
internal static class Idas3AndroidStorage
{
    public static string AssetRoot => Path.Combine(Application.persistentDataPath, "runtime", "IDAS3");
    public static bool Ready { get; private set; }
    public static bool Preparing { get; private set; }
    public static string Error { get; private set; }
    public static string Status { get; private set; } = "Android runtime data has not been prepared.";
    public static float Progress { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string PackageUrl => Application.streamingAssetsPath.TrimEnd('/') + "/IDAS3-Android/runtime.zip";
    private static string HashUrl => Application.streamingAssetsPath.TrimEnd('/') + "/IDAS3-Android/runtime.sha256";
    private static string MarkerPath => Path.Combine(AssetRoot, ".package.sha256");

    public static IEnumerator Prepare()
    {
        if (Ready || Preparing) yield break;
        Preparing = true; Error = null; Progress = 0;
        string expectedHash = null;
        string temporaryZip = Path.Combine(Application.temporaryCachePath, "idas3-runtime-" + Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            Status = "Reading Android runtime package…";
            using (var request = UnityWebRequest.Get(HashUrl))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException("Could not read the packaged runtime hash: " + request.error);
                expectedHash = (request.downloadHandler.text ?? "").Trim().ToLowerInvariant();
            }
            if (expectedHash.Length != 64)
                throw new InvalidDataException("Android runtime package hash is invalid.");

            if (File.Exists(MarkerPath) &&
                string.Equals(File.ReadAllText(MarkerPath).Trim(), expectedHash, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(Path.Combine(AssetRoot, "data.manifest.json")) &&
                Directory.Exists(Path.Combine(AssetRoot, "data", "original_physics")))
            {
                Ready = true; Progress = 1; Status = "Android runtime data ready.";
                yield break;
            }

            Directory.CreateDirectory(Application.temporaryCachePath);
            try { if (File.Exists(temporaryZip)) File.Delete(temporaryZip); } catch { }
            Status = "Copying packaged game data…";
            using (var request = new UnityWebRequest(PackageUrl, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerFile(temporaryZip);
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    Progress = Mathf.Clamp01(request.downloadProgress * .65f);
                    yield return null;
                }
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException("Could not copy the Android runtime package: " + request.error);
            }

            Status = "Verifying packaged game data…";
            var hashTask = Task.Run(() => HashFile(temporaryZip));
            while (!hashTask.IsCompleted) { Progress = .67f; yield return null; }
            if (hashTask.IsFaulted) throw hashTask.Exception?.GetBaseException() ?? new IOException("Runtime hash failed.");
            if (!string.Equals(hashTask.Result, expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Android runtime package failed SHA-256 verification.");

            Status = "Installing game data…";
            var installTask = Task.Run(() => Install(temporaryZip, expectedHash));
            while (!installTask.IsCompleted) { Progress = Mathf.Min(.97f, Progress + .0005f); yield return null; }
            if (installTask.IsFaulted) throw installTask.Exception?.GetBaseException() ?? new IOException("Runtime installation failed.");

            Ready = true; Progress = 1; Status = "Android runtime data ready.";
        }
        catch (Exception error)
        {
            Error = error.Message; Status = "Android game-data setup failed: " + error.Message;
            Debug.LogError(Status);
        }
        finally
        {
            Preparing = false;
            try { if (File.Exists(temporaryZip)) File.Delete(temporaryZip); } catch { }
        }
    }

    private static string HashFile(string path)
    {
        using (var sha = SHA256.Create())
        using (var input = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }

    private static void Install(string zipPath, string expectedHash)
    {
        string finalRoot = AssetRoot;
        string parent = Path.GetDirectoryName(finalRoot);
        Directory.CreateDirectory(parent);
        string staging = finalRoot + ".install-" + Guid.NewGuid().ToString("N");
        string backup = finalRoot + ".previous";
        try
        {
            Directory.CreateDirectory(staging);
            string stagingFull = Path.GetFullPath(staging).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            using (var file = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read, false))
            {
                foreach (var entry in archive.Entries)
                {
                    string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    if (relative.Length == 0) continue;
                    string destination = Path.GetFullPath(Path.Combine(staging, relative));
                    if (!destination.StartsWith(stagingFull, StringComparison.Ordinal))
                        throw new InvalidDataException("Runtime archive contains an unsafe path.");
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var input = entry.Open())
                    using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }
            }
            if (!File.Exists(Path.Combine(staging, "data.manifest.json")) ||
                !Directory.Exists(Path.Combine(staging, "data", "original_physics")))
                throw new InvalidDataException("Android runtime package is incomplete.");
            File.WriteAllText(Path.Combine(staging, ".package.sha256"), expectedHash + "\n");

            if (Directory.Exists(backup)) Directory.Delete(backup, true);
            if (Directory.Exists(finalRoot)) Directory.Move(finalRoot, backup);
            try { Directory.Move(staging, finalRoot); }
            catch
            {
                if (!Directory.Exists(finalRoot) && Directory.Exists(backup)) Directory.Move(backup, finalRoot);
                throw;
            }
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
        }
        finally
        {
            if (Directory.Exists(staging)) try { Directory.Delete(staging, true); } catch { }
        }
    }
#else
    public static IEnumerator Prepare()
    {
        Ready = true; Preparing = false; Error = null; Progress = 1;
        Status = "Desktop runtime data uses the normal deployment path.";
        yield break;
    }
#endif
}
