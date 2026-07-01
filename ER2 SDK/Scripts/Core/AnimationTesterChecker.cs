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

        var skeletonPrefab = EditorAssetFinder.Find<GameObject>("skeleton for animations");
        if (skeletonPrefab == null)
        {
            Debug.LogError("Animation skeleton prefab not found (skeleton for animations)");
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

        // Mirror runtime behavior: when recycledAnimationsWeaponId is set, the weapon's
        // root GameObject is renamed to that ID so animation curve paths resolve
        // (and so the spin_pos curve-strip prompt can match the bolt path).
        string recycleId = weapClone.fpsAnimations.recycledAnimationsWeaponId;
        if (!string.IsNullOrEmpty(recycleId))
            weapClone.gameObject.name = recycleId;

        // Spawna TUTTI i magazine compatibili sulla magazinePosition
        if (weapClone.magazinePosition != null && !string.IsNullOrEmpty(weapClone.magazineSocket))
            SpawnMatchingMagazines(weapClone);

        // Animazioni
        Animation anim = animRoot.transform.Find("ROOT").GetComponent<Animation>();
        alreadyAdded.Clear();

        // Equip clip (named fps_putaway for legacy reasons): after adding, prompt the user
        // to strip any spin_pos keyframes so bolt_movementDistance can drive it at runtime.
        var equipClips = AddAnimation(weapClone.fpsAnimations.fps_putaway, anim);
        foreach (var clip in equipClips)
            MaybePromptStripSpinPosFromClip(weapClone, clip, anim.transform, "equip");

        AddAnimation(weapClone.fpsAnimations.fps_unequip, anim);
        //AddAnimation(weapClone.fpsAnimations.fps_fire, anim);
        //AddAnimation(weapClone.fpsAnimations.fps_fire_no_ammos, anim);
        AddAnimation(weapClone.fpsAnimations.fps_reload_full, anim);
        AddAnimation(weapClone.fpsAnimations.fps_reload_half, anim);
        AddAnimation(weapClone.fpsAnimations.fps_bolt_action, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_open, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_open_noAmmo, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_close, anim);
        AddAnimation(weapClone.fpsAnimations.fps_chamber_close_noAmmo, anim);
        AddAnimation(weapClone.fpsAnimations.fps_change_barrell, anim);

        foreach (var multianim in weapClone.fpsAnimations.multi_ammo_chambering)
            AddAnimation(multianim.anim_id, anim);

        // Lock: tutto NotEditable, poi sblocca CenterSpine2_BJ + figli (per animare),
        // poi rilocka l'arma (figlia di CenterSpine) e gli SkinnedMeshRenderer
        SetHideFlagsRecursive(animRoot.transform, HideFlags.NotEditable);
        foreach (Transform child in centerSpine.parent)
            SetHideFlagsRecursive(child, HideFlags.None);
        weapClone.hideFlags = HideFlags.None;
        weapClone.transform.hideFlags = HideFlags.None;
        if(weapClone.GetComponent<Animation>())
            GameObject.DestroyImmediate(weapClone.GetComponent<Animation>());
        //SetHideFlagsRecursive(weapClone.transform, HideFlags.NotEditable);
        foreach (var smr in animRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.gameObject.hideFlags = HideFlags.NotEditable;
        animRoot.gameObject.GetComponentInChildren<Animation>().hideFlags = HideFlags.None;

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

    /// <summary>
    /// Loads all AnimationClip assets matching <paramref name="anim_name"/>, ensures they
    /// are legacy + ClampForever, and adds them to the Animation component. Returns the
    /// list of clips actually added (so callers can post-process the asset itself).
    /// </summary>
    private static List<AnimationClip> AddAnimation(string anim_name, Animation anim)
    {
        var added = new List<AnimationClip>();
        if (string.IsNullOrEmpty(anim_name)) return added;

        string[] assets = AssetDatabase.FindAssets(anim_name + " t:AnimationClip");
        foreach (string guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".fbx")) continue;
            if (alreadyAdded.Contains(guid)) continue;

            AnimationClip temp = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (temp == null) continue;

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
            added.Add(temp);
        }
        return added;
    }

    /// <summary>
    /// If the weapon has a spin_pos and the given clip animates that exact transform,
    /// prompt the user to strip those curves so bolt_movementDistance can drive the bolt
    /// at runtime. Modifies the asset on disk if the user confirms.
    /// </summary>
    private static void MaybePromptStripSpinPosFromClip(GenericGun weapon, AnimationClip clip, Transform animationRoot, string clipLabel)
    {
        if (weapon == null || weapon.spin_pos == null) return;
        if (clip == null || animationRoot == null) return;

        // Path of spin_pos relative to the Animation component's transform (ROOT of the tester rig).
        // If spin_pos isn't a descendant, CalculateTransformPath returns an empty string.
        string spinPosPath = AnimationUtility.CalculateTransformPath(weapon.spin_pos, animationRoot);
        if (string.IsNullOrEmpty(spinPosPath)) return;

        var matching = new List<EditorCurveBinding>();
        foreach (var b in AnimationUtility.GetCurveBindings(clip))
        {
            if (b.path == spinPosPath) matching.Add(b);
        }

        if (matching.Count == 0) return;

        bool yes = EditorUtility.DisplayDialog(
            "spin_pos animated in " + clipLabel + " clip",
            "The " + clipLabel + " clip '" + clip.name + "' contains " + matching.Count +
            " keyframed curve(s) on the spin_pos Transform.\n\n" +
            "Path: " + spinPosPath + "\n\n" +
            "Remove these curves so the bolt can be driven at runtime by bolt_movementDistance?",
            "Yes, remove",
            "No, keep");

        if (!yes) return;

        foreach (var b in matching)
            AnimationUtility.SetEditorCurve(clip, b, null);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AnimationTester] Removed " + matching.Count + " curve(s) on '" + spinPosPath +
                  "' from clip '" + clip.name + "'.", clip);
    }
}
#endif