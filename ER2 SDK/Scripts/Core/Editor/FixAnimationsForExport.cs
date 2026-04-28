using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public class ModelImporterEditor:Editor
{
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


    [MenuItem("Assets/ER2 TOOLS/Tools/Animation making/Fix animation as FPS anim", false, 2003)]
    public static void SetUpPlaneFlap()
    {
        if (!IsNewActionTime())
            return;

        foreach (UnityEngine.Object activeObject in Selection.objects)
        {
            if (activeObject == null)
            {
                Debug.Log("Please select a GameObject in the scene or a model asset in the project view.");
                return;
            }

            ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(activeObject)) as ModelImporter;

            if (modelImporter != null)
            {
                FixAnimation(modelImporter, activeObject);
            }
            else
            {
                Debug.Log("Selected object is not a model asset.");
            }
        }
    }

    public static void FixAnimation(ModelImporter singleImporter, UnityEngine.Object selectedObject)
    {
        singleImporter.globalScale = 1;
        singleImporter.animationPositionError = .1f;
        singleImporter.animationRotationError = .1f;
        singleImporter.animationScaleError = .1f;
        singleImporter.resampleCurves = true;
        singleImporter.materialImportMode = ModelImporterMaterialImportMode.None;

        List<ModelImporterClipAnimation> validClips = new List<ModelImporterClipAnimation>();
        foreach (ModelImporterClipAnimation clip in singleImporter.defaultClipAnimations)
        {
            if (clip.name.Contains("|Action"))
            {
                Debug.Log(clip.name + " -> " + selectedObject.name);
                clip.name = selectedObject.name;
                clip.wrapMode = WrapMode.ClampForever;
                validClips.Add(clip);
            }
        }
        singleImporter.clipAnimations = validClips.ToArray();
        validClips.Clear();

        //serializedObject.ApplyModifiedProperties();
        singleImporter.SaveAndReimport();
    }

    private static HashSet<string> alreadyAdded = new HashSet<string>();
    [MenuItem("Assets/ER2 TOOLS/Tools/Animation making/Set up FPS animator", false, 2004)]
    [MenuItem("GameObject/ER2 TOOLS/Animation making/Set up FPS animator", false, 2004)]
    public static void SetUpAnimationTesting()
    {
        if (!ModTemplates.IsNewActionTime()) return;

        GenericGun weapon = null;
        foreach (GameObject go in Selection.objects)
        {
            if (go != null && go.GetComponent<GenericGun>())
            {
                weapon = go.GetComponent<GenericGun>();
                break;
            }
        }
        if (!weapon) return;

        AnimationTesterTool.ToggleAnimationTester(weapon);
    }

    private static void AddAnimation(string anim_name,Animation anim)
    {
        if (string.IsNullOrEmpty(anim_name))
            return;

        //Riporta path di assset che sto cercando
        string[] assets = AssetDatabase.FindAssets(anim_name + " t:AnimationClip");
        foreach (string guid in assets)
        {
            //fix as legacy
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx") && !alreadyAdded.Contains(guid))
            {
                //fix clips if necessary
                AnimationClip temp = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
                if (temp.wrapMode != WrapMode.ClampForever || !temp.legacy)
                {
                    temp.wrapMode = WrapMode.ClampForever;
                    temp.legacy = true;
                    UnityEditor.EditorUtility.SetDirty(temp);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                //actually adds
                anim.AddClip(temp, anim_name);
                alreadyAdded.Add(guid);
                //Debug.Log(temp+": "+ anim.GetClipCount(), anim);
            }
        }
    }

}