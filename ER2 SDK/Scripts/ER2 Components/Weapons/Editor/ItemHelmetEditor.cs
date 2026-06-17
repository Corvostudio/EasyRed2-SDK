

#if UNITY_EDITOR
using static UnityEngine.GraphicsBuffer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemHelmet))]
public partial class ItemHelmetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ItemHelmet helmet = (ItemHelmet)target;

        // === Linked banner ===
        if (TPSRigTesterManager.IsHelmetLinked(helmet))
            TPSRigTesterManager.DrawLinkedBanner("● LIVE-LINKED TO TPS TESTER (HEADGEAR)");

        // === TPS Rig Tester buttons ===
        GUILayout.Space(8);
        GUILayout.Label(" --- TPS Rig Tester ---");

        if (!TPSRigTesterAutoSelection.DrawInspectorUI())
        {
            if (GUILayout.Button("Test on Rig"))
                TPSRigTesterManager.LinkHelmet(helmet);

            EditorGUILayout.BeginHorizontal();
            if (TPSRigTesterManager.IsHelmetLinked(helmet))
            {
                /*if (GUILayout.Button("Detach from Rig"))
                    TPSRigTesterManager.UnlinkHelmet();*/
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (TPSRigTesterManager.FindTester() != null)
            {
                if (GUILayout.Button("Disable Tester"))
                    TPSRigTesterManager.DisableTester();
            }
        }

        GUILayout.Space(8);

        // === Existing head placement checker ===
        if (GUILayout.Button("Enable / Disable head placement checker") && !Application.isPlaying)
        {
            var existing = Resources.FindObjectsOfTypeAll<HeadPlacementChecker>();
            HeadPlacementChecker mine = null;
            foreach (var c in existing)
            {
                if (c == null || EditorUtility.IsPersistent(c)) continue;
                if (c.attached_headgear == helmet) { mine = c; continue; }
                DestroyImmediate(c.gameObject); // sweep stale ones
            }

            if (mine != null)
            {
                DestroyImmediate(mine.gameObject);
            }
            else
            {
                var checker = Instantiate(EditorAssetFinder.Find<GameObject>("HeadPlacementChecker"))
                    .GetComponent<HeadPlacementChecker>();
                checker.attached_headgear = helmet;
            }
        }

        DrawDefaultInspector();
    }
}
#endif