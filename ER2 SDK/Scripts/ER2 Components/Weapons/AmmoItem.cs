using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AmmoItem : ItemObjectStackable
{
    [Header("Shooted bullet data")]
    [Tooltip("Ballistic bullet data. Read the documentation to use an existing caliber type, or to create a new one.")]
    public string caliber_type = "792mm";
    [Tooltip("Extracted catrdrige model. Currently supports 'ExtractedBullet' or 'ExtractedBulletSmall'")]
    public string empty_casing_id = "ExtractedBullet";//or ExtractedBulletSmall

    /*[Header("Custom Ammo Data (Optional if not using caliber_type from game)")]
    public bool useCustomData = false;
    public string custom_fxs_bundle = "er2fxs";
    public BulletData customBulletData;*/
}


/*
#if UNITY_EDITOR
[CustomEditor(typeof(AmmoItem))]
public class AmmoItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AmmoItem myScript = (AmmoItem)target;

        if (myScript.isCustom && !string.IsNullOrEmpty(myScript.custom_caliber_id))
            myScript.caliber_type

    }
}
#endif*/