using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class VehViewPort : MonoBehaviour
{
    [Tooltip("Position of the camera on the view port")]
    public Transform camPos;
    [Tooltip("Shift position left/right on the camera when looking aroung")]
    [Range(-.4f, .4f)]
    public float camShiftOnRotate = .1f;

    private void OnDrawGizmosSelected()
    {
        if (camPos == null)
            return;

        Gizmos.color = Color.cyan;

        Vector3 center = camPos.position;
        Vector3 offset = camPos.right * camShiftOnRotate;

        Gizmos.DrawLine(center - offset, center + offset);
    }
}
