#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ModTemplates : MonoBehaviour
{
    private static double next_action_time = 0;

    private static GameObject GenerateBaseRoot(string objectName, UnityEngine.Object modelObject = null)
    {
        GameObject root = new GameObject(objectName);
        root.transform.position = Vector3.zero;
        if (modelObject != null && modelObject is GameObject)
        {
            GameObject tmpModel = Instantiate((GameObject)modelObject, root.transform);
            tmpModel.transform.localPosition = Vector3.zero;
            tmpModel.transform.localRotation = Quaternion.identity;
        }
        root.AddComponent<LODGroup>();
        return root;
    }

    public static bool IsInstance(GameObject selectedGameObject)
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go == selectedGameObject)
                return true;
        }
        return false;
    }


    #region PROP

    [MenuItem("GameObject/ER2 MODS/Prop Template", false, 0)]
    [MenuItem("Assets/ER2 MODS/Prop Template", false, 0)]
    private static void OnPropTemplate()
    {
        GameObject root = GenerateBaseRoot("My Prop", Selection.activeObject);
        
    }

    #endregion

    #region UNIFORM/GEARS

    [MenuItem("GameObject/ER2 MODS/Clothing Template/HeadGear", false, 101)]
    [MenuItem("Assets/ER2 MODS/Clothing Template/HeadGear", false, 101)]
    private static void OnHeadGearTemplate()
    {
        GameObject headGear = GenerateBaseRoot("My Headgear", Selection.activeObject);
        headGear.AddComponent<ItemHelmet>();
        headGear.GetComponent<ItemHelmet>().icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/helmet_sample_icon.png", typeof(Sprite));
        BoxCollider box = headGear.AddComponent<BoxCollider>();
        box.center = new Vector3(0.005258079f, 0.02f, -0.001629297f);
        box.size = new Vector3(0.1978775f, 0.1014323f, 0.19939f);
    }

    [MenuItem("GameObject/ER2 MODS/Clothing Template/Uniform", false , 100)]
    [MenuItem("Assets/ER2 MODS/Clothing Template/Uniform", false, 100)]
    private static void OnUniformTemplate()
    {
        GameObject uni = GenerateClothingTemplate("My Uniform");
        ItemClothing ic = uni.GetComponent<ItemClothing>();
        ic.type = WearableType.uniform;
        ic.materials_fps = new Material[] {  };
        ic.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/icon_uniform_usa.png", typeof(Sprite));
        if (Selection.activeObject != null)
        {
            SkinnedMeshRenderer smr = Selection.activeGameObject? Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>():null;
            if (smr==null && Selection.activeObject is Mesh)//prova a prenderlo in un altro modo
                TPSRigPreviewManager.GetSelectedSkinnedMeshRenderer((Mesh)Selection.activeObject, out smr, out int bonesCount);
            if (smr)
            {
                ic.mesh = ic.mesh_lod = smr.sharedMesh;
                ic.materials = smr.sharedMaterials;
            }
        }

        uni.GetComponent<LODGroup>().SetLODs(new LOD[1] { new LOD(.02f, uni.GetComponentsInChildren<Renderer>()) });
    }

    [MenuItem("GameObject/ER2 MODS/Clothing Template/Gear",false, 102)]
    [MenuItem("Assets/ER2 MODS/Clothing Template/Gear", false, 102)]
    private static void OnGearTemplate()
    {
        GameObject gear = GenerateClothingTemplate("My Gear");
        gear.GetComponent<ItemClothing>().type = WearableType.gear;
        gear.GetComponent<ItemClothing>().icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/icon_gear_eng.png", typeof(Sprite));
        if (Selection.activeObject != null)
        {
            SkinnedMeshRenderer smr = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() : null;
            if (smr == null && Selection.activeObject is Mesh)//prova a prenderlo in un altro modo
                TPSRigPreviewManager.GetSelectedSkinnedMeshRenderer((Mesh)Selection.activeObject, out smr, out int bonesCount);
            if (smr)
            {
                gear.GetComponent<ItemClothing>().mesh = gear.GetComponent<ItemClothing>().mesh_lod = smr.sharedMesh;
                gear.GetComponent<ItemClothing>().materials = smr.sharedMaterials;
            }
        }

        gear.GetComponent<LODGroup>().SetLODs(new LOD[1] { new LOD(.02f, gear.GetComponentsInChildren<Renderer>()) });
    }

    private static GameObject GenerateClothingTemplate(string type)
    {
        GameObject root = GenerateBaseRoot(type + "UniformTemplate");
        root.AddComponent<ItemClothing>();
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0.01488298f, 0.05060583f, 0.02130306f);
        box.size = new Vector3(0.2884414f, 0.08745337f, 0.2993522f);
        GameObject mesh= Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Models/Clothes Item/folded_uniform_item_1.fbx", typeof(GameObject)));
        mesh.transform.SetParent(root.transform);
        return root;
    }

    #endregion

    #region WEAPONS

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Small Machine Gun", false, 200)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/SMG", false, 200)]
    private static void OnSMGTemplate()
    {
        GenericGun smg = GenerateWeaponTemplate(Selection.activeObject, "Smg"); 
        GenerateWeaponWithMagazineTemplate(smg);

        smg.compatibleAmmo = "ammo_9mm";
        smg.tpsAnims.userController = "CCV2_thompson";
        smg.fireSound= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Smg/smg_fire.ogg", typeof(AudioClip));
        smg.fireSound_distance= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Smg/distantfire_smg.wav", typeof(AudioClip));
        smg.fpsAnimations.reload_sound_full= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Magazine/generic_magazine_reload_full.mp3", typeof(AudioClip));
        smg.fpsAnimations.reload_sound_half= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Magazine/generic_magazine_reload_half.mp3", typeof(AudioClip));
        smg.fpsAnimations.animationSet = AnimationData.FPSAnimationSet.GenericMagazine;
    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/HandGun",false, 201)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/HandGun", false, 201)]
    private static void OnHandGunTemplate()
    {
        GenericGun HG = GenerateWeaponTemplate(Selection.activeObject, "Handgun");
        GenerateWeaponWithMagazineTemplate(HG,true);

        HG.compatibleAmmo = "ammo_9mm";
        HG.leftHandHoldPosition = null;
        HG.tpsAnims.userController = "CCV2_pistol";
        HG.tpsAnims.fpsAnimSet = HandheldItem.FPSAnimationSetID.pistolRun;
        HG.tpsAnims.IKWhenSprinting = false;
        HG.weaponPose = WeaponPose.pistol;
        HG.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Handgun/handgun_fire.mp3", typeof(AudioClip));
        HG.fireSound_distance = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Handgun/distantfire_handgun.wav", typeof(AudioClip));
        HG.fpsAnimations.reload_sound_full = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/HandGun/generic_HG_reload.mp3", typeof(AudioClip));
        HG.fpsAnimations.reload_sound_half = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/HandGun/generic_HG_reload.mp3", typeof(AudioClip));
        HG.fpsAnimations.animationSet = AnimationData.FPSAnimationSet.GenericHandgun;

    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Bolt Action", false, 202)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Bolt Action", false, 202)]
    private static void OnBoltActionTemplate()
    {
        GenericGun gg = GenerateWeaponTemplate(Selection.activeObject, "Bolt Action");

        gg.compatibleAmmo = "ammo_792mm";
        gg.isManualBoltOperated = true;
        gg.tpsAnims.userController = "CCV2_rifle";
        gg.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rifles/rifle_fire.wav", typeof(AudioClip));
        gg.fireSound_distance = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rifles/distance_fire_generic.wav", typeof(AudioClip));
        gg.fpsAnimations.boltaction_sound= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Bolt Action/bolt_action.ogg", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound_open = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Bolt Action/bolt_action_open.ogg", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Bolt Action/bolt_feed.ogg", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound_close = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Bolt Action/bolt_action_close.ogg", typeof(AudioClip));
        gg.fpsAnimations.animationSet = AnimationData.FPSAnimationSet.GenericBoltAction;
    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Machine Gun", false, 203)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Machine Gun", false, 203)]
    private static void OnMGTemplate()
    {
        GenericGun gg = GenerateWeaponTemplate(Selection.activeObject, "MG");

        gg.compatibleAmmo = "ammo_792mm";
        gg.tpsAnims.userController = "CCV2_MG";
        gg.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rifles/rifle_fire.wav", typeof(AudioClip));
        gg.fireSound_distance = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/MG/distant_mg.wav", typeof(AudioClip));
        gg.fpsAnimations.reload_sound_full = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/MG/generic_MG_reload.mp3", typeof(AudioClip));
        gg.fpsAnimations.reload_sound_half = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Magazine/weapon_magazine_reload_half.ogg", typeof(AudioClip));
        gg.fpsAnimations.animationSet = AnimationData.FPSAnimationSet.GenericMG;
    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Pump Action", false, 204)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Pump Action", false, 204)]
    private static void OnPumpActionTemplate()
    {
        GenericGun gg = GenerateWeaponTemplate(Selection.activeObject, "Pump Action");

        gg.compatibleAmmo = "ammo_12gauge";
        gg.bulletDispersion = 1.6f;
        gg.tpsAnims.userController = "CCV2_shotgun";
        gg.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/shotgun_fire.wav", typeof(AudioClip));
        gg.fireSound_distance = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/distantfire_shotgun.wav", typeof(AudioClip));
        gg.fpsAnimations.boltaction_sound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/shotgun_action.mp3", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound_open = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/shotgun_open_n_close_action.wav", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/shotgun_feed.mp3", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound_close = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/shotgun_open_n_close_action.wav", typeof(AudioClip));
        gg.fpsAnimations.chamber_sound_close_noAmmo = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Pump Action/shotgun_close_action_empty.wav", typeof(AudioClip));
        gg.fpsAnimations.animationSet = AnimationData.FPSAnimationSet.GenericShotgun;
    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Rocket Launcher", false, 205)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Rocket Launcher", false, 205)]
    private static void OnRocketLauncheTemplate()
    {
        GenericGun gg = GenerateWeaponTemplate(Selection.activeObject, "Rocket Launcher");

        gg.compatibleAmmo = "bazooka_rocket";
        gg.tpsAnims.userController = "CCV2_bazooka";
        gg.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rocket Launcher/rocket_launcher_fire.ogg", typeof(AudioClip));
        gg.fireSound_distance = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rocket Launcher/distantfire_rocket_launcher.wav", typeof(AudioClip));
        gg.fpsAnimations.reload_sound_full = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rocket Launcher/rocket_launcher_reload.ogg", typeof(AudioClip));
        gg.fpsAnimations.animationSet = AnimationData.FPSAnimationSet.GenericRocketLauncher;
    }
    [MenuItem("GameObject/ER2 MODS/Weapon Template/Grenade and Mines", false, 306)]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Grenade and Mines", false, 306)]
    private static void OnGrenadeTemplate()
    {
        ItemGrenade gg = GenerateBaseRoot("Grenade Template", Selection.activeObject).AddComponent<ItemGrenade>();
        gg.grenadeSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Grenade/grenade_throw_sound.ogg", typeof(AudioClip));
        gg.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/gun_icon.png", typeof(Sprite));

        ImpactSoundManager ism = gg.gameObject.AddComponent<ImpactSoundManager>();
        ism.impactSound_generic = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Generic/item_impact_generic.ogg", typeof(AudioClip));
        ism.impactSound_concrete = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Generic/item_impact_concrete.ogg", typeof(AudioClip));

        BoxCollider box = gg.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 0, 0);
        box.size = new Vector3(0.065f, 0.11f, 0.065f);
    }

    /*[MenuItem("GameObject/ER2 MODS/Items/Weapon Attachments/Bayonet", false, 301)]
    [MenuItem("Assets/ER2 MODS/Items/Weapon Attachments/Bayonet", false, 301)]
    private static void SetupBayonet()
    {
        GameObject root = new GameObject("My Bayonet");
        if (Selection.activeObject != null && Selection.activeObject is GameObject)
        {

        }

    }*/




    [MenuItem("GameObject/ER2 MODS/Items/Life Recover Item", false, 301)]
    [MenuItem("Assets/ER2 MODS/Items/Life Recover Ite", false, 301)]
    private static void LifeRecoverItemTemplate()
    {
        GameObject root = new GameObject("LifeRecoverItemTemplate");
        root.transform.position = Vector3.zero;
        if (Selection.activeObject != null && Selection.activeObject is GameObject)
        {
            GameObject tmpModel = Instantiate((GameObject)Selection.activeObject, root.transform);
            tmpModel.transform.localPosition = Vector3.zero;
            tmpModel.transform.localRotation = Quaternion.identity;
        }

        root.AddComponent<LODGroup>();
        root.AddComponent<ItemObjectRecoverLife>();

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0,0,0);
        box.size = new Vector3(0.2f, 0.1f, 0.3f);
    }

    private static GenericGun GenerateWeaponTemplate(UnityEngine.Object activeObject,string weaponType)
    {
        GameObject root = GenerateBaseRoot("My "+ weaponType , activeObject);

        BoxCollider box = root.AddComponent<BoxCollider>();
        if (weaponType == "Handgun")
        {
            box.center = new Vector3(0.02f, 0.0722841f, 0.06f);
            box.size = new Vector3(0.06f, 0.25f, 0.32f);
        }
        else if (weaponType == "Rocket Launcher")
        {
            box.center = new Vector3(0.02f, 0.06386377f, 0.2351769f);
            box.size = new Vector3(0.05999999f, 0.3485218f, 1.521792f);
        }
        else
        {
            box.center = new Vector3(0, 0.0722841f, .1368029f);
            box.size = new Vector3(0.01728112f, 0.1162274f, 0.7679455f);
        }
        GenericGun defaultValues = root.AddComponent<GenericGun>();
        defaultValues.fpsAnimations = new AnimationData();

        GameObject weaponSightPosition = new GameObject("weaponSightPosition");
        weaponSightPosition.transform.SetParent(root.transform);
        weaponSightPosition.transform.localPosition = new Vector3(0, 0.125f, -0.010f);
        defaultValues.aimPositions = new Transform[] { weaponSightPosition.transform };


        GameObject firePosition = new GameObject("firePosition");
        firePosition.transform.SetParent(root.transform);
        if (weaponType != "Handgun")
            firePosition.transform.localPosition = new Vector3(0, 0.085f, 0.795f);
        else
            firePosition.transform.localPosition = new Vector3(0, 0.085f, 0.22f);
        defaultValues.firePos = firePosition.transform;

        GameObject ejectPosition = new GameObject("ejectPosition");
        ejectPosition.transform.SetParent(root.transform);
        ejectPosition.transform.localPosition = new Vector3(0.025f, 0.085f, 0.120f);
        defaultValues.bulletEjectPos = ejectPosition.transform;

        GameObject LFHandPosition = new GameObject("LFHandPosition");
        LFHandPosition.transform.SetParent(root.transform);
        LFHandPosition.transform.localPosition = new Vector3(-0.049f, -0.0241f, 0.252f);
        LFHandPosition.transform.localEulerAngles = new Vector3(-0.491f, 49.988f, -200.85f);
        defaultValues.leftHandHoldPosition = LFHandPosition.transform;

        defaultValues.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/gun_icon.png", typeof(Sprite));

        return defaultValues;
    }

    private static void GenerateWeaponWithMagazineTemplate(GenericGun gunRoot,bool isPistol=false)
    {
        GameObject magPosition = new GameObject("magPosition");
        magPosition.transform.SetParent(gunRoot.transform);
        magPosition.transform.localPosition = isPistol ?
            new Vector3(0, 0, 0)://pistol mag pos
            new Vector3(0, 0, 0.160f);//rifle mag pos
        gunRoot.magazinePosition = magPosition.transform;
    }

    public static bool IsNewActionTime()
    {
        if (EditorApplication.timeSinceStartup > next_action_time)
        {
            next_action_time = EditorApplication.timeSinceStartup + .5f;
            return true;
        }
        return false;
    }

    private static List<GameObject> GetSelectedWeaponAndgameObjects(out GenericGun rootGun)
    {
        rootGun = null;
        List<GameObject> selectedGameObject = new List<GameObject>();
        foreach (UnityEngine.Object go in Selection.objects)
        {
            if (go is GameObject)
            {
                selectedGameObject.Add((GameObject)go);
                if (!rootGun && ((GameObject)go).GetComponentInParent<GenericGun>())
                {
                    rootGun = ((GameObject)go).GetComponentInParent<GenericGun>();
                }
            }
        }
        return selectedGameObject;
    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Gun Accessories/Magazine")]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Gun Accessories/Magazine")]
    private static void GenerateMagazine()
    {
        if (!IsNewActionTime())
            return;

        string genericSocketName = "Example Socket";


        //get list of selected gameobjects
        List<GameObject> selectedGameObjects = GetSelectedWeaponAndgameObjects(out GenericGun rootGun);


        //if generic gun found in one of the roots
        if (rootGun)
        {
            rootGun.magazineSocket = genericSocketName;

            //crea magazine position if missing
            if (rootGun.magazinePosition==null)
            {
                rootGun.magazinePosition = new GameObject("magPosition").transform;
                rootGun.magazinePosition.transform.SetParent(rootGun.transform);
            }
        }

        //crea magazine template
        Magazine magazine = new GameObject("GenericMagazine").AddComponent<Magazine>();
        magazine.socket = genericSocketName;
        magazine.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/mag_icon.png", typeof(Sprite));


        //fix mag position of the generic gun
        if (rootGun)
        {
            magazine.compatibleAmmo = rootGun.compatibleAmmo;
            if (!string.IsNullOrEmpty(rootGun.magazineSocket))
                magazine.socket = rootGun.magazineSocket;

            if (selectedGameObjects.Count > 0)
            {
                rootGun.magazinePosition.transform.position = selectedGameObjects[0].transform.position;
            }
            else
            {
                //assegna posizione placeholder
                bool isPistol = rootGun.tpsAnims.fpsAnimSet == HandheldItem.FPSAnimationSetID.pistolRun;
                rootGun.magazinePosition.transform.localPosition = isPistol ?
                    new Vector3(0, 0, 0) ://pistol mag pos
                    new Vector3(0, 0, 0.160f);//rifle mag pos
            }
        }

        //fix magazine position of the Magazine gameobject
        magazine.transform.position = rootGun ?
            rootGun.magazinePosition.transform.position :
            Vector3.zero;


        //reparent or instantiate
        ReparentGunAccessory(selectedGameObjects, magazine.transform);

        //finalize with last scripts
        BoxCollider box = magazine.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0.004863327f, -0.06637072f, -0.0008514896f);
        box.size = new Vector3(0.04015234f, 0.1462805f, 0.09086706f);
        magazine.gameObject.AddComponent<LODGroup>();
    }

    [MenuItem("GameObject/ER2 MODS/Weapon Template/Gun Accessories/Simple Scope")]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Gun Accessories/Simple Scope")]
    private static void GenerateAttachmentScopeSimple()
    {
        if (!IsNewActionTime()) return;

        AttachmentScope attachment = (AttachmentScope)SetUpAttachment(typeof(AttachmentScope), "Scope");
        attachment.aimPos = new GameObject("CamPos").transform;
        attachment.aimPos.SetParent(attachment.transform);
        attachment.aimPos.transform.localPosition = new Vector3(0, 0.0251f, -.2f);

        //finalize with last scripts
        attachment.GetComponent<BoxCollider>().center = new Vector3(0.004863327f, 0.01609762f, 0.006838739f);
        attachment.GetComponent<BoxCollider>().size = new Vector3(0.04015234f, 0.0559434f, 0.1695825f);
    }
    [MenuItem("GameObject/ER2 MODS/Weapon Template/Gun Accessories/Magnification Scope")]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Gun Accessories/Magnification Scope")]
    private static void GenerateAttachmentScopeMagnification()
    {
        if (!IsNewActionTime()) return;

        AttachmentScope attachment = (AttachmentScope)SetUpAttachment(typeof(AttachmentScope), "Scope");

        //finalize with last scripts
        attachment.GetComponent<BoxCollider>().center = new Vector3(0.004863327f, 0.01609762f, 0.006838739f);
        attachment.GetComponent<BoxCollider>().size = new Vector3(0.04015234f, 0.0559434f, 0.1695825f);


        GameObject subScope = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Prefabs/Magnification.prefab", typeof(GameObject)));
        subScope.transform.SetParent(attachment.transform);
        subScope.transform.localPosition = new Vector3(0, 0.0251f, -0.105f);
        attachment.aimPos = new GameObject("CamPos").transform;
        attachment.aimPos.SetParent(subScope.transform);
        attachment.aimPos.transform.localPosition = new Vector3(0, 0, -.12f);
    }
    [MenuItem("GameObject/ER2 MODS/Weapon Template/Gun Accessories/Bipod")]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Gun Accessories/Bipod")]
    private static void GenerateAttachmentBipod()
    {
        if (!IsNewActionTime()) return;

        AttachmentBipod attachment = (AttachmentBipod)SetUpAttachment(typeof(AttachmentBipod), "Bipod");
        Animation anim = attachment.gameObject.AddComponent<Animation>();
        anim.playAutomatically = false;
        AnimationClip bipodOpenAni = (AnimationClip)AssetDatabase.LoadAssetAtPath("Assets/Examples/Weapons/Custom Animations/Generic_Bipod_Open.anim", typeof(AnimationClip));
        anim.clip = bipodOpenAni;
        anim.AddClip(bipodOpenAni, bipodOpenAni.name);

        //finalize with last scripts
        attachment.GetComponent<BoxCollider>().center = new Vector3(0.004863327f, 0.01609762f, 0.006838739f);
        attachment.GetComponent<BoxCollider>().size = new Vector3(0.04015234f, 0.0559434f, 0.1695825f);
    }
    [MenuItem("GameObject/ER2 MODS/Weapon Template/Gun Accessories/Bayonet")]
    [MenuItem("Assets/ER2 MODS/Weapon Template/Gun Accessories/Bayonet")]
    private static void GenerateAttachmentBayonet()
    {
        if (!IsNewActionTime()) return;

        AttachmentBayonet attachment = (AttachmentBayonet)SetUpAttachment(typeof(AttachmentBayonet), "Bayonet");

        //finalize with last scripts
        attachment.GetComponent<BoxCollider>().center = new Vector3(0.0001868731f, -0.00110326f, 0.197107f);
        attachment.GetComponent<BoxCollider>().size = new Vector3(0.02367238f, 0.04378714f, 0.5574284f);

    }



    [MenuItem("Assets/ER2 MODS/Terrain/Create Terrain Detail 2D")]
    private static void GenerateTerrainDetail2D()
    {
        if (!IsNewActionTime()) return;
        GenerateTerrainDetail(0);
    }
    [MenuItem("Assets/ER2 MODS/Terrain/Create Terrain Detail X")]
    private static void GenerateTerrainDetailX()
    {
        if (!IsNewActionTime()) return;
        GenerateTerrainDetail(1);
    }
    private static void GenerateTerrainDetail(int type)
    {
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            if (obj is Texture)
            {
                Texture selectedTex = (Texture)obj;
                GameObject grass_patches = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Models/Terrain Details/grass_detail_simple.fbx", typeof(GameObject));

                string name = "Example Terrain Detail " + (type == 0 ? "2D " : "X ");
                GameObject patch_go =
                    type == 0 ?
                    Instantiate(grass_patches.transform.Find("grass_detail_simple_plane").gameObject) :
                    Instantiate(grass_patches.transform.Find("grass_detail_simple_cross").gameObject);
                patch_go.transform.localEulerAngles = Vector3.zero;
                patch_go.transform.localScale = Vector3.one;
                Material mat = Instantiate((Material)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Models/Terrain Details/GrassSampleMat.mat", typeof(Material)));
                mat.mainTexture = selectedTex;
                patch_go.GetComponent<Renderer>().sharedMaterial = mat;
                patch_go.name = name + selectedTex.name;
                CreateTag("TerrainDetail");
                patch_go.tag = "TerrainDetail";
                patch_go.AddComponent<TerrainDetailData>();

                string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(obj));
                AssetDatabase.CreateAsset(mat, path+"/"+ patch_go.name+"_Material.mat");
            }
            else
            {
                Debug.LogError("Make sure to select a texture! "+ obj, obj);
            }
        }
    }

    private static Attachment SetUpAttachment(System.Type componentToAdd, string name)
    {
        //get list of selected gameobjects
        List<GameObject> selectedGameObjects = GetSelectedWeaponAndgameObjects(out GenericGun rootGun);

        //crea attachment template
        Attachment attachment = (Attachment)new GameObject(
            selectedGameObjects.Count > 0 ?
            name+" ("+selectedGameObjects[0].name+")" : 
            "My "+ name
        ).AddComponent(componentToAdd);

        //fix scope position in first selected mesh positon
        AdjustAccessoryPosition(attachment.transform, selectedGameObjects);

        //reparent or instantiate
        ReparentGunAccessory(selectedGameObjects, attachment.transform);

        //add attachment in the supported atachment list
        RegisterAttachmentInWeapon(rootGun, attachment, name);

        //icon
        attachment.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/att_icon.png", typeof(Sprite));

        //add final components
        attachment.gameObject.AddComponent<BoxCollider>();
        attachment.gameObject.AddComponent<LODGroup>();

        return attachment;
    }



    private static void AdjustAccessoryPosition(Transform transform, List<GameObject> selectedGameObjects)
    {
        transform.position = selectedGameObjects.Count > 0 && IsInstance(selectedGameObjects[0]) ?//se è selezionato una mesh di uno scope, ed è attiva nell'hierarchy
            selectedGameObjects[0].transform.position ://usala come posizione
            Vector3.zero;//mettila al centro del mondo
    }

    private static void ReparentGunAccessory(List<GameObject> selectedgameObjects, Transform accessory)
    {
        foreach (GameObject sel in selectedgameObjects)
        {
            UnpackPrefab(sel);
            if (IsInstance(sel))
                sel.transform.SetParent(accessory);
            else
                Instantiate(sel, accessory.position, accessory.rotation, accessory);
        }
    }

    private static void RegisterAttachmentInWeapon(GenericGun rootGun, Attachment attachment, string attachment_name)
    {
        if (rootGun)
        {
            //add to array
            if (rootGun.supportedAttachments == null)
                rootGun.supportedAttachments = new SupportedAttachment[0];
            SupportedAttachment[] newAttachmentList = new SupportedAttachment[rootGun.supportedAttachments.Length + 1];
            for (int i = 0; i < rootGun.supportedAttachments.Length; i++)
                newAttachmentList[i] = rootGun.supportedAttachments[i];
            rootGun.supportedAttachments = newAttachmentList;
            SupportedAttachment newAttachment = new SupportedAttachment();
            rootGun.supportedAttachments[rootGun.supportedAttachments.Length - 1] = newAttachment;

            //set properties
            newAttachment.attachment_id = attachment.name;
            newAttachment.attachmentPos = new GameObject(attachment_name + " pos " + rootGun.supportedAttachments.Length).transform;
            newAttachment.attachmentPos.transform.position = attachment.transform.position;
            newAttachment.attachmentPos.transform.SetParent(rootGun.transform);
        }
    }




    #endregion

    #region BUILDING

    [MenuItem("GameObject/ER2 MODS/Buildings/Building Template", false, 300)]
    [MenuItem("Assets/ER2 MODS/Building Template", false, 300)]
    private static void OnBuildingTemplate()
    {
       GameObject building =GenerateBaseRoot("My Building", Selection.activeObject);
        
        DestructibleManager ds= building.AddComponent<DestructibleManager>();
        int parts_count = 2;
        ds.destructibleParts = new DestructableBuilding[parts_count];
        for (int i = 0; i < parts_count; i++)
        {
            //create sub detsructible part
            GameObject part = new GameObject("BuildingPart_" + i);
            DestructibleBuildingPhased bPart = part.AddComponent<DestructibleBuildingPhased>();
            part.transform.SetParent(building.transform);
            ds.destructibleParts[i] = part.GetComponent<DestructibleBuildingPhased>();

            //create intact and detsroyed versions of the part
            GameObject intactPart = new GameObject("Intact_"+i+" (Intact mesh should be here)");
            GameObject destrPart = new GameObject("Destroyed_"+i+ " (Destroyed mesh should be here)");
            intactPart.transform.position = destrPart.transform.position = building.transform.position;
            intactPart.transform.SetParent(part.transform);
            destrPart.transform.SetParent(part.transform);
            //SetUpAsdamageablePart(intactPart);
            //SetUpAsdamageablePart(destrPart);
            destrPart.gameObject.SetActive(false);
            bPart.statusPhases = new BuildingStatus[2];
            bPart.statusPhases[0].destructionLevel_objects = new GameObject[] { intactPart };
            bPart.statusPhases[1].destructionLevel_objects = new GameObject[] { destrPart };
        }

        building.AddComponent<DoorRegister>();

    }



    [MenuItem("GameObject/ER2 MODS/Buildings/Add Collisions to damageable part", false, 300)]
    private static void SetUpAsdamageablePart()
    {
        if (!IsNewActionTime())
            return;

        foreach (UnityEngine.Object selected in Selection.objects)
        {
            if (selected is GameObject && IsInstance((GameObject)selected))
                SetUpAsdamageablePart(((GameObject)selected));
        }
    }
    private static void SetUpAsdamageablePart(GameObject part)
    {
        if (part != null)
        {
            if (!part.GetComponent<BuildingImpact>())
                part.AddComponent<BuildingImpact>();
            if (!part.GetComponent<MeshCollider>())
                part.AddComponent<MeshCollider>();
            //layer?
            //cover positions?
            //foot step specifier
        }
    }


    [MenuItem("GameObject/ER2 MODS/Buildings/Convert Mesh into collision", false, 300)]
    private static void SetUpCollisionAndRemoveMeshPart()
    {
        if (!IsNewActionTime())
            return;

        foreach (UnityEngine.Object selected in Selection.objects)
        {
            if (selected is GameObject && IsInstance((GameObject)selected))
                SetUpCollisionAndRemoveMesh(((GameObject)selected));
        }
    }
    private static void SetUpCollisionAndRemoveMesh(GameObject part)
    {
        if (part != null)
        {
            if (!part.GetComponent<BuildingImpact>())
                part.AddComponent<BuildingImpact>();
            if (!part.GetComponent<MeshCollider>())
                part.AddComponent<MeshCollider>();
            //layer?
            //cover positions?
            //foot step specifier
            if (part.GetComponent<MeshRenderer>())
                DestroyImmediate(part.GetComponent<MeshRenderer>());
            if (part.GetComponent<MeshFilter>())
                DestroyImmediate(part.GetComponent<MeshFilter>());
        }
    }


    #endregion

    #region VEHICLES



    [MenuItem("GameObject/ER2 MODS/Vehicle/Static", false, 399)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Static", false, 399)]
    public static void OnStaticVehicleTemplate()
    {
        GameObject root = GenerateBaseRoot("My Static Vehicle", Selection.activeGameObject);

        root.AddComponent<VehicleDamagablePart>();
        root.AddComponent<InventoryManager>();
        root.AddComponent<SyncVehicle>();

        Vehicle mv = root.AddComponent<Vehicle>();
        mv.playerCanDriveFromAnySeat = false;
        SetUpStaticVehicle(mv);
    }

    private static void SetUpStaticVehicle(Vehicle vehicle)
    {
        //add collisions
        BoxCollider cl1 = vehicle.gameObject.AddComponent<BoxCollider>();
        cl1.center = new Vector3(-0.003900528f, 1.089162f, 0.1564527f);
        cl1.size = new Vector3(1.442336f, 0.7008705f, 0.2050485f);

        BoxCollider cl2 = vehicle.gameObject.AddComponent<BoxCollider>();
        cl2.center = new Vector3(-0.003900528f, 0.6063746f, -0.2254975f);
        cl2.size = new Vector3(1.782511f, 0.3447463f, 0.9111093f);

        vehicle.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite));

        //SEAT PARTS
        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(vehicle.transform);
        List<GameObject> seatList = GenerateSeatList(seats, vehicle, 2, false);
        seatList[0].transform.localPosition = new Vector3(-0.388f, 0.25f, -1.107f);
        vehicle.seats[0].isDriver = false;
        vehicle.seats[0].seat.onSeatAnimator_id = "OnVehicleSeat_Pak40_user";
        seatList[1].transform.localPosition = new Vector3(0.35f, 0.25f, -1.107f);
        vehicle.seats[1].isDriver = true;
        vehicle.seats[1].seat.onSeatAnimator_id = "OnVehicleSeat_Pak40_secondplace";
    }




    #region LAND VEHICLES
    [MenuItem("GameObject/ER2 MODS/Vehicle/Wheeled Template", false, 400)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Wheeled Template", false, 400)]
    public static void OnWheeledTemplate()
    {
        GameObject root = GenerateBaseRoot("My Wheeled Vehicle", Selection.activeGameObject);
        GenerateVehicleTemplate(root, 1200f, false);
        VehicleWithWheels mv = root.AddComponent<VehicleWithWheels>();
        BoxCollider[] boxes = new BoxCollider[2];
        SetUpMovableVehicle(mv, out GameObject mufflerSmoke, (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite)), boxes, 8);
        
        //HITBOX REPOSITIONING
        
        boxes[0].center = new Vector3(-0.003900528f, 1.728621f, 0.746038f);
        boxes[0].size = new Vector3(1.442336f, 0.6330223f, 0.1117871f);
        boxes[1].center = new Vector3(-0.003900528f, 0.992584f, -0.002175093f);
        boxes[1].size = new Vector3(1.782511f, 0.8848135f, 4.471274f);
        mv.maxMotorTorque = 800;
        mv.accelerationSpeed = 50;
        mv.maxKmhSpeed = 30;

        //load and assign particle system

        mufflerSmoke.transform.localPosition = new Vector3(-.27f, 0.519f, -2.111f);
        root.GetComponent<MovableVehicle>().mufflerSmoke = new ParticleSystem[] { mufflerSmoke.GetComponent<ParticleSystem>() };

        //INTERNAL PARTS

        //Engine
        mv.internalParts[0].transform.localPosition = new Vector3(0, 0.967f, 1.725f);
        mv.internalParts[0].width = 0.865f;
        mv.internalParts[0].height = 0.674f;
        //Gear
        mv.internalParts[1].transform.localPosition = new Vector3(0, 0.798f, 1.019f);
        mv.internalParts[1].width = 1.419f;
        mv.internalParts[1].height = 0.804f;
        //Fuel Tank
        mv.internalParts[2].transform.localPosition = new Vector3(-0.978f, 1.003f, -1.571f);
        mv.internalParts[2].width = 0.407f;
        mv.internalParts[2].height = 0.407f;
        //Transmission
        mv.internalParts[3].transform.localPosition = new Vector3(0, 0.662f, 0.022f);
        mv.internalParts[3].width = 1.377f;
        mv.internalParts[3].height = 0.121f;



        //SEATS REPOSITIONING FOR TEMPLATE


        mv.seats[0].seat.transform.localPosition = new Vector3(-0.388f, 0.75f, 0.401f);
        mv.seats[1].seat.transform.localPosition = new Vector3(0.335f, 0.811f, 0.247f);
        mv.seats[2].seat.transform.localPosition = new Vector3(-0.515f, 0.745f, -0.474f);
        mv.seats[2].seat.transform.localEulerAngles = new Vector3(0, 90, 0);
        mv.seats[3].seat.transform.localPosition = new Vector3(-0.515f, 0.745f, -1.099f);
        mv.seats[3].seat.transform.localEulerAngles = new Vector3(0, 90, 0);
        mv.seats[4].seat.transform.localPosition = new Vector3(-0.515f, 0.745f, -1.761f);
        mv.seats[4].seat.transform.localEulerAngles = new Vector3(0, 90, 0);
        mv.seats[5].seat.transform.localPosition = new Vector3(0.422f, 0.745f, -0.474f);
        mv.seats[5].seat.transform.localEulerAngles = new Vector3(0, -90, 0);
        mv.seats[6].seat.transform.localPosition = new Vector3(0.422f, 0.745f, -1.099f);
        mv.seats[6].seat.transform.localEulerAngles = new Vector3(0, -90, 0);
        mv.seats[7].seat.transform.localPosition = new Vector3(0.422f, 0.745f, -1.761f);
        mv.seats[7].seat.transform.localEulerAngles = new Vector3(0, -90, 0);


        //Wheels sound
        GameObject wheels_sound = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Prefabs/WheelsSound.prefab", typeof(GameObject)));
        wheels_sound.transform.SetParent(mv.gameObject.transform);


        root.AddComponent<RigidbodyMassSetter>().localMassPos = new Vector3(0, .5f, 0);
        mv.crashSound= (AudioClip)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/vehicle_crash_1.ogg", typeof(AudioClip)));

    }
    public static void SetUpMovableVehicle(MovableVehicle vehicle, out GameObject muffleSmoke_root, Sprite icon,  BoxCollider[] bxCol, int numberOfSits = 4)
    {
        //LOAD COLLIDER
        for(int i=0; i< bxCol.Length; i++)
        {
            
            BoxCollider boxCollider = vehicle.gameObject.AddComponent<BoxCollider>();
            bxCol[i] = boxCollider;
        }

        //MUFFLER SMOKE
        muffleSmoke_root = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/particles/MufflerSmoke.prefab", typeof(GameObject)));
        muffleSmoke_root.transform.SetParent(vehicle.transform);

        //SOUNDS
        //load + assign sounds
        vehicle.engine_start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Start_Up_Exterior.wav", typeof(AudioClip));
        vehicle.engine_stop = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Turn_Off_Exterior.wav", typeof(AudioClip));
        vehicle.engine_move = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Engine_Loop_Exterior.wav", typeof(AudioClip));


        //ASSIGN THE ICON
        vehicle.icon= icon;


        //INTERNAL PARTS

        vehicle.internalParts = new InternalVehiclePart[4];
        GameObject internalParts = new GameObject("InternalParts");
        internalParts.transform.SetParent(vehicle.transform);
        for(int i=0; i<4; i++)
        {
            InternalVehiclePart part = AddPart(internalParts, (VehicleInternalPart) i);
            vehicle.internalParts[i] = part;
        }



        //SEAT PARTS

        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(vehicle.transform);
        GenerateSeatList(seats, vehicle, numberOfSits, false);



    }


    [MenuItem("GameObject/ER2 MODS/Vehicle/Tank Template", false, 401)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Tank Template", false, 401)]
    public static void OnTanktemplate()
    {
        GameObject root = GenerateBaseRoot("My Tank Vehicle", Selection.activeGameObject);
        GenerateVehicleTemplate(root, 5000, false);
        root.AddComponent<TrackWeelsUpdater>();
        BoxCollider[] boxes = new BoxCollider[1];
        VehicleTank mv = root.AddComponent<VehicleTank>();
        SetUpMovableVehicle(mv, out GameObject mufflerSmoke, (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite)), boxes, 3);
        root.GetComponent<VehicleDamagablePart>().armorThickness= 50;
        root.GetComponent<VehicleDamagablePart>().impactType = ImpactType.metal;
        mv.maxMotorTorque = 1200;
        mv.accelerationSpeed = 40;
        mv.maxKmhSpeed = 18;

        //COLLIDER
        boxes[0].center = new Vector3(0.04046845f, 0.6752116f, -0.3028845f);
        boxes[0].size = new Vector3(2.399778f, 0.817413f, 3.269618f);
        GameObject frontBox = new GameObject("FrontBoxCollider");
        frontBox.transform.SetParent(root.transform);
        frontBox.transform.localPosition = new Vector3(0, 0.432f, 1.576f);
        frontBox.transform.localEulerAngles = new Vector3(-69.076f, 0, 0);
        BoxCollider fb= frontBox.AddComponent<BoxCollider>();
        fb.center = new Vector3(-0.0156033f, -0.05171413f, 0.3228208f);
        fb.size = new Vector3(2.442272f, 1.136573f, 0.3543585f);
        frontBox.AddComponent<VehicleDamagablePart>();
        frontBox.GetComponent<VehicleDamagablePart>().impactType = ImpactType.metal;

        //PARTICLE SYS

        root.GetComponent<MovableVehicle>().mufflerSmoke = new ParticleSystem[] { mufflerSmoke.GetComponent<ParticleSystem>() };

        //INTERNAL PARTS

        //Engine
        mv.internalParts[0].transform.localPosition = new Vector3(0, 0.967f, -0.263f);
        mv.internalParts[0].width = 1.494f;
        mv.internalParts[0].height = 0.674f;
        //Gear
        mv.internalParts[1].transform.localPosition = new Vector3(0, 0.577f, 0.204f);
        mv.internalParts[1].width = 0.54f;
        mv.internalParts[1].height = 1.619f;
        //Fuel Tank
        mv.internalParts[2].transform.localPosition = new Vector3(0.955f, 0.76f, -1.32f);
        mv.internalParts[2].width = 0.575f;
        mv.internalParts[2].height = 0.407f;
        //Transmission
        mv.internalParts[3].transform.localPosition = new Vector3(0, 0.54f, 1.225f);
        mv.internalParts[3].width = 1.377f;
        mv.internalParts[3].height = 0.453f;



        //SEATS REPOSITIONING FOR TEMPLATE
        mv.seats[0].seat.transform.localPosition = new Vector3(0, -0.185f, 1.361f);
        mv.seats[0].seat.gameObject.SetActive(false);
        mv.seats[1].seat.transform.localPosition = new Vector3(-0.37f, 0.181f, 0.247f);
        mv.seats[1].seat.gameObject.SetActive(false);
        mv.seats[2].seat.transform.localPosition = new Vector3(0.32f, 0.429f, -0.77f);
        mv.seats[2].seat.gameObject.SetActive(false);


        //tracks sound
        GameObject tracks_sound = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Prefabs/TankTruckSound.prefab", typeof(GameObject)));
        tracks_sound.transform.SetParent(mv.gameObject.transform);


        root.AddComponent<RigidbodyMassSetter>().localMassPos = new Vector3(0, .5f, 0);
        mv.crashSound = (AudioClip)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/vehicle_crash_1.ogg", typeof(AudioClip)));

    }

    #endregion

    #region AUTO TRANSPORT

    [MenuItem("GameObject/ER2 MODS/Vehicle/Auto Transport Boat Template", false, 402)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Auto Transport Boat Template", false, 402)]
    public static void OnATBoatTemplate()
    {
        GameObject root = GenerateBaseRoot("My AT Boat", Selection.activeGameObject);
        root.AddComponent<SyncVehicle>();
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 0.3385902f, -0.321753f);
        box.size = new Vector3(1.978139f, 0.4710519f, 5.656177f);
        LandingCraft mv = root.AddComponent<LandingCraft>();
        mv.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite));

        mv.maxKmhSpeed = 18;

        // AUDIO
        mv.engine_start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Start_Up_Exterior.wav", typeof(AudioClip));
        mv.engine_move = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Engine_Loop_Exterior.wav", typeof(AudioClip));
        mv.whistle= (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/whistle_lcvp.ogg", typeof(AudioClip));
        
        //Particle Sys
        GameObject ms = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Particles/LCVP water FX.prefab", typeof(GameObject)));
        ms.transform.SetParent(root.transform);
        ms.transform.localPosition = new Vector3(-0.04f, -0.028f, 2.759f);
        mv.waterSplash = ms.GetComponent<ParticleSystem>(); 

        //Seats

        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(root.transform);
        List<GameObject> seatList = GenerateSeatList(seats, mv, 6, true);
        seatList[0].transform.localPosition = new Vector3(0.4f, -0.1f, -1.45f);
        seatList[1].transform.localPosition = new Vector3(-0.4f, -0.1f, -1.45f);
        seatList[2].transform.localPosition = new Vector3(0.4f, -0.1f, -0.05f);
        seatList[3].transform.localPosition = new Vector3(-0.4f, -0.1f, -0.05f);
        seatList[4].transform.localPosition = new Vector3(0.4f, -0.1f, 1.25f);
        seatList[5].transform.localPosition = new Vector3(-0.4f, -0.1f, 1.25f);
    }


    [MenuItem("GameObject/ER2 MODS/Vehicle/Auto Transport Glider Template", false, 403)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Auto Transport Glider Template", false, 403)]
    public static void OnATGliderTemplate()
    {
        GameObject root = GenerateBaseRoot("My AT Glider", Selection.activeGameObject);
        root.AddComponent<SyncVehicle>();
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 5000;
        rb.isKinematic = true;
        rb.angularDrag = 0.1f;
        rb.drag = 0.1f;
        BoxCollider[] box = new BoxCollider[4];
        box[0]= root.AddComponent<BoxCollider>();
        box[0].center = new Vector3(0.01767862f, 1.853587f, 1.798799f);
        box[0].size = new Vector3(1.722506f, 1.4025f, 8.034792f);
        box[1] = root.AddComponent<BoxCollider>();
        box[1].center = new Vector3(-0.1106834f, 2.144096f, -1.097694f);
        box[1].size = new Vector3(57.94471f, 0.2195537f, 2.470001f);
        box[2] = root.AddComponent<BoxCollider>();
        box[2].center = new Vector3(-0.008642554f, 1.740508f, -7.374557f);
        box[2].size = new Vector3(0.593473f, 0.504797f, 15.74911f);
        box[3] = root.AddComponent<BoxCollider>();
        box[3].center = new Vector3(-0.0143311f, 2.359359f, -12.99273f);
        box[3].size = new Vector3(7.243034f, 0.2382803f, 0.8822422f);

        Glider mv = root.AddComponent<Glider>();
        mv.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite));

        // AUDIO
        mv.engine_start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Start_Up_Exterior.wav", typeof(AudioClip));
        mv.engine_move = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Engine_Loop_Exterior.wav", typeof(AudioClip));


        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(root.transform);
        List<GameObject> seatList = GenerateSeatList(seats, mv, 12, true);
        seatList[0].transform.localPosition = new Vector3(0, 0.85f, 3.85f);
        seatList[1].transform.localPosition = new Vector3(0, 0.85f, 1.4f);
        seatList[2].transform.localPosition = new Vector3(0, 0.85f, -2.5f);
        seatList[2].SetActive(false);
        seatList[3].transform.localPosition = new Vector3(0, 0.85f, -2.8f);
        seatList[3].SetActive(false);
        seatList[4].transform.localPosition = new Vector3(0, 0.85f, -3.1f);
        seatList[4].SetActive(false);
        seatList[5].transform.localPosition = new Vector3(0, 0.85f, -3.4f);
        seatList[5].SetActive(false);
        seatList[6].transform.localPosition = new Vector3(0, 0.85f, -3.7f);
        seatList[6].SetActive(false);
        seatList[7].transform.localPosition = new Vector3(0, 0.85f, -4f);
        seatList[7].SetActive(false);
        seatList[8].transform.localPosition = new Vector3(0, 0.85f, -4.3f);
        seatList[8].SetActive(false);
        seatList[9].transform.localPosition = new Vector3(0, 0.85f, -4.6f);
        seatList[9].SetActive(false);
        seatList[10].transform.localPosition = new Vector3(0, 0.85f, -4.9f);
        seatList[10].SetActive(false);
        seatList[11].transform.localPosition = new Vector3(0, 0.85f, -5.2f);
        seatList[11].SetActive(false);
    }


    [MenuItem("GameObject/ER2 MODS/Vehicle/Auto Transport Cargo Template", false, 404)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Auto Transport Cargo Template", false, 404)]
    public static void OnATCargoTemplate()
    {
        GameObject root = GenerateBaseRoot("My AT Cargo", Selection.activeGameObject);
        root.AddComponent<SyncVehicle>();
        BoxCollider[] box = new BoxCollider[2];
        box[0] = root.AddComponent<BoxCollider>();
        box[0].center = new Vector3(-0.05046821f, 1.523426f, -2.529927f);
        box[0].size = new Vector3(1.636089f, 1.73651f, 15.27669f);
        
        box[1] = root.AddComponent<BoxCollider>();
        box[1].center = new Vector3(-0.04423428f, 1.483439f, -1.69201f);
        box[1].size = new Vector3(16.5308f, 0.07711351f, 4.29598f);

        CargoPlane mv = root.AddComponent<CargoPlane>();

        GameObject exitPos = new GameObject("exitPos");
        exitPos.transform.SetParent(root.transform);
        exitPos.transform.localPosition = new Vector3(-0.996f, 1.96f, -5.75f);
        exitPos.transform.localEulerAngles = new Vector3(0, -90f, 0);
        mv.exitPosition = exitPos.transform;

        mv.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite));


        // Sounds 
        mv.engine_start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Start_Up_Exterior.wav", typeof(AudioClip));
        mv.engine_move = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Engine_Loop_Exterior.wav", typeof(AudioClip));



        // Seats

        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(root.transform);
        List<GameObject> seatList = GenerateSeatList(seats, mv, 11, true);
        seatList[0].transform.localPosition = new Vector3(0, 1, 4);
        seatList[1].transform.localPosition = new Vector3(0, 1, 2);
        seatList[1].transform.localEulerAngles = new Vector3(0, 180, 0);
        seatList[1].SetActive(false);
        seatList[2].transform.localPosition = new Vector3(0, 1, 1.1f);
        seatList[2].SetActive(false);
        seatList[3].transform.localPosition = new Vector3(0, 1, 0.3f);
        seatList[3].SetActive(false);
        seatList[4].transform.localPosition = new Vector3(0, 1, -0.4f);
        seatList[4].SetActive(false);
        seatList[5].transform.localPosition = new Vector3(0, 1, -1.2f);
        seatList[5].SetActive(false);
        seatList[6].transform.localPosition = new Vector3(0, 1, -2f);
        seatList[6].SetActive(false);
        seatList[7].transform.localPosition = new Vector3(0, 1, -2.8f);
        seatList[7].SetActive(false);
        seatList[8].transform.localPosition = new Vector3(0, 1, -3.7f);
        seatList[8].SetActive(false);
        seatList[9].transform.localPosition = new Vector3(0, 1, -4.6f);
        seatList[9].SetActive(false);
        seatList[10].transform.localPosition = new Vector3(0, 1, -5.9f);
        seatList[10].SetActive(false);
    }



    #endregion


    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Plane Template", false, 405)]
    [MenuItem("Assets/ER2 MODS/Vehicle/Planes/Plane Template", false, 405)]
    public static void OnPlaneTemplate()
    {
        GameObject root = GenerateBaseRoot("My Plane", Selection.activeGameObject);
        GenerateVehicleTemplate(root, 3000, true);
        Rigidbody rb = root.GetComponent<Rigidbody>();
        rb.drag = 0.06f;
        rb.angularDrag = 0.2f;
        BoxCollider[] box = new BoxCollider[2];
        box[0] = root.AddComponent<BoxCollider>();
        box[0].center = new Vector3(0.01660877f, 1.228153f, 0.0699932f);
        box[0].size = new Vector3(0.7547828f, 0.6439853f, 3.964773f);
        box[1] = root.AddComponent<BoxCollider>();
        box[1].center = new Vector3(0.03071752f, 1.708493f, -0.7849936f);
        box[1].size = new Vector3(0.5587591f, 0.338726f, 3.178361f);
        VehiclePlane mv= root.AddComponent<VehiclePlane>();
        root.GetComponent<VehicleDamagablePart>().armorThickness = 0;
        root.GetComponent<VehicleDamagablePart>().considerAngledArmor = false;
        root.GetComponent<VehicleDamagablePart>().canRicochet = false;
        root.GetComponent<VehicleDamagablePart>().impactType = ImpactType.metal;

        mv.internalParts = new InternalVehiclePart[3];
        GameObject internalParts = new GameObject("InternalParts");
        internalParts.transform.SetParent(mv.transform);
        for (int i = 0; i < 3; i++)
        {
            InternalVehiclePart part = AddPart(internalParts, (VehicleInternalPart)i);
            mv.internalParts[i] = part;
        }



        //Engine
        mv.internalParts[0].transform.localPosition = new Vector3(0, 1.259f, 1.173f);
        mv.internalParts[0].width = 0.519f;
        mv.internalParts[0].height = 0.46f;
        //Gear
        mv.internalParts[1].transform.localPosition = new Vector3(0, 1.197f, -1.59f);
        mv.internalParts[1].width = 0.597f;
        mv.internalParts[1].height = 0.745f;
        //Fuel Tank
        mv.internalParts[2].transform.localPosition = new Vector3(-0.201f, 1.164f, -1.023f);
        mv.internalParts[2].width = 0.407f;
        mv.internalParts[2].height = 0.378f;

        //Speed Values

        mv.maxKmhSpeed = 300;
        mv.timeFromZeroToMaxThrust = 25;
        mv.tpsCamDist = 2.2f;

        mv.icon = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/Icons/veh_icon.png", typeof(Sprite));

        mv.engine_start = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Start_Up_Exterior.wav", typeof(AudioClip));
        mv.engine_move = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/M5_Engine_Loop_Exterior.wav", typeof(AudioClip));
        
        mv.crashSound= (AudioClip)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/Vehicles/vehicle_crash_1.ogg", typeof(AudioClip)));

        GameObject swgo = new GameObject("SteeringWheel");
        swgo.transform.SetParent(root.transform);
        swgo.transform.localPosition = new Vector3(-0.33f, 1.435f, 0.508f);
        PlaneSteer sw = swgo.AddComponent<PlaneSteer>();
        sw.connectedPlane = mv;
        sw.transform.localPosition = new Vector3(0, sw.transform.localPosition.y, sw.transform.localPosition.z);

        mv.detachableTails = new VehicleDamagableDetachablePart[0];
        mv.detachablePropellers = new VehicleDamagableDetachablePart[0];

        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(root.transform);
        List<GameObject> seatList = GenerateSeatList(seats, mv, 1, false);
        seatList[0].SetActive(false);
        seatList[0].transform.localPosition = new Vector3(0, 1.03f, 0.22f);
        mv.seats[0].seat.onSeatAnimator_id = "OnVehicleSeat_sitted_planeSmall";
        
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Make BombBay", false , 1000)]
    public static void SetUpBombBay()
    {
        if (!IsNewActionTime())
            return;

        VehiclePlane rootplane = null;

        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go != null && IsInstance(go))
            {
                if (rootplane == null)
                {
                    rootplane = go.GetComponentInParent<VehiclePlane>();
                }

                GameObject bomb = new GameObject("BombBay" + i);
                bomb.transform.SetParent(go.transform);
                BombBay bb = new BombBay();
                bb.bomb_parent = bomb.transform;
                List<BombBay> bombs = new List<BombBay>();
                for (int j = 0; j < rootplane.bombBay.Length; j++)
                    if (rootplane.bombBay[j] != null)
                        bombs.Add(rootplane.bombBay[j]);
                bombs.Add(bb);
                rootplane.bombBay = bombs.ToArray();
                bombs.Clear();
            }
        }
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Make Detachable part(s)/Tail", false, 1001)]
    public static void SetUpDetachableTail()
    {
        if (!IsNewActionTime())
            return;

        VehiclePlane rootplane = null;

        foreach (GameObject go in Selection.gameObjects)
        {
            if (go!=null && IsInstance(go) && !go.GetComponentInParent<VehicleDamagableDetachablePart>())
            {
                if (rootplane == null)
                    rootplane = go.GetComponentInParent<VehiclePlane>();

                VehicleDamagableDetachablePart tail = go.AddComponent<VehicleDamagableDetachablePart>();
                tail.armorThickness = 0;
                tail.considerAngledArmor = false;
                tail.canRicochet = false;
                tail.impactType = ImpactType.metal;
                tail.damagePlayerOnCollision = true;
                tail.detachablePartLife = 100;
                tail.minImpactSpeedToDamage = 43;
                BoxCollider coll= go.AddComponent<BoxCollider>();

                //register tail on root plane
                List<VehicleDamagableDetachablePart> tails = new List<VehicleDamagableDetachablePart>();
                for (int i = 0; i < rootplane.detachableTails.Length; i++)
                    if (rootplane.detachableTails[i]!=null)
                        tails.Add( rootplane.detachableTails[i]);
                tails.Add(tail);
                rootplane.detachableTails = tails.ToArray();
                tails.Clear();
            }
        }
    }
    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Make Detachable part(s)/Left Wing", false, 1002)]
    public static void SetUpDetachableWingLeft()
    {
        SetUpDetachableWing(true);
    }
    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Make Detachable part(s)/Right Wing", false, 1002)]
    public static void SetUpDetachableWingRight()
    {
        SetUpDetachableWing(false);
    }
    public static void SetUpDetachableWing(bool isLeftWing)
    {
        if (!IsNewActionTime())
            return;

        VehiclePlane rootplane = null;

        foreach (GameObject go in Selection.gameObjects)
        {
            if (go != null && IsInstance(go) && !go.GetComponentInParent<VehicleDamagableDetachablePart>())
            {
                if (rootplane == null)
                    rootplane = go.GetComponentInParent<VehiclePlane>();

                VehicleDamagableDetachablePart wing = go.AddComponent<VehicleDamagableDetachablePart>();
                wing.armorThickness = 0;
                wing.considerAngledArmor = false;
                wing.canRicochet = false;
                wing.damagePlayerOnCollision=true;
                wing.detachablePartLife = 100;
                wing.minImpactSpeedToDamage = 22;
                wing.impactType = ImpactType.metal;
                BoxCollider coll = go.AddComponent<BoxCollider>();

                //register wing
                if (isLeftWing)
                    rootplane.leftDetachableWing = wing;
                else
                    rootplane.rightDetachableWing = wing;

                return;
            }
        }
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Make Detachable part(s)/Propellers", false, 1003)]
    public static void SetUpDetachablePropellers()
    {
        if (!IsNewActionTime())
            return;

        VehiclePlane rootplane = null;

        foreach (GameObject go in Selection.gameObjects)
        {
            if (go != null && IsInstance(go) && !go.GetComponentInParent<VehicleDamagableDetachablePart>())
            {
                if (rootplane == null)
                    rootplane = go.GetComponentInParent<VehiclePlane>();

                GameObject propeller = new GameObject("Propeller " + go.name);
                propeller.transform.SetParent(rootplane.transform);
                propeller.transform.position = go.transform.position;
                propeller.transform.localEulerAngles = new Vector3(90, 0, 0);
                VehicleDamagableDetachablePart vddp  = propeller.AddComponent<VehicleDamagableDetachablePart>();
                vddp.armorThickness = 0;
                vddp.considerAngledArmor = false;
                vddp.canRicochet = false;
                vddp.damagePlayerOnCollision = true;
                vddp.impactType = ImpactType.metal;
                vddp.detachablePartLife = 100;
                vddp.minImpactSpeedToDamage = 2;
                Propeller p = propeller.AddComponent<Propeller>();
                GameObject pivot = new GameObject("Propeller_Slow Pivot" + go.name);
                pivot.transform.SetParent(propeller.transform);
                pivot.transform.localPosition = Vector3.zero;
                pivot.transform.localEulerAngles = new Vector3(0, 0, 0);
                go.transform.SetParent(pivot.transform);
                go.transform.localPosition = Vector3.zero;
                GameObject blur = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Prefabs/PropellerFastBlur_pivot.prefab", typeof(GameObject)));
                blur.transform.SetParent(propeller.transform);
                p.connectedPlane = rootplane;
                p.propeller_slow = pivot.transform;
                p.propeller_fast = blur.transform;
                blur.SetActive(false);
                blur.transform.localEulerAngles = new Vector3(0, 0, 0);

                BoxCollider box = propeller.AddComponent<BoxCollider>();
                box.center = new Vector3(0.003796697f, 0.02330244f, 0.06394637f);
                box.size = new Vector3(0.5025457f, 0.5272539f, 0.523617f);


                //register propeller on root plane
                List<VehicleDamagableDetachablePart> propellers = new List<VehicleDamagableDetachablePart>();
                for (int i = 0; i < rootplane.detachablePropellers.Length; i++)
                    if (rootplane.detachablePropellers[i] != null)
                        propellers.Add(rootplane.detachablePropellers[i]);
                propellers.Add(vddp);
                rootplane.detachablePropellers = propellers.ToArray();
                propellers.Clear();
            }
        }
    }
    [MenuItem("GameObject/ER2 MODS/Vehicle/Planes/Make Flap", false, 1003)]
    public static void SetUpPlaneFlap()
    {
        if (!IsNewActionTime())
            return;
        VehiclePlane rootplane = null;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go != null && IsInstance(go) && !go.GetComponent<FlapRotator>())
            {
                if (rootplane == null)
                {
                    rootplane = go.GetComponentInParent<VehiclePlane>();
                }
                GameObject flapPivot = new GameObject("Flap Pivot " + go.name);
                Transform parentObj = go.transform.parent;
                flapPivot.transform.position = go.transform.position;
                go.transform.SetParent(flapPivot.transform);
                go.AddComponent<FlapRotator>();
                go.GetComponent<FlapRotator>().connectedPlane = rootplane;
                flapPivot.transform.SetParent(parentObj);
            }
        }
    }

    private static List<VehicleDamagableDetachablePart> CheckForDubsPropeller(UnityEngine.Object[] objects)
    {
        List<VehicleDamagableDetachablePart> parts = new List<VehicleDamagableDetachablePart>();
        return parts;
    }

    private static List<VehicleDamagableDetachablePart> CheckForDubsTail(UnityEngine.Object[] objects)
    {
        List<VehicleDamagableDetachablePart> parts = new List<VehicleDamagableDetachablePart>();
        return parts;
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Turret/Gun", false, 800)]
    public static void SetUpTurretGun()
    {
        TurretGun tg = (TurretGun)InitTurrets(typeof(TurretGun));
        if (tg)
        {
            AddGunToTurret(tg);
        }
    }
    [MenuItem("GameObject/ER2 MODS/Vehicle/Turret/MG", false, 801)]
    public static void SetUpTurretMG()
    {
        TurretMG tm = (TurretMG)InitTurrets(typeof(TurretMG));
        if (tm)
        {
            AddMgToTurret(tm);
        }
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Turret/Gun and MG", false, 802)]
    public static void SetUpTurretGunNMG()
    {
        TurretGun tg = (TurretGun)InitTurrets(typeof(TurretGun));
        if (tg)
        {
            AddGunToTurret(tg);
            AddMgToTurret(tg);
        }
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Turret/Commander", false, 803)]
    public static void SetUpTurretCommander()
    {
        InitTurrets(typeof(TurretCommander));
    }

    public static Turret InitTurrets(System.Type componentToAdd)
    {
        if (!IsNewActionTime())
            return null;

        if (Selection.activeObject is GameObject && ((GameObject)Selection.activeObject).GetComponentInParent<Vehicle>())
            return SetUpTurret(Selection.objects, componentToAdd);

        return null;
    }

    private static Turret SetUpTurret(UnityEngine.Object[] selectedmeshes, System.Type componentToAdd)
    {
        GameObject turretGO = new GameObject((componentToAdd.ToString()));

        //select first selected GO
        if (selectedmeshes.Length == 0 || !(selectedmeshes[0] is GameObject))
            return null;
        GameObject firstSelectedmesh = (GameObject)selectedmeshes[0];

        //set up Y axis
        turretGO.transform.SetParent(firstSelectedmesh.transform.parent);
        turretGO.transform.position = firstSelectedmesh.transform.position;
        Turret turret = (Turret)turretGO.AddComponent(componentToAdd);
        turret.yAxisBond = turret is TurretCommander?180: 30;
        turret.xAxisBond = 25;
        turret.xAxisBondDown = 5;
        GameObject YAxis = new GameObject("YAxis");
        YAxis.transform.SetParent(turretGO.transform);
        YAxis.transform.localPosition = Vector3.zero;
        YAxis.AddComponent<BoxCollider>();
        YAxis.GetComponent<BoxCollider>().center=Vector3.one;
        YAxis.GetComponent<BoxCollider>().size = new Vector3(1, .5f, 1);
        YAxis.AddComponent<VehicleDamagablePart>().impactType = ImpactType.metal;

        //parent selected meshes to Y axis
        foreach (UnityEngine.Object go in selectedmeshes)
        {
            if (go is GameObject && IsInstance((GameObject)go))
            {
                UnpackPrefab((GameObject)go);
                ((GameObject)go).transform.SetParent(YAxis.transform);
            }
        }

        //set up X axis
        GameObject XAxis = new GameObject("XAxis");
        XAxis.transform.SetParent(YAxis.transform);
        XAxis.transform.localPosition = Vector3.zero;

        //finalize
        GameObject lookPos = new GameObject("scopePos");
        lookPos.transform.SetParent(XAxis.transform);
        lookPos.transform.localPosition = new Vector3(0, 0.5f, .35f);
        turret.yAxis = YAxis;
        turret.xAxis = XAxis;
        turret.scopePosition = lookPos.transform;

        //make turret inside turret
        if (turret.transform.parent != null && turret.transform.parent.GetComponentInParent<Turret>())
            turret.independentLookRotation = true;

        //turret seat
        Seats[] seatsList = turretGO.GetComponentInParent<Vehicle>().seats;

        AddTurretSeat(seatsList, turret);


        return turret;

    }

    private static void AddTurretSeat(Seats[] seatsList, Turret turret)
    {
        foreach(Seats seat in seatsList)
        {
            if (!seat.isDriver && seat.connectedTurret == null)
            {
                seat.forceHideWeaponWhenHere = true;
                seat.connectedTurret = turret;
                return;
            }
        }

        //retry
        foreach (Seats seat in seatsList)
        {
            if (seat.connectedTurret == null)
            {
                seat.forceHideWeaponWhenHere = true;
                seat.connectedTurret = turret;
                return;
            }
        }
        Debug.Log("There are no available seats for the turret");
    }

    private static void AddGunToTurret(TurretGun turret)
    {
        GameObject firePos = new GameObject("FirePos_gun");
        firePos.transform.SetParent(turret.xAxis.transform);
        firePos.transform.localPosition = new Vector3(0, 0, 1.5f);

        //create turret
        TurretWeapon newWeapon = new TurretWeapon();
        newWeapon.firePos = firePos.transform;
        TurretWeaponBullet[] ammos = new TurretWeaponBullet[] { new TurretWeaponBullet(), new TurretWeaponBullet(), new TurretWeaponBullet() };
        ammos[0].bullet_id = "75mm_l48_he";
        ammos[0].start_ammo_count = 30;
        ammos[1].bullet_id = "75mm_l48_ap";
        ammos[1].start_ammo_count = 25;
        ammos[2].bullet_id = "75mm_l48_smoke";
        ammos[2].start_ammo_count = 10;
        newWeapon.ammos = ammos;

        //Setting up sounds for turret
        newWeapon.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rocket Launcher/rocket_launcher_fire.ogg", typeof(AudioClip));

        //Finalize - add new weapon to turret
        TurretWeapon[] newWeapons = new TurretWeapon[turret.weapons.Length+1];
        for (int i = 0; i < turret.weapons.Length; i++)
            newWeapons[i] = turret.weapons[i];
        newWeapons[turret.weapons.Length] = newWeapon;
        turret.weapons = newWeapons;
    }
    private static void AddMgToTurret(TurretGun turret)
    {
        GameObject firePos = new GameObject("FirePos_mg");
        firePos.transform.SetParent(turret.xAxis.transform);
        firePos.transform.localPosition = new Vector3(.2f, 0, .5f);

        //create turret
        TurretWeapon newWeapon = new TurretWeapon();
        newWeapon.firePos = firePos.transform;
        TurretWeaponBullet[] ammos = new TurretWeaponBullet[] { new TurretWeaponBullet() };
        ammos[0].bullet_id = "mg42_belt";
        ammos[0].start_ammo_count = 1500;
        newWeapon.ammos = ammos;
        newWeapon.specifiedTracersRatio = 3;

        //Setting up sounds for turret
        newWeapon.fireSound = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/Rifles/rifle_fire.wav", typeof(AudioClip));
        newWeapon.fireSound_distance = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Sounds/WeaponTemplate/MG/distant_mg.wav", typeof(AudioClip));

        //Finalize - add new weapon to turret
        TurretWeapon[] newWeapons = new TurretWeapon[turret.weapons.Length + 1];
        for (int i = 0; i < turret.weapons.Length; i++)
            newWeapons[i] = turret.weapons[i];
        newWeapons[turret.weapons.Length] = newWeapon;
        turret.weapons = newWeapons;
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/Wheels/SetUp Wheels", false, 801)]
    public static void OnSetupWheels()
    {
        if (!IsNewActionTime())
            return;

        EditorWindow.GetWindow(typeof(WheelWindow));
        WheelWindow.Init(Selection.gameObjects);
    }
    [MenuItem("GameObject/ER2 MODS/Vehicle/Wheels/Simple Wheels SetUp", false, 801)]
    public static void OnSetupWheelsSimple()
    {
        if (!IsNewActionTime())
            return;

        if (Selection.gameObjects.Length>8)
        {
            Debug.Log("You can't set up more than 8 whele colliders in a vehicle.");
            return;
        }

        WheelWindow.SetUpWheelsSimple(Selection.gameObjects);
    }

    public static void UnpackPrefab(GameObject selected)
    {
        if (PrefabUtility.IsPartOfAnyPrefab(selected))
            PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(PrefabUtility.GetOutermostPrefabInstanceRoot(selected), PrefabUnpackMode.OutermostRoot);
    }

    private static void GenerateVehicleTemplate(GameObject root, float rbMass, bool isPlane)
    {
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = rbMass;
        root.AddComponent<VehicleDamagablePart>().impactType = ImpactType.metal;
        root.AddComponent<InventoryManager>();
        if (!isPlane)
            root.AddComponent<SyncVehicle>();
        else
            root.AddComponent<SyncVehiclePlane>();

    }

    private static InternalVehiclePart AddPart(GameObject vehicle, VehicleInternalPart partType)
    {
        GameObject part = new GameObject(partType.ToString());
        part.transform.SetParent(vehicle.transform);
        InternalVehiclePart vip = part.AddComponent<InternalVehiclePart>();
        vip.part = partType;

        return vip;
    }

    public static List<GameObject> GenerateSeatList(GameObject parent, Vehicle rootMVFunction,  int numberOfSeats, bool isAT)
    {
        List<GameObject> list = new List<GameObject>();
        rootMVFunction.seats = new Seats[numberOfSeats];
        for (int i = 0; i<numberOfSeats; i++)
        {
            rootMVFunction.seats[i] = new Seats();
            GameObject seat = new GameObject("Seat" + (i+1));
            seat.transform.SetParent(parent.transform);
            seat.AddComponent<VehicleSeat>();
            list.Add(seat);
            rootMVFunction.seats[i].seat = seat.GetComponent<VehicleSeat>();
            rootMVFunction.seats[i].seat.onSeatAnimator_id = "VCC_JeepPass";
        }
        if (!isAT)
        {
            rootMVFunction.seats[0].isDriver = true;
            rootMVFunction.seats[0].forceHideWeaponWhenHere = true;
            rootMVFunction.seats[0].seat.onSeatAnimator_id = "VCC_JeepDriver";
        }
        return list;
    }

    [MenuItem("GameObject/ER2 MODS/Vehicle/SetUp Steering Wheel", false, 1001)]
    public static void GenerateSteeringWheel()
    {
        if (Selection.activeObject is GameObject && ((GameObject)Selection.activeObject).GetComponentInParent<MovableVehicle>())
        {
            MovableVehicle mv = ((GameObject)Selection.activeObject).GetComponentInParent<MovableVehicle>();
            GameObject swMesh = (GameObject)Selection.activeObject;
            GameObject swRoot = new GameObject("SteeringWheel");
            GameObject swPivot = new GameObject("SteeringWheelPivot");

            //root of the steering wheel
            swRoot.transform.SetParent(mv.transform);
            swRoot.transform.localEulerAngles = new Vector3(40.92f, 0, 0);
            swRoot.transform.position = swMesh.transform.position;
            //pivot that rotates
            swPivot.transform.SetParent(swRoot.transform);
            swPivot.transform.localPosition = Vector3.zero;
            swPivot.transform.localEulerAngles = Vector3.zero;

            //hands position
            GameObject lfPos = new GameObject("LF_Hand_SW_Pos");
            GameObject rhPos = new GameObject("RH_Hand_SW_Pos");
            lfPos.transform.SetParent(swPivot.transform);
            lfPos.transform.localPosition = new Vector3(-0.245f, 0.031f, -0.08f);
            lfPos.transform.localEulerAngles = new Vector3(-30.225f, 32.833f, 75.335f);

            rhPos.transform.SetParent(swPivot.transform);
            rhPos.transform.localPosition = new Vector3(0.192f, 0.05f, -0.067f);
            rhPos.transform.localEulerAngles = new Vector3(-28.998f, -48.639f, -18.08f);

            Seats driverSeat = GetDriverSeat(mv.seats);
            driverSeat.lefthandIsIK = lfPos.transform;
            driverSeat.rightHandIK = rhPos.transform;

            //actual mesh
            swMesh.transform.SetParent(swPivot.transform);

            //scripts
            SteeringWheel sw = swPivot.AddComponent<SteeringWheel>();
            sw.connectedVeh = mv;
            sw.axis = 2;
            sw.rotationMultiplier = 45;
            
        }

    }

    private static Seats GetDriverSeat(Seats[] seats) //SI POTREBBE FARE SIA LA VERSIONE SINGOLA CHE LA VERSIONE CON PIù SEDILI DA PILOTA
    {
        foreach (Seats seat in seats)
        {
            if (seat.isDriver)
            {
                return seat;
            }
        }
        Debug.LogWarning("Unable to find a driver seat in the current vehicle");
        return null;
    }


    #endregion


    /// <summary>
    /// Adds the tag.
    /// </summary>
    /// <returns><c>true</c>, if tag was added, <c>false</c> otherwise.</returns>
    /// <param name="tagName">Tag name.</param>
    public static bool CreateTag(string tagName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Tags Property
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        // if not found, add it
        if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName))
        {
            int index = tagsProp.arraySize;
            // Insert new array element
            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
            // Set array element to tagName
            sp.stringValue = tagName;
            //Debug.Log("Tag: " + tagName + " has been added");
            // Save settings
            tagManager.ApplyModifiedProperties();

            return true;
        }
        else
        {
            //Debug.Log ("Tag: " + tagName + " already exists");
        }
        return false;
    }
    private static bool PropertyExists(SerializedProperty property, int start, int end, string value)
    {
        for (int i = start; i < end; i++)
        {
            SerializedProperty t = property.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(value))
            {
                return true;
            }
        }
        return false;
    }

    public static string NewTag(string name)
    {
        CreateTag(name);

        if (name == null || name == "")
        {
            name = "Untagged";
        }

        return name;
    }

}

#endif