using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial interface ImpactSpecifier
{

}

public enum HitType
{
    Projectile,
    Explosion,
    Fire,
    Melee,
    VehicleCollision
}

public enum ImpactType
{
    generic,
    terrain,
    cement,
    blood,
    metal,
    wood,
    water
}
