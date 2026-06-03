using UnityEngine;
using System.Linq;
using System.Collections.Generic;


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
        if (!Selection.gameObjects.Contains(gameObject))
            return;

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
    [Tooltip("Optional shared weapon definition. When assigned, ammo/RPM/reload/dispersion/tracer/fire-FX/sounds are taken from this asset at runtime, and they are hidden + cleared on the prefab to keep builds light.")]
    public TurretWeaponData data;

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

    [Tooltip("ID of the base game sound. Not available in modding SDK yet.")]
    public string reload_sound = "";
    [Tooltip("Prevent aim down sight while reloading")]
    public bool preventAimWhenReloading = false;
    [Tooltip("ID of the base game animation. Not available in modding SDK yet.")]
    public TurretWeaponAnimation onFireAnimation = null;


    /// <summary>
    /// If a TurretWeaponData asset is assigned, copy its values onto this instance.
    /// Called once from TurretGun.Awake before anything reads the weapon fields.
    /// Mounting-specific fields (firePosMulti, reload_sound, preventAimWhenReloading,
    /// onFireAnimation) are left untouched. Prefabs without an asset keep their inline values.
    /// </summary>
    public void ApplyData()
    {
        if (data == null)
            return;

        ammos = (data.ammos != null) ? (TurretWeaponBullet[])data.ammos.Clone() : new TurretWeaponBullet[0];

        fireFx_id = data.fireFx_id;
        fireFxContinuous_id = data.fireFxContinuous_id;
        fireFx_bundle = data.fireFx_bundle;

        fireSoundVolume = data.fireSoundVolume;
        fireSound = data.fireSound;
        fireSound_start = data.fireSound_start;
        fireSound_loop = data.fireSound_loop;
        fireSound_tail = data.fireSound_tail;
        fireSound_distance = data.fireSound_distance;
        fireSound_distance_loop = data.fireSound_distance_loop;
        fireSound_distance_tail = data.fireSound_distance_tail;

        isRecoilless = data.isRecoilless;
        roundPerMinute = data.roundPerMinute;
        reloadWhenFiring = data.reloadWhenFiring;
        reloadTime = data.reloadTime;
        dispersionAngle = data.dispersionAngle;
        specifiedTracersRatio = data.specifiedTracersRatio;
    }
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

#if UNITY_EDITOR

/// <summary>
/// Dynamic inspector for <see cref="TurretWeapon"/> elements.
///
/// When a <see cref="TurretWeaponData"/> asset is assigned to "data":
///   - the fields the asset owns (ammo, RPM, reload, dispersion, tracer, fire FX,
///     sounds) are hidden, and
///   - they are also CLEARED on the instance so they don't stay serialized in the
///     prefab (no stale AudioClip / value references adding weight to the build).
///
/// When no asset is assigned the full authoring layout is shown (legacy workflow),
/// plus a one-click button to extract the current values into a new asset.
///
/// Editor-only file: the whole thing is wrapped in UNITY_EDITOR so it never ends
/// up in a player build, regardless of which folder you drop it in.
/// </summary>
[CustomPropertyDrawer(typeof(TurretWeapon))]
public class TurretWeaponDrawer : PropertyDrawer
{
    // Owned by TurretWeaponData -> hidden & cleared on the instance when data != null.
    static readonly string[] dataBackedFields =
    {
        "ammos",
        "fireFx_id", "fireFxContinuous_id", "fireFx_bundle",
        "fireSoundVolume",
        "fireSound", "fireSound_start", "fireSound_loop", "fireSound_tail",
        "fireSound_distance", "fireSound_distance_loop", "fireSound_distance_tail",
        "isRecoilless", "roundPerMinute", "reloadWhenFiring",
        "reloadTime", "dispersionAngle", "specifiedTracersRatio",
    };

    // Mounting / scene specific -> always shown. ("firePos" is [HideInInspector].)
    static readonly string[] mountingFields =
    {
        "firePosMulti",
        "reload_sound", "preventAimWhenReloading", "onFireAnimation",
    };

    const float Pad = 2f;

    static bool HasData(SerializedProperty property)
    {
        var dataProp = property.FindPropertyRelative("data");
        return dataProp != null && dataProp.objectReferenceValue != null;
    }

    static IEnumerable<SerializedProperty> VisibleChildren(SerializedProperty property, bool hasData)
    {
        var ordered = new List<string> { "data" };
        if (!hasData)
            ordered.AddRange(dataBackedFields);
        ordered.AddRange(mountingFields);

        foreach (var name in ordered)
        {
            var p = property.FindPropertyRelative(name);
            if (p != null) yield return p;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float h = EditorGUIUtility.singleLineHeight + Pad; // foldout
        bool hasData = HasData(property);

        if (hasData)
            h += EditorGUIUtility.singleLineHeight * 2f + Pad; // info help box
        else
            h += EditorGUIUtility.singleLineHeight + Pad;      // "extract" button

        foreach (var child in VisibleChildren(property, hasData))
            h += EditorGUI.GetPropertyHeight(child, true) + Pad;

        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // strip the SO-owned fields from the serialized prefab so they don't bloat the build.
        // edit-time only: in play mode these fields hold the values ApplyData() copied from the asset.
        if (!Application.isPlaying && HasData(property))
            ClearDataBackedFields(property);

        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = line.y + EditorGUIUtility.singleLineHeight + Pad;
            bool hasData = HasData(property);

            if (hasData)
            {
                // keep the prefab clean: strip everything the asset now provides
                ClearDataBackedFields(property);

                Rect info = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight * 2f);
                EditorGUI.HelpBox(info,
                    "Ammo, RPM, reload, dispersion, tracer, fire FX and sounds come from the assigned data asset. " +
                    "Remove the asset to edit them inline again.", MessageType.Info);
                y += EditorGUIUtility.singleLineHeight * 2f + Pad;
            }
            else
            {
                Rect btn = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btn, "Extract these values to a Turret Weapon Data asset…"))
                    CreateAssetFromInline(property);
                y += EditorGUIUtility.singleLineHeight + Pad;
            }

            foreach (var child in VisibleChildren(property, hasData))
            {
                float ch = EditorGUI.GetPropertyHeight(child, true);
                Rect r = new Rect(position.x, y, position.width, ch);
                EditorGUI.PropertyField(r, child, true);
                y += ch + Pad;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    // ---- clearing ---------------------------------------------------------

    static void ClearDataBackedFields(SerializedProperty property)
    {
        foreach (var name in dataBackedFields)
        {
            var p = property.FindPropertyRelative(name);
            if (p != null) ClearProperty(p);
        }
    }

    static void ClearProperty(SerializedProperty p)
    {
        if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
        {
            if (p.arraySize != 0) p.arraySize = 0;
            return;
        }

        switch (p.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                if (p.objectReferenceValue != null) p.objectReferenceValue = null;
                break;
            case SerializedPropertyType.String:
                if (!string.IsNullOrEmpty(p.stringValue)) p.stringValue = string.Empty;
                break;
            case SerializedPropertyType.Integer:
                if (p.intValue != 0) p.intValue = 0;
                break;
            case SerializedPropertyType.Float:
                if (p.floatValue != 0f) p.floatValue = 0f;
                break;
            case SerializedPropertyType.Boolean:
                if (p.boolValue) p.boolValue = false;
                break;
        }
    }

    // ---- extract to asset -------------------------------------------------

    static void CreateAssetFromInline(SerializedProperty property)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Turret Weapon Data", DefaultAssetName(property), "asset",
            "Choose where to save the new weapon data asset");
        if (string.IsNullOrEmpty(path)) return;

        var so = ScriptableObject.CreateInstance<TurretWeaponData>();
        var dst = new SerializedObject(so);

        foreach (var name in dataBackedFields)
        {
            var src = property.FindPropertyRelative(name);
            var d = dst.FindProperty(name);
            if (src != null && d != null) CopyProperty(src, d);
        }
        dst.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(so, path);
        AssetDatabase.SaveAssets();

        // assign it; the inline fields get auto-cleared on the next repaint
        property.FindPropertyRelative("data").objectReferenceValue = so;
        EditorGUIUtility.PingObject(so);
    }

    static string DefaultAssetName(SerializedProperty property)
    {
        string ammo = "noammo";
        var ammos = property.FindPropertyRelative("ammos");
        if (ammos != null && ammos.isArray && ammos.arraySize > 0)
        {
            var id = ammos.GetArrayElementAtIndex(0).FindPropertyRelative("bullet_id");
            if (id != null && !string.IsNullOrEmpty(id.stringValue)) ammo = id.stringValue;
        }

        int fpc = 0;
        var fpm = property.FindPropertyRelative("firePosMulti");
        if (fpm != null && fpm.isArray)
            for (int i = 0; i < fpm.arraySize; i++)
                if (fpm.GetArrayElementAtIndex(i).objectReferenceValue != null) fpc++;
        var firePos = property.FindPropertyRelative("firePos");
        if (firePos != null && firePos.objectReferenceValue != null) fpc = Mathf.Max(fpc, 1);

        string fp = fpc > 1 ? "_x" + fpc : "";
        return Sanitize("TWD_" + ammo + fp);
    }

    static string Sanitize(string s)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }

    // generic, recursive value copy between two SerializedProperties of the same shape
    static void CopyProperty(SerializedProperty src, SerializedProperty dst)
    {
        if (src.propertyType == SerializedPropertyType.Generic)
        {
            if (src.isArray)
            {
                dst.arraySize = src.arraySize;
                for (int i = 0; i < src.arraySize; i++)
                    CopyProperty(src.GetArrayElementAtIndex(i), dst.GetArrayElementAtIndex(i));
            }
            else
            {
                var child = src.Copy();
                var end = src.GetEndProperty();
                bool enter = true;
                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;
                    var dChild = dst.FindPropertyRelative(child.name);
                    if (dChild != null) CopyProperty(child, dChild);
                }
            }
            return;
        }

        switch (src.propertyType)
        {
            case SerializedPropertyType.Integer: dst.intValue = src.intValue; break;
            case SerializedPropertyType.Boolean: dst.boolValue = src.boolValue; break;
            case SerializedPropertyType.Float: dst.floatValue = src.floatValue; break;
            case SerializedPropertyType.String: dst.stringValue = src.stringValue; break;
            case SerializedPropertyType.Enum: dst.enumValueIndex = src.enumValueIndex; break;
            case SerializedPropertyType.Color: dst.colorValue = src.colorValue; break;
            case SerializedPropertyType.ObjectReference: dst.objectReferenceValue = src.objectReferenceValue; break;
            case SerializedPropertyType.Vector2: dst.vector2Value = src.vector2Value; break;
            case SerializedPropertyType.Vector3: dst.vector3Value = src.vector3Value; break;
            case SerializedPropertyType.Vector4: dst.vector4Value = src.vector4Value; break;
            case SerializedPropertyType.Rect: dst.rectValue = src.rectValue; break;
            case SerializedPropertyType.Bounds: dst.boundsValue = src.boundsValue; break;
        }
    }
}
#endif