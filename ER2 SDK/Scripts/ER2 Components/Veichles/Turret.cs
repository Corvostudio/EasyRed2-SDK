using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using UnityEditor;
using UnityEngine;

public partial class Turret : MonoBehaviour
{
    [Header("Traverse")]
    [Tooltip("Pivot object of the turret's horizontal rotation (traverse).")]
    public GameObject yAxis;
    [Tooltip("How far the turret turns left and right from the centre, in degrees to each side. 180 = full 360 degree traverse.")]
    [Range(0, 180)]
    public int yAxisBond;

    [Header("Elevation")]
    [Tooltip("Pivot object of the gun's vertical rotation (elevation).")]
    public GameObject xAxis;
    [Tooltip("Maximum elevation of the gun, in degrees.")]
    [Range(-89, 90)]
    public int xAxisBond;
    [Tooltip("Maximum depression of the gun, in degrees.")]
    [Range(-89, 90)]
    public int xAxisBondDown = 10;
    [Tooltip("Angle of standard zeroing of the gun. if very high (over 45°) it will behave as mortar.")]
    [Range(0, 90)]
    public float zeroingAngle = 0;

    [Header("Movement")]
    [Tooltip("Traverse and elevation speed, in degrees per second. It drops by 40% when the vehicle's engine is destroyed.")]
    [Range(0, 100)]
    public float rotationSpeed = 8;
    [Tooltip("Loop played while the turret is turning, heard up to 30 m.")]
    public AudioClip turnSoundLoop_outside;

    [Header("Camera and sight")]
    [Tooltip("Where the first-person camera sits when the gunner is not aiming.")]
    public Transform lookPosition;
    [Tooltip("The camera turns on its own instead of following the turret. Useful for a turret mounted on another turret.")]
    public bool independentLookRotation = false;//camera ruota indipendentemente da mg
    [Tooltip("Where the camera sits while aiming.")]
    public Transform scopePosition;
    [Tooltip("Field of view while aiming, in degrees.")]
    [Range(15, 65)]
    public float scopeFOV = 25;
    [Tooltip("Name of the Tank Scope Data asset (in the er2gui bundle) drawn as the sight while aiming.")]
    public string scopeSprite_id="tank_scope_2";
    [Tooltip("If enabled and the assigned Tank Scope Data (scopeSprite_id) defines a FOV > 0, that FOV is used when aiming instead of this turret's scopeFOV.")]
    public bool useScopeDataFOV = false;

    [Header("AI")]
    [Tooltip("Anti-aircraft turret: the AI gunner attacks planes first. Other turrets attack planes only with time-fuse flak ammo.")]
    public bool isAA = false;//when isAA=true AI will target planes first, other later, when isAA=false AI will not target planes at all
    [Tooltip("Minimum night vision of the AI gunner on this turret: 0 = as dark as the night is, 1 = he sees as by day.")]
    [Range(0, 1)] public float minimumNightVision = 0;

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