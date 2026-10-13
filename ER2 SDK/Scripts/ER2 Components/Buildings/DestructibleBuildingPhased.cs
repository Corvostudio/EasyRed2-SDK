using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class DestructibleBuildingPhased : DestructableBuilding
{
    [Tooltip("Defines all the possible integrity levels (status) of the buildings; First level must be intact, last must be fully destroyed.")]
    public BuildingStatus[] statusPhases = new BuildingStatus[0];
    private sbyte phase = 0;

    public void RefreshDestruction(sbyte current_phase)
    {
        phase = current_phase;
        for (int i = 0; i < statusPhases.Length; i++)
        {
            bool setActive = (i == current_phase);

            foreach (GameObject go in statusPhases[i].destructionLevel_objects)
            {
                if (go != null && go.gameObject.activeSelf != setActive)
                {
                    go.SetActive(setActive);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(go);
#endif
                }
            }
        }
    }

    public sbyte GetDestructionPhase()
    {
        return phase;
    }
}
[System.Serializable]
public struct BuildingStatus
{
    [Tooltip("The models that should be enabled in the hierachy when the building is set to this status.")]
    public GameObject[] destructionLevel_objects;
}



#if UNITY_EDITOR
[CustomEditor(typeof(DestructibleBuildingPhased)), CanEditMultipleObjects]
public partial class DestructibleBuildingPhasedEditor : Editor
{
    private int phaseSlider = 0;

    //---------------------------------------------------------------------------------------------------
    // Destruction VFX: an orange handle of its own (never the look of the transform tool) and a test that
    // spawns the effect in edit mode where the game would, runs it by hand and removes it.
    //---------------------------------------------------------------------------------------------------
    private static readonly Color vfxHandleColor = new Color(1f, .45f, 0f);
    private static GameObject vfxTest;
    private static ParticleSystem[] vfxTestParticles;
    private static double vfxTestLastTime, vfxTestEndTime;

    private static Vector3 VfxPositionHandle(Vector3 worldPos, Vector3 pivot)
    {
        float size = HandleUtility.GetHandleSize(worldPos);
        Handles.color = new Color(vfxHandleColor.r, vfxHandleColor.g, vfxHandleColor.b, .6f);
        Handles.DrawDottedLine(pivot, worldPos, 4f);//from the pivot of the object: tells the two handles apart
        Handles.color = vfxHandleColor;
        Handles.SphereHandleCap(0, worldPos, Quaternion.identity, size * .16f, EventType.Repaint);
        Handles.Label(worldPos + Vector3.up * size * .4f, "Destruction VFX", EditorStyles.whiteBoldLabel);
        Vector3 result = worldPos;
        result += Handles.Slider(worldPos, Vector3.right, size * .75f, Handles.ArrowHandleCap, 0f) - worldPos;
        result += Handles.Slider(worldPos, Vector3.up, size * .75f, Handles.ArrowHandleCap, 0f) - worldPos;
        result += Handles.Slider(worldPos, Vector3.forward, size * .75f, Handles.ArrowHandleCap, 0f) - worldPos;
        Handles.color = Color.white;
        return result;
    }

    private void DrawVfxTest()
    {
        var b = (DestructibleBuildingPhased)target;
        EditorGUILayout.LabelField("Destruction VFX", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(targets.Length != 1 || string.IsNullOrEmpty(b.onPhaseChangeEffect)))
                if (GUILayout.Button(vfxTest ? "Restart VFX" : "Test VFX in scene"))//while playing: stops and starts over
                    StartVfxTest(b);
            using (new EditorGUI.DisabledScope(!vfxTest))
                if (GUILayout.Button("Stop", GUILayout.Width(60f)))
                    StopVfxTest();
        }
        if (vfxTest)
            EditorGUILayout.HelpBox("Playing where the game spawns it: Dst Vfx Position, with the rotation and scale of the object. It goes away by itself.", MessageType.Info);
        else if (targets.Length == 1 && string.IsNullOrEmpty(b.onPhaseChangeEffect))
            EditorGUILayout.HelpBox("No effect assigned: the orange VFX handle appears in the scene once On Phase Change Effect is set.", MessageType.None);
        EditorGUILayout.Space();
    }

    private static void StartVfxTest(DestructibleBuildingPhased b)
    {
        StopVfxTest();
        GameObject prefab = FindPrefab(b.onPhaseChangeEffect);
        if (!prefab)
        {
            Debug.LogWarning($"Destruction VFX test: no prefab named '{b.onPhaseChangeEffect}' in the project", b);
            return;
        }
        //as PlayPhaseChangeFX does in game
        vfxTest = Object.Instantiate(prefab, b.transform.TransformPoint(b.dstVfxPosition), b.transform.rotation);
        vfxTest.name = prefab.name + " (VFX test)";
        vfxTest.transform.localScale = b.transform.localScale;
        vfxTest.hideFlags = HideFlags.HideAndDontSave;
        UnityEditor.SceneManagement.StageUtility.PlaceGameObjectInCurrentStage(vfxTest);//prefab mode too
        vfxTestParticles = vfxTest.GetComponentsInChildren<ParticleSystem>(true);
        float duration = 5f;
        foreach (ParticleSystem ps in vfxTestParticles)
        {
            ps.Simulate(0f, false, true);//from the start, by hand: edit mode does not run them
            duration = Mathf.Max(duration, ps.main.startDelay.constantMax + ps.main.duration + ps.main.startLifetime.constantMax);
        }
        vfxTestLastTime = EditorApplication.timeSinceStartup;
        vfxTestEndTime = vfxTestLastTime + Mathf.Min(duration, 20f);
        EditorApplication.update += UpdateVfxTest;
        EditorApplication.playModeStateChanged += StopVfxTest;
        AssemblyReloadEvents.beforeAssemblyReload += StopVfxTest;
    }
    private static void UpdateVfxTest()
    {
        if (!vfxTest || EditorApplication.timeSinceStartup >= vfxTestEndTime)
        {
            StopVfxTest();
            return;
        }
        float dt = (float)(EditorApplication.timeSinceStartup - vfxTestLastTime);
        vfxTestLastTime = EditorApplication.timeSinceStartup;
        foreach (ParticleSystem ps in vfxTestParticles)
            if (ps)
                ps.Simulate(dt, false, false);
        SceneView.RepaintAll();
    }
    private static void StopVfxTest(PlayModeStateChange change) { StopVfxTest(); }
    private static void StopVfxTest()
    {
        EditorApplication.update -= UpdateVfxTest;
        EditorApplication.playModeStateChanged -= StopVfxTest;
        AssemblyReloadEvents.beforeAssemblyReload -= StopVfxTest;
        if (vfxTest)
            Object.DestroyImmediate(vfxTest);
        vfxTest = null;
        vfxTestParticles = null;
        SceneView.RepaintAll();
    }
    private static GameObject FindPrefab(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    partial void ApplySetDestructionPhase(sbyte phase, bool keepLifevalue, bool apply_destruction_around, DestructibleBuildingPhased b, ref bool done);

    private void TestDestructionSimple(sbyte phase, DestructibleBuildingPhased b)
    {
        for (int i = 0; i < b.statusPhases.Length; i++)
        {
            bool setActive = (i == phase);

            foreach (GameObject go in b.statusPhases[i].destructionLevel_objects)
            {
                if (go != null && go.gameObject.activeSelf != setActive)
                {
                    go.SetActive(setActive);
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        var first = (DestructibleBuildingPhased)target;
        if (first == null) return;

        //no effect, nothing to place: and the VFX handle never sits on the transform tool of the object
        if (string.IsNullOrEmpty(first.onPhaseChangeEffect))
            return;

        // Prendi tutti i DestructibleBuildingPhased selezionati (multi-edit) senza usare targets
        var selected = Selection.GetFiltered<DestructibleBuildingPhased>(
            SelectionMode.Editable | SelectionMode.ExcludePrefab
        );

        // Fallback: se per qualche motivo non risulta selezione, applica solo a "target"
        if (selected == null || selected.Length == 0)
            selected = new[] { first };

        Vector3 worldPos = first.transform.TransformPoint(first.dstVfxPosition);

        EditorGUI.BeginChangeCheck();
        Vector3 newWorldPos = VfxPositionHandle(worldPos, first.transform.position);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(selected, "Move Target Position");

            Vector3 deltaWorld = newWorldPos - worldPos;

            foreach (var b in selected)
            {
                if (b == null) continue;

                Vector3 bWorld = b.transform.TransformPoint(b.dstVfxPosition);
                b.dstVfxPosition = b.transform.InverseTransformPoint(bWorld + deltaWorld);
                EditorUtility.SetDirty(b);
            }
        }
    }


    public override void OnInspectorGUI()
    {
        if (targets.Length == 1)
        {
            var b = (DestructibleBuildingPhased)targets[0];
            if (!b.gameObject.GetComponentInParent<DestructibleManager>(true))
                EditorGUILayout.HelpBox("Missing DestructibleManager on the root of the prop!", MessageType.Error);
        }


        // Default inspector (supporta multi-edit automaticamente per i campi serializzati)
        DrawDefaultInspector();

        EditorGUILayout.Space();
        DrawVfxTest();

        // Trova un numero di fasi "comune" per evitare out-of-range se selezioni oggetti diversi
        int commonPhases = int.MaxValue;
        foreach (var t in targets)
        {
            var b = (DestructibleBuildingPhased)t;
            int len = (b.statusPhases != null) ? b.statusPhases.Length : 0;
            commonPhases = Mathf.Min(commonPhases, len);
        }

        if (commonPhases <= 0)
        {
            EditorGUILayout.HelpBox("Nessuna fase disponibile (statusPhases vuoto su uno o più oggetti selezionati).", MessageType.Info);
            return;
        }

        // Info se le lunghezze differiscono
        int firstLen = ((DestructibleBuildingPhased)targets[0]).statusPhases != null
            ? ((DestructibleBuildingPhased)targets[0]).statusPhases.Length
            : 0;

        if (targets.Length == 1)
        {
            var b = (DestructibleBuildingPhased)targets[0];

            int len = (b.statusPhases != null) ? b.statusPhases.Length : 0;
            if (len != firstLen) { return; }


            // slider
            int phasesCount = b.statusPhases != null ? b.statusPhases.Length : 0;
            int maxPhase = Mathf.Max(0, phasesCount - 1);

            if (phasesCount <= 0)
            {
                EditorGUILayout.LabelField("<b>statusPhases</b> is empty.");
                return;
            }

            int key = b.GetInstanceID();
            int current = Mathf.Clamp(b.GetDestructionPhase(), 0, maxPhase);
            phaseSlider = current;

            EditorGUI.BeginChangeCheck();
            int newPhase = EditorGUILayout.IntSlider("Destruction phase", current, 0, maxPhase);
            if (EditorGUI.EndChangeCheck())
            {
                phaseSlider = newPhase;

                Undo.RecordObject(b, "Set Destruction Phase");
                b.RefreshDestruction((sbyte)newPhase);
                EditorUtility.SetDirty(b);
                SceneView.RepaintAll();
            }
        }
    }
}
#endif