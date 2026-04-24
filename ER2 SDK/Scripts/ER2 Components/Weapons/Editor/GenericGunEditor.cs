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
                checker = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Models/HandsPlacement/Prefabs/HandsPlacementChecker.prefab", typeof(GameObject))).GetComponent<HandsPlacementChecker>();
                checker.attached_weapon = myScript;
            }
        }


        DrawDefaultInspector();
    }
}
#endif
