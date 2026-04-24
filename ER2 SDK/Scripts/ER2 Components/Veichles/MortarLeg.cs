using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MortarLeg : MonoBehaviour
{
    [Tooltip("Mortar turret connected to this mortar bipod")]
    public Turret connectedTurret;

    [Tooltip("Axis of rotation of the mortar bipod")]
    [Range(0,2)]
    public byte axis = 0;
    [Tooltip("Multiplier of the angle of rotation of the mortar bipod")]
    [Range(-2, 4)]
    public float rotationMultiplier = 2;

}
