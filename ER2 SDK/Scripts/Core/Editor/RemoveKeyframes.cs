#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RemoveKeyframes : Editor
{
    static AnimationClip targetAnimationClip;


    public static void RemoveSpecificKeyframes(string animPath, GenericGun gg, bool isEquip)
    {
        if (gg == null || string.IsNullOrEmpty(animPath))
            return;

        targetAnimationClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(animPath, typeof(AnimationClip));

        AnimationClipCurveData[] curveDataArray = AnimationUtility.GetAllCurves(targetAnimationClip);
        bool dirty = false;
        foreach (AnimationClipCurveData curveData in curveDataArray)
        {
            if (MustRemove(curveData.path, gg, isEquip))
            {
                dirty = true;

                /*if (curveData.propertyName[curveData.propertyName.Length - 1] == 'x')
                    Debug.Log("REMOVING: '" + curveData.path + "' -> " + curveData.propertyName);*/

                AnimationCurve curve = curveData.curve;
                Keyframe[] keyframes = curve.keys;

                for (int i = keyframes.Length - 1; i >= 0; i--)
                    curve.RemoveKey(i);

                AnimationUtility.SetEditorCurve(targetAnimationClip, curveData.path, curveData.type, curveData.propertyName, curve);


                /*targetAnimationClip.SetCurve(curveData.path, typeof(Transform), "m_LocalPosition", null);
                targetAnimationClip.SetCurve(curveData.path, typeof(Transform), "m_LocalRotation", null);
                targetAnimationClip.SetCurve(curveData.path, typeof(Transform), "m_LocalScale", null);*/
            }
        }

        if (dirty)
            UnityEditor.EditorUtility.SetDirty(targetAnimationClip);
    }


    private readonly static string root2_name = "RIGROOT_BJ";
    public static bool MustRemove(string keyframeName, GenericGun gg, bool isEquip)
    {
        if (keyframeName.Length == 0)
            return true;

        if (keyframeName.EndsWith(root2_name))
            return true;

        if (gg!=null)
        {
            //rimuovi keyframe spin in equip animation
            if (isEquip && gg.spin_pos != null && keyframeName.EndsWith("/" + gg.spin_pos.name))
                return true;

            //rimuovi keyframe magazine
            if (gg.magazinePosition != null && keyframeName.Contains("/" + gg.magazinePosition.name+"/"))
                return true;
        }


        return false;
    }
}

#endif