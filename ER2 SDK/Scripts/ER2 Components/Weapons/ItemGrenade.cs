using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class ItemGrenade : ItemObject
{
    [Header("Configuration")]
    [Tooltip("Grenade throw animation in FPS (Check String Tables document)")]
    public string FPSthrowAnimation = "throw_grenade_1";
    public string recycledAnimationsId = "";

    [Tooltip("Grenade type")]
    public GrenadeType grenadeType = GrenadeType.infantryGrenate;
    [Tooltip("Fuse type")]
    public FuseType fuseType = FuseType.TimeFuse;
    [Tooltip("Explosion delay (for TimeFuse)")]
    [Range(3, 20)]
    public float explodeAfter = 4.5f;
    [Tooltip("Grenade throw sound effect")]
    public AudioClip grenadeSound;

    [Tooltip("Throw distance multiplier")]
    [Range(0, 3)]
    public float throw_mult = 1;


    [Header("Explosion data")]
    [Tooltip("Damage on explosion")]
    [Range(1, 1000)]
    public float explosionDamage = 200;
    [Tooltip("Penetration of explosion (in mm)")]
    [Range(0, 800)]
    public float explosionMaxPenetration = 1;
    [Tooltip("Explosion radius")]
    [Range(.1f, 40)]
    public float explosionRadius = 5;

    [Header("Explosion FX")]
    [Tooltip("Explosion fx (Check String Tables document or create your own)")]
    public string explosion_fx = "explosion_grenade";
    [Tooltip("Bundle where the explosion fx comes from")]
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