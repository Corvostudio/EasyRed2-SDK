using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class Magazine : ItemObject //, IAmmo
{
    [Tooltip("The magazine is loaded with tracer rounds.")]
    public bool use_tracer = false;

    [Header("Magazine ammo settings")]
    [Tooltip("ID of the ammo this magazine holds.")]
    public string compatibleAmmo = "ammo_792mm";
    [Tooltip("Socket ID: the magazine fits the weapons with the same Magazine Socket.")]
    public string socket;
    [Tooltip("Rounds the magazine holds.")]
    [Range(0, 2500)]
    public int capacity = 30;
    //[Tooltip("Bullet inside the magazine on initialize")]
    //[Range(0, 2500)] public int ammoInside = 30;

    [Header("Reload override (optional)")]
    //customize change magazine aniamtions
    [Tooltip("Point of the partial reload animation (magazine not empty), 0-1 of its length, where the magazine is swapped. 0 = default.")]
    [Range(0, 1)]
    public float swap_mag_time_perc_half = 0;
    [Tooltip("Partial reload animation used with this magazine. Empty = the one of the weapon.")]
    public string override_anim_reload_half = "";
    [Tooltip("Partial reload sound used with this magazine (optional).")]
    public AudioClip reload_half_sound = null;
    [Tooltip("Point of the full reload animation (magazine empty), 0-1 of its length, where the magazine is swapped. 0 = default.")]
    [Range(0, 1)]
    public float swap_mag_time_perc_full = 0;
    [Tooltip("Full reload animation used with this magazine. Empty = the one of the weapon.")]
    public string override_anim_reload_full = "";
    [Tooltip("Full reload sound used with this magazine (optional).")]
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