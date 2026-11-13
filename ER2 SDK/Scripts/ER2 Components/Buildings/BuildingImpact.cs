using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BuildingImpact : MonoBehaviour, ImpactSpecifier
{
    /*[Range(0, 1000)]
    public int minPenetrationForDamage = 0;*/
    [Tooltip("Surface material: sets the effect and sound of impacts and the footstep sound.")]
    public ImpactType impactType;
    [Tooltip("Projectiles can ricochet off this surface.")]
    public bool canRicochet = true;
}
