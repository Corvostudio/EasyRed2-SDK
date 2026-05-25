using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using static AnimationData;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class GenericGun : Weapon
{
    // ───── Static / constants ─────────────────────────────────────────────────
    public static readonly string genericAnimBundleName = "er2modanims";
    public static readonly string baseGameAnimationsBundleName = "er2animations";

    // ───── Mod info (hidden / runtime) ────────────────────────────────────────
    [HideInInspector] public string modBundleName = "";
    [System.NonSerialized] public string animBundleFolder = null;

    #region attributes


    // ═════════════════════════════════════════════════════════════════════════
    //  AMMUNITION & MAGAZINE
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Ammunition & Magazine")]

    [Tooltip("Item id of the cartridge this weapon fires (e.g. 'ammo_792mm'). Used for top-loading into chamber and for default ammo if no magazine is installed.")]
    public string compatibleAmmo = "ammo_792mm";

    [Tooltip("How many rounds fit inside the chamber/internal magazine. For weapons WITHOUT a detachable magazine (e.g. Kar98), this is the total capacity (e.g. 5). For weapons WITH a magazine, this is usually 1 (one chambered round).")]
    [Range(0, 250)][SerializeField] private int ammoSpace = 100;

    [Tooltip("Magazine socket id. Empty string = this weapon does NOT use detachable magazines (loads bullets directly into chamber). Any other value = mag socket type (only mags with the same socket can be installed).")]
    public string magazineSocket = "";

    [Tooltip("Transform where the magazine is parented when installed.")]
    public Transform magazinePosition;

    [Tooltip("G43-like behavior: allows loading individual cartridges INTO the currently installed magazine instead of swapping the whole magazine.")]
    public bool allowTopLoadIntoInstalledMagazine;


    // ═════════════════════════════════════════════════════════════════════════
    //  BALLISTICS & RECOIL
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Ballistics & Recoil")]

    [Tooltip("Bullet exit speed in m/s. Higher = flatter trajectory and less drop.")]
    [Range(5, 2000)] public int muzzleVelocity = 750;

    [Tooltip("Bullet spread cone (degrees of dispersion). 0 = perfectly accurate; higher values randomize bullet direction.")]
    [Range(0, 35)] public float bulletDispersion = .1f;

    [Tooltip("Recoil multiplier applied to the player when firing. 0 = no recoil, 1 = standard, up to 2 = heavy. Clamped to 2x at runtime.")]
    [Range(0, 2f)] public float recoilIntensity = 1;


    // ═════════════════════════════════════════════════════════════════════════
    //  FIRE MODE & RATE
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Fire Mode & Rate of Fire")]

    [Tooltip("Selector type:\n• Single = semi-auto only\n• Auto = full-auto only\n• AutoOrSingle = switchable, defaults to Auto\n• AutoOrSingle_DefaultSingle = switchable, defaults to Single")]
    public FireSelector fireMode = FireSelector.Single;

    [Tooltip("Primary rate of fire in rounds per minute.")]
    [Range(1, 2000)] public int roundPerMinute = 300;

    [Tooltip("Optional additional rates of fire (some weapons let the user cycle between multiple RPMs via the fire selector). Leave empty if the weapon only has one RPM.")]
    public int[] alternativeRoundsPerMinute = new int[0];


    // ═════════════════════════════════════════════════════════════════════════
    //  BOLT / MANUAL ACTION
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Bolt / Manual Action")]

    [Tooltip("True for bolt-action / pump-action weapons that require a manual cycle between shots (Kar98, Mosin, shotguns, PzB39…).")]
    public bool isManualBoltOperated = false;

    [Tooltip("If true, bolt action is performed BEFORE firing (e.g. PzB39: cycle, then shoot). If false (default), the bolt is cycled AFTER firing.")]
    public bool boltActionBeforeFiring = false;

    [Tooltip("If true, the player can keep aiming down sights while the bolt is being cycled.")]
    public bool canAimWhileUsingBolt = false;

    [Tooltip("Transform of the bolt / charging handle that visually slides back-and-forth on each shot. Leave empty for weapons without a visible reciprocating bolt.")]
    public Transform spin_pos;

    [Tooltip("How far (in meters along local Z) the bolt slides back when cycling. Negative values reverse the direction.")]
    [Range(-0.25f, 0.25f)] public float bolt_movementDistance = 0.07f;

    [Tooltip("If true, the bolt stays in the open position after the last round is fired (e.g. M1 Garand, BAR).")]
    public bool boltOpenAfterLastShot = false;

    [Tooltip("Transform where empty cartridge cases spawn and are ejected from after each shot.")]
    public Transform bulletEjectPos;


    // ═════════════════════════════════════════════════════════════════════════
    //  FIRE POSITION & MUZZLE FX
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Fire Position & Muzzle FX")]

    [Tooltip("Transform where bullets spawn from and where the muzzle flash effect is anchored. Forward axis = fire direction.")]
    public Transform firePos;

    [Tooltip("Prefab id of the muzzle flash particle effect spawned at firePos when shooting.")]
    public string fireFx_id = "gunFireFX";

    [Tooltip("AssetBundle name where the muzzle flash prefab (fireFx_id) is loaded from.")]
    public string fireFx_bundle = "er2fxs";

    [Tooltip("Optional GameObject that gets disabled the moment the gun fires (e.g. a chambered round mesh) and re-enabled on reload. Leave empty if not needed.")]
    public GameObject disable_on_fire;


    // ═════════════════════════════════════════════════════════════════════════
    //  AUDIO — FIRE SOUNDS
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Audio — Fire Sounds (close range)")]

    [Tooltip("Single shot fire sound. Used for non-cycle weapons OR as the fallback when start/loop/tail are not assigned.")]
    public AudioClip fireSound;

    [Tooltip("Optional intro sound played at the START of an automatic burst (e.g. flamethrower whoosh, MG spool-up). Leave empty for normal weapons.")]
    public AudioClip fireSound_start;

    [Tooltip("Optional looping sound played WHILE firing in full auto. Leave empty to use repeated single-shot sounds instead.")]
    public AudioClip fireSound_loop;

    [Tooltip("Optional tail sound played at the END of an automatic burst (echo / cooldown).")]
    public AudioClip fireSound_tail;

    [Header("⏺ Audio — Fire Sounds (distant)")]

    [Tooltip("Single-shot fire sound used when the listener is far from the shooter.")]
    public AudioClip fireSound_distance;

    [Tooltip("Looping fire sound used at distance for full-auto weapons.")]
    public AudioClip fireSound_distance_loop;

    [Tooltip("Tail sound used at distance at the end of a burst.")]
    public AudioClip fireSound_distance_tail;

    [Tooltip("Pitch offset added to all fire sounds. Suggested range: SMGs [-0.1, +0.2], Rifles [-0.15, +0.5]. Use to make sounds beefier or sharper.")]
    [Range(-.2f, .5f)] public float firing_sound_variance = 0;

    [Header("⏺ Audio — Equip / Holster")]

    [Tooltip("Sound id played when the player equips this weapon.")]
    public string sound_equip_gun = "Foley_Equip_Firearm01";

    [Tooltip("Sound id played when the player holsters this weapon.")]
    public string sound_holster_gun = "Foley_Holster_Firearm01";


    // ═════════════════════════════════════════════════════════════════════════
    //  AIMING & IRON SIGHTS
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Aiming & Iron Sights")]

    [Tooltip("Iron sight aim positions (one per zeroing step). Each transform represents the camera position when aiming down sights at that zeroing distance.")]
    public Transform[] aimPositions = new Transform[0];

    [Tooltip("Optional custom zeroing distances (in meters) per aimPosition. Length must match aimPositions, or leave empty to auto-calculate distance from sight geometry.")]
    public int[] customZeroing = new int[0];

    [Tooltip("Approximate distance (meters) between the player's eye and the front sight post. Used when auto-computing zeroing.")]
    [Range(.1f, 2.5f)] public float distanceBwEyeAndFrontSight = 1;

    [Tooltip("FOV used while aiming down sights. Lower values = more zoom-in.")]
    [Range(20, 65)] public float aimingFOV = 55;

    [Tooltip("If false, this weapon can only be aimed when its bipod is deployed (typical for heavy MGs).")]
    public bool canAimWithoutBipod = true;

    [Tooltip("Optional adjustable sight component (e.g. flip-up rear sight) that is updated when zeroing changes.")]
    public AdjustableSight adjustableSight = null;

    [Tooltip("GameObject of the iron sight in its UP / standard position. Auto-disabled when a scope is mounted.")]
    public GameObject sight_up;

    [Tooltip("GameObject of the iron sight in its DOWN / folded position. Auto-enabled when a scope is mounted.")]
    public GameObject sight_down;


    // ═════════════════════════════════════════════════════════════════════════
    //  ATTACHMENTS & SCOPE
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ Attachments & Scope")]

    [Tooltip("List of attachment slots this weapon supports (scope, bipod, bayonet…). Each entry defines the attachment id and the transform where it gets parented.")]
    public SupportedAttachment[] supportedAttachments;

    [Tooltip("Optional built-in scope (a scope that's part of the weapon and not a separate attachment, e.g. a sniper variant with a permanently mounted scope).")]
    public GameObject integratedScope = null;


    // ═════════════════════════════════════════════════════════════════════════
    //  ANIMATIONS
    // ═════════════════════════════════════════════════════════════════════════
    [Header("⏺ FPS Animations (set)")]

    [Tooltip("FPS animation set: equip, reload, bolt-action, chamber, sounds, etc. The bulk of weapon animation data lives inside this struct.")]
    public AnimationData fpsAnimations;

    [Header("⏺ Animations on the weapon GameObject")]

    [Tooltip("If true, the fire animation plays even when there are no bullets (used by revolvers so the cylinder still spins on dry-fire).")]
    public bool playFireAnimWhenNoAmmo = false;

    [Tooltip("Name of the Animation clip on this GameObject played each time a bullet is fired (e.g. cylinder rotation on a revolver). Leave empty if not used.")]
    public string fire_anim;

    [Tooltip("Name of the Animation clip played specifically on the LAST shot (used by Luger toggle-lock open and similar). Leave empty if not used.")]
    public string fire_anim_lastBullet;

    #endregion


    // ───── Migration / serialization callback ─────────────────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        // This runs in editor when the object is loaded/modified
        // Unity will have already migrated oldFieldName -> newFieldName
        // So the data is already there
    }
#endif


    // ───── Derived properties (not serialized, kept here for context) ─────────
    public bool isFlamethrower { get { return compatibleAmmo == "ammo_flamethrower_1"; } }

    public Transform AimPos { get { return aimPositions[Mathf.Max(aimPosIndex, 0)]; } }
    public bool HasAimPos { get { return aimPositions.Length > 0; } }
    [System.NonSerialized] private sbyte aimPosIndex = 0;

    public GameObject installedMagazine { get { return magazinePosition && magazinePosition.childCount > 0 ? magazinePosition.GetChild(0).gameObject : null; } }
    public Magazine currentMagazine { get { return installedMagazine != null ? installedMagazine.GetComponent<Magazine>() : null; } }
}


[System.Serializable]
public partial class SupportedAttachment
{
    [Tooltip("Item id of the attachment that fits in this slot.")]
    public string attachment_id;

    [Tooltip("Transform where the attachment is parented when installed.")]
    public Transform attachmentPos;

    private AttachmentType type = AttachmentType.unknown;

    [Tooltip("If true, this scope slot allows reloading via stripper clip while the scope is mounted (most scopes block stripper clip reloads).")]
    public bool scopeAllowReloadStripperClip = false;

    [Tooltip("If true, the player can still use iron sights even with this attachment installed (e.g. low-profile / co-witness scopes).")]
    public bool allowIronSight = false;

    [Tooltip("Force gun shift when this attachment is installed. Meant to be used to prevent scope clippings with camera.")]
    public Vector3 fpsShift = Vector3.zero;
}

public enum AttachmentType
{
    unknown,
    scope,
    bipod,
    bayonet
}

public enum FireSelector
{
    Single,
    Auto,
    AutoOrSingle,
    AutoOrSingle_DefaultSingle
}