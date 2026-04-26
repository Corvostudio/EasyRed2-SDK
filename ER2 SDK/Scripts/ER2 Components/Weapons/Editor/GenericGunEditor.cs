#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(GenericGun))]
public partial class GenericGunEditor : Editor
{
    public override void OnInspectorGUI()
    {

        GenericGun myScript = (GenericGun)target;

        if (!string.IsNullOrEmpty(myScript.modBundleName))
            GUILayout.Label("Mod bunlde name: " + myScript.modBundleName);

        if (GUILayout.Button("Enable / Disable hands placement checker") && !Application.isPlaying)
        {
            HandsPlacementChecker checker = GameObject.FindFirstObjectByType<HandsPlacementChecker>();
            if (checker)
            {
                if (checker.attached_weapon == myScript)
                    DestroyImmediate(checker.gameObject);
                else
                    checker.attached_weapon = myScript;
            }
            else
            {
                checker = Instantiate(EditorAssetFinder.Find<GameObject>("HandsPlacementChecker")).GetComponent<HandsPlacementChecker>();
                checker.attached_weapon = myScript;
            }
        }


        DrawDefaultInspector();
    }
}
#endif
