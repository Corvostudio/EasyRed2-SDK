using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ER2CheckUsedTool
{
    // Estensioni dei file che possono contenere riferimenti a GUID
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

    [MenuItem("Assets/ER2 Tools/Check Used", false, 2000)]
    private static void CheckUsed()
    {
        var selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[ER2 Check Used] Nessun asset selezionato.");
            return;
        }

        // Raccoglie i GUID target (guid -> path)
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
            Debug.LogWarning("[ER2 Check Used] Selezione non valida.");
            return;
        }

        // Raccoglie tutti i file da scansionare in un'unica lista
        var allFiles = new List<string>();
        foreach (var ext in ScanExtensions)
            allFiles.AddRange(Directory.GetFiles("Assets", ext, SearchOption.AllDirectories));

        var foundGuids = new HashSet<string>();
        int total = allFiles.Count;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            for (int i = 0; i < total; i++)
            {
                if ((i & 63) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "ER2 Check Used",
                        $"Scansione {i}/{total}",
                        (float)i / total))
                    break;

                string file = allFiles[i];
                string fileGuid = AssetDatabase.AssetPathToGUID(file.Replace('\\', '/'));

                // Legge il contenuto una volta sola e testa tutti i target non ancora trovati
                string content;
                try { content = File.ReadAllText(file); }
                catch { continue; }

                foreach (var kvp in targets)
                {
                    if (foundGuids.Contains(kvp.Key)) continue;
                    if (kvp.Key == fileGuid) continue; // ignora auto-riferimento
                    if (content.Contains(kvp.Key))
                        foundGuids.Add(kvp.Key);
                }

                if (foundGuids.Count == targets.Count) break; // tutto trovato
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        sw.Stop();

        // Report: usa LogError con context per rendere il link cliccabile (ping asset)
        int unused = 0;
        foreach (var kvp in targets)
        {
            if (!foundGuids.Contains(kvp.Key))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(kvp.Value);
                Debug.LogError($"[ER2 Check Used] UNUSED: {kvp.Value}", asset);
                unused++;
            }
        }

        Debug.Log($"[ER2 Check Used] Completato in {sw.ElapsedMilliseconds} ms. " +
                  $"Controllati: {targets.Count} | Non usati: {unused} | Usati: {targets.Count - unused}");
    }

    [MenuItem("Assets/ER2 Tools/Check Used", true)]
    private static bool CheckUsedValidate() =>
        Selection.objects != null && Selection.objects.Length > 0;
}