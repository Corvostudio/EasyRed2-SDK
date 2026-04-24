using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class VehicleWithWheels : MovableVehicle
{
    public enum OnEnemyEncounter
    {
        nothing,
        ejectPassengers,
        ejectAll
    }

    public Vector3 centerOfMass = new Vector3(0, 1, 0);

    [Tooltip("attackVehicle: Attack objective, selfPropelledArtillery: Prioritize keeping distance and answering radio calls.")]
    public WheeledVehicleAi aiType = WheeledVehicleAi.attackVehicle;
    public enum WheeledVehicleAi { attackVehicle = 0, selfPropelledArtillery = 1 }

    [Tooltip("Defines the maximum torque of the motor")]
    [Range(50, 5000)]
    public float maxMotorTorque = 800;
    [Tooltip("Defines the acceleration speed of the vehicle")]
    [Range(1, 200)]
    public float accelerationSpeed = 50;
    [Tooltip("Defines the sound effect to play when the vehicle's crashes")]
    public AudioClip crashSound;

    [Tooltip("List of wheels on the right side of the vehicle")]
    public WheelCollider[] rightTrack;
    [Tooltip("List of wheels on the left side of the vehicle")]
    public WheelCollider[] leftTrack;

    //public GameObject onExplodeTurretJump;

    [Tooltip("Action to perform when vehicle encounter the enemey")]
    public OnEnemyEncounter onEnemyEncounter = OnEnemyEncounter.nothing;



}
