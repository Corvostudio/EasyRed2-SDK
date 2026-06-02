using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Propeller : MonoBehaviour
{
    [Tooltip("Defines the propeller mesh blur effect when moving slow and fast")]
    public Transform propeller_slow, propeller_fast;

    [Tooltip("The connected Plane to the propeller")]
    public MovableVehicle connectedPlane;

    [Tooltip("Propeller rotation axis")]
    public Vector3 rotationAxis = new Vector3(0, 1, 0);

}