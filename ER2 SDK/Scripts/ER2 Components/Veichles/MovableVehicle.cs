using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MovableVehicle : Vehicle
{
    [Tooltip("Max speed of the vehicle in Kmh")]
    [Range(3, 520)]
    public float maxKmhSpeed = 40;

    //[Tooltip("Max hear distance for engine sound")]
    //[Range(5, 1000)]
    //public float maxEngineSoundDistance = 200;

    [Tooltip("Sound of the starting engine")]
    public AudioClip engine_start;
    [Tooltip("Sound of the engine shutting down")]
    public AudioClip engine_stop;
    [Tooltip("Loop sound of the close by engine")]
    public AudioClip engine_move;
    [Tooltip("Loop sound of the engine when far")]
    public AudioClip engine_move_far;

    [Tooltip("Internal modules of the vehicle")]
    public InternalVehiclePart[] internalParts;

    [Tooltip("Muffler smoke FXs inside the prefab")]
    public ParticleSystem[] mufflerSmoke;

}
