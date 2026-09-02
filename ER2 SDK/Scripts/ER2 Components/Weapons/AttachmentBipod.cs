using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AttachmentBipod : Attachment
{
    [Header("Anchor (deployed pivot)")]

    [Tooltip("Physical hinge point of the bipod: while deployed, the weapon (and the soldier view) pivots around this node. If null, the attachment root transform is used.")]
    public Transform pivotPoint;

    [Tooltip("Height above the support surface at which the pivot (pivotPoint, or this attachment root if unset) rests while deployed (bipods are not all the same height). The FPS view/rig is softly pinned so the pivot sits exactly there.")]
    public float deployedPivotHeight = .28f;

    [Tooltip("Maximum yaw rotation (degrees, per side) around the deployed bipod. The actual usable arc can be smaller: obstacles are checked when deploying.")]
    [Range(5, 80)]
    public float maxYawRange = 30;

    [Tooltip("Maximum pitch rotation (degrees, per side) while deployed. The actual usable arc can be smaller: obstacles (ground, ceilings, the support itself) are checked when deploying.")]
    [Range(5, 60)]
    public float maxPitchRange = 25;

    [Tooltip("[Optional] Middle swivel node (the bipod legs): while deployed it is counter-rotated so the legs stay planted while the weapon rotates around them. Must not be the pivotPoint itself, and should not be keyframed in the open/close animation.")]
    public Transform swivelPivot;

#if UNITY_EDITOR
    //Placement gizmo: pivot (cyan), rest surface at deployedPivotHeight below it (yellow, with the leg span
    //used by the deploy scan), yaw fan (green), pitch fan (red), swivel node (magenta) 
    private void OnDrawGizmosSelected()
    {
        Transform pivot = pivotPoint ? pivotPoint : transform;
        Vector3 p = pivot.position;

        //approximate barrel direction (flattened, like the runtime sweep)
        Vector3 fwd = transform.forward;
        fwd.y = 0;
        fwd = fwd.sqrMagnitude > .001f ? fwd.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        //pivot point (the anchor everything rotates around)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p, .012f);
        Gizmos.DrawLine(p - right * .03f, p + right * .03f);
        Gizmos.DrawLine(p - fwd * .03f, p + fwd * .03f);

        //rest surface: while deployed the pivot sits deployedPivotHeight above it; box = leg span checked by the scan
        Vector3 surface = p + Vector3.down * deployedPivotHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p, surface);
        Vector3 a = surface - right * .12f - fwd * .05f;
        Vector3 b = surface + right * .12f - fwd * .05f;
        Vector3 c = surface + right * .12f + fwd * .05f;
        Vector3 d = surface - right * .12f + fwd * .05f;
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        UnityEditor.Handles.Label(surface + Vector3.up * .015f, "rest surface (h=" + deployedPivotHeight.ToString("0.00") + ")");

        //fan radius: pivot -> muzzle if available
        float radius = .45f;
        GenericGun parentGun = GetComponentInParent<GenericGun>();
        if (parentGun && parentGun.firePos)
            radius = Vector3.Distance(p, parentGun.firePos.position);

        //yaw fan around world up
        Vector3 yawFrom = Quaternion.AngleAxis(-maxYawRange, Vector3.up) * fwd;
        UnityEditor.Handles.color = new Color(.3f, 1f, .3f, .9f);
        UnityEditor.Handles.DrawWireArc(p, Vector3.up, yawFrom, maxYawRange * 2f, radius);
        UnityEditor.Handles.DrawLine(p, p + yawFrom * radius);
        UnityEditor.Handles.DrawLine(p, p + Quaternion.AngleAxis(maxYawRange, Vector3.up) * fwd * radius);

        //pitch fan around the lateral axis (at center yaw)
        Vector3 pitchFrom = Quaternion.AngleAxis(-maxPitchRange, right) * fwd;
        UnityEditor.Handles.color = new Color(1f, .4f, .3f, .9f);
        UnityEditor.Handles.DrawWireArc(p, right, pitchFrom, maxPitchRange * 2f, radius);
        UnityEditor.Handles.DrawLine(p, p + pitchFrom * radius);
        UnityEditor.Handles.DrawLine(p, p + Quaternion.AngleAxis(maxPitchRange, right) * fwd * radius);

        //swivel node (counter-rotated legs)
        if (swivelPivot && swivelPivot != pivotPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(swivelPivot.position, .01f);
            UnityEditor.Handles.Label(swivelPivot.position + Vector3.up * .015f, "swivel");
        }
    }
#endif
}
