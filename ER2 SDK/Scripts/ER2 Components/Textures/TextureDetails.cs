using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public partial class TextureDetails : ScriptableObject
{
    [HideInInspector]
    public string bundleName = "";

    public Texture texture;
    public TextureType textureType = TextureType.LUT;

    public enum TextureType { LUT,Crosshair};


#if UNITY_EDITOR
    /// <summary>
    /// Check if it's completly setted up
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    {
        switch (textureType)
        {
            case TextureType.LUT:
                if (!IsValidLUT())
                    return false;
                string assetPath = AssetDatabase.GetAssetPath(texture);
                var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (tImporter == null)
                {
                    Debug.LogError("Unknown issue with LUT texture " + texture, texture);
                    return false;
                }
                TextureImporterPlatformSettings tips = tImporter.GetPlatformTextureSettings("Standalone");
                if (!tImporter.isReadable || !tips.overridden || tips.format!= TextureImporterFormat.RGB24 || tImporter.textureCompression != TextureImporterCompression.Uncompressed || tImporter.maxTextureSize!=1024)
                {
                    tImporter.isReadable = true;
                    tips.overridden = true;
                    tips.format = TextureImporterFormat.RGB24;
                    tImporter.SetPlatformTextureSettings(tips);
                    tImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    tImporter.maxTextureSize = 1024;
                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();
                }
                return true;
            case TextureType.Crosshair:
                if (!IsValidCrosshair())
                    return false;
                return true;
            default:
                Debug.LogError("Texture type not assigned correctly!", this);
                return false;
        }
    }

    /*public ModPropType ModPropType
    {
        get
        {
            switch (textureType)
            {
                case TextureType.LUT:
                    return global::ModPropType.lut_texture;
                case TextureType.Crosshair:
                    return global::ModPropType.crosshair_texture;
                default:
                    Debug.LogError("Texture type not assigned correctly!", this);
                    return global::ModPropType.crosshair_texture;
            }
        }
    }*/

    [MenuItem("Assets/ER2 MODS/Textures/Set up LUT filter", false, 2001)]
    public static void SetUpLUT()
    {
        foreach (Object selected in Selection.objects)
        {
            if (selected is Texture)
            {
                Texture tex = (Texture)selected;
                if (!IsValidLUT(tex))
                    return;

                TextureDetails td = ScriptableObject.CreateInstance<TextureDetails>();
                td.texture = tex;
                td.textureType = TextureType.LUT;
                string clickedAssetGuid = Selection.assetGUIDs[0];
                string path = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(clickedAssetGuid));
                path += "/"+ tex.name+" LUT.asset";
                UnityEditor.AssetDatabase.CreateAsset(td, path);
            }
            else
                Debug.LogError("Make sure to select a texture to create the LUT from!", selected);
        }
    }
    public bool IsValidLUT()
    {
        return IsValidLUT(texture);
    }
    public static bool IsValidLUT(Texture tex=null)
    {
        if (tex == null)
        {
            Debug.LogError("No texture selected!");
            return false;
        }

        if (tex.width != 1024 || tex.height != 32)
        {
            Debug.LogError("A valid LUT must be 1024x32!",tex);
            return false;
        }
        return true; // ok
    }


    [MenuItem("Assets/ER2 MODS/Textures/Set up crosshair", false, 2001)]
    public static void SetUpCrosshair()
    {
        foreach (Object selected in Selection.objects)
        {
            if (selected is Texture)
            {
                Texture tex = (Texture)selected;
                if (!IsValidCrosshair(tex))
                    return;

                TextureDetails td = ScriptableObject.CreateInstance<TextureDetails>();
                td.texture = tex;
                td.textureType = TextureType.Crosshair;
                string clickedAssetGuid = Selection.assetGUIDs[0];
                string path = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(clickedAssetGuid));
                path += "/" + tex.name + " CH.asset";
                UnityEditor.AssetDatabase.CreateAsset(td, path);
            }
            else
                Debug.LogError("Make sure to select a texture to create the LUT from!", selected);
        }
    }
    public bool IsValidCrosshair()
    {
        return IsValidCrosshair(texture);
    }
    public static bool IsValidCrosshair(Texture tex = null)
    {
        if (tex == null)
        {
            Debug.LogError("No texture selected!");
            return false;
        }

        if (tex.width != 512 || tex.height != 512)
        {
            Debug.LogError("A valid Crosshair must be 512x512!", tex);
            return false;
        }
        return true; // ok
    }
#endif
}