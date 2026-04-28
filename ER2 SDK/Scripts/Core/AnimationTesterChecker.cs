#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class AnimationTesterChecker : MonoBehaviour
{
    public GenericGun attached_weapon;
}

public static class AnimationTesterTool
{
    private const string SkeletonPrefabPath = "Assets/ER2 SDK/Rigs/skeleton for animations.prefab";
    private const string CenterSpineRelativePath = "ROOT/RIGROOT_BJ/CenterSpine2_BJ";
    private static readonly HashSet<string> alreadyAdded = new HashSet<string>();

    public static void ToggleAnimationTester(GenericGun weapon)
    {
        if (weapon == null) return;

        var existing = Object.FindObjectsOfType<AnimationTesterChecker>();
        AnimationTesterChecker mine = null;

        foreach (var c in existing)
        {
            if (c == null || EditorUtility.IsPersistent(c)) continue;

            bool isAttachedWeapon = c.attached_weapon == weapon;
            bool isWeaponInsideTester = weapon != null && weapon.transform.IsChildOf(c.transform);

            if (isAttachedWeapon || isWeaponInsideTester)
            {
                mine = c;
                continue;
            }

            DestroyTester(c);
        }

        if (mine != null)
        {
            DestroyTester(mine);
            return;
        }

        if (weapon == null) return;

        CreateTester(weapon);
    }

    private static void DestroyTester(AnimationTesterChecker checker)
    {
        if (checker == null) return;
        UnlockHierarchy(checker.transform); // sblocca prima di distruggere
        Object.DestroyImmediate(checker.gameObject);
    }

    private static void UnlockHierarchy(Transform t)
    {
        t.gameObject.hideFlags = HideFlags.None;
        foreach (Transform child in t) UnlockHierarchy(child);
    }

    private static void CreateTester(GenericGun weapon)
    {
        Camera bestCam = null;
        foreach (Camera cam in Object.FindObjectsOfType<Camera>())
            if (bestCam == null || bestCam.enabled == false) bestCam = cam;
        if (bestCam == null)
        {
            bestCam = new GameObject("Camera").AddComponent<Camera>();
            bestCam.nearClipPlane = .015f;
            bestCam.farClipPlane = 100;
        }

        var skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        if (skeletonPrefab == null)
        {
            Debug.LogError("Animation skeleton prefab not found at " + SkeletonPrefabPath);
            return;
        }

        if (weapon == null) return;

        string weaponName = weapon.name;
        GameObject weaponObject = weapon.gameObject;

        GameObject animRoot = Object.Instantiate(skeletonPrefab);
        animRoot.name = "Animation Tester (" + weaponName + ")";
        animRoot.transform.SetParent(bestCam.transform, false);
        animRoot.transform.localPosition = Vector3.zero;
        animRoot.transform.localEulerAngles = Vector3.zero;
        animRoot.transform.localScale = Vector3.one;

        var checker = animRoot.AddComponent<AnimationTesterChecker>();
        checker.attached_weapon = weapon;

        Transform centerSpine = animRoot.transform.Find(CenterSpineRelativePath);
        if (centerSpine == null)
        {
            Debug.LogError("Could not find " + CenterSpineRelativePath + " in skeleton prefab.");
            Object.DestroyImmediate(animRoot);
            return;
        }

        // Clone arma sotto CenterSpine2_BJ
        GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(weaponObject);
        GameObject weaponPrefab = src != null ? src : weaponObject;
        GenericGun weapClone = ((GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab)).GetComponent<GenericGun>();
        weapClone.transform.SetParent(centerSpine, false);
        weapClone.transform.localPosition = Vector3.zero;
        weapClone.transform.localScale = Vector3.one;
        weapClone.transform.rotation = animRoot.transform.rotation;

        // Spawna TUTTI i magazine compatibili sulla magazinePosition
        if (weapClone.magazinePosition != null && !string.IsNullOrEmpty(weapClone.magazineSocket))
            SpawnMatchingMagazines(weapClone);

        // Animazioni
        Animation anim = animRoot.transform.Find("ROOT").GetComponent<Animation>();
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

        // Lock: tutto NotEditable, poi sblocca CenterSpine2_BJ + figli (per animare),
        // poi rilocka l'arma (figlia di CenterSpine) e gli SkinnedMeshRenderer
        SetHideFlagsRecursive(animRoot.transform, HideFlags.NotEditable);
        SetHideFlagsRecursive(centerSpine, HideFlags.None);
        //SetHideFlagsRecursive(weapClone.transform, HideFlags.NotEditable);
        foreach (var smr in animRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.gameObject.hideFlags = HideFlags.NotEditable;

        Selection.activeGameObject = weapClone.gameObject;
    }

    private static void SetHideFlagsRecursive(Transform t, HideFlags flags)
    {
        t.gameObject.hideFlags = flags;
        foreach (Transform child in t) SetHideFlagsRecursive(child, flags);
    }

    private static void SpawnMatchingMagazines(GenericGun weapon)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            Magazine mag = prefab.GetComponent<Magazine>();
            if (mag == null) continue;
            if (mag.socket != weapon.magazineSocket) continue;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(weapon.magazinePosition, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }
    }

    private static void AddAnimation(string anim_name, Animation anim)
    {
        if (string.IsNullOrEmpty(anim_name)) return;
        string[] assets = AssetDatabase.FindAssets(anim_name + " t:AnimationClip");
        foreach (string guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx") && !alreadyAdded.Contains(guid))
            {
                AnimationClip temp = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (temp.wrapMode != WrapMode.ClampForever || !temp.legacy)
                {
                    temp.wrapMode = WrapMode.ClampForever;
                    temp.legacy = true;
                    EditorUtility.SetDirty(temp);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                anim.AddClip(temp, anim_name);
                alreadyAdded.Add(guid);
            }
        }
    }
}
#endif