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
            var existing = Resources.FindObjectsOfTypeAll<HandsPlacementChecker>();
            HandsPlacementChecker mine = null;
            foreach (var c in existing)
            {
                if (c == null || EditorUtility.IsPersistent(c)) continue;
                if (c.attached_weapon == myScript) { mine = c; continue; }
                DestroyImmediate(c.gameObject);
            }

            if (mine != null)
            {
                DestroyImmediate(mine.gameObject);
            }
            else
            {
                var checker = Instantiate(EditorAssetFinder.Find<GameObject>("HandsPlacementChecker"))
                    .GetComponent<HandsPlacementChecker>();
                checker.attached_weapon = myScript;
            }
        }


        DrawDefaultInspector();
    }
}
#endif
