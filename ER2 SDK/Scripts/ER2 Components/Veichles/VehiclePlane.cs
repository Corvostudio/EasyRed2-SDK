using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class VehiclePlane : MovableVehicle
{

    [Tooltip("Center of mass")]
    public Vector3 centerOfMass = new Vector3(0, 1, 0);


    [Tooltip("Angle of dive")]
    [Range(0, 89)]
    public float diveAlignAnge = 45;
    [Tooltip("Time to reach max trust for the engine")]
    [Range(0, 50)]
    public float timeFromZeroToMaxThrust = 20;
    [Tooltip("Force of the thrust on the plane")]
    [Range(0, 5000)]
    public float thrustForceMultiplier = 500;



    //simplified flight model variables
    [Tooltip("Max flyght height before stalling (at max speed)")]
    [Range(0, 10000)]
    public float maxFlyHeight = 3000;
    [Tooltip("[0-1] Speed multiplier where the wing start producing lift force")]
    [Range(0, 1)]
    public float startLiftMult = .25f;
    [Tooltip("[0-1] Speed multiplier where the wing produce maximum lift force")]
    [Range(0, 1)]
    public float endLiftMult = .4f;
    /*[Tooltip("Minimum speed to lift the vehicle")]
    [Range(0, 500)]
    public float totalLiftVelocity = 20;
    [Tooltip("Minimum speed to have good control of the vehicle")]
    [Range(0, 500)]
    public float maxControlSpeed = 35;*/

    [Tooltip("Wheel collider of the wheels that can brake on the runway")]
    public WheelCollider[] brakingWheels;

    [Tooltip("Sound when crashing")]
    public AudioClip crashSound;

    [Tooltip("Type of the plane. If set to bomber, adding bombs to it is important to make the AI working correctly.")]
    public PlaneType planeType = PlaneType.Fighter;

    [Tooltip("Registered bombs for the plane")]
    public BombBay[] bombBay = new BombBay[0];


    [Tooltip("Left detachable wing")]
    public VehicleDamagableDetachablePart leftDetachableWing;
    [Tooltip("Right detachable wing")]
    public VehicleDamagableDetachablePart rightDetachableWing;
    [Tooltip("Detachable tail(s)")]
    public VehicleDamagableDetachablePart[] detachableTails;
    [Tooltip("Detachable propeller(s)")]
    public VehicleDamagableDetachablePart[] detachablePropellers;

}

[System.Serializable]
public partial class BombBay
{
    [Tooltip("Delay before dropping the bombs one after another")]
    public float drop_delay = .3f;
    [Tooltip("ID of the bomb prefab")]
    public string bomb_id = "PlaneBomb250";
    [Tooltip("Parent transform where to spawn the bomb in at 0,0,0 pos and rot")]
    public Transform bomb_parent;
}

public enum PlaneType
{
    Fighter,
    Bomber
}