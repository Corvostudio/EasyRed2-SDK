
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

[ExecuteInEditMode]
#endif

public partial class DestructibleManager : MapPropIndexable// MapPropIndexable
{
    [Tooltip("All the individual parts of the building that can recieve damage. Requires DestructableBuilding components")]
    public DestructableBuilding[] destructibleParts = new DestructableBuilding[0];

    //props management
    [HideInInspector] public BuildingFurnitureSpawner[] fornitureSpawners = new BuildingFurnitureSpawner[0];

    //doors management
    [HideInInspector] public InteragibleDoor[] registeredDoors = new InteragibleDoor[0];


#if UNITY_EDITOR
    private void Awake()
    {
        if (!Application.isPlaying)
        {
            CaptureFurnitureSpawners();
            AcquireDestructibleParts();
            CaptureDoors();
        }
    }


    public void AcquireDestructibleParts()
    {
        destructibleParts = GetComponentsInChildren<DestructableBuilding>();
    }
    public void CaptureFurnitureSpawners()
    {
        fornitureSpawners = gameObject.GetComponentsInChildren<BuildingFurnitureSpawner>(true);
    }
    public void CaptureDoors()
    {
        registeredDoors = GetComponentsInChildren<InteragibleDoor>();
        if (TryGetComponent<DoorRegister>(out var reg))
            DestroyImmediate(reg);
    }
#endif
}


// DestructibleManagerEditor.cs
// Put under an "Editor" folder.

#if UNITY_EDITOR

[CustomEditor(typeof(DestructibleManager)), CanEditMultipleObjects]
public class DestructibleManagerEditor : Editor
{
    private DestructibleManager dm;

    // Per-child cached phase sliders (key = instanceID of component)
    private readonly Dictionary<int, int> phaseSlider = new Dictionary<int, int>();

    private bool showTester = true;
    private bool showValidation = true;

    // Rich label (bold via <b>...</b>)
    private GUIStyle _richLabel;
    private GUIStyle RichLabel
    {
        get
        {
            if (_richLabel == null)
            {
                _richLabel = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true
                };
            }
            return _richLabel;
        }
    }

    private void OnEnable()
    {
        dm = (DestructibleManager)target;
        RebuildSliderCache();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        DrawTesterSection();

        EditorGUILayout.Space(8);
        DrawValidationSection();
    }

    #region TESTER

    private void DrawTesterSection()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            showTester = EditorGUILayout.Foldout(showTester, "Tester / Debug", true);
            if (!showTester) return;

            if (dm.destructibleParts == null || dm.destructibleParts.Length == 0)
            {
                EditorGUILayout.HelpBox("No destructible parts to test.", MessageType.Info);
                return;
            }

            for (int i = 0; i < dm.destructibleParts.Length; i++)
            {
                var part = dm.destructibleParts[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(26));
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(part, typeof(DestructableBuilding), true);
                }

                if (part == null)
                {
                    DrawMessage(new VMsg(MessageType.Error, $"Part <b>[{i}]</b> is <b>NULL</b>.", dm));
                    continue;
                }

                var phased = part as DestructibleBuildingPhased;
                if (phased == null)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.IntSlider("Destruction phase (phased only)", 0, 0, 0);

                    DrawMessage(new VMsg(
                        MessageType.Info,
                        "Not a <b>DestructibleBuildingPhased</b>: slider not available for base DestructableBuilding.",
                        part
                    ));
                    EditorGUILayout.Space(4);
                    continue;
                }

                int phasesCount = phased.statusPhases != null ? phased.statusPhases.Length : 0;
                int maxPhase = Mathf.Max(0, phasesCount - 1);

                if (phasesCount <= 0)
                {
                    DrawMessage(new VMsg(MessageType.Error, "<b>statusPhases</b> is empty.", phased));
                    continue;
                }

                int key = phased.GetInstanceID();
                if (!phaseSlider.TryGetValue(key, out int current))
                {
                    current = Mathf.Clamp(phased.GetDestructionPhase(), 0, maxPhase);
                    phaseSlider[key] = current;
                }

                EditorGUI.BeginChangeCheck();
                int newPhase = EditorGUILayout.IntSlider("Destruction phase", current, 0, maxPhase);
                if (EditorGUI.EndChangeCheck())
                {
                    phaseSlider[key] = newPhase;

                    Undo.RecordObject(phased, "Set Destruction Phase");
                    phased.RefreshDestruction((sbyte)newPhase);
                    EditorUtility.SetDirty(phased);
                    SceneView.RepaintAll();
                }

                /*
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Intact (0)", GUILayout.Width(100)))
                        SetPhase(phased, 0);

                    using (new EditorGUI.DisabledScope(maxPhase == 0))
                    {
                        if (GUILayout.Button("Destroyed (last)", GUILayout.Width(130)))
                            SetPhase(phased, maxPhase);
                    }

                    if (GUILayout.Button("Refresh", GUILayout.Width(90)))
                    {
                        Undo.RecordObject(phased, "Refresh Destruction");
                        phased.RefreshDestruction(apply_destruction_effects: true);
                        EditorUtility.SetDirty(phased);
                        SceneView.RepaintAll();
                    }
                }

                EditorGUILayout.Space(6);*/
            }
        }
    }

    private void SetPhase(DestructibleBuildingPhased phased, int newPhase)
    {
        if (phased == null) return;

        int phasesCount = phased.statusPhases != null ? phased.statusPhases.Length : 0;
        int maxPhase = Mathf.Max(0, phasesCount - 1);
        newPhase = Mathf.Clamp(newPhase, 0, maxPhase);

        phaseSlider[phased.GetInstanceID()] = newPhase;

        Undo.RecordObject(phased, "Set Destruction Phase");
        phased.RefreshDestruction((sbyte)newPhase);
        EditorUtility.SetDirty(phased);
        SceneView.RepaintAll();
    }

    #endregion

    #region VALIDATION (FULL)

    private struct VMsg
    {
        public MessageType type;
        public string textRich;              // supports <b>...</b>
        public UnityEngine.Object ping;      // component or GO to select/ping
        public VMsg(MessageType t, string sRich, UnityEngine.Object p = null) { type = t; textRich = sRich; ping = p; }
    }

    private void DrawValidationSection()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            showValidation = EditorGUILayout.Foldout(showValidation, "Setup Validation", true);
            if (!showValidation) return;

            var messages = Validate(dm);

            if (messages.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues detected.", MessageType.Info);
                return;
            }

            int err = 0, warn = 0, info = 0;
            foreach (var m in messages)
            {
                if (m.type == MessageType.Error) err++;
                else if (m.type == MessageType.Warning) warn++;
                else info++;
            }

            EditorGUILayout.LabelField($"Found: {err} error(s), {warn} warning(s), {info} info.", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(4);

            foreach (var m in messages)
                DrawMessage(m);
        }
    }

    private void DrawMessage(VMsg m)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(GetIconForMessageType(m.type), GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(m.textRich, RichLabel);

            using (new EditorGUI.DisabledScope(m.ping == null))
            {
                if (GUILayout.Button("Select", GUILayout.Width(70), GUILayout.Height(20)))
                {
                    Selection.activeObject = m.ping;
                    EditorGUIUtility.PingObject(m.ping);
                }
            }
        }
        GUILayout.Space(2);
    }

    private GUIContent GetIconForMessageType(MessageType type)
    {
        switch (type)
        {
            case MessageType.Error: return EditorGUIUtility.IconContent("console.erroricon");
            case MessageType.Warning: return EditorGUIUtility.IconContent("console.warnicon");
            default: return EditorGUIUtility.IconContent("console.infoicon");
        }
    }

    private List<VMsg> Validate(DestructibleManager manager)
    {
        var msgs = new List<VMsg>();

        if (manager == null)
        {
            msgs.Add(new VMsg(MessageType.Error, "<b>DestructibleManager</b> is null."));
            return msgs;
        }

        //Sub Forniture Spawners
        if (manager.fornitureSpawners.Length > 0)
        {
            msgs.Add(new VMsg(MessageType.Info, $"<b>Forniture Spawners:</b> {manager.fornitureSpawners.Length}", manager.fornitureSpawners[0]));
        }
        else
            msgs.Add(new VMsg(MessageType.Info, $"No <b>Fortniture Spawners</b> found"));

        //Sub Doors managers
        if (manager.registeredDoors.Length > 0)
        {
            msgs.Add(new VMsg(MessageType.Info, $"<b>Doors:</b> {manager.registeredDoors.Length}", manager.registeredDoors[0]));
        }
        else
            msgs.Add(new VMsg(MessageType.Info, $"No <b>Doors</b> found"));

        if (manager.destructibleParts == null || manager.destructibleParts.Length == 0)
        {
            msgs.Add(new VMsg(MessageType.Warning, "<b>destructibleParts</b> is empty. Acquire destructible parts from children.", manager));
            return msgs;
        }

        // --- LODGroup check (usually on root) ---
        var lodOnRoot = manager.GetComponent<LODGroup>();
        if (lodOnRoot == null)
        {
            var lodAny = manager.GetComponentInChildren<LODGroup>(true);
            if (lodAny == null)
                msgs.Add(new VMsg(MessageType.Info, $"<b>LODGroup</b> missing under '<b>{manager.name}</b>' (usually expected on root).", manager));
        }

        // --- CombatCover diagnostics ---
        var covers = manager.GetComponentsInChildren<CombatCover>(true);
        if (covers == null || covers.Length == 0)
            msgs.Add(new VMsg(MessageType.Info, $"No <b>CombatCover</b> found under '<b>{manager.name}</b>'.", manager));
        else
            msgs.Add(new VMsg(MessageType.Info, $"<b>CombatCover</b> found: <b>{covers.Length}</b>.", manager));

        // --- Per-part checks ---
        for (int i = 0; i < manager.destructibleParts.Length; i++)
        {
            var part = manager.destructibleParts[i];
            if (part == null)
            {
                msgs.Add(new VMsg(MessageType.Error, $"Part <b>[{i}]</b> is <b>NULL</b> in destructibleParts array.", manager));
                continue;
            }

            var phased = part as DestructibleBuildingPhased;
            if (phased == null)
            {
                if (string.IsNullOrEmpty(part.onPhaseChangeEffect))
                    msgs.Add(new VMsg(MessageType.Info, $"[{i}] <b>{part.name}</b>: <b>onPhaseChangeEffect</b> is empty (often expected to be set).", part));

                continue;
            }

            // Phases sanity
            int phasesCount = phased.statusPhases != null ? phased.statusPhases.Length : 0;
            if (phasesCount < 2)
                msgs.Add(new VMsg(MessageType.Warning, $"[{i}] <b>{phased.name}</b>: <b>statusPhases</b> has <b>{phasesCount}</b> phase(s). < 2 usually makes no sense.", phased));

            for (int p = 0; p < phasesCount; p++)
            {
                var arr = phased.statusPhases[p].destructionLevel_objects;

                if (arr == null || arr.Length == 0)
                {
                    //msgs.Add(new VMsg(MessageType.Warning, $"[{i}] <b>{phased.name}</b>: Phase <b>{p}</b> has no assigned destructionLevel_objects.", phased));
                    continue;
                }

                bool anyNonNull = false;
                for (int k = 0; k < arr.Length; k++)
                {
                    if (arr[k] == null)
                    {
                        msgs.Add(new VMsg(MessageType.Warning, $"[{i}] <b>{phased.name}</b>: Phase <b>{p}</b> has <b>NULL</b> object at index <b>{k}</b>.", phased));
                        continue;
                    }
                    anyNonNull = true;
                }

                if (!anyNonNull)
                    msgs.Add(new VMsg(MessageType.Warning, $"[{i}] <b>{phased.name}</b>: Phase <b>{p}</b> contains only NULL references.", phased));
            }

            if (string.IsNullOrEmpty(phased.onPhaseChangeEffect))
                msgs.Add(new VMsg(MessageType.Info, $"[{i}] <b>{phased.name}</b>: <b>onPhaseChangeEffect</b> is empty (not always a bug, but usually set).", phased));

            // Colliders (non-trigger)
            var colls = phased.GetComponentsInChildren<Collider>(true);
            bool hasSolidCollider = false;
            foreach (var c in colls)
            {
                if (c == null) continue;
                if (!c.isTrigger) { hasSolidCollider = true; break; }
            }
            if (!hasSolidCollider)
                msgs.Add(new VMsg(MessageType.Warning, $"[{i}] <b>{phased.name}</b>: No <b>non-trigger colliders</b> found in children.", phased));

            // BuildingImpact scripts
            var impacts = phased.GetComponentsInChildren<BuildingImpact>(true);
            if (impacts == null || impacts.Length == 0)
                msgs.Add(new VMsg(MessageType.Warning, $"[{i}] <b>{phased.name}</b>: No <b>BuildingImpact</b> found in children.", phased));

            // Mesh / material / importer checks (strict)
            ValidateMeshesAndMaterials_Strict(phased.gameObject, $"[{i}] <b>{phased.name}</b>", msgs);
        }

        return msgs;
    }

    /// <summary>
    /// Strict checks:
    /// - Missing mesh on MeshFilter / SkinnedMeshRenderer
    /// - Renderer materials NULL
    /// - Materials that are NOT .mat assets
    /// - Materials that are sub-assets or located in model importer assets (.fbx/.blend/...)
    /// </summary>
    private void ValidateMeshesAndMaterials_Strict(GameObject root, string label, List<VMsg> msgs)
    {
        if (root == null) return;

        // MeshFilter missing mesh
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var f in filters)
        {
            if (f == null) continue;
            if (f.sharedMesh == null)
                msgs.Add(new VMsg(MessageType.Error, $"{label}: <b>Missing Mesh</b> (MeshFilter.sharedMesh is null) on '<b>{GetPath(f.transform, root.transform)}</b>'.", f));
        }

        // SkinnedMeshRenderer missing mesh
        var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in skinned)
        {
            if (s == null) continue;
            if (s.sharedMesh == null)
                msgs.Add(new VMsg(MessageType.Error, $"{label}: <b>Missing Mesh</b> (SkinnedMeshRenderer.sharedMesh is null) on '<b>{GetPath(s.transform, root.transform)}</b>'.", s));
        }

        // Renderer materials
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                msgs.Add(new VMsg(MessageType.Warning, $"{label}: Renderer has <b>no materials</b> on '<b>{GetPath(r.transform, root.transform)}</b>'.", r));
                continue;
            }

            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null)
                {
                    msgs.Add(new VMsg(MessageType.Error, $"{label}: <b>NULL material</b> slot <b>{i}</b> on '<b>{GetPath(r.transform, root.transform)}</b>'.", r));
                    continue;
                }

                string matPath = AssetDatabase.GetAssetPath(m);

                // Not an asset at all (scene instance / runtime / embedded)
                if (string.IsNullOrEmpty(matPath))
                {
                    msgs.Add(new VMsg(
                        MessageType.Warning,
                        $"{label}: Material '<b>{m.name}</b>' on '<b>{GetPath(r.transform, root.transform)}</b>' is <b>not an asset</b> (scene instance / generated / embedded). Should be a unique <b>.mat</b> asset.",
                        r
                    ));
                    continue;
                }

                string ext = Path.GetExtension(matPath).ToLowerInvariant();
                bool isMat = ext == ".mat";

                // Embedded/importer patterns
                bool isModelAsset = ext == ".fbx" || ext == ".blend" || ext == ".dae" || ext == ".obj";
                bool isSubAsset = AssetDatabase.IsSubAsset(m);

                if (!isMat)
                {
                    // Strong signal: material is a sub-asset OR path is the model itself.
                    if (isModelAsset || isSubAsset)
                    {
                        msgs.Add(new VMsg(
                            MessageType.Error,
                            $"{label}: <b>Importer material</b> '<b>{m.name}</b>' on '<b>{GetPath(r.transform, root.transform)}</b>' comes from '<b>{matPath}</b>'. Must be an external unique <b>.mat</b> in project.",
                            r
                        ));
                    }
                    else
                    {
                        msgs.Add(new VMsg(
                            MessageType.Warning,
                            $"{label}: Material '<b>{m.name}</b>' on '<b>{GetPath(r.transform, root.transform)}</b>' is <b>not a .mat</b> asset ('{matPath}'). Ensure it's a unique <b>.mat</b> in project.",
                            r
                        ));
                    }
                }
                else
                {
                    // Rare, but keep as warning
                    if (isSubAsset)
                    {
                        msgs.Add(new VMsg(
                            MessageType.Warning,
                            $"{label}: Material '<b>{m.name}</b>' is a <b>sub-asset</b> even though it looks like <b>.mat</b> ('{matPath}'). Ensure materials are external unique assets.",
                            r
                        ));
                    }
                }
            }
        }
    }

    private string GetPath(Transform t, Transform root)
    {
        if (t == null) return "<null>";
        if (root == null) return t.name;

        var stack = new List<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Add(cur.name);
            if (cur == root) break;
            cur = cur.parent;
        }
        stack.Reverse();
        return string.Join("/", stack);
    }

    #endregion

    private void RebuildSliderCache()
    {
        phaseSlider.Clear();

        if (dm == null || dm.destructibleParts == null) return;

        foreach (var part in dm.destructibleParts)
        {
            var phased = part as DestructibleBuildingPhased;
            if (phased == null) continue;

            int phasesCount = phased.statusPhases != null ? phased.statusPhases.Length : 0;
            int maxPhase = Mathf.Max(0, phasesCount - 1);
            phaseSlider[phased.GetInstanceID()] = Mathf.Clamp(phased.GetDestructionPhase(), 0, maxPhase);
        }
    }
}

[InitializeOnLoad]
public static class DestructibleManagerEditorHooks
{
    private static bool _isCapturing;

    static DestructibleManagerEditorHooks()
    {
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
    }


    private static void OnPrefabStageClosing(PrefabStage stage)
    {
        if (_isCapturing)
            return;

        if (stage == null || stage.prefabContentsRoot == null)
            return;

        _isCapturing = true;
        try
        {
            CaptureSpawnersUnderRoot(stage.prefabContentsRoot);
        }
        finally
        {
            _isCapturing = false;
        }
    }

    private static void CaptureSpawnersUnderRoot(GameObject root)
    {
        DestructibleManager manager = root.GetComponent<DestructibleManager>();

        if (manager == null)
            return;

        Undo.RegisterCompleteObjectUndo(manager, "Auto Capture Furniture Preview");
        manager.AcquireDestructibleParts();
        manager.CaptureFurnitureSpawners();
        manager.CaptureDoors();
        EditorUtility.SetDirty(manager);
    }

    public static bool IsInsidePrefabStage()
    {
        return PrefabStageUtility.GetCurrentPrefabStage() != null;
    }
}
public class DestructibleManagerSaveProcessor : AssetModificationProcessor
{
    private static string[] OnWillSaveAssets(string[] paths)
    {
        if (DestructibleManagerEditorHooks.IsInsidePrefabStage())
            return paths;

        DestructibleManager[] managers = Resources.FindObjectsOfTypeAll<DestructibleManager>();

        for (int i = 0; i < managers.Length; i++)
        {
            DestructibleManager manager = managers[i];
            if (manager == null)
                continue;

            if (EditorUtility.IsPersistent(manager))
                continue;

            Undo.RegisterCompleteObjectUndo(manager, "Auto Capture Furniture Preview");
            manager.AcquireDestructibleParts();
            manager.CaptureFurnitureSpawners();
            manager.CaptureDoors();
            EditorUtility.SetDirty(manager);
        }

        return paths;
    }
}

/*
//tool for process all buildings in case anything suddenly change
public static class DestructibleManagerBatchTools
{
    [MenuItem("Tools/Destruction/Reacquire All Destructible Managers")]
    public static void ReacquireAllDestructibleManagers()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int updated = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar(
                    "Reacquiring Destructible Managers",
                    path,
                    (float)i / guids.Length
                );

                GameObject root = null;

                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null)
                        continue;

                    var manager = root.GetComponent<DestructibleManager>();
                    if (manager == null)
                        continue;

                    Undo.RegisterCompleteObjectUndo(manager, "Batch Reacquire Destructible Manager");

                    // Run both if needed
                    manager.AcquireDestructibleParts();
                    manager.CaptureFurnitureSpawners();
                    manager.CaptureDoors();

                    EditorUtility.SetDirty(manager);

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed processing prefab: {path}\n{ex}");
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Reacquisition complete. Updated {updated} prefab(s).");
    }
}*/
#endif