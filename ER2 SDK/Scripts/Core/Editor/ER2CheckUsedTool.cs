using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ER2CheckUsedTool
{
    private static readonly string[] ScanExtensions =
    {
        "*.unity", "*.prefab", "*.mat", "*.asset", "*.controller",
        "*.anim", "*.overrideController", "*.playable", "*.mask",
        "*.preset", "*.spriteatlas", "*.spriteatlasv2", "*.shadervariants",
        "*.mixer", "*.guiskin", "*.fontsettings",
        "*.physicMaterial", "*.physicsMaterial2D",
        "*.renderTexture", "*.cubemap", "*.flare",
        "*.terrainlayer", "*.lighting", "*.brush"
    };

    [MenuItem("Assets/ER2 TOOLS/Check Used", false, 2000)]
    private static void CheckUsed()
    {
        var selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[ER2 Check] Nessun asset selezionato.");
            return;
        }

        var targets = new Dictionary<string, string>();
        foreach (var obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || Directory.Exists(path)) continue;
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid) && !targets.ContainsKey(guid))
                targets[guid] = path;
        }

        if (targets.Count == 0)
        {
            Debug.LogWarning("[ER2 Check] Selezione non valida.");
            return;
        }

        var bundledPaths = new HashSet<string>();
        foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
            foreach (var p in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName))
                bundledPaths.Add(p);

        var firstUser = new Dictionary<string, string>();
        var specialReason = new Dictionary<string, string>();

        // Pre-check Resources / AssetBundle / Shader-by-material (vale per qualsiasi tipo di asset)
        var shaderTargets = new Dictionary<string, Shader>();
        foreach (var kvp in targets)
        {
            string p = kvp.Value;

            if (IsInResources(p)) { specialReason[kvp.Key] = "Resources folder"; continue; }
            if (bundledPaths.Contains(p)) { specialReason[kvp.Key] = "AssetBundle"; continue; }

            if (p.EndsWith(".shader", System.StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith(".shadergraph", System.StringComparison.OrdinalIgnoreCase))
            {
                var sh = AssetDatabase.LoadAssetAtPath<Shader>(p);
                if (sh != null) shaderTargets[kvp.Key] = sh;
            }
        }

        var pendingGuids = new HashSet<string>();
        foreach (var kvp in targets)
            if (!specialReason.ContainsKey(kvp.Key))
                pendingGuids.Add(kvp.Key);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Shader: scansione mirata sui soli .mat
        if (shaderTargets.Count > 0)
        {
            var matFiles = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
            foreach (var matFile in matFiles)
            {
                string normalized = matFile.Replace('\\', '/');
                var mat = AssetDatabase.LoadAssetAtPath<Material>(normalized);
                if (mat == null || mat.shader == null) continue;

                string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                if (string.IsNullOrEmpty(shaderPath)) continue;
                string shaderGuid = AssetDatabase.AssetPathToGUID(shaderPath);

                if (shaderTargets.ContainsKey(shaderGuid) && !specialReason.ContainsKey(shaderGuid))
                {
                    specialReason[shaderGuid] = "Shader used by material";
                    firstUser[shaderGuid] = normalized;
                    pendingGuids.Remove(shaderGuid);
                }
            }
            foreach (var sg in shaderTargets.Keys) pendingGuids.Remove(sg);
        }

        // Scansione testuale per i GUID rimanenti
        if (pendingGuids.Count > 0)
        {
            var allFiles = new List<string>();
            foreach (var ext in ScanExtensions)
                allFiles.AddRange(Directory.GetFiles("Assets", ext, SearchOption.AllDirectories));

            int total = allFiles.Count;
            try
            {
                for (int i = 0; i < total; i++)
                {
                    if ((i & 63) == 0 &&
                        EditorUtility.DisplayCancelableProgressBar(
                            "ER2 Check",
                            $"Scansione {i}/{total}",
                            (float)i / total))
                        break;

                    string file = allFiles[i];
                    string normalized = file.Replace('\\', '/');
                    string fileGuid = AssetDatabase.AssetPathToGUID(normalized);

                    string content;
                    try { content = File.ReadAllText(file); }
                    catch { continue; }

                    var snapshot = new List<string>(pendingGuids);
                    foreach (var g in snapshot)
                    {
                        if (g == fileGuid) continue;
                        if (content.Contains(g))
                        {
                            firstUser[g] = normalized;
                            pendingGuids.Remove(g);
                        }
                    }

                    if (pendingGuids.Count == 0) break;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        sw.Stop();

        // Suddivide in due liste
        var usedList = new List<KeyValuePair<string, string>>();
        var unusedList = new List<KeyValuePair<string, string>>();
        foreach (var kvp in targets)
        {
            bool isUsed = specialReason.ContainsKey(kvp.Key) || firstUser.ContainsKey(kvp.Key);
            (isUsed ? usedList : unusedList).Add(kvp);
        }

        // === USED prima ===
        if (usedList.Count > 0)
        {
            Debug.Log($"<color=#33CC66><b>===== USED ({usedList.Count}) =====</b></color>");
            foreach (var kvp in usedList)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(kvp.Value);
                string reason = specialReason.TryGetValue(kvp.Key, out var r) ? r : "referenced";
                string user = firstUser.TryGetValue(kvp.Key, out var u) ? $" by <b>{u}</b>" : "";
                Debug.Log($"<color=#33CC66><b>[USED]</b></color> {kvp.Value}\n-<i>({reason}{user})</i>", asset);
            }
        }

        // === NOT USED dopo (in fondo alla console = più visibile) ===
        if (unusedList.Count > 0)
        {
            Debug.Log($"<color=#FF4444><b>===== NOT USED ({unusedList.Count}) =====</b></color>");
            foreach (var kvp in unusedList)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(kvp.Value);
                Debug.LogError($"<color=#FF4444><b>[NOT USED]</b></color> {kvp.Value}", asset);
            }
        }

        Debug.Log($"[ER2 Check] Completato in {sw.ElapsedMilliseconds} ms. " +
                  $"Totale: {targets.Count} | Used: {usedList.Count} | Not used: {unusedList.Count}");
    }

    [MenuItem("Assets/ER2 TOOLS/Check Used", true)]
    private static bool Validate() =>
        Selection.objects != null && Selection.objects.Length > 0;

    private static bool IsInResources(string assetPath)
    {
        string p = assetPath.Replace('\\', '/');
        if (p.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return p.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase);
    }
}