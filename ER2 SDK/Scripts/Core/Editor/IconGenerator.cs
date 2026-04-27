#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class IconGenerator : EditorWindow
{
    private static int FINAL_TEX_SIZE = 256;

    private static int TEX_SIZE = 256;
    private bool overrideTexSize = false;

    private TextureGeneratorParameters overrideParameters = null;
    [Range(1, 200)]
    private float objectSize = 20;
    [Range(.2f, 50)]
    private float aperture = 1;
    [Range(.5f, 2)]
    private float exposure = 1;
    private Vector3 shiftCenter = new Vector3(0, 0, 0);
    private GameObject iconizedModel;
    private GameObject objectCopy;

    private bool allowAlpha = true;
    //private GameObject lowPolyModel;

    [MenuItem("ER2 TOOLS/Tools/Texture/Icon Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<IconGenerator>("Icon Generator");
    }

    private void OnGUI()
    {
        overrideTexSize = EditorGUILayout.Toggle("Override Tex Size", overrideTexSize);
        if (overrideTexSize)
            TEX_SIZE = EditorGUILayout.IntField("Custom Tex Size", TEX_SIZE);
        else
            TEX_SIZE = FINAL_TEX_SIZE;

        //inputs
        if (!overrideParameters)
        {
            //objectSize = EditorGUILayout.FloatField("Camera Distance", objectSize);
            objectSize = 50;
            aperture = EditorGUILayout.FloatField("Shot Size", aperture);
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
        allowAlpha = EditorGUILayout.Toggle("Allow Alpha", allowAlpha);
        iconizedModel = EditorGUILayout.ObjectField("Item/Vehicle to make icon for", iconizedModel, typeof(GameObject), true) as GameObject;


        //error wrong item selected
        if (iconizedModel && !iconizedModel.GetComponent<Vehicle>() && !iconizedModel.GetComponent<ItemObject>())
        {
            GUILayout.Label("!Selected object is not a Vehicle nor an Item Object!");
            return;
        }


        //buttons
        if (iconizedModel)
        {
            if (GUILayout.Button("Generate Icon"))
                GenerateTexture();

            Vehicle veh = iconizedModel.GetComponent<Vehicle>();
            if (veh != null && veh.icon != null && GUILayout.Button("Override " + veh.icon.name))
            {
                string path = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + AssetDatabase.GetAssetPath(veh.icon);
                GenerateTexture(path);
            }

            ItemObject item = iconizedModel.GetComponent<ItemObject>();
            if (item != null && item.icon != null && GUILayout.Button("Override " + item.icon.name))
            {
                string path = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + AssetDatabase.GetAssetPath(item.icon);
                GenerateTexture(path);
            }
        }
    }



    private GameObject CreateLightGo(Vector3 rotation, float lightMult = 1)
    {
        GameObject go = new GameObject("LIGHT");
        Light light = go.gameObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = exposure * lightMult * .75f;
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
        CreateLightGo(new Vector3(90, 0, 0), .5f).transform.SetParent(lightsRoot.transform);

        return lightsRoot;
    }

    private string previous_path = null;
    private void GenerateTexture(string path = "")
    {
        if (previous_path == null)
            previous_path = Application.dataPath;

        // Check if there's an existing icon and use its folder path
        Sprite existingIcon = null;
        Vehicle veh = iconizedModel.GetComponent<Vehicle>();
        ItemObject item = iconizedModel.GetComponent<ItemObject>();

        if (veh != null && veh.icon != null)
            existingIcon = veh.icon;
        else if (item != null && item.icon != null)
            existingIcon = item.icon;

        if (existingIcon != null)
        {
            string existingPath = AssetDatabase.GetAssetPath(existingIcon);
            string fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + existingPath;
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                previous_path = directory;
        }

        string fileName = null;
        if (string.IsNullOrEmpty(path))
        {
            fileName = iconizedModel.name + "_tex_ICON.png";
            path = EditorUtility.SaveFilePanel(
                "Save texture as PNG",
                previous_path,//"",
                fileName,
                "png");
        }
        if (string.IsNullOrEmpty(fileName))
            fileName = Path.GetFileName(path);

        if (path == null || path.Length < 3)
        {
            Debug.Log("Canceled.");
            return;
        }
        previous_path = path;


        objectCopy = Instantiate(iconizedModel);
        objectCopy.transform.position = new Vector3(0, 0, 9000);
        objectCopy.transform.eulerAngles = Vector3.zero;


        List<Light> activeLights = new List<Light>();
        foreach (Light l in GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.enabled && l.gameObject.activeSelf)
            {
                activeLights.Add(l);
                l.enabled = false;
            }
        }
        Volume volumeGUI = GameObject.FindObjectOfType<Volume>();
        if (volumeGUI)
        {
            if (!volumeGUI.gameObject.activeSelf)
                volumeGUI = null;
            else
                volumeGUI.gameObject.SetActive(false);
        }


        // Create a new texture
        Texture2D texture = new Texture2D(TEX_SIZE, TEX_SIZE);
        //Texture2D up_texture = new Texture2D(sub_tex_size, sub_tex_size);

        GameObject lights = CreateLightGo();

        // Take screenshots from different views
        SetLODsEnabled(objectCopy, 0);
        TakeScreenshot(objectCopy, texture, "Front");
        //TakeScreenshot(camera, objectCopy, renderTexture, up_texture, Vector3.up, "Top");
        SetLODsEnabled(objectCopy, -1);


        if (path.Length != 0)
        {
            var pngData = texture.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                AssetDatabase.Refresh();
            }
        }

        //re enable stuff
        foreach (Light l in activeLights)
            l.enabled = true;
        if (volumeGUI)
            volumeGUI.gameObject.SetActive(true);

        // Cleanup
        DestroyImmediate(texture);
        DestroyImmediate(lights);


        //save and mark as sprite
        string assetPath = GetLocalAssetPath(path);
        Texture2D assetTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            AssetDatabase.ImportAsset(assetPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            //apply icon and mark dirty
            if (iconizedModel.GetComponent<Vehicle>())
            {
                iconizedModel.GetComponent<Vehicle>().icon = sprite;
                EditorUtility.SetDirty(iconizedModel);
            }
            if (iconizedModel.GetComponent<ItemObject>())
            {
                iconizedModel.GetComponent<ItemObject>().icon = sprite;
                EditorUtility.SetDirty(iconizedModel);
            }
            //Debug.Log(sprite, iconizedModel);
        }


        Selection.activeObject = assetTexture;
        EditorGUIUtility.PingObject(assetTexture);


        DestroyImmediate(objectCopy);
        Debug.Log("Texture generation complete (" + fileName + "). Click here to view in explorer", assetTexture);
    }
    private void TakeScreenshot(GameObject model, Texture2D texture, string viewName)
    {
        bool isVehicle = model.GetComponentInParent<Vehicle>();
        bool isWeapon = model.GetComponentInParent<GenericGun>();
        float camMultiplier;
        if (isVehicle)
            camMultiplier = 3;
        else if (isWeapon)
        {
            if (model.GetComponentInParent<GenericGun>().weaponPose == WeaponPose.pistol)
                camMultiplier = .11f;
            else
                camMultiplier = .4f;
        }
        else
            camMultiplier = .08f;


        // Set up the camera for taking screenshots
        Camera camera = new GameObject("EditorCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = aperture * camMultiplier;
        camera.farClipPlane = 300;
        camera.nearClipPlane = .2f;
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;

        // Set the camera's position and rotation
        if (isVehicle)
        {
            camera.transform.position = model.transform.position + (Vector3.forward + Vector3.left * .5f + Vector3.up * .3f) * objectSize;
            camera.transform.LookAt(model.transform.position);
            camera.transform.position -= new Vector3(0, -.6f, 0);
            camera.transform.position -= camera.transform.forward * 20;
        }
        else if (isWeapon)
        {
            camera.transform.position = model.transform.position + (Vector3.forward + Vector3.right) * objectSize;
            camera.transform.LookAt(model.transform.position);
            camera.transform.Rotate(Vector3.forward, -40);
            if (model.GetComponentInParent<GenericGun>().weaponPose == WeaponPose.pistol)
                camera.transform.position -= new Vector3(0, 0, -0.05f);
            else
                camera.transform.position -= new Vector3(0, 0, -.2f);
        }
        else
        {
            camera.transform.position = model.transform.position + (Vector3.forward + Vector3.right + Vector3.up) * objectSize;
            camera.transform.LookAt(model.transform.position);
        }
        camera.transform.position -= camera.transform.right * shiftCenter.x +
            camera.transform.up * shiftCenter.y +
            camera.transform.forward * shiftCenter.z;

        //set up render texture
        RenderTexture renderTexture = RenderTexture.GetTemporary(TEX_SIZE, TEX_SIZE, 24);

        // Render the model to the render texture
        camera.targetTexture = renderTexture;
        camera.Render();

        // Read the pixels from the render texture and apply them to the texture
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        if (!allowAlpha)
            RemoveAlphaValue(texture);
        texture.Apply();

        // Save the texture as a PNG file
        //byte[] bytes = texture.EncodeToPNG();
        //string filename = $"{viewName}_UV.png";
        //System.IO.File.WriteAllBytes(filename, bytes);

        RenderTexture.ReleaseTemporary(renderTexture);
        DestroyImmediate(camera.gameObject);
    }

    public static void RemoveAlphaValue(Texture2D _texture, float threeshold = 0)
    {
        // Get the width and height of the texture.
        int width = _texture.width;
        int height = _texture.height;

        // Iterate over all the pixels in the texture.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Get the pixel color.
                Color color = _texture.GetPixel(x, y);

                // If the alpha value is not 0, set it to 1.
                if (color.a > threeshold)
                    color.a = 1;

                // Set the pixel color.
                _texture.SetPixel(x, y, color);
            }
        }
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
        int sub_tex_size = TEX_SIZE / 2;
        Texture2D combinedTexture = new Texture2D(TEX_SIZE, TEX_SIZE);

        // Combine textures
        combinedTexture.SetPixels32(0, 0, sub_tex_size, sub_tex_size, frontTexture.GetPixels32());
        combinedTexture.SetPixels32(sub_tex_size, 0, sub_tex_size, sub_tex_size, backTexture.GetPixels32());
        combinedTexture.SetPixels32(0, sub_tex_size, sub_tex_size, sub_tex_size, rightTexture.GetPixels32());
        combinedTexture.SetPixels32(sub_tex_size, sub_tex_size, sub_tex_size, sub_tex_size, leftTexture.GetPixels32());

        combinedTexture.Apply();
        return combinedTexture;
    }

    public static string GetLocalAssetPath(string globalPath)
    {
        int charStart = globalPath.IndexOf("Assets");
        return globalPath.Substring(charStart);
    }
}
#endif