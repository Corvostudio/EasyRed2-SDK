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

    private const string versionJsonBaseURL =
        "https://raw.githubusercontent.com/" + GITHUB_USER + "/" + GITHUB_REPO + "/" + BRANCH + "/version.json";

    private static string versionJsonURL =>
        versionJsonBaseURL + "?t=" + DateTime.UtcNow.Ticks;

    private const string zipURL =
        "https://github.com/" + GITHUB_USER + "/" + GITHUB_REPO + "/archive/refs/heads/" + BRANCH + ".zip";

    private static readonly string[] SYNC_FOLDERS =
    {
        "ER2 SDK",
        "Examples",
        "Steam SDK"
    };

    private static readonly string SYNC_FOLDERS_LIST = string.Join(", ", SYNC_FOLDERS);

    private static readonly HashSet<string> SKIP_ROOT_FILES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".gitignore", ".gitattributes", "README.md", "README.md.meta", "LICENSE", "LICENSE.md"
    };

    private static readonly HashSet<string> SKIP_DIRS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".github"
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
        DownloadedWithErrors,
        Error,
        NewUpdateFound,
        NotInstalled
    }

    private static CheckStatus checkStatus = CheckStatus.Checking;
    private static UnityWebRequest downloadingRequest;

    private static string progressInfo = "";
    private static float progressValue = 0f;
    private static bool isDownloading;

    private static int copiedFiles;
    private static int skippedFiles;
    private static int deletedFiles;

    [Serializable]
    private class FileFailure
    {
        public string Path;
        public string Operation;
        public string Reason;
        public string Detail;
    }

    [Serializable]
    private class FailureListWrapper
    {
        public List<FileFailure> items = new List<FileFailure>();
    }

    private static readonly List<FileFailure> failures = new List<FileFailure>();
    private static Vector2 errorScroll;

    private static ModMenuUpdater _cachedWindow;

    // SessionState keys: stato salvato prima del Refresh per sopravvivere allo script reload.
    private const string SS_Status = "ER2SDK_Upd_Status";
    private const string SS_NewVersion = "ER2SDK_Upd_NewVersion";
    private const string SS_Copied = "ER2SDK_Upd_Copied";
    private const string SS_Skipped = "ER2SDK_Upd_Skipped";
    private const string SS_Deleted = "ER2SDK_Upd_Deleted";
    private const string SS_Error = "ER2SDK_Upd_Error";
    private const string SS_Failures = "ER2SDK_Upd_Failures";

    private static int GetLocalVersion()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "version.json");
            if (!File.Exists(path)) return 0;

            var data = JsonUtility.FromJson<JSONData>(File.ReadAllText(path));
            return data != null ? data.version : 0;
        }
        catch { return 0; }
    }

    private void OnEnable()
    {
        _cachedWindow = this;
        RestoreSessionState();
    }

    private static void RepaintWindow()
    {
        if (_cachedWindow != null) _cachedWindow.Repaint();
    }

    private static void SaveSessionState()
    {
        SessionState.SetInt(SS_Status, (int)checkStatus);
        SessionState.SetInt(SS_NewVersion, newVersion);
        SessionState.SetInt(SS_Copied, copiedFiles);
        SessionState.SetInt(SS_Skipped, skippedFiles);
        SessionState.SetInt(SS_Deleted, deletedFiles);
        SessionState.SetString(SS_Error, error_str ?? "");

        var wrapper = new FailureListWrapper { items = failures };
        SessionState.SetString(SS_Failures, JsonUtility.ToJson(wrapper));
    }

    private static void RestoreSessionState()
    {
        int s = SessionState.GetInt(SS_Status, -1);
        if (s < 0) return;

        checkStatus = (CheckStatus)s;
        newVersion = SessionState.GetInt(SS_NewVersion, 0);
        copiedFiles = SessionState.GetInt(SS_Copied, 0);
        skippedFiles = SessionState.GetInt(SS_Skipped, 0);
        deletedFiles = SessionState.GetInt(SS_Deleted, 0);
        error_str = SessionState.GetString(SS_Error, "");

        string json = SessionState.GetString(SS_Failures, "");
        failures.Clear();
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<FailureListWrapper>(json);
                if (wrapper != null && wrapper.items != null) failures.AddRange(wrapper.items);
            }
            catch { }
        }

        // Se il reload ci ha colti durante uno stato in-progress, lo trattiamo come completato.
        if (checkStatus == CheckStatus.Checking ||
            checkStatus == CheckStatus.Downloading ||
            checkStatus == CheckStatus.Installing)
        {
            checkStatus = failures.Count == 0 ? CheckStatus.Downloaded : CheckStatus.DownloadedWithErrors;
        }
    }

    private static void ClearSessionState()
    {
        SessionState.EraseInt(SS_Status);
        SessionState.EraseInt(SS_NewVersion);
        SessionState.EraseInt(SS_Copied);
        SessionState.EraseInt(SS_Skipped);
        SessionState.EraseInt(SS_Deleted);
        SessionState.EraseString(SS_Error);
        SessionState.EraseString(SS_Failures);
    }

    private static UnityWebRequest BuildNoCacheGet(string url)
    {
        var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        req.SetRequestHeader("Pragma", "no-cache");
        return req;
    }

    // ------------------------------------------------------------------
    // SDK install detection & "not installed" entry points
    // ------------------------------------------------------------------

    /// <summary>
    /// Detects whether the ER2 SDK content is present in the project.
    /// Looks for ModMenu.cs as a sibling of ModMenuUpdater.cs.
    /// If the updater script itself can't be located (very unusual), assumes installed
    /// to avoid false positives.
    /// </summary>
    public static bool IsSdkInstalled()
    {
        string updaterDir = GetUpdaterAssetDirectory();
        if (string.IsNullOrEmpty(updaterDir)) return true;

        string modMenuPath = Path.Combine(updaterDir, "ModMenu.cs");
        return File.Exists(modMenuPath);
    }

    private static string GetUpdaterAssetDirectory()
    {
        var guids = AssetDatabase.FindAssets("ModMenuUpdater t:MonoScript");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == "ModMenuUpdater")
                return Path.GetDirectoryName(path);
        }
        return null;
    }

    /// <summary>
    /// Opens the updater window in the "SDK not installed" state.
    /// Wipes any persisted state from a previous install run: by definition the SDK
    /// is now missing, so old "Downloaded" stats and failure lists are stale.
    /// </summary>
    public static void ShowNotInstalledWindow()
    {
        // L'SDK è mancante: qualsiasi stato persistito da un'install precedente è
        // obsoleto. Pulisci PRIMA di GetWindow, così se la finestra viene creata
        // adesso il suo OnEnable -> RestoreSessionState non ripristina nulla.
        ClearSessionState();
        failures.Clear();
        copiedFiles = 0;
        skippedFiles = 0;
        deletedFiles = 0;
        newVersion = 0;
        error_str = "";

        checkStatus = CheckStatus.NotInstalled;

        var win = GetWindow<ModMenuUpdater>("ModMenuUpdater");
        win.Focus();
        RepaintWindow();
    }

    /// <summary>
    /// First-time install: same as ForceUpdate but skips the backup confirmation
    /// since there is nothing to overwrite when the SDK is not installed.
    /// </summary>
    public static void InstallSdk()
    {
        ClearSessionState();
        GetWindow<ModMenuUpdater>("ModMenuUpdater");
        checkStatus = CheckStatus.Checking;
        error_str = "";
        GetVersionAsync(true);
    }

    // ------------------------------------------------------------------
    // Update flow
    // ------------------------------------------------------------------

    public static void CheckForUpdatesSilent()
    {
        var www = BuildNoCacheGet(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            try
            {
                if (www.result != UnityWebRequest.Result.Success) return;

                var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
                if (data == null) return;

                newVersion = data.version;
                manualUrl = data.manual_url;

                if (newVersion > VERSION && VERSION != 0)
                {
                    checkStatus = CheckStatus.NewUpdateFound;
                    GetWindow<ModMenuUpdater>("ModMenuUpdater");
                }
            }
            finally { www.Dispose(); }
        };
    }

    [MenuItem("ER2 TOOLS/Update/Check for new Updates")]
    public static void AutoDownload()
    {
        if (!ConfirmBackupBeforeUpdate()) return;

        ClearSessionState();
        GetWindow<ModMenuUpdater>("ModMenuUpdater");
        checkStatus = CheckStatus.Checking;
        error_str = "";
        GetVersionAsync(false);
    }

    public static void ForceUpdate()
    {
        if (!ConfirmBackupBeforeUpdate()) return;

        ClearSessionState();
        GetWindow<ModMenuUpdater>("ModMenuUpdater");
        checkStatus = CheckStatus.Checking;
        error_str = "";
        GetVersionAsync(true);
    }

    private static bool ConfirmBackupBeforeUpdate()
    {
        string protectedPathWarning = "";
        if (IsProjectInProtectedPath(out string detectedPath))
        {
            protectedPathWarning =
                "\n\n⚠ WARNING: this Unity project is located inside a protected system folder:\n" +
                "  " + detectedPath + "\n" +
                "Updates from there usually fail unless Unity is launched as Administrator. " +
                "It is strongly recommended to move the project to a normal user folder " +
                "(e.g. Documents or a dedicated Dev folder on your disk) before updating.";
        }

        string message =
            "The updater will overwrite and delete files inside these SDK folders:\n\n" +
            "  " + SYNC_FOLDERS_LIST + "\n\n" +
            "Before continuing, please make sure that:\n" +
            "  • You have a backup of your project (or it is on version control).\n" +
            "  • Any personal assets, mods or work-in-progress files are stored OUTSIDE the folders listed above. " +
            "Anything inside those folders that does not belong to the official SDK will be removed." +
            protectedPathWarning +
            "\n\nDo you want to proceed with the update?";

        return EditorUtility.DisplayDialog(
            "ER2 SDK Update — confirm",
            message,
            "I have a backup, update now",
            "Cancel");
    }

    [MenuItem("ER2 TOOLS/Update/Open SDK Download page")]
    public static void OpenSdkUrl()
    {
        var www = BuildNoCacheGet(versionJsonURL);
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
            finally { www.Dispose(); }
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
                GUILayout.Label("Updated files: " + copiedFiles);
                GUILayout.Label("Unchanged files: " + skippedFiles);
                GUILayout.Label("Removed obsolete files: " + deletedFiles);
                break;

            case CheckStatus.DownloadedWithErrors:
                GUILayout.Label("SDK update partially completed.", EditorStyles.boldLabel);
                GUILayout.Label("Target version: " + VersionToString(newVersion));
                GUILayout.Space(6);
                GUILayout.Label("Updated files: " + copiedFiles);
                GUILayout.Label("Unchanged files: " + skippedFiles);
                GUILayout.Label("Removed obsolete files: " + deletedFiles);
                GUILayout.Label("Files with issues: " + failures.Count, EditorStyles.boldLabel);
                GUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "Some files could not be updated. The SDK still works for everything that succeeded; the entries below explain how to fix the rest.",
                    MessageType.Warning);

                errorScroll = EditorGUILayout.BeginScrollView(errorScroll, GUILayout.MaxHeight(220));
                foreach (var f in failures)
                {
                    EditorGUILayout.LabelField(f.Operation + " — " + Path.GetFileName(f.Path), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(f.Reason, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField(f.Path, EditorStyles.miniLabel);
                    GUILayout.Space(6);
                }
                EditorGUILayout.EndScrollView();

                GUILayout.Space(6);
                if (GUILayout.Button("Retry update")) ForceUpdate();
                if (GUILayout.Button("Open SDK Download page")) OpenSdkUrl();
                break;

            case CheckStatus.Error:
                GUILayout.Label("SDK update error:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(error_str, MessageType.Error);
                GUILayout.Space(8);
                if (GUILayout.Button("Retry")) ForceUpdate();
                if (GUILayout.Button("Open SDK Download page")) OpenSdkUrl();
                break;

            case CheckStatus.NewUpdateFound:
                GUILayout.Label("New update found for ER2 Modding SDK.", EditorStyles.boldLabel);
                GUILayout.Space(8);
                GUILayout.Label("New version: " + VersionToString(newVersion));
                GUILayout.Label("Current version: " + VersionToString(VERSION));
                GUILayout.Space(12);
                if (GUILayout.Button("Update now")) ForceUpdate();
                if (GUILayout.Button("Open SDK Download page")) OpenSdkUrl();
                break;

            case CheckStatus.NotInstalled:
                GUILayout.Label("ER2 SDK not installed", EditorStyles.boldLabel);
                GUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "The ER2 SDK content was not found in this project. Only the updater bootstrap files are present.\n\n" +
                    "Click \"Install SDK now\" to download and install the latest version automatically. " +
                    "If the automatic download fails, use \"Open SDK Download page\" for the manual installer.",
                    MessageType.Error);
                GUILayout.Space(8);
                if (GUILayout.Button("Install SDK now")) InstallSdk();
                if (GUILayout.Button("Open SDK Download page")) OpenSdkUrl();
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
    }

    private static string VersionToString(int version) => (version / 100f).ToString("F2");

    private static void GetVersionAsync(bool forceInstall)
    {
        progressValue = 0f;
        progressInfo = "Fetching version.json...";

        var www = BuildNoCacheGet(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    error_str = "Error fetching version.json: " + www.error;
                    checkStatus = CheckStatus.Error;
                    RepaintWindow();
                    return;
                }

                var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
                if (data == null)
                {
                    error_str = "Invalid version.json.";
                    checkStatus = CheckStatus.Error;
                    RepaintWindow();
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
                RepaintWindow();
            }
            finally { www.Dispose(); }
        };
    }

    private static void DownloadAndInstall()
    {
        copiedFiles = 0;
        skippedFiles = 0;
        deletedFiles = 0;
        failures.Clear();

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
                RepaintWindow();
                return;
            }

            InstallDownloadedZip();
        };
    }

    private static void UpdateDownloadProgress()
    {
        if (!isDownloading || downloadingRequest == null) return;

        float download = Mathf.Clamp01(downloadingRequest.downloadProgress);
        progressValue = download * 0.35f;
        progressInfo = "Downloading GitHub repository zip... " + Mathf.RoundToInt(download * 100f) + "%";
        RepaintWindow();
    }

    private static void InstallDownloadedZip()
    {
        string tempDir = Path.Combine(Application.temporaryCachePath, "er2_sdk_update");

        try
        {
            checkStatus = CheckStatus.Installing;

            SetProgress(0.38f, "Preparing temporary update folder...");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, "sdk.zip");
            File.WriteAllBytes(zipPath, downloadingRequest.downloadHandler.data);
            downloadingRequest.Dispose();
            downloadingRequest = null;

            SetProgress(0.48f, "Extracting GitHub repository zip...");
            string extractDir = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            string[] rootDirs = Directory.GetDirectories(extractDir);
            if (rootDirs.Length == 0) throw new Exception("Extracted zip is empty.");
            string repoRoot = rootDirs[0];

            string projectAssetsFolder = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, TARGET_FOLDER);

            SetProgress(0.58f, "Validating update contents...");
            ValidateRepoContents(repoRoot);

            try
            {
                AssetDatabase.StartAssetEditing();

                SetProgress(0.66f, "Updating files...");
                SyncSdkFolders(repoRoot, projectAssetsFolder);

                SetProgress(0.84f, "Updating version.json...");
                CopyVersionFile(repoRoot, projectAssetsFolder);

                SetProgress(0.90f, "Removing obsolete SDK files...");
                RemoveObsoleteFiles(repoRoot, projectAssetsFolder);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Imposta lo stato finale PRIMA del Refresh: il Refresh può scatenare
            // uno script reload che azzera gli static. Salviamo su SessionState e in
            // OnEnable (dopo il reload) ricarichiamo questi valori.
            checkStatus = failures.Count == 0
                ? CheckStatus.Downloaded
                : CheckStatus.DownloadedWithErrors;

            SetProgress(1f, failures.Count == 0
                ? "SDK update completed."
                : "SDK update completed with issues.");

            Debug.Log(
                "ER2 SDK update done. Version: " + VersionToString(newVersion) +
                " | Updated: " + copiedFiles +
                " | Unchanged: " + skippedFiles +
                " | Removed: " + deletedFiles +
                " | Issues: " + failures.Count);

            foreach (var f in failures)
                Debug.LogWarning("ER2 SDK: " + f.Operation + " failed for '" + f.Path + "': " + f.Reason +
                                 " (" + f.Detail + ")");

            SaveSessionState();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        catch (Exception e)
        {
            error_str =
                "Could not complete the update: " + e.Message +
                "\n\nNo SDK files have been deleted. Please retry; if the issue persists, use the manual download.";

            Debug.LogError("ER2 SDK update error: " + e);
            checkStatus = CheckStatus.Error;
            SaveSessionState();
        }
        finally
        {
            try { if (downloadingRequest != null) { downloadingRequest.Dispose(); downloadingRequest = null; } }
            catch { }
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
            catch { }

            RepaintWindow();
        }
    }

    private static void SetProgress(float value, string info)
    {
        progressValue = Mathf.Clamp01(value);
        progressInfo = info;
        RepaintWindow();
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

    // ---------------- Robust file ops ----------------

    /// <summary>
    /// Sostituisce un file in modo robusto.
    /// Su Windows i DLL caricati da Unity (es. steam_api64.dll) sono lockati per la SCRITTURA
    /// ma NON per la cancellazione: LoadLibrary apre l'immagine con FILE_SHARE_DELETE, quindi
    /// File.Delete riesce anche con la DLL caricata. Cancelliamo prima e ricopiamo da capo.
    /// L'editor in esecuzione continua a usare la vecchia immagine in RAM fino al prossimo avvio,
    /// ma il file su disco è già aggiornato.
    /// </summary>
    private static void SafeReplaceFile(string srcFile, string dstFile)
    {
        string dstDir = Path.GetDirectoryName(dstFile);
        if (!Directory.Exists(dstDir))
            Directory.CreateDirectory(dstDir);

        RunWithRetry(() =>
        {
            if (File.Exists(dstFile))
            {
                try
                {
                    FileAttributes attr = File.GetAttributes(dstFile);
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(dstFile, attr & ~FileAttributes.ReadOnly);
                }
                catch { /* best-effort */ }

                File.Delete(dstFile);
            }

            File.Copy(srcFile, dstFile, false);
        });
    }

    private const int RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 200;

    private static void RunWithRetry(Action op)
    {
        for (int attempt = 0; attempt < RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                op();
                return;
            }
            catch (IOException ioEx)
            {
                int code = ioEx.HResult & 0xFFFF;
                bool isTransient = code == 32 || code == 33;
                if (!isTransient || attempt == RETRY_ATTEMPTS - 1)
                    throw;

                System.Threading.Thread.Sleep(RETRY_DELAY_MS);
            }
        }
    }

    private static bool IsProjectInProtectedPath(out string detectedPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName.ToLowerInvariant();

        string[] protectedRoots =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)
        };

        foreach (string root in protectedRoots)
        {
            if (string.IsNullOrEmpty(root)) continue;
            string normalized = root.ToLowerInvariant();
            if (projectRoot.StartsWith(normalized))
            {
                detectedPath = root;
                return true;
            }
        }

        detectedPath = null;
        return false;
    }

    private static string GetFriendlyReason(Exception e)
    {
        if (e is UnauthorizedAccessException)
            return "Access denied. The file may be read-only or write-protected by version control (Perforce, Plastic, Git LFS). " +
                   "If the project is in a system folder (Program Files, OneDrive, etc.), move it elsewhere or run Unity as Administrator.";

        if (e is IOException ioEx)
        {
            int code = ioEx.HResult & 0xFFFF;
            switch (code)
            {
                case 5: return "Access denied by the operating system. Run Unity as Administrator, or move the project out of protected folders (Program Files, system drives).";
                case 32:
                case 33: return "File is open in another program. Close any running instance of the game, Steam, Visual Studio or other tools that may be using it, then click Retry.";
                case 39:
                case 112: return "Not enough free space on the disk to complete the update.";
                case 145: return "A folder still contains files that could not be removed. Close Unity and the game, then click Retry.";
                case 183: return "A file with the same name already exists and could not be overwritten. Close Unity and the game, then click Retry.";
            }
        }

        return e.Message;
    }

    private static void RecordFailure(string path, string operation, Exception e)
    {
        failures.Add(new FileFailure
        {
            Path = path,
            Operation = operation,
            Reason = GetFriendlyReason(e),
            Detail = e.GetType().Name + ": " + e.Message
        });
    }

    // ---------------- Sync ----------------

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
            if (ShouldSkipDirectory(dir)) continue;
            string rel = GetRelativePath(sourceRoot, dir);
            string fullDst = Path.Combine(destRoot, rel);
            try { Directory.CreateDirectory(fullDst); }
            catch (Exception e) { RecordFailure(fullDst, "Create folder", e); }
        }

        foreach (string srcFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkipFile(srcFile)) continue;

            string rel = GetRelativePath(sourceRoot, srcFile);
            string dstFile = Path.Combine(destRoot, rel);

            try
            {
                if (File.Exists(dstFile) && FilesAreEqual(srcFile, dstFile))
                {
                    skippedFiles++;
                    continue;
                }

                SafeReplaceFile(srcFile, dstFile);
                copiedFiles++;
            }
            catch (Exception e)
            {
                RecordFailure(dstFile, "Update file", e);
            }
        }
    }

    private static void CopyVersionFile(string repoRoot, string projectAssetsFolder)
    {
        string srcVersion = Path.Combine(repoRoot, "version.json");
        string dstVersion = Path.Combine(projectAssetsFolder, "version.json");

        try
        {
            if (!File.Exists(dstVersion) || !FilesAreEqual(srcVersion, dstVersion))
                SafeReplaceFile(srcVersion, dstVersion);
        }
        catch (Exception e) { RecordFailure(dstVersion, "Update file", e); }

        string srcMeta = srcVersion + ".meta";
        string dstMeta = dstVersion + ".meta";
        if (File.Exists(srcMeta))
        {
            try
            {
                if (!File.Exists(dstMeta) || !FilesAreEqual(srcMeta, dstMeta))
                    SafeReplaceFile(srcMeta, dstMeta);
            }
            catch (Exception e) { RecordFailure(dstMeta, "Update file", e); }
        }
    }

    private static void RemoveObsoleteFiles(string repoRoot, string projectAssetsFolder)
    {
        foreach (string folder in SYNC_FOLDERS)
        {
            string srcRoot = Path.Combine(repoRoot, folder);
            string dstRoot = Path.Combine(projectAssetsFolder, folder);
            if (!Directory.Exists(dstRoot)) continue;

            HashSet<string> sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string srcFile in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldSkipFile(srcFile)) continue;
                sourceFiles.Add(NormalizeRelativePath(GetRelativePath(srcRoot, srcFile)));
            }

            foreach (string dstFile in Directory.GetFiles(dstRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldSkipFile(dstFile)) continue;

                string rel = NormalizeRelativePath(GetRelativePath(dstRoot, dstFile));
                if (sourceFiles.Contains(rel)) continue;

                try
                {
                    RunWithRetry(() =>
                    {
                        FileAttributes attr = File.GetAttributes(dstFile);
                        if ((attr & FileAttributes.ReadOnly) != 0)
                            File.SetAttributes(dstFile, attr & ~FileAttributes.ReadOnly);

                        File.Delete(dstFile);
                    });
                    deletedFiles++;
                }
                catch (Exception e)
                {
                    RecordFailure(dstFile, "Delete obsolete file", e);
                }
            }

            try { DeleteEmptyDirectories(dstRoot); }
            catch { /* best-effort */ }
        }
    }

    private static void DeleteEmptyDirectories(string root)
    {
        if (!Directory.Exists(root)) return;

        string[] dirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
        Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length));

        foreach (string dir in dirs)
        {
            if (ShouldSkipDirectory(dir)) continue;
            try
            {
                if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                {
                    string meta = dir + ".meta";
                    Directory.Delete(dir, false);
                    if (File.Exists(meta)) File.Delete(meta);
                }
            }
            catch { /* skip */ }
        }
    }

    private static bool FilesAreEqual(string a, string b)
    {
        FileInfo fa = new FileInfo(a);
        FileInfo fb = new FileInfo(b);

        if (!fb.Exists) return false;
        if (fa.Length != fb.Length) return false;

        using (SHA256 sha = SHA256.Create())
        using (FileStream sa = File.OpenRead(a))
        using (FileStream sb = File.OpenRead(b))
        {
            byte[] ha = sha.ComputeHash(sa);
            byte[] hb = sha.ComputeHash(sb);

            if (ha.Length != hb.Length) return false;
            for (int i = 0; i < ha.Length; i++)
                if (ha[i] != hb[i]) return false;
            return true;
        }
    }

    private static bool ShouldSkipFile(string path) => SKIP_ROOT_FILES.Contains(Path.GetFileName(path));
    private static bool ShouldSkipDirectory(string path) => SKIP_DIRS.Contains(Path.GetFileName(path));

    private static string GetRelativePath(string root, string path)
    {
        Uri rootUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(root)));
        Uri pathUri = new Uri(Path.GetFullPath(path));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static string NormalizeRelativePath(string path) => path.Replace('\\', '/');

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