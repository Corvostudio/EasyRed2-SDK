using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TextureGeneratorLOD : EditorWindow
{
    private int FINAL_TEX_SIZE = 256;

    private TextureGeneratorParameters overrideParameters = null;
    private float objectSize = 20;
    private float aperture = 7.5f;
    private float exposure = 1;
    private Vector3 shiftCenter = new Vector3(0,3,0);
    private GameObject highPolyModel;
    //private GameObject lowPolyModel;
    private bool allowAlpha = false;
    private bool flipY = false;
    private bool isTree = false;
    private bool includeTopView = false;
    private bool isDestroyed = false;

    [MenuItem("ER2 TOOLS/Tools/Texture/LOD Texture Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<TextureGeneratorLOD>("Texture Generator");
    }

    private void OnGUI()
    {
        //inputs
        FINAL_TEX_SIZE = EditorGUILayout.IntField("Texture size", FINAL_TEX_SIZE);
#pragma warning disable CS0618 // Type or member is obsolete
        overrideParameters = (TextureGeneratorParameters)EditorGUILayout.ObjectField("Override Parameters", overrideParameters,typeof(TextureGeneratorParameters));
#pragma warning restore CS0618 // Type or member is obsolete
        if (!overrideParameters)
        {
            objectSize = EditorGUILayout.FloatField("Object size", objectSize);
            aperture = EditorGUILayout.FloatField("Aperture", aperture);
            exposure = EditorGUILayout.FloatField("Exposure", exposure);
            shiftCenter = EditorGUILayout.Vector3Field("ShiftCenter", shiftCenter);
        }
        else
        {
            objectSize = overrideParameters.objectSize;
            aperture = overrideParameters.aperture;
            exposure = overrideParameters.exposure;
            shiftCenter = overrideParameters.shiftCenter;
        }
        highPolyModel = EditorGUILayout.ObjectField("High Poly Model", highPolyModel, typeof(GameObject), true) as GameObject;
        allowAlpha = EditorGUILayout.Toggle("Allow Alpha", allowAlpha);
        flipY = EditorGUILayout.Toggle("Flip Y", flipY);
        isTree = EditorGUILayout.Toggle("Tree Billboard Mode", isTree);  // ← new toggle
        if (isTree)
            includeTopView = EditorGUILayout.Toggle("Inlcude Top View", includeTopView);  // ← new toggle
        isDestroyed = EditorGUILayout.Toggle("Destroyed", isDestroyed);  // ← new toggle


        //buttons
        if (GUILayout.Button("Generate Texture"))
        {
            if (highPolyModel != null /*&& lowPolyModel != null*/)
            {
                GenerateTexture();
            }
            else
            {
                Debug.LogError("Please assign both high poly and low poly models.");
            }
        }
    }



    private GameObject CreateLightGo(Vector3 rotation, float lightMult=1)
    {
        GameObject go = new GameObject("LIGHT");
        Light light = go.gameObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = exposure* lightMult*.75f;
        go.transform.eulerAngles = rotation;
        return go;
    }
    private GameObject CreateLightGo()
    {
        GameObject lightsRoot = new GameObject("LIGHTS");
        CreateLightGo(new Vector3(0, 0, 0)).transform.SetParent(lightsRoot.transform);
        CreateLightGo(new Vector3(0, 90, 0)).transform.SetParent(lightsRoot.transform);
        CreateLightGo(new Vector3(0, 180, 0)).transform.SetParent(lightsRoot.transform);
        CreateLightGo(new Vector3(0, 270, 0)).transform.SetParent(lightsRoot.transform);
        CreateLightGo(new Vector3(90, 0, 0),.5f).transform.SetParent(lightsRoot.transform);

        return lightsRoot;
    }
    private void GenerateTexture()
    {
        string fileName = highPolyModel.name + (isDestroyed? "_DST_LOD_TEX.png" : "_INTACT_LOD_TEX.png");
        var path = EditorUtility.SaveFilePanel(
            "Save texture as PNG",
            "",//Application.dataPath+"/Corvostudio/Buildings",
            fileName,
            "png");

        if (path == null || path.Length < 3)
        {
            Debug.Log("Canceled.");
            return;
        }


        Vector3 originalPos = highPolyModel.transform.position;
        highPolyModel.transform.position = new Vector3(0, 1000, 1000);

        List<Light> lights = new List<Light>();
        foreach (var l in FindObjectsOfType<Light>())
        {
            if (l.gameObject.activeSelf)
            {
                lights.Add(l);
                l.gameObject.SetActive(false);
            }
        }
        UnityEngine.Rendering.Volume volume = FindObjectOfType<UnityEngine.Rendering.Volume>();
        bool volumeWasActive = volume && volume.gameObject.activeSelf;
        if (volumeWasActive)
            volume.gameObject.SetActive(false);


        //camera.gameObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        //camera.backgroundColor = Color.clear;

        // Set up the render texture

        // Create a new texture

        Texture2D front_texture = null;
        Texture2D right_texture = null;
        Texture2D back_texture = null;
        Texture2D left_texture = null;
        //Texture2D up_texture = new Texture2D(sub_tex_size, sub_tex_size);

        Texture2D finalTex = null;

        //GameObject lights = CreateLightGo();
        Color restore_ambient_color = RenderSettings.ambientLight;
        RenderSettings.ambientLight = new Color(1,1,1, 1);

        // Take screenshots from different views
        SetLODsEnabled(highPolyModel, 0);

        if (isTree)
        {
            if (!includeTopView)
            {
                front_texture = new Texture2D(FINAL_TEX_SIZE/2, FINAL_TEX_SIZE);
                right_texture = new Texture2D(FINAL_TEX_SIZE/2, FINAL_TEX_SIZE);
                // only front & right
                TakeScreenshot(highPolyModel, front_texture, Vector3.forward, "Front", FINAL_TEX_SIZE/2, FINAL_TEX_SIZE);
                TakeScreenshot(highPolyModel, right_texture, Vector3.right, "Right", FINAL_TEX_SIZE/2, FINAL_TEX_SIZE);
                finalTex = flipY ? ComposeVerticalHalf(front_texture, right_texture) : ComposeVerticalHalf(right_texture, front_texture);
            }
            else
            {
                // 3
                int x_size = (int)((FINAL_TEX_SIZE / 3f) * 2f);
                front_texture = new Texture2D(x_size, FINAL_TEX_SIZE);
                right_texture = new Texture2D(x_size, FINAL_TEX_SIZE);
                back_texture = new Texture2D(x_size, FINAL_TEX_SIZE);

                TakeScreenshot(highPolyModel, front_texture, Vector3.forward, "Front", x_size, FINAL_TEX_SIZE);
                TakeScreenshot(highPolyModel, right_texture, Vector3.right, "Right", x_size, FINAL_TEX_SIZE);
                TakeScreenshot(highPolyModel, back_texture, Vector3.up, "Top", x_size, FINAL_TEX_SIZE);
                finalTex = flipY ? ComposeVerticalHalfWithTop(front_texture, right_texture, back_texture) : ComposeVerticalHalfWithTop(right_texture, front_texture, back_texture);
            }
        }
        else
        {
            // all four
            int sub_tex_size = FINAL_TEX_SIZE / 2;
            front_texture = new Texture2D(sub_tex_size, sub_tex_size);
            right_texture = new Texture2D(sub_tex_size, sub_tex_size);
            back_texture = new Texture2D(sub_tex_size, sub_tex_size);
            left_texture = new Texture2D(sub_tex_size, sub_tex_size);

            TakeScreenshot(highPolyModel, front_texture, Vector3.forward, "Front", sub_tex_size, sub_tex_size);
            TakeScreenshot(highPolyModel, back_texture, Vector3.back, "Back", sub_tex_size, sub_tex_size);
            TakeScreenshot(highPolyModel, right_texture, Vector3.right, "Right", sub_tex_size, sub_tex_size);
            TakeScreenshot(highPolyModel, left_texture, Vector3.left, "Left", sub_tex_size, sub_tex_size);

            finalTex = flipY
                ? GenerateCompositeTexture(back_texture, front_texture, left_texture, right_texture)
                : GenerateCompositeTexture(front_texture, back_texture, right_texture, left_texture);
        }


        //TakeScreenshot(camera, highPolyModel, renderTexture, up_texture, Vector3.up, "Top");
        SetLODsEnabled(highPolyModel, -1);

        if (path.Length != 0)
        {
            var pngData = finalTex.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                AssetDatabase.Refresh();
            }
        }

        //re enable stuff
        foreach (var l in lights)
            l.gameObject.SetActive(true);
        if (volumeWasActive)
            volume.gameObject.SetActive(true);


        // Cleanup
        DestroyImmediate(front_texture);
        if (back_texture != null) DestroyImmediate(back_texture);
        DestroyImmediate(right_texture);
        if (left_texture != null) DestroyImmediate(left_texture);
        //DestroyImmediate(up_texture);
        DestroyImmediate(finalTex);
        //DestroyImmediate(lights);
        RenderSettings.ambientLight = restore_ambient_color;


        string assetPath = GetLocalAssetPath(path);
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);

        highPolyModel.transform.position = originalPos;
        Debug.Log("Texture generation complete ("+ fileName + "). Click here to view in explorer", obj);
    }
    private void TakeScreenshot(GameObject model, Texture2D texture, Vector3 direction, string viewName, int texWidth, int texHeight)
    {
        Camera camera = new GameObject("EditorCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = aperture;
        camera.farClipPlane = objectSize * 2;
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = model.transform.position + direction * objectSize;
        camera.transform.LookAt(model.transform.position);
        camera.transform.position += shiftCenter;

        RenderTexture rt = RenderTexture.GetTemporary(texWidth, texHeight, 24);
        camera.targetTexture = rt;
        camera.Render();

        RenderTexture.active = rt;
        texture.Reinitialize(texWidth, texHeight);
        texture.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
        if (!allowAlpha)
            IconGenerator.RemoveAlphaValue(texture, -1);
        texture.Apply();

        RenderTexture.ReleaseTemporary(rt);
        DestroyImmediate(camera.gameObject);
    }

    private void SetLODsEnabled(GameObject model, int lodValue)
    {
        foreach (LODGroup lod in model.GetComponentsInChildren<LODGroup>())
        {
            lod.ForceLOD(lodValue);
        }
    }

    private Texture2D GenerateCompositeTexture(Texture2D frontTexture, Texture2D backTexture, Texture2D rightTexture, Texture2D leftTexture/*, Texture2D topTexture*/)
    {
        // Create a new texture for the composite image
        int sub_tex_size = FINAL_TEX_SIZE / 2;
        Texture2D combinedTexture = new Texture2D(FINAL_TEX_SIZE, FINAL_TEX_SIZE);

        // Combine textures
        combinedTexture.SetPixels32(0, 0, sub_tex_size, sub_tex_size, frontTexture.GetPixels32());
        combinedTexture.SetPixels32(sub_tex_size, 0, sub_tex_size, sub_tex_size, backTexture.GetPixels32());
        combinedTexture.SetPixels32(0, sub_tex_size, sub_tex_size, sub_tex_size, rightTexture.GetPixels32());
        combinedTexture.SetPixels32(sub_tex_size, sub_tex_size, sub_tex_size, sub_tex_size, leftTexture.GetPixels32());

        combinedTexture.Apply();
        return combinedTexture;
    }
    /// <summary>
    /// Allunga verticalmente un 128×128 a 128×256 (o comunque metà×FULL).
    /// </summary>
    private Texture2D StretchVertical(Texture2D src)
    {
        int w = src.width;
        int h = FINAL_TEX_SIZE;
        var dst = new Texture2D(w, h, src.format, false);
        var srcPix = src.GetPixels32();
        var dstPix = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            int srcY = (int)((long)y * src.height / h);
            for (int x = 0; x < w; x++)
                dstPix[y * w + x] = srcPix[srcY * w + x];
        }
        dst.SetPixels32(dstPix);
        dst.Apply();
        if (!allowAlpha)
            IconGenerator.RemoveAlphaValue(dst, -1);
        return dst;
    }

    /// <summary>
    /// Componi i due rettangoli 128×256 uno a fianco all’altro in un 256×256.
    /// </summary>
    private Texture2D ComposeVerticalHalf(Texture2D leftFull, Texture2D rightFull)
    {
        var combined = new Texture2D(FINAL_TEX_SIZE, FINAL_TEX_SIZE, leftFull.format, false);
        // copy left
        combined.SetPixels32(0, 0, leftFull.width, leftFull.height, leftFull.GetPixels32());
        // copy right
        combined.SetPixels32(leftFull.width, 0, rightFull.width, rightFull.height, rightFull.GetPixels32());
        combined.Apply();
        return combined;
    }
    /// <summary>
    /// Componi i due rettangoli 128×256 uno a fianco all’altro in un 256×256.
    /// </summary>
    private Texture2D ComposeVerticalHalfWithTop(Texture2D leftFull, Texture2D rightFull, Texture2D topView)
    {
        var combined = new Texture2D(FINAL_TEX_SIZE*2, FINAL_TEX_SIZE, leftFull.format, false);
        // copy left
        combined.SetPixels32(0, 0, leftFull.width, leftFull.height, leftFull.GetPixels32());
        // copy right
        combined.SetPixels32(leftFull.width, 0, rightFull.width, rightFull.height, rightFull.GetPixels32());
        // copy top
        combined.SetPixels32(leftFull.width + rightFull.width, 0, topView.width, topView.height, topView.GetPixels32());

        combined.Apply();
        return combined;
    }

    public static string GetLocalAssetPath(string globalPath)
    {
        int charStart = globalPath.IndexOf("Assets");
        return globalPath.Substring(charStart);
    }
}
