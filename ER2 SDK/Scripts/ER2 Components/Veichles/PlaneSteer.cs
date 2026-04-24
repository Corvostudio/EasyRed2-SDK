using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlaneSteer : MonoBehaviour
{
    [Tooltip("Defines the plane connected to this steer control bar")]
    public VehiclePlane connectedPlane;


    [Tooltip("The multiplier rotation of the steer bar on plane turn")]
    [Range(-2, 2)]
    public float rotationMultiplier = .05f;
}
