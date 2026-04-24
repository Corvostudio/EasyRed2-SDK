using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Tooltip("Flap Axis.")]
    public AxisDirection axisDirection = 0;

    [Tooltip("The axis which the flap rotates around. 0=Roll, 1=Pitch, 2=Yaw")]
    [Range(0, 2)]
    public byte axisFlap = 0;
    [Tooltip("Multiplier of flap rotation")]
    [Range(-90, 90)]
    public float rotationMultiplier = 45;
    [Tooltip("Minimum and maximum flap rotation angle")]
    [Range(0, 90)]
    public float rotationClamp = 45;

}
