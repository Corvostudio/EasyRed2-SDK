#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

public class ModMenuUpdater : EditorWindow
{
    public static int VERSION => GetLocalVersion();

    private const string GITHUB_USER = "Corvostudio";
    private const string GITHUB_REPO = "EasyRed2-SDK";
    private const string BRANCH = "main";
    private const string TARGET_FOLDER = "Assets";

    private const string versionJsonURL =
        "https://raw.githubusercontent.com/" + GITHUB_USER + "/" + GITHUB_REPO + "/" + BRANCH + "/version.json";

    private const string zipURL =
        "https://github.com/" + GITHUB_USER + "/" + GITHUB_REPO + "/archive/refs/heads/" + BRANCH + ".zip";

    private static readonly string[] SYNC_FOLDERS =
    {
        "ER2 SDK",
        "Steam SDK"
    };

    private static readonly HashSet<string> SKIP_ROOT_FILES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".gitignore",
        ".gitattributes",
        "README.md",
        "README.md.meta",
        "LICENSE",
        "LICENSE.md"
    };

    private static readonly HashSet<string> SKIP_DIRS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".github"
    };

    private static int newVersion;
    private static string manualUrl;
    private static string error_str;

    private enum CheckStatus
    {
        Checking,
        Downloading,
        Installing,
        UpToDate,
        Downloaded,
        Error,
        NewUpdateFound
    }

    private static CheckStatus checkStatus = CheckStatus.Checking;
    private static UnityWebRequest downloadingRequest;

    private static string progressInfo = "";
    private static float progressValue = 0f;
    private static bool isDownloading;

    private static int copiedFiles;
    private static int skippedFiles;
    private static int deletedFiles;

    private static int GetLocalVersion()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "version.json");
            if (!File.Exists(path)) return 0;

            var data = JsonUtility.FromJson<JSONData>(File.ReadAllText(path));
            return data != null ? data.version : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static void CheckForUpdatesSilent()
    {
        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                    return;

                var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
                if (data == null)
                    return;

                newVersion = data.version;
                manualUrl = data.manual_url;

                if (newVersion > VERSION && VERSION != 0)
                {
                    checkStatus = CheckStatus.NewUpdateFound;
                    GetWindow<ModMenuUpdater>("ModMenuUpdater");
                }
            }
            finally
            {
                www.Dispose();
            }
        };
    }

    [MenuItem("ER2 TOOLS/Update/Check for new Updates")]
    public static void AutoDownload()
    {
        GetWindow<ModMenuUpdater>("ModMenuUpdater");
        checkStatus = CheckStatus.Checking;
        error_str = "";
        GetVersionAsync(false);
    }

    public static void ForceUpdate()
    {
        GetWindow<ModMenuUpdater>("ModMenuUpdater");
        checkStatus = CheckStatus.Checking;
        error_str = "";
        GetVersionAsync(true);
    }

    [MenuItem("ER2 TOOLS/Update/Open SDK Download page")]
    public static void OpenSdkUrl()
    {
        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error fetching version.json: " + www.error);
                    return;
                }

                var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
                if (data != null && !string.IsNullOrEmpty(data.manual_url))
                    Application.OpenURL(data.manual_url);
            }
            finally
            {
                www.Dispose();
            }
        };
    }

    private void OnGUI()
    {
        GUILayout.Space(8);

        switch (checkStatus)
        {
            case CheckStatus.Checking:
                GUILayout.Label("Checking for updates...", EditorStyles.boldLabel);
                break;

            case CheckStatus.Downloading:
                DrawProgress("Downloading SDK update...");
                break;

            case CheckStatus.Installing:
                DrawProgress("Installing SDK update...");
                break;

            case CheckStatus.UpToDate:
                GUILayout.Label("Already up to date.", EditorStyles.boldLabel);
                GUILayout.Label("Current version: " + VersionToString(VERSION));
                break;

            case CheckStatus.Downloaded:
                GUILayout.Label("SDK updated successfully.", EditorStyles.boldLabel);
                GUILayout.Label("New version: " + VersionToString(newVersion));
                GUILayout.Space(8);
                GUILayout.Label("Copied/updated files: " + copiedFiles);
                GUILayout.Label("Skipped unchanged files: " + skippedFiles);
                GUILayout.Label("Deleted obsolete files: " + deletedFiles);
                break;

            case CheckStatus.Error:
                GUILayout.Label("SDK update error:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(error_str, MessageType.Error);
                GUILayout.Space(8);
                if (GUILayout.Button("Open SDK Download page"))
                    OpenSdkUrl();
                break;

            case CheckStatus.NewUpdateFound:
                GUILayout.Label("New update found for ER2 Modding SDK.", EditorStyles.boldLabel);
                GUILayout.Space(8);
                GUILayout.Label("New version: " + VersionToString(newVersion));
                GUILayout.Label("Current version: " + VersionToString(VERSION));
                GUILayout.Space(12);

                if (GUILayout.Button("Update now"))
                    ForceUpdate();

                if (GUILayout.Button("Open SDK Download page"))
                    OpenSdkUrl();
                break;
        }
    }

    private static void DrawProgress(string title)
    {
        int pct = Mathf.RoundToInt(Mathf.Clamp01(progressValue) * 100f);

        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Label(pct + "% - " + progressInfo);
        EditorGUILayout.Space();

        Rect rect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.ProgressBar(rect, Mathf.Clamp01(progressValue), pct + "%");

        var window = GetWindow<ModMenuUpdater>(false);
        if (window != null)
            window.Repaint();
    }

    private static string VersionToString(int version)
    {
        return (version / 100f).ToString("F2");
    }

    private static void GetVersionAsync(bool forceInstall)
    {
        progressValue = 0f;
        progressInfo = "Fetching version.json...";

        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    error_str = "Error fetching version.json: " + www.error;
                    checkStatus = CheckStatus.Error;
                    return;
                }

                var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
                if (data == null)
                {
                    error_str = "Invalid version.json.";
                    checkStatus = CheckStatus.Error;
                    return;
                }

                newVersion = data.version;
                manualUrl = data.manual_url;

                if (forceInstall || newVersion > VERSION)
                {
                    checkStatus = CheckStatus.Downloading;
                    DownloadAndInstall();
                }
                else
                {
                    checkStatus = CheckStatus.UpToDate;
                }
            }
            finally
            {
                www.Dispose();
            }
        };
    }

    private static void DownloadAndInstall()
    {
        copiedFiles = 0;
        skippedFiles = 0;
        deletedFiles = 0;

        downloadingRequest = UnityWebRequest.Get(zipURL);
        isDownloading = true;
        progressValue = 0f;
        progressInfo = "Downloading GitHub repository zip...";

        EditorApplication.update -= UpdateDownloadProgress;
        EditorApplication.update += UpdateDownloadProgress;

        downloadingRequest.SendWebRequest().completed += _ =>
        {
            EditorApplication.update -= UpdateDownloadProgress;
            isDownloading = false;

            if (downloadingRequest.result != UnityWebRequest.Result.Success)
            {
                error_str = "Failed to download zip: " + downloadingRequest.error;
                checkStatus = CheckStatus.Error;
                downloadingRequest.Dispose();
                EditorUtility.ClearProgressBar();
                return;
            }

            InstallDownloadedZip();
        };
    }

    private static void UpdateDownloadProgress()
    {
        if (!isDownloading || downloadingRequest == null)
            return;

        float download = Mathf.Clamp01(downloadingRequest.downloadProgress);
        progressValue = download * 0.35f;
        progressInfo = "Downloading GitHub repository zip... " + Mathf.RoundToInt(download * 100f) + "%";

        EditorUtility.DisplayProgressBar("ER2 SDK Update", progressInfo, progressValue);

        var window = GetWindow<ModMenuUpdater>(false);
        if (window != null)
            window.Repaint();
    }

    private static void InstallDownloadedZip()
    {
        string tempDir = Path.Combine(Application.temporaryCachePath, "er2_sdk_update");

        try
        {
            checkStatus = CheckStatus.Installing;

            SetProgress(0.38f, "Preparing temporary update folder...");

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, "sdk.zip");
            File.WriteAllBytes(zipPath, downloadingRequest.downloadHandler.data);
            downloadingRequest.Dispose();
            downloadingRequest = null;

            SetProgress(0.48f, "Extracting GitHub repository zip...");

            string extractDir = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            string[] rootDirs = Directory.GetDirectories(extractDir);
            if (rootDirs.Length == 0)
                throw new Exception("Extracted zip is empty.");

            string repoRoot = rootDirs[0];

            string projectAssetsFolder = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                TARGET_FOLDER);

            SetProgress(0.58f, "Validating update contents...");
            ValidateRepoContents(repoRoot);

            SetProgress(0.64f, "Checking file access before writing...");
            ValidateWritableTargets(repoRoot, projectAssetsFolder);

            try
            {
                AssetDatabase.StartAssetEditing();

                SetProgress(0.70f, "Copying missing and changed files...");
                SyncSdkFolders(repoRoot, projectAssetsFolder);

                SetProgress(0.86f, "Updating version.json...");
                CopyVersionFile(repoRoot, projectAssetsFolder);

                SetProgress(0.90f, "Removing obsolete SDK files...");
                RemoveObsoleteFiles(repoRoot, projectAssetsFolder);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            SetProgress(0.96f, "Refreshing Unity AssetDatabase...");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            SetProgress(1f, "SDK update completed.");

            Debug.Log(
                "ER2 SDK updated successfully. Version: " + VersionToString(newVersion) +
                " | Copied/updated: " + copiedFiles +
                " | Unchanged: " + skippedFiles +
                " | Deleted obsolete: " + deletedFiles);

            checkStatus = CheckStatus.Downloaded;
        }
        catch (Exception e)
        {
            error_str =
                e.Message +
                "\n\nUpdate aborted safely. Existing SDK files were not pre-deleted. " +
                "If the error mentions steam_api64.dll or another DLL, close Unity/Game/Steam-related processes that may be locking it, then retry.";

            Debug.LogError("ER2 SDK update error: " + e);
            checkStatus = CheckStatus.Error;
        }
        finally
        {
            try
            {
                if (downloadingRequest != null)
                {
                    downloadingRequest.Dispose();
                    downloadingRequest = null;
                }
            }
            catch { }

            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch { }

            EditorUtility.ClearProgressBar();

            var window = GetWindow<ModMenuUpdater>(false);
            if (window != null)
                window.Repaint();
        }
    }

    private static void SetProgress(float value, string info)
    {
        progressValue = Mathf.Clamp01(value);
        progressInfo = info;

        EditorUtility.DisplayProgressBar("ER2 SDK Update", progressInfo, progressValue);

        var window = GetWindow<ModMenuUpdater>(false);
        if (window != null)
            window.Repaint();
    }

    private static void ValidateRepoContents(string repoRoot)
    {
        foreach (string folder in SYNC_FOLDERS)
        {
            string src = Path.Combine(repoRoot, folder);
            if (!Directory.Exists(src))
                throw new Exception("Update package is missing required folder: " + folder);
        }

        string versionPath = Path.Combine(repoRoot, "version.json");
        if (!File.Exists(versionPath))
            throw new Exception("Update package is missing version.json.");
    }

    private static void ValidateWritableTargets(string repoRoot, string projectAssetsFolder)
    {
        foreach (string folder in SYNC_FOLDERS)
        {
            string srcRoot = Path.Combine(repoRoot, folder);
            string dstRoot = Path.Combine(projectAssetsFolder, folder);

            foreach (string srcFile in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldSkipFile(srcFile))
                    continue;

                string rel = GetRelativePath(srcRoot, srcFile);
                string dstFile = Path.Combine(dstRoot, rel);

                if (!File.Exists(dstFile))
                    continue;

                if (FilesAreEqual(srcFile, dstFile))
                    continue;

                EnsureWritableFile(dstFile);
            }
        }

        string dstVersion = Path.Combine(projectAssetsFolder, "version.json");
        if (File.Exists(dstVersion))
            EnsureWritableFile(dstVersion);
    }

    private static void EnsureWritableFile(string path)
    {
        FileInfo info = new FileInfo(path);

        if ((info.Attributes & FileAttributes.ReadOnly) != 0)
            info.Attributes &= ~FileAttributes.ReadOnly;

        try
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }
        }
        catch (Exception e)
        {
            throw new Exception("File is locked or not writable: " + path + "\n" + e.Message);
        }
    }

    private static void SyncSdkFolders(string repoRoot, string projectAssetsFolder)
    {
        foreach (string folder in SYNC_FOLDERS)
        {
            string srcRoot = Path.Combine(repoRoot, folder);
            string dstRoot = Path.Combine(projectAssetsFolder, folder);

            CopyChangedAndMissingFiles(srcRoot, dstRoot);
        }
    }

    private static void CopyChangedAndMissingFiles(string sourceRoot, string destRoot)
    {
        foreach (string dir in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkipDirectory(dir))
                continue;

            string rel = GetRelativePath(sourceRoot, dir);
            Directory.CreateDirectory(Path.Combine(destRoot, rel));
        }

        foreach (string srcFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkipFile(srcFile))
                continue;

            string rel = GetRelativePath(sourceRoot, srcFile);
            string dstFile = Path.Combine(destRoot, rel);
            string dstDir = Path.GetDirectoryName(dstFile);

            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);

            if (File.Exists(dstFile) && FilesAreEqual(srcFile, dstFile))
            {
                skippedFiles++;
                continue;
            }

            if (File.Exists(dstFile))
                EnsureWritableFile(dstFile);

            File.Copy(srcFile, dstFile, true);
            copiedFiles++;
        }
    }

    private static void CopyVersionFile(string repoRoot, string projectAssetsFolder)
    {
        string srcVersion = Path.Combine(repoRoot, "version.json");
        string dstVersion = Path.Combine(projectAssetsFolder, "version.json");

        if (File.Exists(dstVersion) && FilesAreEqual(srcVersion, dstVersion))
            return;

        if (File.Exists(dstVersion))
            EnsureWritableFile(dstVersion);

        File.Copy(srcVersion, dstVersion, true);

        string srcMeta = srcVersion + ".meta";
        string dstMeta = dstVersion + ".meta";

        if (File.Exists(srcMeta))
        {
            if (File.Exists(dstMeta))
                EnsureWritableFile(dstMeta);

            File.Copy(srcMeta, dstMeta, true);
        }
    }

    private static void RemoveObsoleteFiles(string repoRoot, string projectAssetsFolder)
    {
        foreach (string folder in SYNC_FOLDERS)
        {
            string srcRoot = Path.Combine(repoRoot, folder);
            string dstRoot = Path.Combine(projectAssetsFolder, folder);

            if (!Directory.Exists(dstRoot))
                continue;

            HashSet<string> sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string srcFile in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldSkipFile(srcFile))
                    continue;

                sourceFiles.Add(NormalizeRelativePath(GetRelativePath(srcRoot, srcFile)));
            }

            foreach (string dstFile in Directory.GetFiles(dstRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldSkipFile(dstFile))
                    continue;

                string rel = NormalizeRelativePath(GetRelativePath(dstRoot, dstFile));

                if (sourceFiles.Contains(rel))
                    continue;

                EnsureWritableFile(dstFile);
                File.Delete(dstFile);
                deletedFiles++;
            }

            DeleteEmptyDirectories(dstRoot);
        }
    }

    private static void DeleteEmptyDirectories(string root)
    {
        if (!Directory.Exists(root))
            return;

        string[] dirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
        Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length));

        foreach (string dir in dirs)
        {
            if (ShouldSkipDirectory(dir))
                continue;

            if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
            {
                string meta = dir + ".meta";
                Directory.Delete(dir, false);

                if (File.Exists(meta))
                    File.Delete(meta);
            }
        }
    }

    private static bool FilesAreEqual(string a, string b)
    {
        FileInfo fa = new FileInfo(a);
        FileInfo fb = new FileInfo(b);

        if (!fb.Exists)
            return false;

        if (fa.Length != fb.Length)
            return false;

        using (SHA256 sha = SHA256.Create())
        using (FileStream sa = File.OpenRead(a))
        using (FileStream sb = File.OpenRead(b))
        {
            byte[] ha = sha.ComputeHash(sa);
            byte[] hb = sha.ComputeHash(sb);

            if (ha.Length != hb.Length)
                return false;

            for (int i = 0; i < ha.Length; i++)
                if (ha[i] != hb[i])
                    return false;

            return true;
        }
    }

    private static bool ShouldSkipFile(string path)
    {
        string name = Path.GetFileName(path);

        if (SKIP_ROOT_FILES.Contains(name))
            return true;

        return false;
    }

    private static bool ShouldSkipDirectory(string path)
    {
        string name = Path.GetFileName(path);
        return SKIP_DIRS.Contains(name);
    }

    private static string GetRelativePath(string root, string path)
    {
        Uri rootUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(root)));
        Uri pathUri = new Uri(Path.GetFullPath(path));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            return path + Path.DirectorySeparatorChar;

        return path;
    }

    [Serializable]
    private class JSONData
    {
        public int version;
        public string manual_url;
    }
}
#endif