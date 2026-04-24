using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public partial class TurretKnob : MonoBehaviour
{
    [Tooltip("Turret rotated by this Knob lever")]
    public Turret connectedTurret;
    [Tooltip("Knob lever axis of rotation for the turret")]
    public TurretAxis turretAxis = TurretAxis.leftRight;
    [Tooltip("Pivot of the rotation of the knob lever")]
    public Transform pivot;
    [Tooltip("Axis of rotation of the knob lever")]
    public RotationAxis konbAxis = RotationAxis.x;
    [Tooltip("Angle multiplier of rotation of the knob lever")]
    [Range(-100, 100)]
    public float rotationMultiplier = 15;

    [Tooltip("Hand IK position of the knob lever.")]
    public Transform handIKPosition;
    [Tooltip("Transform used to keep the hand rotated from a good angle on the knob")]
    public Transform handRelativePosition;

    public enum TurretAxis { leftRight, upDown }
    public enum RotationAxis { x, y, z }
}
