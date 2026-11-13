using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MovableVehicle : Vehicle
{
    [Header("Movement")]
    [Tooltip("Top speed of the vehicle, in km/h.")]
    [Range(3, 520)]
    public float maxKmhSpeed = 40;

    [Header("Engine sounds")]
    [Tooltip("Sound of the starting engine")]
    public AudioClip engine_start;
    [Tooltip("Sound of the engine shutting down")]
    public AudioClip engine_stop;
    [Tooltip("Engine loop heard from close by. Alone it carries up to 200 m; with a far loop set it plays only within about 65 m.")]
    public AudioClip engine_move;
    [Tooltip("Optional engine loop for distant listeners: it replaces the close loop beyond about 65 m and carries up to 600 m.")]
    public AudioClip engine_move_far;

    [Header("Parts and effects")]
    [Tooltip("Internal modules of the vehicle (engine, fuel tank, tracks, wheels): each one is an Internal Vehicle Part with its own hit points.")]
    public InternalVehiclePart[] internalParts;
    [Tooltip("Exhaust smoke particle systems inside the prefab: they play while the engine is running.")]
    public ParticleSystem[] mufflerSmoke;

}
