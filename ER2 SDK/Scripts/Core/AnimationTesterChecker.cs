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

public static partial class AnimationTesterTool
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

        // Spawna i magazine compatibili e gli attachment che cambiano le animazioni (bipode...)
        SpawnCompatibleMagazinesAndAttachments(weapClone);

        // Animazioni
        Animation anim = animRoot.transform.Find("ROOT").GetComponent<Animation>();
        alreadyAdded.Clear();

        // Equip clip (named fps_putaway for legacy reasons): after adding, prompt the user
        // to strip any spin_pos keyframes so bolt_movementDistance can drive it at runtime.
        var equipClips = AddAnimation(weapClone.fpsAnimations.fps_putaway, anim);
        foreach (var clip in equipClips)
            MaybePromptStripSpinPosFromClip(weapClone, clip, anim.transform, "equip");

        //tutte le altre animazioni che l'arma puo' riprodurre: set base, override attivati dagli attachment
        //(bipode e simili, varianti "deployed" comprese), override per-magazine del belt manager e dei magazine
        foreach (string anim_id in CollectWeaponAnimationIds(weapClone))
            AddAnimation(anim_id, anim);

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

    /// <summary>
    /// Hook implementato dal gioco (file partial fuori dall'SDK) per aggiungere le sorgenti di animazioni
    /// che l'SDK non conosce. Senza quella parte la chiamata viene rimossa dal compilatore.
    /// </summary>
    static partial void CollectGameSpecificAnimationIds(GenericGun weapon, List<string> ids);

    /// <summary>
    /// Ogni animazione che l'arma puo' riprodurre: AnimationData base, override attivati da un attachment
    /// installato (es. bipode, varianti "deployed" comprese), override per-magazine ospitati nel belt manager
    /// e override dei magazine compatibili. I nomi duplicati vengono scartati.
    /// </summary>
    public static List<string> CollectWeaponAnimationIds(GenericGun weapon)
    {
        var ids = new List<string>();
        if (weapon == null) return ids;

        //set base dell'arma
        AddAnimationDataIds(weapon.fpsAnimations, ids);

        //override attivati da un attachment installato (bipode e simili)
        if (weapon.attachmentAnimationOverrides != null)
        {
            foreach (AttachmentAnimationOverride attachment_over in weapon.attachmentAnimationOverrides)
            {
                if (attachment_over == null) continue;

                AddAnimationDataIds(attachment_over.animations, ids);
                AddAnimationId(attachment_over.fps_change_barrell_deployed, ids);
                if (attachment_over.deployedReloadAnimations != null)
                {
                    AddAnimationId(attachment_over.deployedReloadAnimations.override_anim_reload_half, ids);
                    AddAnimationId(attachment_over.deployedReloadAnimations.override_anim_reload_full, ids);
                }
            }
        }

        //override per-magazine ospitati nel belt manager (ricariche standard e set bipode)
        AmmoBeltsFPSManager beltManager = weapon.GetComponent<AmmoBeltsFPSManager>();
        if (beltManager != null && beltManager.compatibleMagazines != null)
        {
            foreach (FPSMagManager fps_mag in beltManager.compatibleMagazines)
            {
                if (fps_mag == null) continue;

                AddAnimationId(fps_mag.override_reload_anim_partial, ids);
                AddAnimationId(fps_mag.override_reload_anim_full, ids);

                BipodMagazineOverride bipod_over = fps_mag.bipodOverrides;
                if (bipod_over != null)
                {
                    AddAnimationId(bipod_over.override_reload_anim_partial, ids);
                    AddAnimationId(bipod_over.override_reload_anim_full, ids);
                    AddAnimationId(bipod_over.override_reload_anim_partial_deployed, ids);
                    AddAnimationId(bipod_over.override_reload_anim_full_deployed, ids);
                    AddAnimationId(bipod_over.override_putaway_anim, ids);
                    AddAnimationId(bipod_over.override_unequip_anim, ids);
                    AddAnimationId(bipod_over.override_change_barrell_anim, ids);
                    AddAnimationId(bipod_over.override_change_barrell_anim_deployed, ids);
                }
            }
        }

        //magazine compatibili (gia' montati da SpawnCompatibleMagazinesAndAttachments): i loro override
        foreach (Magazine mag in weapon.GetComponentsInChildren<Magazine>(true))
        {
            if (mag == null) continue;

            AddAnimationId(mag.override_anim_reload_half, ids);
            AddAnimationId(mag.override_anim_reload_full, ids);
        }

        //sorgenti specifiche del gioco (non presenti nell'SDK): no-op se questa parte non e' inclusa
        CollectGameSpecificAnimationIds(weapon, ids);
        return ids;
    }

    private static void AddAnimationDataIds(AnimationData data, List<string> ids)
    {
        if (data == null) return;

        AddAnimationId(data.fps_putaway, ids);
        AddAnimationId(data.fps_unequip, ids);
        AddAnimationId(data.fps_reload_full, ids);
        AddAnimationId(data.fps_reload_half, ids);
        AddAnimationId(data.fps_bolt_action, ids);
        AddAnimationId(data.fps_chamber_open, ids);
        AddAnimationId(data.fps_chamber_open_noAmmo, ids);
        AddAnimationId(data.fps_chamber, ids);
        AddAnimationId(data.fps_chamber_close, ids);
        AddAnimationId(data.fps_chamber_close_noAmmo, ids);
        AddAnimationId(data.fps_change_barrell, ids);
        AddAnimationId(data.fps_bipod_deploy, ids);
        AddAnimationId(data.fps_bipod_undeploy, ids);

        if (data.multi_ammo_chambering != null)
        {
            foreach (var multianim in data.multi_ammo_chambering)
                AddAnimationId(multianim.anim_id, ids);
        }
    }

    private static void AddAnimationId(string anim_id, List<string> ids)
    {
        if (!string.IsNullOrEmpty(anim_id) && !ids.Contains(anim_id))
            ids.Add(anim_id);
    }

    private static void SetHideFlagsRecursive(Transform t, HideFlags flags)
    {
        t.gameObject.hideFlags = flags;
        foreach (Transform child in t) SetHideFlagsRecursive(child, flags);
    }

    /// <summary>
    /// Mette sull'arma di test tutto cio' che serve ad animare: i magazine compatibili (per socket) e gli
    /// attachment che portano un override di animazioni (es. il bipode), cosi' le clip si animano sul modello
    /// reale invece che su un'arma spoglia. Un solo scan dei prefab del progetto.
    /// </summary>
    private static void SpawnCompatibleMagazinesAndAttachments(GenericGun weapon)
    {
        bool wantsMagazines = weapon.magazinePosition != null && !string.IsNullOrEmpty(weapon.magazineSocket);

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            //magazine compatibili -> magazinePosition
            Magazine mag = prefab.GetComponent<Magazine>();
            if (mag != null)
            {
                if (wantsMagazines && mag.socket == weapon.magazineSocket)
                    SpawnOnTester(prefab, weapon.magazinePosition);
                continue;
            }

            //attachment con override di animazioni -> la loro slot (serve vederli mentre si anima)
            Attachment att = prefab.GetComponent<Attachment>();
            if (att == null || string.IsNullOrEmpty(att.item_id)) continue;
            if (!HasAnimationOverrideFor(weapon, att.item_id)) continue;

            Transform slot = FindAttachmentSlot(weapon, att.item_id);
            if (slot != null && slot.childCount == 0)
                SpawnOnTester(prefab, slot);
        }
    }

    private static void SpawnOnTester(GameObject prefab, Transform parent)
    {
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
    }

    private static bool HasAnimationOverrideFor(GenericGun weapon, string attachment_id)
    {
        if (weapon.attachmentAnimationOverrides == null) return false;

        foreach (AttachmentAnimationOverride attachment_over in weapon.attachmentAnimationOverrides)
        {
            if (attachment_over != null && attachment_id.Equals(attachment_over.attachment_id))
                return true;
        }
        return false;
    }

    private static Transform FindAttachmentSlot(GenericGun weapon, string attachment_id)
    {
        if (weapon.supportedAttachments == null) return null;

        foreach (SupportedAttachment slot in weapon.supportedAttachments)
        {
            if (slot != null && attachment_id.Equals(slot.attachment_id))
                return slot.attachmentPos;
        }
        return null;
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
