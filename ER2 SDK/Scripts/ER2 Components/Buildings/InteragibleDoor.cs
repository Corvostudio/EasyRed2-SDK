using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class InteragibleDoor : Interagible
{
    [Tooltip("Moving parts of the door: one entry per leaf.")]
    public DoorPivot[] doors;
}

[System.Serializable]
public class DoorPivot
{
    [Tooltip("Transform rotated to open and close the leaf (its hinge).")]
    public Transform pivot;
    [Tooltip("Local X rotation of the pivot with the door closed, in degrees.")]
    public int closedRotX = 0;
    [Tooltip("Local Y rotation of the pivot with the door closed, in degrees.")]
    public int closedRotY = 0;
    [Tooltip("Local Z rotation of the pivot with the door closed, in degrees.")]
    public int closedRotZ = 0;
    [Tooltip("Local X rotation of the pivot with the door open, in degrees.")]
    public int openRotX = 0;
    [Tooltip("Local Y rotation of the pivot with the door open, in degrees.")]
    public int openRotY = 0;
    [Tooltip("Local Z rotation of the pivot with the door open, in degrees.")]
    public int openRotZ = 0;
}

#if UNITY_EDITOR

[CustomEditor(typeof(InteragibleDoor))]
[CanEditMultipleObjects]
public class InteragibleDoorEditor : Editor
{
    private class DoorPreviewAnimation
    {
        public InteragibleDoor door;
        public bool opening;
        public double startTime;
        public float duration;
    }

    private static readonly List<DoorPreviewAnimation> activeAnimations = new List<DoorPreviewAnimation>();
    private static bool updateRegistered = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Door Position Testing", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Test Closed Position", GUILayout.Height(30)))
        {
            AnimateDoorPosition(false);
        }

        if (GUILayout.Button("Test Open Position", GUILayout.Height(30)))
        {
            AnimateDoorPosition(true);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Set Current Position As:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Set as Closed Position", GUILayout.Height(25)))
        {
            SetCurrentAsPosition(false);
        }

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Set as Open Position", GUILayout.Height(25)))
        {
            SetCurrentAsPosition(true);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Test buttons: Preview door positions smoothly in the editor.\n" +
            "Set buttons: Save current door rotation as open/closed state.",
            MessageType.Info);
    }

    private void AnimateDoorPosition(bool open)
    {
        foreach (Object targetObject in targets)
        {
            InteragibleDoor door = targetObject as InteragibleDoor;
            if (door == null || door.doors == null)
                continue;

            Undo.RecordObject(door, open ? "Preview Open Door" : "Preview Closed Door");

            foreach (DoorPivot doorPivot in door.doors)
            {
                if (doorPivot != null && doorPivot.pivot != null)
                    Undo.RecordObject(doorPivot.pivot, open ? "Preview Open Door" : "Preview Closed Door");
            }

            activeAnimations.RemoveAll(a => a.door == door);

            activeAnimations.Add(new DoorPreviewAnimation
            {
                door = door,
                opening = open,
                startTime = EditorApplication.timeSinceStartup,
                duration = 0.5f
            });

            EditorUtility.SetDirty(door);
        }

        if (!updateRegistered)
        {
            EditorApplication.update += UpdateAnimations;
            updateRegistered = true;
        }
    }

    private static void UpdateAnimations()
    {
        double currentTime = EditorApplication.timeSinceStartup;

        for (int a = activeAnimations.Count - 1; a >= 0; a--)
        {
            DoorPreviewAnimation anim = activeAnimations[a];

            if (anim.door == null || anim.door.doors == null)
            {
                activeAnimations.RemoveAt(a);
                continue;
            }

            float t = Mathf.Clamp01((float)((currentTime - anim.startTime) / anim.duration));

            foreach (DoorPivot doorPivot in anim.door.doors)
            {
                if (doorPivot == null || doorPivot.pivot == null)
                    continue;

                Vector3 closedRot = new Vector3(
                    doorPivot.closedRotX,
                    doorPivot.closedRotY,
                    doorPivot.closedRotZ);

                Vector3 openRot = new Vector3(
                    doorPivot.openRotX,
                    doorPivot.openRotY,
                    doorPivot.openRotZ);

                Vector3 targetRot = anim.opening
                    ? Vector3.Lerp(closedRot, openRot, t)
                    : Vector3.Lerp(openRot, closedRot, t);

                doorPivot.pivot.localEulerAngles = targetRot;

                EditorUtility.SetDirty(doorPivot.pivot);
            }

            if (t >= 1f)
            {
                activeAnimations.RemoveAt(a);
            }
        }

        SceneView.RepaintAll();

        if (activeAnimations.Count == 0 && updateRegistered)
        {
            EditorApplication.update -= UpdateAnimations;
            updateRegistered = false;
        }
    }

    private void SetCurrentAsPosition(bool asOpen)
    {
        foreach (Object targetObject in targets)
        {
            InteragibleDoor door = targetObject as InteragibleDoor;
            if (door != null && door.doors != null)
            {
                Undo.RecordObject(door, asOpen ? "Set as Open Position" : "Set as Closed Position");

                foreach (DoorPivot doorPivot in door.doors)
                {
                    if (doorPivot != null && doorPivot.pivot != null)
                    {
                        SerializedObject pivotSO = new SerializedObject(doorPivot.pivot);
                        SerializedProperty eulerProp = pivotSO.FindProperty("m_LocalEulerAnglesHint");

                        Vector3 inspectorRotation = doorPivot.pivot.localRotation.eulerAngles;
                        if (eulerProp != null)
                            inspectorRotation = eulerProp.vector3Value;

                        if (asOpen)
                        {
                            doorPivot.openRotX = Mathf.RoundToInt(inspectorRotation.x);
                            doorPivot.openRotY = Mathf.RoundToInt(inspectorRotation.y);
                            doorPivot.openRotZ = Mathf.RoundToInt(inspectorRotation.z);
                        }
                        else
                        {
                            doorPivot.closedRotX = Mathf.RoundToInt(inspectorRotation.x);
                            doorPivot.closedRotY = Mathf.RoundToInt(inspectorRotation.y);
                            doorPivot.closedRotZ = Mathf.RoundToInt(inspectorRotation.z);
                        }
                    }
                }

                EditorUtility.SetDirty(door);
            }
        }

        Repaint();
    }
}
#endif