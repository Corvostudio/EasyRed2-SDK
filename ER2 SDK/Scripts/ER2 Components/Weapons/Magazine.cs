using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class Magazine : ItemObject //, IAmmo
{
    [Tooltip("Is magazine using tracer bullets?")]
    public bool use_tracer = false;

    [Header("Magazine ammo settings")]
    [Tooltip("Defines type of ammo that can be load")]
    public string compatibleAmmo = "ammo_792mm";
    [Tooltip("Defines the platform that can host the magazine")]
    public string socket;
    [Tooltip("Bullet capacity of the magazine")]
    [Range(0, 2500)]
    public int capacity = 30;
    //[Tooltip("Bullet inside the magazine on initialize")]
    //[Range(0, 2500)] public int ammoInside = 30;

    [Header("Optional override animation in case base weapon can fit alternative magazines")]
    //customize change magazine aniamtions
    [Tooltip("Percent time relative to animation lenght were partial magazine is swapped with the new one. 0=Auto")]
    [Range(0, 1)]
    public float swap_mag_time_perc_half = 0;
    [Tooltip("Name of partial reload animation for this magazine from the bundle (optional)")]
    public string override_anim_reload_half = "";
    [Tooltip("Override partial reload sound effect depending by the type of magazine (optional)")]
    public AudioClip reload_half_sound = null;
    [Tooltip("Percent time relative to animation lenght were empty magazine is swapped with the new one. 0=Auto")]
    [Range(0, 1)]
    public float swap_mag_time_perc_full = 0;
    [Tooltip("Name of full reload animation for this magazine from the bundle (optional)")]
    public string override_anim_reload_full = "";
    [Tooltip("Override full reload sound effect depending by the type of magazine (optional)")]
    public AudioClip reload_full_sound = null;
}


[System.Serializable]
public class MagazineOverrideAnimation
{
    public string gun_id;
    [Range(0, 1)]
    public float swap_mag_time_perc_half = 0;
    public string override_anim_reload_half = "";
    public AudioClip reload_half_sound = null;
    [Range(0, 1)]
    public float swap_mag_time_perc_full = 0;
    public string override_anim_reload_full = "";
    public AudioClip reload_full_sound = null;
}