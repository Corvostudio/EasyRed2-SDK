using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Shared, reusable definition for a turret weapon: the "what it is" data
/// (ammo, fire rate, reload, dispersion, tracer ratio, fire FX and sounds).
///
/// This is the DATA-ONLY partial, so it can ship inside the modding SDK exactly
/// like the other turret data classes. Any runtime/editor logic the base game
/// wants to add can live in a separate "TurretWeaponData.Runtime.cs" partial
/// without touching what modders compile.
///
/// Fields that depend on HOW the weapon is mounted (fire positions, on-fire
/// animation, reload sound, prevent-aim-while-reloading) intentionally stay on
/// the <see cref="TurretWeapon"/> instance and are NOT part of this asset.
/// </summary>
[CreateAssetMenu(fileName = "TurretWeaponData", menuName = "Turret/Turret Weapon Data", order = 0)]
public partial class TurretWeaponData : ScriptableObject
{
    [Tooltip("Ammo type of the weapon")]
    public TurretWeaponBullet[] ammos = new TurretWeaponBullet[1];

    [Header("Fire FX")]
    [Tooltip("Fire FX when shooting the gun")]
    public string fireFx_id = "gunFireFX";
    [Tooltip("Fire FX when shooting the gun continuously (normally flamethrowers)")]
    public string fireFxContinuous_id = null;
    [Tooltip("Bundle that contains the FXs")]
    public string fireFx_bundle = "er2fxs";

    [Tooltip("Volume of fire sound")]
    public float fireSoundVolume = .5f;

    [Header("Correlated prefabs")]
    [Tooltip("Assign only if you don't need the cycle sound effect")]
    public AudioClip fireSound;
    [Tooltip("Assign only if you need the cycle sound effect like Flamethrower")]
    public AudioClip fireSound_start;
    [Tooltip("Assign only if you need the cycle sound effect like Flamethrower")]
    public AudioClip fireSound_loop;
    [Tooltip("Assign only if you need the cycle sound effect like Flamethrower")]
    public AudioClip fireSound_tail;
    [Tooltip("Fire sound used at distance")]
    public AudioClip fireSound_distance;
    [Tooltip("Fire sound used at distance Loop")]
    public AudioClip fireSound_distance_loop;
    [Tooltip("Fire sound used at distance Tail")]
    public AudioClip fireSound_distance_tail;

    [Header("Ballistics / cycle")]
    [Tooltip("Enable / disable recoil when firing")]
    public bool isRecoilless = false;

    [Tooltip("Rounds per minute (on each gun)")]
    [Range(1, 2000)]
    public int roundPerMinute = 300;

    [Tooltip("reload before firing (like a mortar), else reload after firing")]
    public bool reloadWhenFiring = false;

    [Tooltip("Reload time duration")]
    [Range(0.1f, 200)]
    public float reloadTime = 5;

    [Tooltip("Dispersion angle of the bullet")]
    [Range(0, 25)]
    public float dispersionAngle = .1f;

    [Tooltip("Show tracer every X shots. 0 means always use normal bullet FXs.")]
    [Range(0, 40)]
    public byte specifiedTracersRatio;
}