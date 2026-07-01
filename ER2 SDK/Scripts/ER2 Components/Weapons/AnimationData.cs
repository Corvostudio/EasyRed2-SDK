using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public partial class AnimationData
{
    public enum FPSAnimationSet
    {
        UseCustom = 0,
        GenericMagazine,
        GenericBoltAction,
        GenericHandgun,
        GenericRocketLauncher,
        GenericShotgun,
        GenericMG
    }

    [Tooltip("Reuse animations from another weapon. At runtime the root GameObject of this gun is renamed to match the source weapon ID. The entire transform hierarchy must be identical to the source prefab (only the root name can differ, as it must always be unique).")]
    public string recycledAnimationsWeaponId = "";//recycle animations from anothewr weapon

    [Tooltip("Select which default FPS animation set this weapon uses. Choose \"Use Custom\" to manually assign all animations.")]
    public FPSAnimationSet animationSet = FPSAnimationSet.GenericMagazine;

    [Tooltip("Weapon tilt angle (in degrees) applied while aiming.")]
    [Range(-90, 90)]
    public float tiltOnAim = 0;


    [Header("Generic weapons")]

    [Tooltip("Animation name for equipping the weapon (Bad naming convention comes from how the system used to work much time ago, sorry for that).")]
    public string fps_putaway;
    [Tooltip("[Optional] Animation name for un-equipping the weapon. If empty it will use the fps_putaway equip animation but in reverse.")]
    public string fps_unequip;

    /*[Tooltip("[Optional] Animation to play on fps rig when firing a gun. Generally used for bolt action guns firing pin.")]
    public string fps_fire;
    [Tooltip("[Optional] Animation to play on fps rig when firing a gun (when out of bullets). Generally used for bolt action guns firing pin.")]
    public string fps_fire_no_ammos;*/

    [Tooltip("Animation name for a full reload (empty weapon).")]
    public string fps_reload_full;
    [Tooltip("Sound played during a full reload.")]
    public AudioClip reload_sound_full;

    [Tooltip("Animation name for a partial reload (ammo still in weapon).")]
    public string fps_reload_half;
    [Tooltip("Sound played during a partial reload.")]
    public AudioClip reload_sound_half;


    [Header("Bolt-action weapons")]

    [Tooltip("Animation name for cycling the bolt.")]
    public string fps_bolt_action;
    [Tooltip("Sound played when cycling the bolt.")]
    public AudioClip boltaction_sound;


    [Header("Chamber-loaded weapons")]

    [Tooltip("Animation name for opening the weapon chamber.")]
    public string fps_chamber_open;
    [Tooltip("Sound played when opening the chamber.")]
    public AudioClip chamber_sound_open;

    [Tooltip("Animation name for opening the weapon chamber, when the gun is empty. If not assigned fall backs to fps_chamber_open")]
    public string fps_chamber_open_noAmmo;
    [Tooltip("Sound played when opening the chamber, when the gun is empty. If not assigned fall backs to chamber_sound_open")]
    public AudioClip chamber_sound_open_noAmmo;


    [Tooltip("Animation name for loading individual rounds into the chamber.")]
    public string fps_chamber;
    [Tooltip("Sound played while loading rounds into the chamber.")]
    public AudioClip chamber_sound;


    [Tooltip("Animation name for closing the chamber.")]
    public string fps_chamber_close;
    [Tooltip("Sound played when closing the chamber.")]
    public AudioClip chamber_sound_close;

    [Tooltip("Animation name for closing the chamber, when the gun was empty before the reload started. If not assigned fall backs to fps_chamber_close")]
    public string fps_chamber_close_noAmmo;
    [Tooltip("Sound played when closing the chamber, when the gun was empty before the reload started. If not assigned fall backs to chamber_sound_close")]
    public AudioClip chamber_sound_close_noAmmo;


    [Tooltip("During single bullet reload, you can specify a group-ammo reload animation (for example: Reloading a 5-bullet stripper clip on Gewehr 43)")]
    public MultiAmmoChamberingAnimation[] multi_ammo_chambering = new MultiAmmoChamberingAnimation[0];

    [Tooltip("This group of objects will be hidden during single bullet reload when the ammo is empty. It will be activated as soon as at least 1 bullet is loaded.")]
    public GameObject[] nonEmptyStaticMeshes;


    [Header("Extra animations / Overheat")]

    [Tooltip("Number of consecutive shots required to overheat the weapon. Set to 0 to disable overheating.")]
    public short rounds_to_overheat = 0;

    [Tooltip("Animation name for changing the weapon barrel.")]
    public string fps_change_barrell;

    [Tooltip("Sound played while changing the barrel.")]
    public AudioClip fps_change_barrell_sound;

    [Tooltip("Particle effect used to visually indicate barrel overheating.")]
    public ParticleSystem barrell_heat_fx;


    [System.Serializable]
    public struct MultiAmmoChamberingAnimation
    {
        [Tooltip("How many ammo will be loaded with this pass")]
        public int ammo_count;
        [Tooltip("Group ammo load sound")]
        public AudioClip sound;
        [Tooltip("Group ammo load animation")]
        public string anim_id;
    }
}

