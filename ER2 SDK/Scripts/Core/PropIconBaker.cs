using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#endif

// =====================================================================================
//  PropIconBaker — editor-only authoring tool. Bakes the inventory/HUD icon for the
//  Vehicle / ItemObject sitting on the same GameObject. Replaces the old "Icon
//  Generator" EditorWindow: framing now lives on the prefab and updating is one click.
//  Not shipped at runtime (all fields are editor-only).
// =====================================================================================

[AddComponentMenu("ER2/Texture/Prop Icon Baker")]
[DisallowMultipleComponent]
public class PropIconBaker : MonoBehaviour
{
#if UNITY_EDITOR
    [Tooltip("Output icon resolution in pixels (square).")]
    public int texSize = 256;

    [Tooltip("Orthographic framing. Smaller = more zoomed in. (This was 'Shot Size' in the old Icon Generator window.)")]
    public float shotSize = 1f;

    [Tooltip("Brightness of the temporary capture light rig. 1 = default.")]
    public float exposure = 1f;

    [Tooltip("Pans the camera to recenter the subject (the pivot is rarely the visual center). X = right, Y = up, Z = depth.")]
    public Vector3 shiftCenter = Vector3.zero;

    [Tooltip("Keep semi-transparent pixels as they are. Off = every visible pixel becomes fully opaque. The background stays transparent either way.")]
    public bool allowAlpha = true;

    [Tooltip("Distance of the capture camera from the prop. Orthographic, so this only affects clip range / depth sorting and rarely needs changing.")]
    public float objectSize = 50f;
#endif
}

#if UNITY_EDITOR
// =====================================================================================
//  Custom inspector + capture pipeline
// =====================================================================================

[CustomEditor(typeof(PropIconBaker))]
public class PropIconBakerEditor : Editor
{
    // Remembered between "Save as new icon" calls within the session (fallback only).
    static string s_lastDir;

    // ---------------------------------------------------------------- inspector

    public override void OnInspectorGUI()
    {
        var baker = (PropIconBaker)target;
        serializedObject.Update();

        Vehicle veh = baker.GetComponent<Vehicle>();
        ItemObject item = baker.GetComponent<ItemObject>();
        bool hasTarget = veh != null || item != null;
        Sprite current = veh != null ? veh.icon : (item != null ? item.icon : null);

        if (!hasTarget)
        {
            EditorGUILayout.HelpBox(
                "Put this component on the same GameObject as a Vehicle or an ItemObject.\n" +
                "This object has neither, so there is no 'icon' field to write to.",
                MessageType.Warning);
        }
        else
        {
            string kind = veh != null ? "Vehicle" : "ItemObject";
            EditorGUILayout.HelpBox(
                $"Target: {kind}\nCurrent icon: {(current != null ? current.name : "<none>")}",
                MessageType.None);
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("texSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shotSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("exposure"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shiftCenter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allowAlpha"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("objectSize"));

        EditorGUILayout.Space(6);

        // The prop is shot through a temporary copy, and a copy made in Play mode would start running its game logic.
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        if (playing)
            EditorGUILayout.HelpBox("Exit Play mode to bake icons.", MessageType.Info);

        using (new EditorGUI.DisabledScope(!hasTarget || playing))
        {
            // One-click: re-bake straight over the currently assigned icon, in place.
            using (new EditorGUI.DisabledScope(current == null))
            {
                string label = current != null
                    ? $"Update icon  ({current.name})"
                    : "Update icon  (assign one first)";
                if (GUILayout.Button(label, GUILayout.Height(24)))
                {
                    serializedObject.ApplyModifiedProperties();
                    UpdateExistingIcon(current);
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("Save as new icon…", GUILayout.Height(24)))
            {
                serializedObject.ApplyModifiedProperties();
                SaveAsNewIcon(baker, veh, item, current);
                GUIUtility.ExitGUI();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------------- actions

    /// <summary>Bakes over the PNG asset bound to the current icon, keeping its GUID, import
    /// settings and every reference to it. Nothing on the component changes.</summary>
    void UpdateExistingIcon(Sprite current)
    {
        var baker = (PropIconBaker)target;

        string rel = AssetDatabase.GetAssetPath(current);
        if (string.IsNullOrEmpty(rel))
        {
            Debug.LogError("Current icon is not a saved asset; use 'Save as new icon' instead.", current);
            return;
        }

        var baked = Capture(baker);
        if (baked == null) return;

        File.WriteAllBytes(ToAbsolute(rel), baked.EncodeToPNG());
        Object.DestroyImmediate(baked);
        AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceUpdate); // re-ingest pixels, keep Sprite import settings

        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"Icon updated in place: {rel}", asset);
    }

    /// <summary>Saves a brand new PNG, imports it as a Sprite and assigns it to the
    /// Vehicle/ItemObject. The save dialog opens on the current icon's folder when there is one.</summary>
    void SaveAsNewIcon(PropIconBaker baker, Vehicle veh, ItemObject item, Sprite current)
    {
        string defaultDir = DefaultDir(current);
        string defaultName = baker.gameObject.name + "_tex_ICON.png";

        string abs = EditorUtility.SaveFilePanel("Save icon as PNG", defaultDir, defaultName, "png");
        if (string.IsNullOrEmpty(abs)) { Debug.Log("Canceled."); return; }

        string rel = ToProjectRelative(abs);
        if (rel == null)
        {
            Debug.LogError("Save the icon inside this project's Assets folder.");
            return;
        }
        s_lastDir = Path.GetDirectoryName(abs);

        var baked = Capture(baker);
        if (baked == null) return;

        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllBytes(abs, baked.EncodeToPNG());
        Object.DestroyImmediate(baked);
        AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(rel) as TextureImporter;
        if (ti != null)
        {
            // Same import as the existing icons. Alpha is always kept: Allow Alpha only decides what happens to
            // semi-transparent pixels and the background is transparent either way, so importing without alpha
            // would turn it into a black square. Setting the type from code does not switch mipmaps off like the
            // inspector does, hence the explicit line.
            ti.textureType = TextureImporterType.Sprite;
            ti.mipmapEnabled = false;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(rel);
        if (sprite == null) { Debug.LogError("Saved the PNG but could not load it back as a Sprite.", baker); return; }

        if (veh != null)
        {
            Undo.RecordObject(veh, "Assign Icon");
            veh.icon = sprite;
            EditorUtility.SetDirty(veh);
        }
        else if (item != null)
        {
            Undo.RecordObject(item, "Assign Icon");
            item.icon = sprite;
            EditorUtility.SetDirty(item);
        }

        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"New icon saved and assigned: {rel}", asset);
    }

    // ---------------------------------------------------------------- capture pipeline

    /// <summary>Shoots a temporary copy of the prop in a private preview scene, lit only by a neutral rig,
    /// at LOD0, with a single orthographic front frame. The original is never moved or modified, which is
    /// what makes this work the same on a scene object, in Prefab Mode and on a prefab asset selected in the
    /// Project window: Prefab Mode keeps the prefab in a preview scene of its own, and a camera living in the
    /// open scene cannot see into it.</summary>
    static Texture2D Capture(PropIconBaker baker)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        var disabledVolumes = new List<Volume>();
        bool fog = RenderSettings.fog;
        GameObject subject = null;
        GameObject rig = null;

        try
        {
            // Lights of the open scene cannot reach a preview scene, but volumes and fog are global. Done before
            // the copy exists, so a volume inside the prop can never switch the prop itself off.
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.gameObject.activeSelf) { disabledVolumes.Add(v); v.gameObject.SetActive(false); }
            RenderSettings.fog = false;

            // Canonical pose (far away, facing front), whatever the original's parent or position.
            subject = Object.Instantiate(baker.gameObject);
            SceneManager.MoveGameObjectToScene(subject, scene);
            subject.transform.SetPositionAndRotation(new Vector3(0, 1000, 1000), Quaternion.identity);
            subject.SetActive(true);
            foreach (var l in subject.GetComponentsInChildren<Light>(true))
                l.enabled = false; // headlights and lamps of the prop must not light their own icon
            SetLODs(subject, 0);

            rig = CreateLightRig(baker.exposure);
            SceneManager.MoveGameObjectToScene(rig, scene);

            return Shoot(baker, subject, scene);
        }
        finally
        {
            if (rig != null) Object.DestroyImmediate(rig);
            if (subject != null) Object.DestroyImmediate(subject);
            EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.fog = fog;
            foreach (var v in disabledVolumes) if (v != null) v.gameObject.SetActive(true);
        }
    }

    static Texture2D Shoot(PropIconBaker baker, GameObject subject, Scene scene)
    {
        int size = Mathf.Clamp(baker.texSize, 4, SystemInfo.maxTextureSize);

        // Framing presets, identical to the old window. Detection is on the baker's own GameObject (the
        // baker sits on the prop root). If a weapon keeps its GenericGun on a child, switch this to
        // GetComponentInChildren<GenericGun>(true).
        bool isVehicle = baker.GetComponent<Vehicle>() != null;
        GenericGun gun = baker.GetComponent<GenericGun>();
        bool isWeapon = !isVehicle && gun != null;
        bool isPistol = isWeapon && gun.weaponPose == WeaponPose.pistol;

        float camMultiplier;
        if (isVehicle) camMultiplier = 3f;
        else if (isWeapon) camMultiplier = isPistol ? .11f : .4f;
        else camMultiplier = .08f;

        Camera camera = new GameObject("EditorCamera_IconBake").AddComponent<Camera>();
        camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        camera.scene = scene; // renders only the copy and the rig
        // Every URP asset of the project has Opaque Texture on, which makes URP draw an offscreen camera into
        // an intermediate buffer; with HDR (the High and Medium assets) that buffer is B10G11R11 and has no
        // alpha, so the background would come out opaque black depending on the quality level open in the
        // editor. Nothing here needs HDR: no post processing runs on this camera and the PNG is 8 bit.
        camera.allowHDR = false;
        camera.orthographic = true;
        camera.orthographicSize = baker.shotSize * camMultiplier;
        camera.farClipPlane = 300f;
        camera.nearClipPlane = .2f;
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;

        Vector3 center = subject.transform.position;
        float dist = baker.objectSize;

        if (isVehicle)
        {
            camera.transform.position = center + (Vector3.forward + Vector3.left * .5f + Vector3.up * .3f) * dist;
            camera.transform.LookAt(center);
            camera.transform.position -= new Vector3(0, -.6f, 0);
            camera.transform.position -= camera.transform.forward * 20f;
        }
        else if (isWeapon)
        {
            camera.transform.position = center + (Vector3.forward + Vector3.right) * dist;
            camera.transform.LookAt(center);
            camera.transform.Rotate(Vector3.forward, -40f);
            camera.transform.position -= isPistol ? new Vector3(0, 0, -0.05f) : new Vector3(0, 0, -.2f);
        }
        else
        {
            camera.transform.position = center + (Vector3.forward + Vector3.right + Vector3.up) * dist;
            camera.transform.LookAt(center);
        }

        camera.transform.position -= camera.transform.right * baker.shiftCenter.x
                                   + camera.transform.up * baker.shiftCenter.y
                                   + camera.transform.forward * baker.shiftCenter.z;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        RenderTexture rt = RenderTexture.GetTemporary(size, size, 24);
        RenderTexture prev = RenderTexture.active;
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        }
        finally
        {
            // Even on an exception: the editor must not be left drawing into this texture.
            RenderTexture.active = prev;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(camera.gameObject);
        }

        // Never hand back an empty frame: "Update icon" would write it over a good icon.
        Color32[] px = tex.GetPixels32();
        if (!HasVisiblePixels(px))
        {
            Object.DestroyImmediate(tex);
            Debug.LogError("Nothing ended up in the frame, so no icon was written. Check Shot Size and Shift Center, and that the prop has visible renderers.", baker);
            return null;
        }
        if (!baker.allowAlpha)
        {
            ForceOpaque(px);
            tex.SetPixels32(px);
        }
        tex.Apply();
        return tex;
    }

    // ---------------------------------------------------------------- lighting / LOD / pixels

    static GameObject CreateLightRig(float exposure)
    {
        GameObject root = new GameObject("ICON_BAKE_LIGHTS") { hideFlags = HideFlags.HideAndDontSave };
        AddLight(root, new Vector3(0, 0, 0), exposure, 1f);
        AddLight(root, new Vector3(0, 90, 0), exposure, 1f);
        AddLight(root, new Vector3(0, 180, 0), exposure, 1f);
        AddLight(root, new Vector3(0, 270, 0), exposure, 1f);
        AddLight(root, new Vector3(90, 0, 0), exposure, .5f);
        return root;
    }

    static void AddLight(GameObject parent, Vector3 euler, float exposure, float mult)
    {
        var go = new GameObject("LIGHT") { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(parent.transform);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = exposure * mult * .75f;
        go.transform.eulerAngles = euler;
    }

    static void SetLODs(GameObject root, int lod)
    {
        foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
            g.ForceLOD(lod);
    }

    /// <summary>True if at least one pixel is not fully transparent.</summary>
    static bool HasVisiblePixels(Color32[] px)
    {
        for (int i = 0; i < px.Length; i++)
            if (px[i].a > 0) return true;
        return false;
    }

    /// <summary>Forces every visible pixel (alpha > 0) to fully opaque, leaving cut-out alpha = 0 as is.</summary>
    static void ForceOpaque(Color32[] px)
    {
        for (int i = 0; i < px.Length; i++)
            if (px[i].a > 0) px[i].a = 255;
    }

    // ---------------------------------------------------------------- paths

    static string DefaultDir(Sprite current)
    {
        if (current != null)
        {
            string rel = AssetDatabase.GetAssetPath(current);
            if (!string.IsNullOrEmpty(rel))
            {
                string absDir = Path.GetDirectoryName(ToAbsolute(rel));
                if (Directory.Exists(absDir)) return absDir;
            }
        }
        if (!string.IsNullOrEmpty(s_lastDir) && Directory.Exists(s_lastDir)) return s_lastDir;
        return Application.dataPath;
    }

    static string ToAbsolute(string projectRelative)
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(root, projectRelative);
    }

    /// <summary>Absolute path -> "Assets/…" project-relative path, or null if outside the project.</summary>
    static string ToProjectRelative(string absolute)
    {
        absolute = absolute.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/'); // …/Project/Assets
        if (!absolute.StartsWith(dataPath)) return null;
        return "Assets" + absolute.Substring(dataPath.Length);
    }
}
#endif
