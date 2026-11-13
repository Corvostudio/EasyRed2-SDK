using UnityEngine;

public partial class VehicleDamagableDetachablePart : VehicleDamagablePart
{
    [Tooltip("Hit points of this part: it breaks off when they run out.")]
    [Range(0, 1000)]
    public float detachablePartLife = 100;

    [HideInInspector]//deprecated: not read by the game, the impact speeds are fixed in code
    public float minImpactSpeedToDamage = 40;

}
