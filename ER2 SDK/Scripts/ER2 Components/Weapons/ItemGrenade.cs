using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class ItemGrenade : ItemObject
{
    [Header("Configuration")]
    [Tooltip("First-person throw animation (see the String Tables document).")]
    public string FPSthrowAnimation = "throw_grenade_1";
    [Tooltip("Optional: ID of another grenade whose first-person animations are reused (same hierarchy required).")]
    public string recycledAnimationsId = "";

    [Tooltip("Infantry Grenade: explosive. Smoke Grenade: smoke, no damage. Phosphorus Grenade: smoke that also damages. AT Grenade: anti-tank.")]
    public GrenadeType grenadeType = GrenadeType.infantryGrenate;
    [Tooltip("How it goes off: after a delay (Time Fuse, Sticky Time Fuse), on impact, or under the pressure of infantry or vehicles.")]
    public FuseType fuseType = FuseType.TimeFuse;
    [Tooltip("Seconds before it explodes, for the time fuses.")]
    [Range(3, 20)]
    public float explodeAfter = 4.5f;
    [Tooltip("Sound played when the grenade is thrown.")]
    public AudioClip grenadeSound;

    [Tooltip("Throw speed multiplier: 1 = normal, higher values throw farther.")]
    [Range(0, 3)]
    public float throw_mult = 1;


    [Header("Explosion data")]
    [Tooltip("Damage of the explosion.")]
    [Range(1, 1000)]
    public float explosionDamage = 200;
    [Tooltip("Armour penetration of the explosion, in mm.")]
    [Range(0, 800)]
    public float explosionMaxPenetration = 1;
    [Tooltip("Radius of the explosion, in metres.")]
    [Range(.1f, 40)]
    public float explosionRadius = 5;

    [Header("Explosion FX")]
    [Tooltip("Prefab name of the explosion effect (see the String Tables document, or make your own).")]
    public string explosion_fx = "explosion_grenade";
    [Tooltip("Bundle the explosion effect is loaded from.")]
    public string explosion_bundle = "er2fxs";
}

public enum FuseType
{
    TimeFuse,
    InfantryPressionFuse,
    VehiclePressionFuse,
    ExplodeOnImpact,
    StickyTimeFuse
}
public enum GrenadeType
{
    infantryGrenate,
    smokeGrenade,
    ATGrenade,
    phosporusGrenade
}