using UnityEngine;

public partial class VehicleDamagablePart : Interagible, ImpactSpecifier
{
    [Tooltip("Defines impact effect type")]
    public ImpactType impactType=ImpactType.metal;

    [Tooltip("Defines thickness of the armor in mm")]
    [Range(0, 250)]
    public float armorThickness = 70;//in mm

    [Tooltip("Allows angled armor to be considered as higher thickness")]
    public bool considerAngledArmor = true;
    [Tooltip("Allows bullets to ricochet")]
    public bool canRicochet = true;

    [Tooltip("Allows to damage unit when collide with something")]
    public bool damagePlayerOnCollision = true;

    [Tooltip("When something collides with this, this vehicle get damages (only for static vehicles)")]
    public bool damageHullOnGettingRammed = true;

}
