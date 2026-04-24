using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class ChangeAssetGUID : EditorWindow
{
    private string newGuid = "";
    private string currentGuid = "";
    private string assetPath = "";

    private const string MenuPath = "Assets/ER2 Tools/Change GUID...";

    // Text-serialized Unity asset extensions that can contain `guid:` references,
    // plus .meta (for the target's own guid and for sub-asset / LazyLoadReference maps),
    // .asmdef / .asmref (JSON `"GUID:<hex>"` references).
    private static readonly HashSet<string> ScannedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".unity", ".prefab", ".asset", ".mat", ".anim", ".controller", ".overridecontroller",
        ".mask", ".physicmaterial", ".physicsmaterial", ".physicsmaterial2d", ".mixer",
        ".spriteatlas", ".spriteatlasv2", ".playable", ".terrainlayer", ".lighting",
        ".giparams", ".rendertexture", ".cubemap", ".fontsettings", ".guiskin", ".flare",
        ".brush", ".shadervariants", ".preset", ".signal", ".meta", ".asmdef", ".asmref"
    };

    [MenuItem(MenuPath, false, 2000)]
    private static void OpenWindow()
    {
        var obj = Selection.activeObject;
        if (obj == null) return;

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;

        var window = GetWindow<ChangeAssetGUID>(true, "Change Asset GUID", true);
        window.assetPath = path;
        window.currentGuid = AssetDatabase.AssetPathToGUID(path);
        window.newGuid = window.currentGuid;
        window.minSize = new Vector2(480, 220);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateOpenWindow()
    {
        var obj = Selection.activeObject;
        if (obj == null) return false;
        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return false;
        return !AssetDatabase.IsValidFolder(path);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Asset", assetPath, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Current GUID", currentGuid);
        GUILayout.Space(8);

        EditorGUILayout.LabelField("New GUID (32 hex chars, no dashes)");
        newGuid = EditorGUILayout.TextField(newGuid).Trim().ToLowerInvariant();

        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "This will scan every file in 'Assets/' and 'ProjectSettings/' and rewrite every reference " +
            "to the current GUID. Back up or commit your project first.",
            MessageType.Warning);

        GUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Random"))
            {
                newGuid = Guid.NewGuid().ToString("N");
                GUI.FocusControl(null);
            }

            GUI.enabled = IsValidGuid(newGuid) && newGuid != currentGuid;
            if (GUILayout.Button("Apply"))
            {
                if (ApplyGuid(assetPath, currentGuid, newGuid))
                    Close();
            }
            GUI.enabled = true;
        }
    }

    private static bool IsValidGuid(string g)
    {
        return !string.IsNullOrEmpty(g) && Regex.IsMatch(g, "^[0-9a-f]{32}$");
    }

    private static bool ApplyGuid(string assetPath, string oldGuid, string newGuid)
    {
        // Collision check: no other asset may already own the new GUID.
        string existing = AssetDatabase.GUIDToAssetPath(newGuid);
        if (!string.IsNullOrEmpty(existing) && existing != assetPath)
        {
            EditorUtility.DisplayDialog("GUID Collision",
                "This GUID is already used by:\n" + existing, "OK");
            return false;
        }

        if (!EditorUtility.DisplayDialog("Confirm GUID Change",
                $"Rewrite every file in 'Assets/' and 'ProjectSettings/' that references:\n\n{assetPath}\n\n" +
                $"Old GUID: {oldGuid}\nNew GUID: {newGuid}\n\n" +
                "Make sure your project is backed up / committed. Continue?",
                "Change GUID", "Cancel"))
            return false;

        // Flush any pending in-memory asset changes to disk before we rewrite files.
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayProgressBar("Change GUID", "Enumerating project files...", 0f);

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var roots = new[]
        {
            Application.dataPath,
            Path.Combine(projectRoot, "ProjectSettings")
        };

        var files = new List<string>(16384);
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (ScannedExtensions.Contains(Path.GetExtension(f)))
                    files.Add(f);
            }
        }

        int modifiedCount = 0;
        int errorCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            EditorUtility.DisplayProgressBar("Change GUID",
                $"Scanning {files.Count} files...", 0.1f);

            Parallel.ForEach(files, file =>
            {
                try
                {
                    string text = File.ReadAllText(file);
                    // Fast path: cheap substring test avoids allocating a new string for most files.
                    if (text.IndexOf(oldGuid, StringComparison.Ordinal) < 0) return;

                    string replaced = text.Replace(oldGuid, newGuid);
                    if (replaced != text)
                    {
                        File.WriteAllText(file, replaced);
                        Interlocked.Increment(ref modifiedCount);
                    }
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref errorCount);
                    Debug.LogWarning($"[ER2 Change GUID] Skipped {file}: {e.Message}");
                }
            });
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        EditorUtility.DisplayDialog("GUID Changed",
            $"GUID updated.\n\nOld: {oldGuid}\nNew: {newGuid}\n\n" +
            $"Files scanned: {files.Count}\nFiles modified: {modifiedCount}" +
            (errorCount > 0 ? $"\nErrors: {errorCount} (see Console)" : ""),
            "OK");

        return true;
    }
}