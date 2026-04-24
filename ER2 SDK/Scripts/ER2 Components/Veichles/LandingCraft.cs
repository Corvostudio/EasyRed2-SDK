using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LandingCraft : AutoTransportVehicle
{
    [Tooltip("Whistle sound when opening the ramp of the landing craft")]
    public AudioClip whistle;

    //settare max speed attorno a 5

    [Tooltip("Water splash particle effect of the boat in movement")]
    public ParticleSystem waterSplash;

    [Tooltip("Exit position type")]
    public ExitPosition exitPosition = ExitPosition.SeatPosition;
    [Tooltip("Force the soldier to run forward when the ramp is open for this amount of meters")]
    [Range(0,6)]public float exitPositionDistance = 1.3f;
    public Vector3 runOutsidePosition = new Vector3(0, 0, 11);

    public enum ExitPosition
    {
        SeatPosition,
        Every,
        LeftSide,
        RightSide
    }
}
