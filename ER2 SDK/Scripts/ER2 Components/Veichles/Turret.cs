using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using UnityEditor;
using UnityEngine;

public partial class Turret : MonoBehaviour
{
    [Tooltip("Max angle of rotation (left/right) of the turret. If 180, the turret can rotate 360")]
    [Range(0, 180)]
    public int yAxisBond;
    [Tooltip("Axis pivot for the Y rotation of the turret")]
    public GameObject yAxis;

    [Tooltip("Maximum angle of elevation of the gun on the turret")]
    [Range(-89, 90)]
    public int xAxisBond;
    [Tooltip("Maximum value of depression of the gun of the turret")]
    [Range(-89, 90)]
    public int xAxisBondDown = 10;
    [Tooltip("Pivot of elevation of the gun")]
    public GameObject xAxis;
    [Tooltip("Angle of standard zeroing of the gun. if very high (over 45°) it will behave as mortar.")]
    [Range(0, 90)]
    public float zeroingAngle = 0;

    [Tooltip("Look position of the camera")]
    public Transform lookPosition;

    [Tooltip("FOV when aming")]
    [Range(15, 65)]
    public float scopeFOV = 25;
    [Tooltip("Position of the camera when aiming")]
    public Transform scopePosition;
    [Tooltip("Sprites of the scope when aiming")]
    public string scopeSprite_id="tank_scope_2";

    [Tooltip("Rotation speed of the turret")]
    [Range(0, 100)]
    public float rotationSpeed = 8;
    [Tooltip("Camera rotates independently from the turret. Usefull if there is a turret parented to another turret")]
    public bool independentLookRotation = false;//camera ruota indipendentemente da mg

    [Tooltip("When true it will prioritize attacking planes with any of it's weapons. However for the case of bombers, it will prioritize them anyway if the gun has Time Fuse Flak ammos.")]
    public bool isAA = false;//when isAA=true AI will target planes first, other later, when isAA=false AI will not target planes at all

    [Tooltip("Increased night time AI visibility on this turret")]
    [Range(0, 1)] public float minimumNightVision = 0;

    [Tooltip("Rotation turn sound")]
    public AudioClip turnSoundLoop_outside;

    public static readonly byte MAX_TURRET_ROTATION = 180;

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Selection.gameObjects.Contains(gameObject))
            return;

        // reference frames (most accurate at rest pose; in play mode the arcs follow the live traverse)
        Vector3 turretUp = transform.up;
        Vector3 yawPivot = yAxis != null ? yAxis.transform.position : transform.position;
        Vector3 pitchPivot = xAxis != null ? xAxis.transform.position : yawPivot;

        Vector3 yawFwd = transform.forward;                                   // neutral traverse reference
        Vector3 elevRef = yAxis != null ? yAxis.transform.forward : transform.forward; // traverse-only forward (no elevation baked in)
        Vector3 pitchAxis = xAxis != null ? xAxis.transform.right
                                          : (yAxis != null ? yAxis.transform.right : transform.right);

        float traverseRadius = 3.5f;   // tweak to your turret scale
        float elevationRadius = 2.5f;

        // --- traverse (yaw) limits ---
        bool full360 = yAxisBond >= MAX_TURRET_ROTATION;
        Vector3 yawFrom = full360 ? yawFwd : Quaternion.AngleAxis(-yAxisBond, turretUp) * yawFwd;
        float yawSweep = full360 ? 360f : 2f * yAxisBond;

        UnityEditor.Handles.color = new Color(1f, 0.85f, 0.1f, 0.10f);
        UnityEditor.Handles.DrawSolidArc(yawPivot, turretUp, yawFrom, yawSweep, traverseRadius);
        UnityEditor.Handles.color = new Color(1f, 0.85f, 0.1f, 1f);
        UnityEditor.Handles.DrawWireArc(yawPivot, turretUp, yawFrom, yawSweep, traverseRadius);
        if (!full360)
        {
            UnityEditor.Handles.DrawLine(yawPivot, yawPivot + (Quaternion.AngleAxis(-yAxisBond, turretUp) * yawFwd) * traverseRadius);
            UnityEditor.Handles.DrawLine(yawPivot, yawPivot + (Quaternion.AngleAxis(yAxisBond, turretUp) * yawFwd) * traverseRadius);
        }
        UnityEditor.Handles.Label(yawPivot + yawFwd * traverseRadius, full360 ? "Traverse 360°" : $"Traverse ±{yAxisBond}°");

        // --- elevation / depression limits ---
        Vector3 upLimit = Quaternion.AngleAxis(-xAxisBond, pitchAxis) * elevRef; // nose up   (-xAxisBond)
        Vector3 downLimit = Quaternion.AngleAxis(xAxisBondDown, pitchAxis) * elevRef; // nose down (+xAxisBondDown)
        float elevSweep = xAxisBond + xAxisBondDown;

        UnityEditor.Handles.color = new Color(0.1f, 0.8f, 1f, 0.12f);
        UnityEditor.Handles.DrawSolidArc(pitchPivot, pitchAxis, upLimit, elevSweep, elevationRadius);
        UnityEditor.Handles.color = new Color(0.1f, 0.8f, 1f, 1f);
        UnityEditor.Handles.DrawWireArc(pitchPivot, pitchAxis, upLimit, elevSweep, elevationRadius);
        UnityEditor.Handles.DrawLine(pitchPivot, pitchPivot + upLimit * elevationRadius);
        UnityEditor.Handles.DrawLine(pitchPivot, pitchPivot + downLimit * elevationRadius);
        UnityEditor.Handles.Label(pitchPivot + upLimit * elevationRadius, $"Elev {xAxisBond}°");
        UnityEditor.Handles.Label(pitchPivot + downLimit * elevationRadius, $"Dep {xAxisBondDown}°");

        // --- standard zeroing direction ---
        if (zeroingAngle > 0.01f)
        {
            Vector3 zeroDir = Quaternion.AngleAxis(-zeroingAngle, pitchAxis) * elevRef;
            UnityEditor.Handles.color = new Color(1f, 0.2f, 0.6f, 1f);
            UnityEditor.Handles.DrawLine(pitchPivot, pitchPivot + zeroDir * (elevationRadius + 0.5f));
            UnityEditor.Handles.Label(pitchPivot + zeroDir * (elevationRadius + 0.6f), $"Zeroing {zeroingAngle:0.#}°");
        }

        // --- camera / scope markers ---
        if (lookPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lookPosition.position, 0.12f);
        }
        if (scopePosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(scopePosition.position, 0.12f);
            Gizmos.DrawLine(scopePosition.position, scopePosition.position + scopePosition.forward * 0.5f);
        }
    }
#endif

}