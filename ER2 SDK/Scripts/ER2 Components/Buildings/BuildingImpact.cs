using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BuildingImpact : MonoBehaviour, ImpactSpecifier
{
    /*[Range(0, 1000)]
    public int minPenetrationForDamage = 0;*/
    [Tooltip("Define the type of impact based on the collision material")]
    public ImpactType impactType;
    [Tooltip("Specifies if bullets can ricochet on impact")]
    public bool canRicochet = true;
}
