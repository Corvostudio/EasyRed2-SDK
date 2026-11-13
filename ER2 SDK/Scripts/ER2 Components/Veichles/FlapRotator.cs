using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class FlapRotator : MonoBehaviour
{
    [Tooltip("Defines the plane connected to this flap")]
    public VehiclePlane connectedPlane;

    public enum AxisDirection
    {
        pitch = 0,
        roll = 1,
        yaw = 2
    }
    [Tooltip("Which control moves this flap: Pitch (elevator), Roll (aileron) or Yaw (rudder).")]
    public AxisDirection axisDirection = 0;

    [Tooltip("Local axis the flap turns around: 0 = X, 1 = Y, 2 = Z.")]
    [Range(0, 2)]
    public byte axisFlap = 0;
    [Tooltip("Flap angle per unit of turn rate, with simplified controls (negative to invert). With realistic controls only its sign counts.")]
    [Range(-90, 90)]
    public float rotationMultiplier = 45;
    [Tooltip("Maximum deflection of the flap, in degrees to each side.")]
    [Range(0, 90)]
    public float rotationClamp = 45;

}

#if UNITY_EDITOR
[CustomEditor(typeof(FlapRotator))]
public class FlapRotatorEditor : Editor
{
    private float testAngle;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FlapRotator flap = (FlapRotator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Test", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        testAngle = EditorGUILayout.Slider(
            "Test Angle",
            testAngle,
            -flap.rotationClamp,
            flap.rotationClamp
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(flap.transform, "Flap Rotation Preview");

            Vector3 angles = Vector3.zero;

            if (flap.axisFlap == 0)
                angles.x = testAngle;
            else if (flap.axisFlap == 1)
                angles.y = testAngle;
            else if (flap.axisFlap == 2)
                angles.z = testAngle;

            flap.transform.localRotation = Quaternion.Euler(angles);

            EditorUtility.SetDirty(flap.transform);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Reset Rotation"))
        {
            Undo.RecordObject(flap.transform, "Reset Flap Rotation");

            flap.transform.localRotation = Quaternion.identity;
            testAngle = 0f;

            EditorUtility.SetDirty(flap.transform);
            SceneView.RepaintAll();
        }
    }
}
#endif