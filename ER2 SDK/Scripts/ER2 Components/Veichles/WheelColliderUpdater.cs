using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(WheelCollider))]
public partial class WheelColliderUpdater : MonoBehaviour
{
    [Tooltip("Steering angle of this wheel at full input, in degrees. 0 = the wheel does not steer; a negative value steers the opposite way.")]
    [Range(-70, 70)]
    public float steer_angle = 0;

    [Tooltip("Wheel meshes that follow this wheel collider: they spin and steer with it.")]
    public Transform[] wheelsTransform;
    [Tooltip("Wheel meshes that only spin with this wheel collider, without steering.")]
    public Transform[] wheelsTransform_syncOnlyRot;

    [Tooltip("Optional bone of a skinned track: it moves up and down with the suspension of this wheel.")]
    public Transform connectedBone;


    [Tooltip("Side of the vehicle this wheel collider is on.")]
    public WheelColliderSide side = WheelColliderSide.Left;


    [System.NonSerialized] private WheelCollider wheel;


#if UNITY_EDITOR
    public static bool IsSceneCameraClose(Vector3 worldPos, float maxDistance)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
            return false;

        float dist = Vector3.Distance(sv.camera.transform.position, worldPos);
        return dist <= maxDistance;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Selection.gameObjects.Contains(gameObject))
            return;

        bool isCameraVeryClose = IsSceneCameraClose(transform.position, 50f);
        if (!isCameraVeryClose)
            return;

        bool isCameraClose = IsSceneCameraClose(transform.position, 10f);

        // Try to cache the wheel collider in editor too
        if (wheel == null)
            wheel = GetComponent<WheelCollider>();

        // Wheels with suspension (main visual wheels)
        if (wheelsTransform != null)
        {
            Gizmos.color = Color.green;
            foreach (var t in wheelsTransform)
            {
                if (t == null) continue;

                Gizmos.DrawWireSphere(t.position, 0.05f);
                Gizmos.DrawLine(transform.position, t.position);

                if (isCameraClose)
                    Handles.Label(t.position + Vector3.up * 0.03f, "Wheel");
            }
        }

        // Wheels that only sync rotation
        if (wheelsTransform_syncOnlyRot != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var t in wheelsTransform_syncOnlyRot)
            {
                if (t == null) continue;

                Gizmos.DrawWireCube(t.position, Vector3.one * 0.1f);
                Gizmos.DrawLine(transform.position, t.position);

                if (isCameraClose)
                    Handles.Label(t.position + Vector3.up * 0.03f, "RotOnly");
            }
        }

        // Connected bone (e.g. suspension arm)
        if (connectedBone != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(connectedBone.position, Vector3.one * 0.1f);
            Gizmos.DrawLine(transform.position, connectedBone.position);

            if (isCameraClose)
                Handles.Label(connectedBone.position + Vector3.up * 0.03f, "Bone");
        }
    }
#endif
}


public enum WheelColliderSide
{
    Left = 0,
    Right = 1
}