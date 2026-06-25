#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static class ER2MemoryEstimateMenu_Shared
{
    private static bool _isRunning;

    [MenuItem("ER2 TOOLS/Estimate memory usage (shared-aware)", true)]
    private static bool Validate() => Selection.objects != null && Selection.objects.Length > 0;

    public static readonly HashSet<System.Type> BlacklistedComponentTypes = new HashSet<System.Type>
    {
        typeof(CamouflageManager),
    };

    private static void EstimateSelection_Internal()
    {
        var selection = Selection.objects;
        if (selection == null || selection.Length == 0) return;

        // Global dedupe across the whole run (shared resources count once).
        var globalCounted = new HashSet<Object>();

        long totalBytes = 0;
        int countedRoots = 0;

        foreach (var o in selection)
        {
            var go = o as GameObject;
            if (!go) continue;

            countedRoots++;

            // Measure "incremental": snapshot before, estimate, then delta
            int beforeCount = globalCounted.Count;

            var rep = PrefabMemoryEstimator_SharedAware.EstimatePrefabCpuRam(go, globalCounted, includeInactive: true);

            // rep.totalBytes is the "added bytes" only (because Estimate uses globalCounted and only adds new objects)
            totalBytes += rep.totalBytes;

            float mb = rep.totalBytes / (1024f * 1024f);
            int newlyAddedObjects = globalCounted.Count - beforeCount;

            // print single
            if (selection.Length == 1)
            {
                Debug.Log(
                    $"<color=#f7ff0f><b>MEM TEST</b></color> <color=#FFFFFF>(<color=#AAAAAA>Prefab:</color> <color=#FFFFFF>{go.name}</color>)</color> <color=#AAAAAA>RAM Usage:</color> <color=#FF5555><b>{mb:0.00} MB</b></color>\n" +
                    $"<color=#AAAAAA>New unique objects:</color> <color=#FFFFFF>{newlyAddedObjects}</color>\n" +
                    $"<color=#AAAAAA>- Textures:</color> {PrefabMemoryEstimator_SharedAware.FormatMB(rep.textureBytes)}  " +
                    $"<color=#AAAAAA>- Meshes:</color> {PrefabMemoryEstimator_SharedAware.FormatMB(rep.meshBytes)}  " +
                    $"<color=#AAAAAA>- Materials:</color> {PrefabMemoryEstimator_SharedAware.FormatMB(rep.materialBytes)}"
                );
            }
        }

        // print multi
        if (selection.Length > 1)
        {
            Debug.Log(
                $"<color=#f7ff0f><b>MEM TEST</b></color> (<color=#AAAAAA>Prefabs:</color> <color=#FFFFFF>{countedRoots}</color>) <color=#AAAAAA>Total RAM Usage:</color> <color=#FF5555><b>{(totalBytes / (1024f * 1024f)):0.00} MB</b></color>\n" +
                $"<color=#AAAAAA>Unique objects counted:</color> <color=#FFFFFF>{globalCounted.Count}</color>"
            );
        }
    }

    [MenuItem("GameObject/ER2 TOOLS/Estimate memory usage (shared-aware)", false, 49)]
    private static void EstimateFromHierarchy(MenuCommand cmd)
    {
        // Unity invokes this once per selected object.
        // Run only for the active one, so it executes once.
        if (cmd.context != Selection.activeObject)
            return;

        EstimateSelection();
    }

    [MenuItem("ER2 TOOLS/Estimate memory usage (shared-aware)", false, 2001)]
    private static void EstimateSelection()
    {
        if (_isRunning) return;
        _isRunning = true;
        try
        {
            EstimateSelection_Internal();
        }
        finally
        {
            _isRunning = false;
        }
    }
}

public static class PrefabMemoryEstimator_SharedAware
{
    public struct Report
    {
        public long totalBytes; // incremental bytes added (given the global HashSet)

        public long prefabBytes;
        public long meshBytes;
        public long textureBytes;
        public long materialBytes;
        public long animationClipBytes;
        public long audioClipBytes;
        public long miscBytes;
    }

    /// <summary>
    /// Shared-aware estimator.
    /// Reworked to scan generic Components via Unity serialization (SerializedObject),
    /// so custom MonoBehaviours that store Texture/Material/Sprite (including arrays/lists)
    /// get picked up automatically. Also expands Material -> referenced textures.
    /// </summary>
    public static Report EstimatePrefabCpuRam(GameObject prefabOrInstance, HashSet<Object> globalCounted, bool includeInactive = true)
    {
        if (!prefabOrInstance) return default;
        if (globalCounted == null) globalCounted = new HashSet<Object>();

        var report = new Report();

        bool needsTempInstance = PrefabUtility.IsPartOfPrefabAsset(prefabOrInstance);
        GameObject root = prefabOrInstance;

        // Prevent infinite loops when traversing ScriptableObject graphs etc.
        var visitedOwners = new HashSet<int>(1024);

        if (needsTempInstance)
        {
            root = Object.Instantiate(prefabOrInstance);
            root.hideFlags = HideFlags.HideAndDontSave;
        }

        try
        {
            // Count the prefab object itself (small, but keep it)
            Accumulate(prefabOrInstance, globalCounted, ref report, Category.Prefab);
            if (root != prefabOrInstance) Accumulate(root, globalCounted, ref report, Category.Prefab);

            // Core idea: scan every Component and traverse its serialized Object references.
            // This catches:
            // - Renderer.sharedMaterials, MeshFilter.sharedMesh, SpriteRenderer.sprite, AudioSource.clip, etc.
            // - Custom MonoBehaviours with Texture/Material/Sprite fields (arrays/lists too)
            // - ScriptableObjects referenced by components (and their graphs), if serialized
            var comps = root.GetComponentsInChildren<Component>(includeInactive);
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (!c) continue;

                // --- ADD THIS CHECK ---
                if (ER2MemoryEstimateMenu_Shared.BlacklistedComponentTypes.Contains(c.GetType())) continue;

                // Count component object itself
                Accumulate(c, globalCounted, ref report, Category.Misc);

                // Scan serialized refs on this component (Unity-serialization aware)
                AccumulateSerializedObjectReferences(c, globalCounted, ref report, visitedOwners);
            }

            report.totalBytes =
                report.prefabBytes +
                report.meshBytes +
                report.textureBytes +
                report.materialBytes +
                report.animationClipBytes +
                report.audioClipBytes +
                report.miscBytes;

            return report;
        }
        finally
        {
            if (needsTempInstance && root)
                Object.DestroyImmediate(root);
        }
    }

    private enum Category { Prefab, Mesh, Texture, Material, AnimationClip, AudioClip, Misc }

    private static void Accumulate(Object obj, HashSet<Object> counted, ref Report report, Category cat)
    {
        if (!obj) return;

        // Dedupe here. If already counted globally, contributes 0 bytes.
        if (!counted.Add(obj)) return;

        long bytes = Profiler.GetRuntimeMemorySizeLong(obj);
        switch (cat)
        {
            case Category.Prefab: report.prefabBytes += bytes; break;
            case Category.Mesh: report.meshBytes += bytes; break;
            case Category.Texture: report.textureBytes += bytes; break;
            case Category.Material: report.materialBytes += bytes; break;
            case Category.AnimationClip: report.animationClipBytes += bytes; break;
            case Category.AudioClip: report.audioClipBytes += bytes; break;
            default: report.miscBytes += bytes; break;
        }
    }

    // --- Reworked part: serialized scanning (Unity-serialization aware) ---

    private static void AccumulateSerializedObjectReferences(
        Object owner,
        HashSet<Object> counted,
        ref Report report,
        HashSet<int> visitedOwners)
    {
        if (!owner) return;

        int id = owner.GetInstanceID();
        if (!visitedOwners.Add(id))
            return;

        SerializedObject so;
        try { so = new SerializedObject(owner); }
        catch { return; }

        SerializedProperty it;
        try { it = so.GetIterator(); }
        catch { return; }

        // IMPORTANT: always enter children so arrays/lists/nested fields are traversed
        while (it.NextVisible(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            var obj = it.objectReferenceValue;
            if (!obj) continue;

            AccumulateByType(obj, counted, ref report);

            if (obj is Material mat)
                AccumulateMaterialTextures(mat, counted, ref report);

            if (obj is Sprite sp && sp.texture)
                Accumulate(sp.texture, counted, ref report, Category.Texture);

            if (obj is ScriptableObject)
                AccumulateSerializedObjectReferences(obj, counted, ref report, visitedOwners);
        }
    }


    private static void AccumulateByType(Object obj, HashSet<Object> counted, ref Report report)
    {
        if (!obj) return;

        // Important: categorize first, then Accumulate (still deduped globally)
        if (obj is Texture)
        {
            Accumulate(obj, counted, ref report, Category.Texture);
            return;
        }

        if (obj is Material)
        {
            Accumulate(obj, counted, ref report, Category.Material);
            return;
        }

        if (obj is Mesh)
        {
            Accumulate(obj, counted, ref report, Category.Mesh);
            return;
        }

        if (obj is AnimationClip)
        {
            Accumulate(obj, counted, ref report, Category.AnimationClip);
            return;
        }

        if (obj is AudioClip)
        {
            Accumulate(obj, counted, ref report, Category.AudioClip);
            return;
        }

        // Controllers, shaders, sprites, fonts, ScriptableObjects, etc.
        Accumulate(obj, counted, ref report, Category.Misc);
    }

    private static void AccumulateMaterialTextures(Material m, HashSet<Object> counted, ref Report report)
    {
        if (!m) return;

        try
        {
            var texNames = m.GetTexturePropertyNames();
            for (int i = 0; i < texNames.Length; i++)
            {
                var t = m.GetTexture(texNames[i]);
                if (t) Accumulate(t, counted, ref report, Category.Texture);
            }
        }
        catch
        {
            // ignore materials/shaders that throw here
        }
    }

    public static string FormatMB(long bytes)
    {
        float mb = bytes / (1024f * 1024f);
        return $"{mb:0.00} MB";
    }
}
#endif
