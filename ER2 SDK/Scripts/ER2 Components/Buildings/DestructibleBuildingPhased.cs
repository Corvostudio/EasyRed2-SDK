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

        // La tua condizione originale
        if (string.IsNullOrEmpty(first.onPhaseChangeEffect) && first.dstVfxPosition != Vector3.zero)
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
        Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
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