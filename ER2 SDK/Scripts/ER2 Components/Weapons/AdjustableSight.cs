using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdjustableSight : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("Sight shift position for 100mt")]
    public Vector3 at100MetersShiftPos;
    [Tooltip("Sight shift position for 500mt")]
    public Vector3 at500MetersShiftPos;
    [Header("Rotation")]
    [Tooltip("Sight shift rotation for 100mt")]
    public Vector3 at100MetersShiftRot;
    [Tooltip("Sight shift rotation for 500mt")]
    public Vector3 at500MetersShiftRot;
    
    [Header("Max Parameters")]
    [Tooltip("Sight shift maximum distance")]
    [Range(0, 3000)]
    public float maxShiftDistance = 800;

}
