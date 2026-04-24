#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleSeat))]
[CanEditMultipleObjects]
public partial class VehicleSeatEditor : Editor
{
    private const string GUIDE_URL =
        "https://wiki.easyred2.com/page.php?slug=string-tables-for-custom-properties#section-606";

    // Cache properties (so we can selectively draw/hide fields)
    private SerializedProperty _onSeatAnimatorId;
    private SerializedProperty _canCrouch;
    private SerializedProperty _manualPreviewType; // only exists in editor builds in your VehicleSeat

    private void OnEnable()
    {
        _onSeatAnimatorId = serializedObject.FindProperty("onSeatAnimator_id");
        _canCrouch = serializedObject.FindProperty("canCrouch");
        _manualPreviewType = serializedObject.FindProperty("manualPreviewType"); // may be null if you renamed it
    }

    partial void OnInspectorGUIPrivate();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Draw core fields (and optionally hide manual preview when rule is found) ---

        if (_onSeatAnimatorId != null) EditorGUILayout.PropertyField(_onSeatAnimatorId);
        if (_canCrouch != null) EditorGUILayout.PropertyField(_canCrouch);

        // Determine mapping status across selection
        int total = targets != null ? targets.Length : 0;
        int mapped = 0;

        if (targets != null)
        {
            foreach (var obj in targets)
            {
                var seat = obj as VehicleSeat;
                if (!seat) continue;

                if (GUILayout.Button("Make sitted soldier " + (seat.gameObject.activeSelf ? "hidden" : "visible")))
                    seat.gameObject.SetActive(!seat.gameObject.activeSelf);

                // Uses your VehicleSeat.TryGetRule(animatorId, out rule)
                if (VehicleSeat.TryGetRule(seat.onSeatAnimator_id, out _))
                    mapped++;
            }
        }

        bool allMapped = (total > 0 && mapped == total);
        bool someMapped = (mapped > 0 && mapped < total);

        // If rule exists for all selected, hide the manual preview picker
        if (allMapped)
        {
            EditorGUILayout.HelpBox(
                "Preview is driven by Animator Id mapping (rule table). Manual preview is hidden.",
                MessageType.Info);
        }
        else
        {
            if (someMapped)
            {
                EditorGUILayout.HelpBox(
                    $"Some selected seats ({mapped}/{total}) have mapped Animator Ids.\n" +
                    "Mapped seats ignore the manual preview setting; unmapped seats use the fallback.",
                    MessageType.Warning);
            }

            // Show fallback manual preview (only if the property exists)
            if (_manualPreviewType != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_manualPreviewType, new GUIContent("Editor Preview (Fallback)"));
            }
        }

        serializedObject.ApplyModifiedProperties();

        // --- Extra UI (warning + help) ---
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Seat Origin (Transform Position) is the reference point used by the new system:\n" +
            "� where feet touch the ground OR where the butt sits on the seat.\n" +
            "Orient the seat so +Z (forward) matches the sitting direction.",
            MessageType.Warning);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("String table / Guide", GUILayout.Width(110)))
            {
                Application.OpenURL(GUIDE_URL);
            }
        }

        //editor part
        OnInspectorGUIPrivate();
    }

    // Keep this minimal since your gizmo already draws direction.
    // (If you want: we can show ONLY text here too, but you already do Handles.Label in VehicleSeat.)
    private void OnSceneGUI()
    {
        // Intentionally empty / minimal.
        // Your VehicleSeat gizmo+label already does the job without extra clutter.
    }

}
#endif
