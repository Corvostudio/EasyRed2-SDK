using UnityEngine;

public partial class VehicleDamagableDetachablePart : VehicleDamagablePart
{
    [Tooltip("Life before detaching this part")]
    [Range(0, 1000)]
    public float detachablePartLife = 100;

    [Tooltip("Defines minimum impact speed to damage detachable part")]
    [Range(0, 300)]
    public float minImpactSpeedToDamage = 40;

}
