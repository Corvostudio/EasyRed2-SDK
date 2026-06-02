using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AdjustableSight : MonoBehaviour
{
    [Header("Position")]
    [FormerlySerializedAs("at100MetersShiftPos")]
    public Vector3 minPos;
    [FormerlySerializedAs("at500MetersShiftPos")]
    public Vector3 maxPos;

    [Header("Rotation")]
    [FormerlySerializedAs("at100MetersShiftRot")]
    public Vector3 minRot;
    [FormerlySerializedAs("at500MetersShiftRot")]
    public Vector3 maxRot;

    [Tooltip("At which level of zeroing the adjustable sight should maximise its position/rotation? If 0, uses the max zeroing level.")]
    [Range(0, 20)]
    public int maximiseAt = 0;

    [Tooltip("Also adjust another sight when adjusting this one")]
    public AdjustableSight connected_sight = null;

#if UNITY_EDITOR
    // Editor-only visual preview: lerps the transform between min and max.
    public void EditorPreview(float t)
    {
        transform.localPosition = Vector3.Lerp(minPos, maxPos, t);
        transform.localRotation = Quaternion.Euler(Vector3.Lerp(minRot, maxRot, t));
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(AdjustableSight))]
public class AdjustableSightEditor : Editor
{
    private float previewT;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AdjustableSight sight = (AdjustableSight)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Zeroing Preview (editor only)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        previewT = EditorGUILayout.Slider("Position", previewT, 0f, 1f);
        bool changed = EditorGUI.EndChangeCheck();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Min")) { previewT = 0f; changed = true; }
            if (GUILayout.Button("Mid")) { previewT = 0.5f; changed = true; }
            if (GUILayout.Button("Max")) { previewT = 1f; changed = true; }
        }

        if (changed)
        {
            Undo.RecordObject(sight.transform, "Adjustable Sight Preview");
            sight.EditorPreview(previewT);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Record current pose (editor only)", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Set Min from current"))
            {
                Undo.RecordObject(sight, "Set Adjustable Sight Min");
                sight.minPos = sight.transform.localPosition;
                sight.minRot = sight.transform.localEulerAngles;
                EditorUtility.SetDirty(sight);
            }
            if (GUILayout.Button("Set Max from current"))
            {
                Undo.RecordObject(sight, "Set Adjustable Sight Max");
                sight.maxPos = sight.transform.localPosition;
                sight.maxRot = sight.transform.localEulerAngles;
                EditorUtility.SetDirty(sight);
            }
        }
    }
}
#endif