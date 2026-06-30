using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TrackWeelsUpdater : MonoBehaviour
{
    [Tooltip("Define the left track mesh to have the sliding left track effect")]
    public Renderer leftTrack;

    [Tooltip("Define the right track mesh to have the sliding right track effect")]
    public Renderer rightTrack;

    [Tooltip("The vertical UV direction and speed of the track sliding effect. If the tank is rotated in the standard ER2 rotation, it should be '-1'")]
    [Range(-20f, 20f)]
    public float trackSpeedMultiplier = -1;

    [Tooltip("The horizontal UV direction and speed of the track sliding effect. If the tank is rotated in the standard ER2 rotation, it should be '0'")]
    [Range(-20f, 20f)]
    public float trackSpeedMultiplier_h = 0;

}
