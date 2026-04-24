#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ItemHelmet))]
public partial class ItemHelmetEditor : Editor
{
    public override void OnInspectorGUI()
    {

        ItemHelmet myScript = (ItemHelmet)target;

        if (GUILayout.Button("Enable / Disable head placement checker") && !Application.isPlaying)
        {
            HeadPlacementChecker checker = GameObject.FindFirstObjectByType<HeadPlacementChecker>();
            if (checker)
            {
                if (checker.attached_headgear == myScript)
                    DestroyImmediate(checker.gameObject);
                else
                    checker.attached_headgear = myScript;
            }
            else
            {
                checker = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Models/HeadPlacement/Prefabs/HeadPlacementChecker.prefab", typeof(GameObject))).GetComponent<HeadPlacementChecker>();
                checker.attached_headgear = myScript;
            }
        }


        DrawDefaultInspector();
    }
}
#endif