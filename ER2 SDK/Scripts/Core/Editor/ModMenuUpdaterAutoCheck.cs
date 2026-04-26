using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class ModMenuUpdaterAutoCheck
{
    // SessionState persists across domain reloads (recompiles, play mode)
    // but is cleared when Unity closes -> effectively "once per project open".
    private const string SessionKey_Checked = "ER2SDK_UpdateChecked";
    private const string SessionKey_QualityFixed = "ER2SDK_QualityFixed";

    static ModMenuUpdaterAutoCheck()
    {
        // Defer out of the static constructor to avoid touching EditorWindow
        // APIs while Unity is still initializing.
        EditorApplication.delayCall += RunOnceOnProjectOpen;
    }

    private static void RunOnceOnProjectOpen()
    {
        if (!SessionState.GetBool(SessionKey_QualityFixed, false))
        {
            SessionState.SetBool(SessionKey_QualityFixed, true);
            AutoFixQualityLevels();
        }

        if (!SessionState.GetBool(SessionKey_Checked, false))
        {
            SessionState.SetBool(SessionKey_Checked, true);
            ModMenuUpdater.CheckForUpdatesSilent();
        }
    }

    private static void AutoFixQualityLevels()
    {
        string sourceFolder = Application.dataPath + "/ER2 SDK/Settings";
        string targetFolder = Directory.GetParent(Application.dataPath) + "/ProjectSettings";
        const string fileName = "QualitySettings.asset";

        string sourceFilePath = Path.Combine(sourceFolder, fileName);
        string targetFilePath = Path.Combine(targetFolder, fileName);

        try
        {
            if (File.Exists(sourceFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath, true);
                QualitySettings.SetQualityLevel(0, true);
            }
        }
        catch (IOException e)
        {
            Debug.LogError("An error occurred while copying the file: " + e.Message);
        }
    }
}