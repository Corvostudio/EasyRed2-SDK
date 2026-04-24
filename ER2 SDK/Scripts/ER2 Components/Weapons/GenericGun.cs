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

    //static
    public static readonly string genericAnimBundleName = "er2modanims";
    public static readonly string baseGameAnimationsBundleName = "er2animations";


    //mod stuff
    [HideInInspector] public string modBundleName = "";
    [System.NonSerialized] public string animBundleFolder = null;

    //normal stuff
    #region attributes
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
    [Tooltip("Suggested range | SMGs [-.1,.2] | Rifles [-.15,.5]")]
    [Range(-.2f, .5f)] public float firing_sound_variance = 0;

    [Tooltip("Equip gun sound")]
    public string sound_equip_gun = "Foley_Equip_Firearm01";
    [Tooltip("Holster gun sound")]
    public string sound_holster_gun = "Foley_Holster_Firearm01";

    /*[Range(0, 150)]
    public int bulletDamage = 55;*/
    [Range(5, 2000)]
    public int muzzleVelocity = 750;
    [Range(0, 35)]
    public float bulletDispersion = .1f;

    public string fireFx_id = "gunFireFX";
    public string fireFx_bundle = "er2fxs";

    [Header("Weapon Attributes")]

    public string compatibleAmmo = "ammo_792mm";

    //protected capacity
    [Range(0, 250)][SerializeField] private int ammoSpace = 100;//rifle such as Kar98 with no magazine must be set to ammoSpace=5

    public bool isManualBoltOperated = false;
    public bool boltActionBeforeFiring = false;//Tipo pzb prima ha bolt action anim (reload) e poi spara, invece che il contrario
    public bool allowTopLoadIntoInstalledMagazine; // G43-like behavior

    public bool canAimWhileUsingBolt = false;
    [Range(-0.25f, 0.25f)]
    public float bolt_movementDistance = 0.07f;
    public Transform spin_pos;
    public bool boltOpenAfterLastShot = false;
    public Transform bulletEjectPos;

    public string magazineSocket = "";//null == no magazine
    public Transform magazinePosition;
    public GameObject installedMagazine { get { return magazinePosition && magazinePosition.childCount > 0 ? magazinePosition.GetChild(0).gameObject : null; } }
    public Magazine currentMagazine { get { return installedMagazine != null ? installedMagazine.GetComponent<Magazine>() : null; } }

    public GameObject disable_on_fire;

    public FireSelector fireMode = FireSelector.Single;
#if UNITY_EDITOR
    private void OnValidate()
    {
        // This runs in editor when the object is loaded/modified
        // Unity will have already migrated oldFieldName -> newFieldName
        // So the data is already there
    }
#endif

    public bool isFlamethrower { get { return compatibleAmmo == "ammo_flamethrower_1"; } }
    [Range(1, 2000)]
    public int roundPerMinute = 300;
    public int[] alternativeRoundsPerMinute = new int[0];
    
    [Range(0, 2f)]
    public float recoilIntensity = 1;

    public bool canAimWithoutBipod = true;
    public Transform AimPos { get { return aimPositions[Mathf.Max(aimPosIndex, 0)]; } }
    public bool HasAimPos { get { return aimPositions.Length > 0; } }
    [System.NonSerialized]private sbyte aimPosIndex = 0;
    public Transform[] aimPositions = new Transform[0];
    public int[] customZeroing = new int[0];
    [Range(.1f, 2.5f)]
    public float distanceBwEyeAndFrontSight = 1;
    public AdjustableSight adjustableSight = null;
    [Range(20, 65)]
    public float aimingFOV = 55;


    public Transform firePos;
    #endregion

    [Header("Attachments")]
    public SupportedAttachment[] supportedAttachments;
    public GameObject sight_down;//set down when attache scope
    public GameObject sight_up;

    [Header("FPS data")]
    public AnimationData fpsAnimations;
    [Header("Animations directly on weapon")]
    public bool playFireAnimWhenNoAmmo = false;
    public string fire_anim;
    public string fire_anim_lastBullet;

    
}


[System.Serializable]
public partial class SupportedAttachment
{
    public string attachment_id;
    public Transform attachmentPos;
    private AttachmentType type = AttachmentType.unknown;
    public bool scopeAllowReloadStripperClip=false;
    public bool allowIronSight = false;
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