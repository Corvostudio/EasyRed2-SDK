using UnityEditor;
using UnityEngine;

// Inspector of the foldable integrated bayonet. The folded (rest) pose is the one authored in the prefab and is never
// typed by hand: the tools below put it aside, let you move the transform in the scene to define the mounted pose,
// and put it back (on Save, Cancel, end of preview or deselection), so the prefab can never be left saved mounted.
[CustomEditor(typeof(AttachmentBayonet))]
public class AttachmentBayonetEditor : Editor
{
    private const string EditingKey = "ER2.Bayonet.EditingMounted.";// SessionState: survives domain reloads

    // animated preview (edit mode)
    private double previewStartTime = -1;
    private Vector3 previewFoldedPosition;
    private Quaternion previewFoldedRotation;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AttachmentBayonet bayonet = (AttachmentBayonet)target;
        if (!bayonet.foldable || Application.isPlaying)
            return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Mounted pose", EditorStyles.boldLabel);

        if (IsEditing(bayonet))
        {
            EditorGUILayout.HelpBox("Move the bayonet in the scene to where it sits when mounted, then Save. The folded pose is kept aside and comes back on Save, Cancel or deselect.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save mounted pose"))
                {
                    Undo.RecordObject(bayonet, "Mounted pose");
                    bayonet.mountedLocalPosition = bayonet.transform.localPosition;
                    bayonet.mountedLocalRotation = bayonet.transform.localEulerAngles;
                    EditorUtility.SetDirty(bayonet);
                    EndEditing(bayonet);
                }
                if (GUILayout.Button("Cancel"))
                    EndEditing(bayonet);
            }
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Edit mounted pose in scene"))
                BeginEditing(bayonet);

            bool previewing = previewStartTime >= 0;
            using (new EditorGUI.DisabledScope(!previewing && bayonet.foldSeconds <= 0))
            {
                if (GUILayout.Button(previewing ? "Stop preview" : "Preview mount / fold"))
                {
                    if (previewing)
                        StopPreview(bayonet);
                    else
                        StartPreview(bayonet);
                }
            }
        }
    }

    private void OnDisable()
    {
        // deselection or domain reload while editing/previewing: the folded pose must come back
        AttachmentBayonet bayonet = target as AttachmentBayonet;
        if (!bayonet)
            return;
        if (previewStartTime >= 0)
            StopPreview(bayonet);
        if (IsEditing(bayonet))
            EndEditing(bayonet);
    }

    // ---------------------------------------------------------------- edit mounted pose in scene

    private static string Key(AttachmentBayonet bayonet)
    {
        return EditingKey + GlobalObjectId.GetGlobalObjectIdSlow(bayonet);
    }

    private static bool IsEditing(AttachmentBayonet bayonet)
    {
        return SessionState.GetBool(Key(bayonet), false);
    }

    private static void BeginEditing(AttachmentBayonet bayonet)
    {
        Transform t = bayonet.transform;
        string key = Key(bayonet);
        SessionState.SetVector3(key + ".pos", t.localPosition);
        SessionState.SetVector3(key + ".rot", t.localEulerAngles);
        SessionState.SetBool(key, true);

        // a mounted pose already saved is a better starting point than the folded one
        if (bayonet.mountedLocalPosition != Vector3.zero || bayonet.mountedLocalRotation != Vector3.zero)
        {
            Undo.RecordObject(t, "Show mounted pose");
            t.localPosition = bayonet.mountedLocalPosition;
            t.localEulerAngles = bayonet.mountedLocalRotation;
        }
    }

    private static void EndEditing(AttachmentBayonet bayonet)
    {
        Transform t = bayonet.transform;
        string key = Key(bayonet);
        Undo.RecordObject(t, "Restore folded pose");
        t.localPosition = SessionState.GetVector3(key + ".pos", t.localPosition);
        t.localEulerAngles = SessionState.GetVector3(key + ".rot", t.localEulerAngles);
        SessionState.EraseBool(key);
        SessionState.EraseVector3(key + ".pos");
        SessionState.EraseVector3(key + ".rot");
    }

    // ---------------------------------------------------------------- animated preview

    private void StartPreview(AttachmentBayonet bayonet)
    {
        previewFoldedPosition = bayonet.transform.localPosition;
        previewFoldedRotation = bayonet.transform.localRotation;
        previewStartTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += PreviewTick;
    }

    private void StopPreview(AttachmentBayonet bayonet)
    {
        EditorApplication.update -= PreviewTick;
        previewStartTime = -1;
        bayonet.transform.localPosition = previewFoldedPosition;
        bayonet.transform.localRotation = previewFoldedRotation;
        SceneView.RepaintAll();
        Repaint();
    }

    private void PreviewTick()
    {
        AttachmentBayonet bayonet = target as AttachmentBayonet;
        if (!bayonet || bayonet.foldSeconds <= 0)
        {
            if (bayonet) StopPreview(bayonet);
            else { EditorApplication.update -= PreviewTick; previewStartTime = -1; }
            return;
        }

        // folded -> mounted -> folded, foldSeconds each way, then stop exactly on the folded pose
        float phase = (float)((EditorApplication.timeSinceStartup - previewStartTime) / bayonet.foldSeconds);
        if (phase >= 2f)
        {
            StopPreview(bayonet);
            return;
        }
        float t = phase < 1f ? phase : 2f - phase;
        bayonet.transform.localPosition = Vector3.Lerp(previewFoldedPosition, bayonet.mountedLocalPosition, t);
        bayonet.transform.localRotation = Quaternion.Slerp(previewFoldedRotation, Quaternion.Euler(bayonet.mountedLocalRotation), t);
        SceneView.RepaintAll();
    }
}
