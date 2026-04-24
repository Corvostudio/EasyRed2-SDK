using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public class ModelImporterEditor:Editor
{
    private static double next_action_time = 0;
    public static bool IsNewActionTime()
    {
        if (EditorApplication.timeSinceStartup > next_action_time)
        {
            next_action_time = EditorApplication.timeSinceStartup + .5f;
            return true;
        }
        return false;
    }


    [MenuItem("Assets/ER2 MODS/Tools/Animation making/Fix animation as FPS anim", false, 2003)]
    public static void SetUpPlaneFlap()
    {
        if (!IsNewActionTime())
            return;

        foreach (UnityEngine.Object activeObject in Selection.objects)
        {
            if (activeObject == null)
            {
                Debug.Log("Please select a GameObject in the scene or a model asset in the project view.");
                return;
            }

            ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(activeObject)) as ModelImporter;

            if (modelImporter != null)
            {
                FixAnimation(modelImporter, activeObject);
            }
            else
            {
                Debug.Log("Selected object is not a model asset.");
            }
        }
    }

    public static void FixAnimation(ModelImporter singleImporter, UnityEngine.Object selectedObject)
    {
        singleImporter.globalScale = 1;
        singleImporter.animationPositionError = .1f;
        singleImporter.animationRotationError = .1f;
        singleImporter.animationScaleError = .1f;
        singleImporter.resampleCurves = true;
        singleImporter.materialImportMode = ModelImporterMaterialImportMode.None;

        List<ModelImporterClipAnimation> validClips = new List<ModelImporterClipAnimation>();
        foreach (ModelImporterClipAnimation clip in singleImporter.defaultClipAnimations)
        {
            if (clip.name.Contains("|Action"))
            {
                Debug.Log(clip.name + " -> " + selectedObject.name);
                clip.name = selectedObject.name;
                clip.wrapMode = WrapMode.ClampForever;
                validClips.Add(clip);
            }
        }
        singleImporter.clipAnimations = validClips.ToArray();
        validClips.Clear();

        //serializedObject.ApplyModifiedProperties();
        singleImporter.SaveAndReimport();
    }

    private static HashSet<string> alreadyAdded = new HashSet<string>();
    [MenuItem("Assets/ER2 MODS/Tools/Animation making/Set up FPS animator", false, 2004)]
    [MenuItem("GameObject/ER2 MODS/Tools/Animation making/Set up FPS animator", false, 2004)]
    public static void SetUpAnimationTesting()
    {
        if (!ModTemplates.IsNewActionTime()) return;

        GenericGun weapon=null;
        Magazine magazine = null;

        //get weapon
        foreach (GameObject go in Selection.objects)
        {
            if (weapon == null && go.GetComponent<GenericGun>())
                weapon = go.GetComponent<GenericGun>();
            else if (magazine == null && go.GetComponent<Magazine>())
                magazine = go.GetComponent<Magazine>();
        }

        if (!weapon) return;
        //if (weapon.fpsAnimations.animationSet!=AnimationData.FPSAnimationSet.UseCustom) return;

        //get camera
        Camera bestCam = null;
        foreach (Camera cam in GameObject.FindObjectsOfType<Camera>())
        {
            if (bestCam == null || bestCam.enabled == false)
                bestCam = cam;
        }

        if (!bestCam)
        {
            bestCam = new GameObject("Camera").AddComponent<Camera>();
            bestCam.nearClipPlane = .015f;
            bestCam.farClipPlane = 100;
        }

        //cleanup camera childs
        foreach (Transform tran in bestCam.transform)
            DestroyImmediate(tran.gameObject);

        //add animation thing
        GameObject animRoot = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Rigs/skeleton for animations.prefab", typeof(GameObject)));
        animRoot.transform.SetParent(bestCam.transform);
        animRoot.transform.localPosition = animRoot.transform.localEulerAngles = Vector3.zero;
        animRoot.transform.localScale = Vector3.one;

        //duplicate weapon and parent to animator
        GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(weapon.gameObject) ? PrefabUtility.GetCorrespondingObjectFromSource(weapon.gameObject) : weapon.gameObject;
        GenericGun weapClone = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<GenericGun>();
        weapClone.transform.SetParent(animRoot.transform.Find("ROOT/RIGROOT_BJ/CenterSpine2_BJ"));
        weapClone.transform.localPosition = Vector3.zero;
        weapClone.transform.localScale = Vector3.one;
        weapClone.transform.rotation = animRoot.transform.rotation;
        Selection.activeGameObject = weapClone.gameObject;

        //duplicate magazine if selected
        if (magazine!=null && weapClone.magazinePosition!=null)
        {
            prefab = PrefabUtility.GetCorrespondingObjectFromSource(magazine.gameObject) ? PrefabUtility.GetCorrespondingObjectFromSource(magazine.gameObject) : magazine.gameObject;
            GameObject magClone = ((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            magClone.transform.SetParent(weapClone.magazinePosition);
            magClone.transform.localPosition = Vector3.zero;
            magClone.transform.localScale = Vector3.one;
            magClone.transform.localEulerAngles = Vector3.zero;
        }

        //set up animations
        Animation anim = animRoot.transform.Find("ROOT").gameObject.GetComponent<Animation>();
        alreadyAdded.Clear();
        AddAnimation(weapClone.fpsAnimations.fps_putaway, anim);
        AddAnimation(weapClone.fpsAnimations.fps_reload_full, anim);
        AddAnimation(weapClone.fpsAnimations.fps_reload_half, anim);
        AddAnimation(weapClone.fpsAnimations.fps_bolt_action, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_open, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_close, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_close_noAmmo, anim);
        AddAnimation(weapClone.fpsAnimations.fps_change_barrell, anim);
    }

    private static void AddAnimation(string anim_name,Animation anim)
    {
        if (string.IsNullOrEmpty(anim_name))
            return;

        //Riporta path di assset che sto cercando
        string[] assets = AssetDatabase.FindAssets(anim_name + " t:AnimationClip");
        foreach (string guid in assets)
        {
            //fix as legacy
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx") && !alreadyAdded.Contains(guid))
            {
                //fix clips if necessary
                AnimationClip temp = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
                if (temp.wrapMode != WrapMode.ClampForever || !temp.legacy)
                {
                    temp.wrapMode = WrapMode.ClampForever;
                    temp.legacy = true;
                    UnityEditor.EditorUtility.SetDirty(temp);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                //actually adds
                anim.AddClip(temp, anim_name);
                alreadyAdded.Add(guid);
                //Debug.Log(temp+": "+ anim.GetClipCount(), anim);
            }
        }
    }

}