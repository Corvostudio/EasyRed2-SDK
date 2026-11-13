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
    [Header("Ammo")]
    [Tooltip("Ammo types this weapon can load.")]
    public TurretWeaponBullet[] ammos = new TurretWeaponBullet[1];

    [Header("Fire FX")]
    [Tooltip("Prefab name of the effect spawned at the muzzle at every shot.")]
    public string fireFx_id = "gunFireFX";
    [Tooltip("Optional prefab name of a muzzle effect that stays on while firing (flamethrowers).")]
    public string fireFxContinuous_id = null;
    [Tooltip("Bundle the two effects above are loaded from.")]
    public string fireFx_bundle = "er2fxs";

    [Header("Sounds")]
    [Tooltip("Volume of fire sound")]
    public float fireSoundVolume = .5f;
    [Tooltip("Shot sound heard from close by. For weapons that do not use the start / loop / tail sounds below.")]
    public AudioClip fireSound;
    [Tooltip("Continuous-fire weapons (flamethrowers, fast MGs): played once when firing starts, heard from close by.")]
    public AudioClip fireSound_start;
    [Tooltip("Continuous-fire weapons: loop played while firing, heard from close by.")]
    public AudioClip fireSound_loop;
    [Tooltip("Continuous-fire weapons: played once when firing stops, heard from close by.")]
    public AudioClip fireSound_tail;
    [Tooltip("Shot sound heard from far away. If empty, the close one is used.")]
    public AudioClip fireSound_distance;
    [Tooltip("Continuous-fire weapons: loop played while firing, heard from far away.")]
    public AudioClip fireSound_distance_loop;
    [Tooltip("Continuous-fire weapons: played once when firing stops, heard from far away.")]
    public AudioClip fireSound_distance_tail;

    [Header("Ballistics and cycle")]
    [Tooltip("Recoil of the vehicle at every shot: 0 = none, 1 = normal. On planes it is reduced to a quarter.")]
    [Range(0.0f, 5.0f)]
    public float recoilIntensity = 1;
    [Tooltip("Rate of fire of each barrel, in rounds per minute.")]
    [Range(1, 2000)]
    public int roundPerMinute = 300;
    [Tooltip("On: the round is loaded when the gunner fires (mortars). Off: the weapon reloads by itself after firing.")]
    public bool reloadWhenFiring = false;
    [Tooltip("Reload time, in seconds.")]
    [Range(0.1f, 200)]
    public float reloadTime = 5;
    [Tooltip("Spread of the shots, in degrees.")]
    [Range(0, 25)]
    public float dispersionAngle = .1f;
    [Tooltip("Show a tracer every this many shots. 0 = use the normal effect of the bullet.")]
    [Range(0, 40)]
    public byte specifiedTracersRatio;
}