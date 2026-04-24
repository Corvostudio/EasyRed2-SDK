using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TPSRigPreviewManager : MonoBehaviour
{
    public SkinnedMeshRenderer uniform;
    public SkinnedMeshRenderer uniform_lod;
    public SkinnedMeshRenderer vest;
    public SkinnedMeshRenderer vest_lod;
    public SkinnedMeshRenderer head;
    public SkinnedMeshRenderer head_lod;
    public SkinnedMeshRenderer hands;
    public SkinnedMeshRenderer hands_lod;
    public SkinnedMeshRenderer uniform_fps;
    public SkinnedMeshRenderer hands_fps;


#if UNITY_EDITOR
    public static TPSRigPreviewManager GetTpsRigtester()
    {
        TPSRigPreviewManager tester = GameObject.FindObjectOfType<TPSRigPreviewManager>();
        if (tester==null)
        {
            GameObject animRoot = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Rigs/tps rig tester.prefab", typeof(GameObject)));
            animRoot.transform.localPosition = animRoot.transform.localEulerAngles = Vector3.zero;
            animRoot.transform.localScale = Vector3.one;
            tester = animRoot.GetComponent<TPSRigPreviewManager>();
        }
        return tester;
    }



    private static double next_action_time = 0;
    public static bool IsNewActionTime()
    {
        if (EditorApplication.timeSinceStartup > next_action_time)
        {
            next_action_time = EditorApplication.timeSinceStartup + .5f;
            return true;
        }
        return false;
    }

    [MenuItem("Assets/ER2 MODS/Tools/RIG Testing/Test rigged mesh", false, 2004)]
    //[MenuItem("GameObject/ER2 MODS/Tools/TPS anims/Test TPS mesh rig", false, 2004)]
    public static void SetUpAnimationTesting()
    {
        if (!IsNewActionTime())
            return;

        foreach (Object obj in Selection.objects)
        {
            if (!(obj is Mesh))
            {
                Debug.LogError("Selected Model '"+ obj+"' is not a skinned mesh.", obj);
                continue;
            }

            //get selected skinned mesh
            if (!GetSelectedSkinnedMeshRenderer((Mesh)obj, out SkinnedMeshRenderer originalMeshRenderer, out int bonecount))
                return;
            //get tester
            TPSRigPreviewManager tester = GetTpsRigtester();
            //test by type and apply
            switch (bonecount)
            {
                case 2:
                    if (TestBoneOrder(originalMeshRenderer, tester.head, "Head"))
                    {
                        tester.head.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.head.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 3:
                    if (TestBoneOrder(originalMeshRenderer, tester.vest, "Vest"))
                    {
                        tester.vest.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.vest.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 4:
                    if (TestBoneOrder(originalMeshRenderer, tester.hands, "Hands"))
                    {
                        tester.hands.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.hands.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 22:
                    if (TestBoneOrder(originalMeshRenderer, tester.uniform, "Uniform"))
                    {
                        tester.uniform.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.uniform.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 51:
                    if (TestBoneOrder(originalMeshRenderer, tester.uniform_fps, "FPS Uniform"))
                    {
                        tester.uniform_fps.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.uniform_fps.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                default:
                    Debug.LogError("Selected skinned mesh '" + originalMeshRenderer.sharedMesh + "' has wrong bone amount ("+ bonecount + ").", originalMeshRenderer.sharedMesh);
                    break;
            }
        }
    }


    //check bone order
    public static bool TestBoneOrder(SkinnedMeshRenderer originalSkinnedMeshRenderer, SkinnedMeshRenderer targetSkinnedMeshRenderer, string type)
    {
        //check bone count
        if (originalSkinnedMeshRenderer.bones.Length != targetSkinnedMeshRenderer.bones.Length)
        {
            Debug.LogError("Selected skinned mesh '" + originalSkinnedMeshRenderer.sharedMesh + "' is not of type '" + type + "' or is not skinned to the correct bones.", originalSkinnedMeshRenderer.sharedMesh);
            return false;
        }

        //check bone order
        for (int i = 0; i < originalSkinnedMeshRenderer.bones.Length; i++)
        {
            if (originalSkinnedMeshRenderer.bones[i].name != targetSkinnedMeshRenderer.bones[i].name)
            {
                Debug.LogError("Selected skinned mesh '" + originalSkinnedMeshRenderer.sharedMesh + "' is not of type '" + type + "' or has wrong bone order.", originalSkinnedMeshRenderer.sharedMesh);
                return false;
            }
        }

        //all good
        Debug.Log("Uniform '"+ originalSkinnedMeshRenderer.sharedMesh+"'  imported correctly.", originalSkinnedMeshRenderer.sharedMesh);
        return true;
    }

    /// <summary>
    /// Do the checks and import the skinned mesh
    /// </summary>
    public static bool GetSelectedSkinnedMeshRenderer(Mesh selectedMesh, out SkinnedMeshRenderer skinnedMeshRenderer, out int bonesCount)
    {
        bonesCount = 0;
        skinnedMeshRenderer = null;
        // Load the main asset (the root of the prefab)
        GameObject model = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(selectedMesh)) as GameObject;
        // Ensure it has a SkinnedMeshRenderer
        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh== (Mesh)selectedMesh)
            {
                skinnedMeshRenderer = smr;
                break;
            }
        }
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("Selected Model '"+ selectedMesh+"' is not a skinned mesh.",selectedMesh);
            return false;
        }

        bonesCount = skinnedMeshRenderer.bones.Length;
        return true;
    }
#endif

}
