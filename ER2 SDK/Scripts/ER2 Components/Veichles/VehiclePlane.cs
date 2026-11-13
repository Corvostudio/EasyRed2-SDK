using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class VehiclePlane : MovableVehicle
{

    [Header("Plane")]
    [Tooltip("Fighter or bomber: it changes the AI (bombers cruise higher and need bombs in Bomb Bay), the map icon, and only fighters use aircraft repair stations.")]
    public PlaneType planeType = PlaneType.Fighter;
    [Tooltip("Centre of mass of the plane, in the local space of the prefab root (metres).")]
    public Vector3 centerOfMass = new Vector3(0, 1, 0);
    [Tooltip("Wheel colliders of the landing gear. They brake when the throttle is at zero and are used to detect that the plane is on the ground.")]
    public WheelCollider[] brakingWheels;
    [Tooltip("Sound when crashing")]
    public AudioClip crashSound;

    [Header("Engine")]
    [Tooltip("Engine power: the push at full throttle is 100 x this value newtons (less in thin air, see Max Fly Height).")]
    [Range(0, 5000)]
    public float thrustForceMultiplier = 500;
    [Tooltip("Engine spool-up time. Seconds from zero to full thrust = this value x Thrust Force Multiplier / 600. Thrust drops twice as fast.")]
    [Range(0, 50)]
    public float timeFromZeroToMaxThrust = 20;

    [Header("Flight model")]
    [Tooltip("Altitude, in metres, of the thinnest air. Lift and thrust are full up to a third of it, then drop steadily to 30% at this height and above.")]
    [Range(0, 10000)]
    public float maxFlyHeight = 3000;
    [Tooltip("Stall speed, as a fraction of Max Kmh Speed. Below it the wings give no lift on the runway and less lift in the air. Climbing raises it.")]
    [Range(0, 1)]
    public float startLiftMult = .25f;
    [Tooltip("No longer affects lift. It only sets the impact speed above which the tail parts take damage, as a fraction of Max Kmh Speed.")]
    [Range(0, 1)]
    public float endLiftMult = .4f;

    [Header("AI")]
    [Tooltip("Dive angle, in degrees, of AI pilots attacking a ground target with bombs. Without bombs they dive at 12 degrees at most.")]
    [Range(0, 89)]
    public float diveAlignAnge = 45;

    [Header("Bombs")]
    [Tooltip("Bombs carried by the plane: one entry per bomb.")]
    public BombBay[] bombBay = new BombBay[0];
    [Tooltip("Optional bomb bay doors animation: it opens at the first drop and closes two seconds after the last one.")]
    [SerializeField] private BombBayAnimator bombBayAnim;

    [Header("Detachable parts")]
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
    [Tooltip("Seconds to wait after this bomb is dropped before the next one can be dropped.")]
    public float drop_delay = .3f;
    [Tooltip("ID (prefab name) of the bomb, as registered in the game items.")]
    public string bomb_id = "PlaneBomb250";
    [Tooltip("Empty transform where the bomb hangs: the bomb is spawned as its child, with its position and rotation.")]
    public Transform bomb_parent;
}

public enum PlaneType
{
    Fighter,
    Bomber
}

[System.Serializable]
public partial class BombBayAnimator
{
    [Tooltip("Legacy Animation component that holds the clip of the bomb bay doors.")]
    public Animation animationComponent;
    [Tooltip("Name of the clip that opens the doors. It is played backward to close them.")]
    public string animationName;
}