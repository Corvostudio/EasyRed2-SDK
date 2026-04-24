using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;

public class ModMenuUpdater : EditorWindow
{
    //current version
    public static readonly int VERSION = 106;

    // === CONFIGURE THESE ===
    private const string GITHUB_USER = "Corvostudio";
    private const string GITHUB_REPO = "EasyRed2-SDK";
    private const string BRANCH = "main";
    // Folder (relative to project root) where repo contents will be placed.
    private const string TARGET_FOLDER = "Assets";
    // =======================

    private const string versionJsonURL =
        "https://raw.githubusercontent.com/" + GITHUB_USER + "/" + GITHUB_REPO + "/" + BRANCH + "/version.json";
    private const string zipURL =
        "https://github.com/" + GITHUB_USER + "/" + GITHUB_REPO + "/archive/refs/heads/" + BRANCH + ".zip";

    private static int newVersion;
    private static string manualUrl;
    private static string error_str;

    private enum CheckStatus { Checking, Downloading, Installing, UpToDate, Downloaded, Error, NewUpdateFound }
    private static CheckStatus checkStatus = CheckStatus.Checking;

    private static UnityWebRequest downloadingRequest;

    // Silent check used by auto-check on project open.
    public static void CheckForUpdatesSilent()
    {
        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
            manualUrl = data.manual_url;
            www.Dispose();

            if (newVersion > VERSION)
            {
                checkStatus = CheckStatus.NewUpdateFound;
                GetWindow(typeof(ModMenuUpdater));
            }
        };
    }

    [MenuItem("ER2 Project/Update/Check for new Updates")]
    public static void AutoDownload()
    {
        GetWindow(typeof(ModMenuUpdater));
        checkStatus = CheckStatus.Checking;
        GetVersionAsync();
    }

    public static void ForceUpdate()
    {
        GetWindow(typeof(ModMenuUpdater));
        checkStatus = CheckStatus.Checking;

        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                error_str = "Error fetching version.json: " + www.error;
                checkStatus = CheckStatus.Error;
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
            manualUrl = data.manual_url;
            www.Dispose();

            checkStatus = CheckStatus.Downloading;
            DownloadAndInstall();
        };
    }

    [MenuItem("ER2 Project/Update/Open SDK Download page")]
    public static void OpenSdkUrl()
    {
        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching version.json: " + www.error);
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            string manual = data.manual_url;
            www.Dispose();

            Application.OpenURL(manual);
        };
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        switch (checkStatus)
        {
            case CheckStatus.Checking:
                GUILayout.Label("Checking for updates...", EditorStyles.boldLabel);
                break;
            case CheckStatus.Downloading:
                int pct = downloadingRequest != null ? (int)(downloadingRequest.downloadProgress * 100) : 0;
                GUILayout.Label("Downloading update " + pct + "%", EditorStyles.boldLabel);
                Repaint();
                break;
            case CheckStatus.Installing:
                GUILayout.Label("Extracting and installing files...", EditorStyles.boldLabel);
                Repaint();
                break;
            case CheckStatus.UpToDate:
                GUILayout.Label("Already up to date!\nCurrent version: " + VersionToString(VERSION), EditorStyles.boldLabel);
                break;
            case CheckStatus.Downloaded:
                GUILayout.Label("SDK updated succesfully!\n New version: " + VersionToString(newVersion) + "!", EditorStyles.boldLabel);
                break;
            case CheckStatus.Error:
                GUILayout.Label("SDK update error: " + error_str, EditorStyles.boldLabel);
                break;
            case CheckStatus.NewUpdateFound:
                GUILayout.Label("New update found for ER2 Modding SDK!", EditorStyles.boldLabel);
                GUILayout.Space(15);
                GUILayout.Label("New update version: " + VersionToString(newVersion));
                GUILayout.Label("Current version: " + VersionToString(VERSION));
                GUILayout.Space(15);
                if (GUILayout.Button("Update now"))
                {
                    Close();
                    ForceUpdate();
                }
                break;
        }
    }

    private static string VersionToString(int version)
    {
        return (version / 100f).ToString("F2");
    }

    private static void GetVersionAsync()
    {
        var www = UnityWebRequest.Get(versionJsonURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                error_str = "Error fetching version.json: " + www.error;
                checkStatus = CheckStatus.Error;
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
            manualUrl = data.manual_url;
            www.Dispose();

            if (newVersion > VERSION)
            {
                checkStatus = CheckStatus.Downloading;
                DownloadAndInstall();
            }
            else
            {
                checkStatus = CheckStatus.UpToDate;
            }
        };
    }

    private static void DownloadAndInstall()
    {
        downloadingRequest = UnityWebRequest.Get(zipURL);
        downloadingRequest.SendWebRequest().completed += _ =>
        {
            if (downloadingRequest.result != UnityWebRequest.Result.Success)
            {
                error_str = "Failed to download zip: " + downloadingRequest.error;
                checkStatus = CheckStatus.Error;
                return;
            }

            try
            {
                checkStatus = CheckStatus.Installing;

                string tempDir = Path.Combine(Application.temporaryCachePath, "er2_sdk_update");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                string zipPath = Path.Combine(tempDir, "sdk.zip");
                File.WriteAllBytes(zipPath, downloadingRequest.downloadHandler.data);

                string extractDir = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                // GitHub archives wrap everything in a single "<repo>-<branch>/" folder.
                var rootDirs = Directory.GetDirectories(extractDir);
                if (rootDirs.Length == 0)
                {
                    error_str = "Extracted zip is empty.";
                    checkStatus = CheckStatus.Error;
                    return;
                }
                string repoRoot = rootDirs[0];

                string projectTargetFolder = Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    TARGET_FOLDER);
                Directory.CreateDirectory(projectTargetFolder);

                try
                {
                    AssetDatabase.StartAssetEditing();
                    CopyDirectory(repoRoot, projectTargetFolder);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                try { Directory.Delete(tempDir, true); } catch { /* not fatal */ }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log("SDK updated from git repo successfully! New version: " + VersionToString(newVersion));
                checkStatus = CheckStatus.Downloaded;
            }
            catch (System.Exception e)
            {
                error_str = "Install error: " + e.Message;
                Debug.LogError("ER2 SDK install error: " + e);
                checkStatus = CheckStatus.Error;
            }
        };
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string name = Path.GetFileName(file);
            // Skip repo housekeeping files we don't want to import as assets.
            if (name.Equals(".gitignore", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals(".gitattributes", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("README.md", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("README.md.meta", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("LICENSE", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("LICENSE.md", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("version.json", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string dest = Path.Combine(destDir, name);
            File.Copy(file, dest, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string name = Path.GetFileName(dir);
            if (name.Equals(".git", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals(".github", System.StringComparison.OrdinalIgnoreCase))
                continue;

            CopyDirectory(dir, Path.Combine(destDir, name));
        }
    }

    [System.Serializable]
    private class JSONData
    {
        public int version;
        public string manual_url;
    }
}