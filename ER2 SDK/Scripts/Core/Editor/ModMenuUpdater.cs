using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class ModMenuUpdater : EditorWindow
{
    //current version
    public static readonly int VERSION = 106;

    private const string websiteURL = "https://corvostudio.it/er2/modding_sdk/versioning.html";
    private static int newVersion;
    private static string error_str;

    private enum CheckStatus { Checking, Downloading, UpToDate, Downloaded, Error, NewUpdateFound }
    private static CheckStatus checkStatus = CheckStatus.Checking;

    private static UnityWebRequest downloadingRequest;

    // Silent check used by auto-check on project open.
    // Opens the update window only if a new version is actually available.
    public static void CheckForUpdatesSilent()
    {
        var www = UnityWebRequest.Get(websiteURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                // Silent: don't pop a window on project open for network errors.
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
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

        var www = UnityWebRequest.Get(websiteURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                error_str = "Error fetching JSON: " + www.error;
                checkStatus = CheckStatus.Error;
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
            www.Dispose();

            checkStatus = CheckStatus.Downloading;
            DownloadNewVersion(data.url);
        };
    }

    [MenuItem("ER2 Project/Update/Open SDK Download page")]
    public static void OpenSdkUrl()
    {
        var www = UnityWebRequest.Get(websiteURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching JSON: " + www.error);
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
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
                Repaint(); // keep the % label live
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
                GUILayout.Label("New update found for ER2 Modding SKD!", EditorStyles.boldLabel);
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
        var www = UnityWebRequest.Get(websiteURL);
        www.SendWebRequest().completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                error_str = "Error fetching JSON: " + www.error;
                checkStatus = CheckStatus.Error;
                www.Dispose();
                return;
            }

            var data = JsonUtility.FromJson<JSONData>(www.downloadHandler.text);
            newVersion = data.version;
            www.Dispose();

            if (newVersion > VERSION)
            {
                checkStatus = CheckStatus.Downloading;
                DownloadNewVersion(data.url);
            }
            else
            {
                checkStatus = CheckStatus.UpToDate;
            }
        };
    }

    private static void DownloadNewVersion(string urlPath)
    {
        downloadingRequest = UnityWebRequest.Get(urlPath);
        downloadingRequest.SendWebRequest().completed += _ =>
        {
            if (downloadingRequest.result != UnityWebRequest.Result.Success)
            {
                error_str = "Failed to download package: " + downloadingRequest.error;
                checkStatus = CheckStatus.Error;
                return;
            }

            string tempPath = "Assets/er2_modding_sdk_v" + VersionToString(newVersion) + ".unitypackage";
            File.WriteAllBytes(tempPath, downloadingRequest.downloadHandler.data);
            AssetDatabase.ImportPackage(tempPath, true);
            Debug.Log("Package downloaded and imported successfully!");
            checkStatus = CheckStatus.Downloaded;
        };
    }

    [System.Serializable]
    private class JSONData
    {
        public int version;
        public string url;
        public string manual_url;
    }
}