using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AutoTransportVehicle : MovableVehicle
{

}


public enum AutoTransportPhase
{
    goingToDestination = 0,
    waitingDisload = 1,
    goingBack = 2
}