

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

        if (GUILayout.Button("Test on Rig"))
            TPSRigTesterManager.LinkHelmet(helmet);

        EditorGUILayout.BeginHorizontal();
        if (TPSRigTesterManager.IsHelmetLinked(helmet))
        {
            /*if (GUILayout.Button("Detach from Rig"))
                TPSRigTesterManager.UnlinkHelmet();*/
        }
        if (TPSRigTesterManager.FindTester() != null)
        {
            if (GUILayout.Button("Disable Tester"))
                TPSRigTesterManager.DisableTester();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        // === Existing head placement checker ===
        if (GUILayout.Button("Enable / Disable head placement checker") && !Application.isPlaying)
        {
            HeadPlacementChecker checker = GameObject.FindFirstObjectByType<HeadPlacementChecker>();
            if (checker)
            {
                if (checker.attached_headgear == helmet)
                    DestroyImmediate(checker.gameObject);
                else
                    checker.attached_headgear = helmet;
            }
            else
            {
                checker = Instantiate(EditorAssetFinder.Find<GameObject>("HeadPlacementChecker")).GetComponent<HeadPlacementChecker>();
                checker.attached_headgear = helmet;
            }
        }

        DrawDefaultInspector();
    }
}
#endif