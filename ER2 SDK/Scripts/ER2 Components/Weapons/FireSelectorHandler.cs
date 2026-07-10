// FireSelectorHandler.cs
using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class LeverPose
{
    public Vector3 localPosition;
    public Vector3 localRotation;
}

[System.Serializable]
public class SelectorLever
{
    public string leverName = "Lever";
    public Transform leverTransform;
    public LeverPose[] poses; // length == handler.StateCount
}

public partial class FireSelectorHandler : MonoBehaviour
{
    [Header("States")]
    [Range(1, 8)]
    public int stateCount = 2;

    [Header("Levers")]
    public SelectorLever[] levers;

    [Header("Animation")]
    [Range(0.05f, 0.5f)]
    public float transitionDuration = 0.18f;


    public void EditorSnapToState(int stateIndex)
    {
        if (levers == null) return;
        stateIndex = Mathf.Clamp(stateIndex, 0, stateCount - 1);
        foreach (var lever in levers)
        {
            if (lever.leverTransform == null) continue;
            if (lever.poses == null || stateIndex >= lever.poses.Length) continue;
            lever.leverTransform.localPosition = lever.poses[stateIndex].localPosition;
            lever.leverTransform.localRotation = Quaternion.Euler(lever.poses[stateIndex].localRotation);
        }
    }

    public void SyncPoseArrays()
    {
        if (levers == null) return;
        foreach (var lever in levers)
        {
            int current = lever.poses != null ? lever.poses.Length : 0;
            if (current == stateCount) continue;
            var newPoses = new LeverPose[stateCount];
            for (int i = 0; i < stateCount; i++)
                newPoses[i] = (i < current && lever.poses[i] != null)
                              ? lever.poses[i]
                              : new LeverPose();
            lever.poses = newPoses;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FireSelectorHandler))]
public class FireSelectorHandlerEditor : Editor
{
    private int    _previewState  = 0;
    private bool[] _leverFoldouts = new bool[0];

    // ── Gun detection ─────────────────────────────────────────────────

    /// <summary>
    /// If a GenericGun sits on the same GameObject, derive state labels
    /// and the expected state count from its fireMode / fire rates.
    /// Returns null when no gun is found (fall back to plain "State N").
    /// </summary>
    private static (string[] labels, int count)? GetLabelsFromGun(FireSelectorHandler fs)
    {
        GenericGun gun = fs.GetComponent<GenericGun>();
        if (gun == null) return null;

        // alternativeRoundsPerMinute in the serialized field contains only
        // the *extra* rates; Awake() prepends roundPerMinute at index 0.
        // We replicate that logic here without running Awake.
        int   baseRpm  = gun.roundPerMinute;
        int[] extraRpm = gun.alternativeRoundsPerMinute;

        // Full rpm list as it will be at runtime
        int[] allRpms = new int[1 + extraRpm.Length];
        allRpms[0] = baseRpm;
        for (int i = 0; i < extraRpm.Length; i++)
            allRpms[i + 1] = extraRpm[i];

        string RpmLabel(int rpm) => rpm >= 1000 ? $"{rpm / 1000f:0.#}k rpm" : $"{rpm} rpm";

        switch (gun.fireMode)
        {
            case FireSelector.Single:
            {
                return (new[] { "Single" }, 1);
            }

            case FireSelector.Auto:
            {
                if (allRpms.Length == 1)
                    return (new[] { $"Auto  {RpmLabel(allRpms[0])}" }, 1);

                var labels = new string[allRpms.Length];
                for (int i = 0; i < allRpms.Length; i++)
                    labels[i] = $"Auto  {RpmLabel(allRpms[i])}";
                return (labels, allRpms.Length);
            }

            case FireSelector.AutoOrSingle:
            case FireSelector.AutoOrSingle_DefaultSingle:
            {
                // State 0 = single, then one entry per auto fire rate
                var labels = new string[1 + allRpms.Length];
                labels[0] = "Single";
                for (int i = 0; i < allRpms.Length; i++)
                    labels[i + 1] = $"Auto  {RpmLabel(allRpms[i])}";
                return (labels, 1 + allRpms.Length);
            }
        }

        return null;
    }

    // ── Inspector ─────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        FireSelectorHandler fs = (FireSelectorHandler)target;

        // Try to read gun data — drives labels and the auto-sync button
        var gunData = GetLabelsFromGun(fs);
        bool hasGun = gunData.HasValue;

        // ── STATES ───────────────────────────────────────────────────
        EditorGUILayout.LabelField("STATES", EditorStyles.boldLabel);

        if (hasGun)
        {
            int  detectedCount = gunData.Value.count;
            bool mismatch      = fs.stateCount != detectedCount;

            EditorGUILayout.HelpBox(
                $"GenericGun detected  →  {detectedCount} state(s)" +
                (mismatch ? $"\n⚠ Current stateCount is {fs.stateCount} — use the button below to sync." : ""),
                mismatch ? MessageType.Warning : MessageType.Info);

            GUI.backgroundColor = mismatch ? new Color(1f, 0.6f, 0.4f) : new Color(0.6f, 1f, 0.7f);
            if (GUILayout.Button($"⟳  Sync from GenericGun  ({detectedCount} states)", GUILayout.Height(24)))
            {
                Undo.RecordObject(fs, "Sync from GenericGun");
                fs.stateCount = detectedCount;
                fs.SyncPoseArrays();
                EditorUtility.SetDirty(fs);
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stateCount"), new GUIContent("State Count"));
            serializedObject.ApplyModifiedProperties();

            GUI.backgroundColor = new Color(0.6f, 1f, 0.7f);
            if (GUILayout.Button("⟳  Sync Pose Arrays to State Count", GUILayout.Height(24)))
            {
                Undo.RecordObject(fs, "Sync Pose Arrays");
                fs.SyncPoseArrays();
                EditorUtility.SetDirty(fs);
            }
            GUI.backgroundColor = Color.white;
        }

        int sc = fs.stateCount;

        EditorGUILayout.Space(6);

        // ── LEVERS ───────────────────────────────────────────────────
        EditorGUILayout.LabelField("LEVERS", EditorStyles.boldLabel);
        serializedObject.Update();
        SerializedProperty leversProp = serializedObject.FindProperty("levers");

        if (_leverFoldouts.Length != leversProp.arraySize)
            System.Array.Resize(ref _leverFoldouts, leversProp.arraySize);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Lever"))
        {
            Undo.RecordObject(fs, "Add Lever");
            leversProp.arraySize++;
            var newLever = leversProp.GetArrayElementAtIndex(leversProp.arraySize - 1);
            newLever.FindPropertyRelative("leverName").stringValue = $"Lever {leversProp.arraySize}";
            newLever.FindPropertyRelative("leverTransform").objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            fs.SyncPoseArrays();
            EditorUtility.SetDirty(fs);
        }
        GUI.enabled = leversProp.arraySize > 0;
        if (GUILayout.Button("- Remove Last"))
        {
            Undo.RecordObject(fs, "Remove Lever");
            leversProp.arraySize--;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(fs);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(4);

        if (fs.levers != null)
        {
            // Build state labels once — reused inside the lever loop for capture buttons
            string[] stateLabels = BuildStateLabels(gunData, sc);

            for (int li = 0; li < fs.levers.Length; li++)
            {
                SelectorLever lever = fs.levers[li];
                string header = string.IsNullOrEmpty(lever.leverName) ? $"Lever {li}" : $"[{li}]  {lever.leverName}";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (li >= _leverFoldouts.Length)
                    System.Array.Resize(ref _leverFoldouts, li + 1);
                _leverFoldouts[li] = EditorGUILayout.Foldout(_leverFoldouts[li], header, true, EditorStyles.foldoutHeader);

                if (_leverFoldouts[li])
                {
                    EditorGUI.indentLevel++;

                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.TextField("Name", lever.leverName);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(fs, "Rename Lever");
                        lever.leverName = newName;
                        EditorUtility.SetDirty(fs);
                    }

                    EditorGUI.BeginChangeCheck();
                    Transform newT = (Transform)EditorGUILayout.ObjectField(
                        "Transform", lever.leverTransform, typeof(Transform), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(fs, "Assign Lever Transform");
                        lever.leverTransform = newT;
                        EditorUtility.SetDirty(fs);
                    }

                    EditorGUILayout.Space(4);

                    if (lever.poses == null || lever.poses.Length != sc)
                    {
                        EditorGUILayout.HelpBox("Esegui 'Sync Pose Arrays' per aggiornare le pose.", MessageType.Warning);
                    }
                    else
                    {
                        for (int si = 0; si < sc; si++)
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            EditorGUILayout.LabelField($"[{si}]  {stateLabels[si]}", EditorStyles.miniBoldLabel);

                            EditorGUI.BeginChangeCheck();
                            Vector3 newPos = EditorGUILayout.Vector3Field("Position", lever.poses[si].localPosition);
                            Vector3 newRot = EditorGUILayout.Vector3Field("Rotation", lever.poses[si].localRotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(fs, "Edit Lever Pose");
                                lever.poses[si].localPosition = newPos;
                                lever.poses[si].localRotation = newRot;
                                EditorUtility.SetDirty(fs);
                            }

                            GUI.enabled = lever.leverTransform != null;
                            GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
                            if (GUILayout.Button($"📷  Capture → [{si}] {stateLabels[si]}"))
                            {
                                Undo.RecordObject(fs, $"Capture Lever {li} State {si}");
                                lever.poses[si].localPosition = lever.leverTransform.localPosition;
                                lever.poses[si].localRotation = lever.leverTransform.localEulerAngles;
                                EditorUtility.SetDirty(fs);
                            }
                            GUI.backgroundColor = Color.white;
                            GUI.enabled = true;

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(2);
                        }
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }

        // ── ANIMATION ────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("ANIMATION", EditorStyles.boldLabel);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionDuration"));
        serializedObject.ApplyModifiedProperties();

        // ── PREVIEW ──────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("PREVIEW  (editor only, no play mode needed)", EditorStyles.boldLabel);

        string[] previewLabels = BuildStateLabels(gunData, sc);

        EditorGUI.BeginChangeCheck();
        _previewState = GUILayout.SelectionGrid(
            Mathf.Clamp(_previewState, 0, sc - 1),
            previewLabels,
            Mathf.Min(sc, 4));
        if (EditorGUI.EndChangeCheck())
        {
            if (fs.levers != null)
                foreach (var lever in fs.levers)
                    if (lever.leverTransform != null)
                        Undo.RecordObject(lever.leverTransform, "Preview FireSelector");
            fs.EditorSnapToState(_previewState);
        }
    }

    // Builds display labels for all N states, using gun data when available
    private static string[] BuildStateLabels((string[] labels, int count)? gunData, int sc)
    {
        string[] result = new string[sc];
        for (int i = 0; i < sc; i++)
        {
            if (gunData.HasValue && i < gunData.Value.labels.Length)
                result[i] = gunData.Value.labels[i];
            else
                result[i] = $"State {i}";
        }
        return result;
    }
}
#endif