using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LandingCraft : AutoTransportVehicle
{
    [Header("Effects")]
    [Tooltip("Whistle sound when opening the ramp of the landing craft")]
    public AudioClip whistle;

    //settare max speed attorno a 5

    [Tooltip("Water splash particle effect of the boat in movement")]
    public ParticleSystem waterSplash;

    [Header("Getting out")]
    [Tooltip("Where units get out. Seat Position: next to their seat. Left Side / Right Side: beside the craft. Every: on a random side.")]
    public ExitPosition exitPosition = ExitPosition.SeatPosition;
    [Tooltip("For the side exits: how far from the craft's centre line units appear, in metres.")]
    [Range(0,6)]public float exitPositionDistance = 1.3f;
    [Tooltip("Point, in the craft's local space, that AI units run to once they are out (by default 11 m ahead).")]
    public Vector3 runOutsidePosition = new Vector3(0, 0, 11);

    public enum ExitPosition
    {
        SeatPosition,
        Every,
        LeftSide,
        RightSide
    }
}
