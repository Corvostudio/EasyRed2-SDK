using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class DestructibleBuildingPhased : DestructableBuilding
{
    [Tooltip("Defines all the possible integrity levels (status) of the buildings; First level must be intact, last must be fully destroyed.")]
    public BuildingStatus[] statusPhases = new BuildingStatus[0];

    public void RefreshDestruction(int current_phase)
    {
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
        bool mismatch = false;
        int firstLen = ((DestructibleBuildingPhased)targets[0]).statusPhases != null
            ? ((DestructibleBuildingPhased)targets[0]).statusPhases.Length
            : 0;

        foreach (var t in targets)
        {
            var b = (DestructibleBuildingPhased)t;
            int len = (b.statusPhases != null) ? b.statusPhases.Length : 0;
            if (len != firstLen) { mismatch = true; break; }
        }

        if (mismatch)
            EditorGUILayout.HelpBox("Attenzione: gli oggetti selezionati hanno statusPhases con lunghezze diverse. Mostro solo le fasi comuni.", MessageType.Warning);

        for (int i = 0; i < commonPhases; i++)
        {
            if (GUILayout.Button($"Set destruction phase {i} (tutti selezionati)"))
            {
                Undo.RecordObjects(targets, "Set Destruction Phase");
                foreach (var t in targets)
                {
                    var b = (DestructibleBuildingPhased)t;

                    bool done = false;
                    ApplySetDestructionPhase((sbyte)i, true, true, b, ref done);
                    if (!done) TestDestructionSimple((sbyte)i, b);

                    EditorUtility.SetDirty(b);
                }
            }
        }
    }
}
#endif