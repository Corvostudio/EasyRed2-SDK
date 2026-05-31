using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


public partial class TurretGun : Turret
{
    [Tooltip("Weapons configured on this vehicle")]
    public TurretWeapon[] weapons = new TurretWeapon[0];


    [Tooltip("Connected animation (firing)")]
    public Animation connectedAim = null;//ANIM
    [Tooltip("Connected animation (reloading)")]
    public Animation connectedReloadAnim = null;//ANIM
    [Tooltip("Normally previous animation doesn't work when gun fires at low rate. With this you can force it.")]
    public bool forceAnimAtLowRate = false;

    [Tooltip("Units seated on seats to animate when shooting the gun (Very optional)")]
    public AnimatedSeat[] animateSeats = new AnimatedSeat[0];
    [System.Serializable]
    public struct AnimatedSeat
    {
        [Tooltip("Vehicle seat to animate the soldier on")]
        public VehicleSeat animatedSeat;
        //public float reloadAnimLenght;

        public AnimatedSeatData data;
    }

    [System.Serializable]
    public class AnimatedSeatData
    {
        public enum OnAnimation { Nothing, LeftHandAmmoReload, RightHandAmmoReload }
        public Vector3 localHandPos = new Vector3(0.08f, -0.1094f, -0.0128f);
        [Tooltip("What to do on fire animation")]
        public OnAnimation onAimation = OnAnimation.Nothing;
        [Tooltip("Spawn shell to reload after delay")]
        [Range(0, 10f)]
        public float spawnItemDelay = 2.45f;
        [Tooltip("Insert shell after delay")]
        [Range(0, 20f)]
        public float insertItemDelay = 3.9f;
        [Tooltip("ID of the shell to spawn")]
        public string shellPropID = "ShellAnimProp_75";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // SetDirty/MarkSceneDirty non sono sicuri dentro OnValidate (gira in deserializzazione).
        EditorApplication.delayCall += DeferredMigrateFirePos;
    }

    private void DeferredMigrateFirePos()
    {
        if (this == null) return;          // distrutto/ricaricato nel frattempo
        if (Application.isPlaying) return; // niente migrazione in play mode

        if (!FixFirePositionsArrays()) return;

        EditorUtility.SetDirty(this);
        var scene = gameObject.scene;
        if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            EditorSceneManager.MarkSceneDirty(scene);
    }
#endif

    private bool FixFirePositionsArrays()
    {
        bool changed = false;
        foreach (TurretWeapon tw in weapons)
        {
            if (tw == null || tw.firePos == null) continue;

            // già contenuta?
            bool isInArray = false;
            Transform[] arr = tw.firePosMulti;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == tw.firePos) { isInArray = true; break; }
                }
            }

            // append se non c'è
            if (!isInArray)
            {
                int oldLen = arr != null ? arr.Length : 0;
                Transform[] newArr = new Transform[oldLen + 1];
                for (int i = 0; i < oldLen; i++) newArr[i] = arr[i];
                newArr[oldLen] = tw.firePos;
                tw.firePosMulti = newArr;
            }

            // unassign del campo deprecato
            tw.firePos = null;
            changed = true;
        }
        return changed;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (weapons == null) return;

        for (int w = 0; w < weapons.Length; w++)
        {
            TurretWeapon tw = weapons[w];
            if (tw == null || tw.firePosMulti == null) continue;

            for (int b = 0; b < tw.firePosMulti.Length; b++)
            {
                Transform fp = tw.firePosMulti[b];
                if (fp == null) continue;

                Gizmos.color = (w == 0) ? Color.red : new Color(1f, 0.5f, 0f); // main weapon red, secondary (coax MG, etc.) orange
                Gizmos.DrawSphere(fp.position, 0.06f);

                // muzzle direction + small arrow head
                Vector3 tip = fp.position + fp.forward * 3f;
                Gizmos.DrawLine(fp.position, tip);
                Gizmos.DrawLine(tip, tip + (-fp.forward + fp.up * 0.5f).normalized * 0.25f);
                Gizmos.DrawLine(tip, tip + (-fp.forward - fp.up * 0.5f).normalized * 0.25f);

                UnityEditor.Handles.Label(fp.position, $"W{w} barrel {b}");
            }
        }
    }
#endif
}


[System.Serializable]
public partial class TurretWeapon
{
    [Tooltip("Ammo type of the weapon")]
    public TurretWeaponBullet[] ammos = new TurretWeaponBullet[1];
    [Tooltip("Fire FX when shooting the gun")]
    public string fireFx_id = "gunFireFX";
    [Tooltip("Fire FX when shooting the gun continuously (normally flamethrowers)")]
    public string fireFxContinuous_id = null;
    [Tooltip("Bundle that contains the FXs")]
    public string fireFx_bundle = "er2fxs";

    //[Tooltip("Transform position and direction to spawn the bullet from")]
    [HideInInspector]public Transform firePos;
    [Tooltip("If this weapon has multiple barrels, you can configure multiple fire positions")]
    public Transform[] firePosMulti;
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



    [Tooltip("Enable / disable recoil when firing")]
    public bool isRecoilless = false;

    [Tooltip("Rounds per minute")]
    [Range(1, 2000)]
    public int roundPerMinute = 300;
    [Tooltip("reload before firing (like a mortar), else reload after firing")]
    public bool reloadWhenFiring = false;//se true carica un colpo quando inizia a sparare. Altriemnti ricaricalo prima
    [Tooltip("Reload time duration")]
    [Range(0.1f, 200)]
    public float reloadTime = 5;
    [Tooltip("Dispersion angle of the bullet")]
    [Range(0, 25)]
    public float dispersionAngle = .1f;
    [Tooltip("Show tracer every X shots. 0 means always use normal bullet FXs.")]
    [Range(0, 40)]
    public byte specifiedTracersRatio;//0: use normal tracer behaviour; >0: show a tracer every X shots
}

[System.Serializable]
public partial struct TurretWeaponBullet
{
    [Header("bullet data ID or magazine ID (setted up as individual item in the bundle)")]
    public string bullet_id;
    //public string bullet_tracer_id;//empty when has no tracer!
    [Header("START AMMO:")]
    [Tooltip("Start ammo count")]
    [Range(0, 2500)]
    public int start_ammo_count;// = 25;
}
