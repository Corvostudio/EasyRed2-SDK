using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteInEditMode]
public class HandsPlacementChecker : MonoBehaviour
{
    public GenericGun attached_weapon;

    public Transform left_hand;
    public Transform right_hand;

    private void Update()
    {
        if (left_hand && right_hand && attached_weapon && transform.parent==null)
        {
            right_hand.transform.localPosition = Vector3.zero;
            right_hand.transform.localScale = left_hand.transform.localScale = transform.localScale = Vector3.one;
            transform.position = attached_weapon.transform.position - attached_weapon.transform.TransformDirection(attached_weapon.tpsAnims.localPosInHands);

            if (attached_weapon.leftHandHoldPosition)
            {
                left_hand.transform.position = attached_weapon.leftHandHoldPosition.position;
                left_hand.transform.rotation = attached_weapon.leftHandHoldPosition.rotation;
                if (!left_hand.gameObject.activeSelf)
                    left_hand.gameObject.SetActive(true);
            }
            else
            {
                if (left_hand.gameObject.activeSelf)
                    left_hand.gameObject.SetActive(false);
            }
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
    private void OnEnable()
    {
        var flags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
        foreach (var t in GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = flags;

#if UNITY_EDITOR
        UnityEditor.SceneVisibilityManager.instance.DisablePicking(gameObject, true);
#endif
    }
}

#if UNITY_EDITOR

[InitializeOnLoad]
public static class PlacementCheckersCleanup
{
    static PlacementCheckersCleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload += CleanAll;
        EditorSceneManager.sceneOpening += (path, mode) => CleanAll();
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.ExitingEditMode) CleanAll();
        };
        // Sweep any leftovers right after domain reload / editor startup
        EditorApplication.delayCall += CleanAll;
    }

   // [MenuItem("Tools/Placement Checkers/Force Clean All")]
    public static void CleanAll()
    {
        Clean<HandsPlacementChecker>();
        Clean<HandsPlacementCheckerSteeringWheel>();
        Clean<HeadPlacementChecker>();
        Clean<HandsPlacementCheckerSeat>();
    }

    private static void Clean<T>() where T : MonoBehaviour
    {
        foreach (var c in Resources.FindObjectsOfTypeAll<T>())
        {
            if (c == null) continue;
            if (EditorUtility.IsPersistent(c)) continue;
            if (c.gameObject == null) continue;
            Object.DestroyImmediate(c.gameObject);
        }
    }
}
#endif