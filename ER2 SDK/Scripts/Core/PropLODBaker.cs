using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("ER2/LOD/Prop LOD Baker")]
[DisallowMultipleComponent]
public class PropLODBaker : MonoBehaviour
{
#if UNITY_EDITOR
    [Tooltip("One section per distinct LOD material in the children. Use 'Auto Setup' to (re)populate.")]
    public List<LODBakeProfile> profiles = new List<LODBakeProfile>();
#endif
}

#if UNITY_EDITOR
// =====================================================================================
//  PropLODBaker — editor-only authoring tool. Bakes LOD/impostor textures for the
//  FarLodShader / CorvoTreeBillboard materials found in the children, one section
//  per distinct material. Not shipped at runtime.
// =====================================================================================

[System.Serializable]
public class LODBakeProfile
{
    [Tooltip("Target LOD material (FarLodShader or CorvoTreeBillboard). Its _BaseMap is what gets baked / overwritten.")]
    public Material material;

    [Header("Framing")]
    [Tooltip("Output atlas size in pixels.")]
    public int texSize = 256;
    [Tooltip("Camera distance from the prop and far clip basis.")]
    public float objectSize = 20f;
    [Tooltip("Orthographic size (half the captured height).")]
    public float aperture = 7.5f;
    [Tooltip("Pan applied to the camera to center the prop (pivot is rarely the visual center).")]
    public Vector3 shiftCenter = new Vector3(0, 3, 0);

    [Header("Options")]
    public bool allowAlpha = false;
    public bool flipY = false;
    public bool isTree = false;
    public bool includeTopView = false;
    [Tooltip("Only changes the default file name when creating a new texture (_DST suffix).")]
    public bool isDestroyed = false;

    [HideInInspector] public bool foldout = true;
}

// =====================================================================================
//  Custom inspector + baking pipeline
// =====================================================================================

[CustomEditor(typeof(PropLODBaker))]
public class PropLODBakerEditor : Editor
{
    const string FarLodShaderName = "CorvoShaders/FarLodShader";
    const string TreeBillboardShaderName = "CorvoShaders/Trees/CorvoTreeBillboard";
    const string MainTexProperty = "_BaseMap";

    // ---------------------------------------------------------------- inspector

    public override void OnInspectorGUI()
    {
        var baker = (PropLODBaker)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "One section per distinct LOD material in the children. Auto Setup creates them.",
            MessageType.None);

        if (GUILayout.Button("Auto Setup (scan children)"))
        {
            AutoSetup(baker);
            GUIUtility.ExitGUI();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview last LOD")) SetPreviewLOD(baker, true);
            if (GUILayout.Button("Reset LOD")) SetPreviewLOD(baker, false);
        }

        EditorGUILayout.Space();

        var profilesProp = serializedObject.FindProperty("profiles");
        for (int i = 0; i < profilesProp.arraySize; i++)
        {
            if (DrawProfile(baker, profilesProp.GetArrayElementAtIndex(i), i))
                GUIUtility.ExitGUI();   // structural change -> restart the GUI pass cleanly
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("+ Add empty section"))
        {
            Undo.RecordObject(baker, "Add LOD section");
            baker.profiles.Add(new LODBakeProfile());
            EditorUtility.SetDirty(baker);
            GUIUtility.ExitGUI();
        }

        using (new EditorGUI.DisabledScope(baker.profiles.Count == 0))
        {
            if (GUILayout.Button("Override ALL textures"))
            {
                serializedObject.ApplyModifiedProperties();
                foreach (var p in baker.profiles) BakeOverride(baker, p);
                GUIUtility.ExitGUI();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <returns>true if a structural change happened (remove / new material) and the GUI pass must restart.</returns>
    bool DrawProfile(PropLODBaker baker, SerializedProperty prof, int index)
    {
        var foldout = prof.FindPropertyRelative("foldout");
        LODBakeProfile p = (index >= 0 && index < baker.profiles.Count) ? baker.profiles[index] : null;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string matName = (p != null && p.material != null) ? p.material.name : "<no material>";
            string tags = p == null ? "" :
                (p.isTree ? "  [tree]" : "") + (p.includeTopView ? " [+top]" : "") + (p.isDestroyed ? "  [destroyed]" : "");

            using (new EditorGUILayout.HorizontalScope())
            {
                foldout.boolValue = EditorGUILayout.Foldout(foldout.boolValue, matName + tags, true);
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    Undo.RecordObject(baker, "Remove LOD section");
                    baker.profiles.RemoveAt(index);
                    EditorUtility.SetDirty(baker);
                    return true;
                }
            }

            if (!foldout.boolValue) return false;

            EditorGUILayout.PropertyField(prof.FindPropertyRelative("material"));

            EditorGUILayout.PropertyField(prof.FindPropertyRelative("texSize"));
            EditorGUILayout.PropertyField(prof.FindPropertyRelative("objectSize"));
            EditorGUILayout.PropertyField(prof.FindPropertyRelative("aperture"));
            EditorGUILayout.PropertyField(prof.FindPropertyRelative("shiftCenter"));

            EditorGUILayout.PropertyField(prof.FindPropertyRelative("allowAlpha"));
            EditorGUILayout.PropertyField(prof.FindPropertyRelative("flipY"));
            var isTree = prof.FindPropertyRelative("isTree");
            EditorGUILayout.PropertyField(isTree);
            if (isTree.boolValue)
                EditorGUILayout.PropertyField(prof.FindPropertyRelative("includeTopView"));
            EditorGUILayout.PropertyField(prof.FindPropertyRelative("isDestroyed"));

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Override texture"))
                {
                    serializedObject.ApplyModifiedProperties();
                    BakeOverride(baker, p);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("New material + texture"))
                {
                    serializedObject.ApplyModifiedProperties();
                    BakeNew(baker, p);
                    return true; // material changed -> restart
                }
            }
        }
        return false;
    }

    // ---------------------------------------------------------------- auto setup / preview

    static void AutoSetup(PropLODBaker baker)
    {
        // keep settings of sections whose material already exists
        var existingByMat = new Dictionary<Material, LODBakeProfile>();
        foreach (var p in baker.profiles)
            if (p.material != null && !existingByMat.ContainsKey(p.material))
                existingByMat[p.material] = p;

        var seen = new HashSet<Material>();
        var result = new List<LODBakeProfile>();

        foreach (var r in baker.GetComponentsInChildren<MeshRenderer>(true))
        {
            foreach (var m in r.sharedMaterials)
            {
                if (!UsesLodShader(m) || !seen.Add(m)) continue; // one section per distinct material

                if (existingByMat.TryGetValue(m, out var old))
                {
                    result.Add(old);                            // keep saved settings
                }
                else
                {
                    string ln = m.name.ToLowerInvariant();
                    result.Add(new LODBakeProfile
                    {
                        material = m,
                        isTree = IsTreeMaterial(m),
                        isDestroyed = ln.Contains("dst") || ln.Contains("destr") ||
                                      ln.Contains("distrut") || ln.Contains("rubble") || ln.Contains("ruin")
                    });
                }
            }
        }

        Undo.RecordObject(baker, "Auto Setup LOD Baker");
        baker.profiles = result;
        EditorUtility.SetDirty(baker);

        if (result.Count == 0)
            Debug.LogWarning($"PropLODBaker: no child renderer uses '{FarLodShaderName}' or " +
                             $"'{TreeBillboardShaderName}'. Add a section and assign a material manually.", baker);
    }

    static void SetPreviewLOD(PropLODBaker baker, bool lastLevel)
    {
        foreach (var g in baker.GetComponentsInChildren<LODGroup>(true))
            g.ForceLOD(lastLevel ? Mathf.Max(0, g.lodCount - 1) : -1);
        SceneView.RepaintAll();
    }

    // ---------------------------------------------------------------- actions

    static void BakeOverride(PropLODBaker baker, LODBakeProfile p)
    {
        if (p == null || p.texSize < 4) { Debug.LogError("Invalid section / texture size."); return; }
        if (p.material == null) { Debug.LogError("Assign the target LOD material to override."); return; }

        var baked = Bake(baker.gameObject, p);
        if (baked == null) return;
        bool ok = OverwriteMaterialTexture(p.material, baked, out string path);
        Object.DestroyImmediate(baked);
        if (ok) Debug.Log($"LOD texture overwritten: {path}", p.material);
    }

    static void BakeNew(PropLODBaker baker, LODBakeProfile p)
    {
        if (p == null || p.texSize < 4) { Debug.LogError("Invalid section / texture size."); return; }

        string baseName = (p.material != null ? p.material.name : baker.name) +
                          (p.isDestroyed ? "_DST" : "") + "_LOD_TEX";
        string rel = EditorUtility.SaveFilePanelInProject(
            "Save new LOD texture", baseName, "png", "Choose where to save the baked LOD texture");
        if (string.IsNullOrEmpty(rel)) return;

        var baked = Bake(baker.gameObject, p);
        if (baked == null) return;

        var newTex = SaveAsNewTexture(baked, rel, p.allowAlpha);
        Object.DestroyImmediate(baked);
        if (newTex == null) { Debug.LogError("Failed to save new texture."); return; }

        string dir = Path.GetDirectoryName(rel).Replace('\\', '/');
        string name = Path.GetFileNameWithoutExtension(rel);
        string matPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{name}.mat");

        Material oldMat = p.material;
        Material newMat = CreateNewLodMaterial(oldMat, newTex, matPath);
        if (newMat == null) return;

        // re-point every child renderer that was using the old material
        if (oldMat != null)
            ReplaceMaterialOnRenderers(RenderersUsing(baker.gameObject, oldMat), oldMat, newMat);

        Undo.RecordObject(baker, "New LOD Material");
        p.material = newMat;
        EditorUtility.SetDirty(baker);

        Selection.activeObject = newMat;
        EditorGUIUtility.PingObject(newMat);
        Debug.Log($"Created new LOD material + texture:\n- {matPath}\n- {rel}", newMat);
    }

    // ---------------------------------------------------------------- detection

    static bool UsesLodShader(Material m)
    {
        if (m == null || m.shader == null) return false;
        string n = m.shader.name;
        return n == FarLodShaderName || n == TreeBillboardShaderName;
    }

    static bool IsTreeMaterial(Material m)
    {
        return m != null && m.shader != null && m.shader.name == TreeBillboardShaderName;
    }

    static List<Renderer> CollectLodRenderers(GameObject root)
    {
        var list = new List<Renderer>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (UsesLodShader(mats[i])) { list.Add(r); break; }
        }
        return list;
    }

    static List<Renderer> RenderersUsing(GameObject root, Material mat)
    {
        var list = new List<Renderer>();
        if (mat == null) return list;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] == mat) { list.Add(r); break; }
        }
        return list;
    }

    // ---------------------------------------------------------------- baking pipeline

    static Texture2D Bake(GameObject root, LODBakeProfile p)
    {
        Vector3 originalPos = root.transform.position;

        var hidden = new List<Renderer>();
        var disabledLights = new List<Light>();
        UnityEngine.Rendering.Volume volume = null;
        bool volumeWasActive = false;
        Color restoreAmbient = RenderSettings.ambientLight;

        Texture2D finalTex = null;
        try
        {
            root.transform.position = new Vector3(0, 1000, 1000); // isolate from the rest of the scene

            foreach (var r in CollectLodRenderers(root))         // hide every LOD billboard so it isn't captured
                if (r.enabled) { hidden.Add(r); r.enabled = false; }

            foreach (var l in Object.FindObjectsOfType<Light>())
                if (l.gameObject.activeSelf) { disabledLights.Add(l); l.gameObject.SetActive(false); }

            volume = Object.FindObjectOfType<UnityEngine.Rendering.Volume>();
            volumeWasActive = volume != null && volume.gameObject.activeSelf;
            if (volumeWasActive) volume.gameObject.SetActive(false);

            RenderSettings.ambientLight = Color.white;

            SetLODs(root, 0);
            finalTex = CaptureAndCompose(root, p);
        }
        finally
        {
            SetLODs(root, -1);
            foreach (var l in disabledLights) if (l != null) l.gameObject.SetActive(true);
            if (volumeWasActive && volume != null) volume.gameObject.SetActive(true);
            foreach (var r in hidden) if (r != null) r.enabled = true;
            RenderSettings.ambientLight = restoreAmbient;
            root.transform.position = originalPos;
        }
        return finalTex;
    }

    static Texture2D CaptureAndCompose(GameObject root, LODBakeProfile p)
    {
        int F = p.texSize;
        Texture2D finalTex;

        if (p.isTree)
        {
            if (!p.includeTopView)
            {
                Texture2D front = Shot(root, p, Vector3.forward, F / 2, F);
                Texture2D right = Shot(root, p, Vector3.right, F / 2, F);
                finalTex = p.flipY ? ComposeVerticalHalf(front, right, F)
                                   : ComposeVerticalHalf(right, front, F);
                Object.DestroyImmediate(front);
                Object.DestroyImmediate(right);
            }
            else
            {
                int x = (int)((F / 3f) * 2f);
                Texture2D front = Shot(root, p, Vector3.forward, x, F);
                Texture2D right = Shot(root, p, Vector3.right, x, F);
                Texture2D top = Shot(root, p, Vector3.up, x, F);
                finalTex = p.flipY ? ComposeVerticalHalfWithTop(front, right, top, F)
                                   : ComposeVerticalHalfWithTop(right, front, top, F);
                Object.DestroyImmediate(front);
                Object.DestroyImmediate(right);
                Object.DestroyImmediate(top);
            }
        }
        else
        {
            int sub = F / 2;
            Texture2D front = Shot(root, p, Vector3.forward, sub, sub);
            Texture2D back = Shot(root, p, Vector3.back, sub, sub);
            Texture2D right = Shot(root, p, Vector3.right, sub, sub);
            Texture2D left = Shot(root, p, Vector3.left, sub, sub);
            finalTex = p.flipY ? GenerateCompositeTexture(back, front, left, right, F)
                               : GenerateCompositeTexture(front, back, right, left, F);
            Object.DestroyImmediate(front);
            Object.DestroyImmediate(back);
            Object.DestroyImmediate(right);
            Object.DestroyImmediate(left);
        }
        return finalTex;
    }

    static Texture2D Shot(GameObject root, LODBakeProfile p, Vector3 dir, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h);

        Camera cam = new GameObject("EditorCamera_LODBake").AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = p.aperture;
        cam.farClipPlane = p.objectSize * 2f;
        cam.backgroundColor = Color.clear;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = root.transform.position + dir * p.objectSize;
        cam.transform.LookAt(root.transform.position);
        cam.transform.position += p.shiftCenter;

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        if (!p.allowAlpha) RemoveAlphaValue(tex, -1f);
        tex.Apply();
        RenderTexture.active = prev;

        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(cam.gameObject);
        return tex;
    }

    static void SetLODs(GameObject root, int lod)
    {
        foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
            g.ForceLOD(lod);
    }

    /// <summary>Forces alpha to 1 wherever alpha > threshold. With threshold = -1 the whole texture becomes opaque.</summary>
    static void RemoveAlphaValue(Texture2D _texture, float threeshold = 0)
    {
        int width = _texture.width;
        int height = _texture.height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = _texture.GetPixel(x, y);
                if (color.a > threeshold)
                    color.a = 1;
                _texture.SetPixel(x, y, color);
            }
        }
    }

    // ---------------------------------------------------------------- compose (layouts preserved 1:1)

    static Texture2D GenerateCompositeTexture(Texture2D front, Texture2D back, Texture2D right, Texture2D left, int F)
    {
        int sub = F / 2;
        Texture2D t = new Texture2D(F, F);
        t.SetPixels32(0, 0, sub, sub, front.GetPixels32());
        t.SetPixels32(sub, 0, sub, sub, back.GetPixels32());
        t.SetPixels32(0, sub, sub, sub, right.GetPixels32());
        t.SetPixels32(sub, sub, sub, sub, left.GetPixels32());
        t.Apply();
        return t;
    }

    static Texture2D ComposeVerticalHalf(Texture2D leftFull, Texture2D rightFull, int F)
    {
        Texture2D c = new Texture2D(F, F, leftFull.format, false);
        c.SetPixels32(0, 0, leftFull.width, leftFull.height, leftFull.GetPixels32());
        c.SetPixels32(leftFull.width, 0, rightFull.width, rightFull.height, rightFull.GetPixels32());
        c.Apply();
        return c;
    }

    static Texture2D ComposeVerticalHalfWithTop(Texture2D leftFull, Texture2D rightFull, Texture2D topView, int F)
    {
        Texture2D c = new Texture2D(F * 2, F, leftFull.format, false);
        c.SetPixels32(0, 0, leftFull.width, leftFull.height, leftFull.GetPixels32());
        c.SetPixels32(leftFull.width, 0, rightFull.width, rightFull.height, rightFull.GetPixels32());
        c.SetPixels32(leftFull.width + rightFull.width, 0, topView.width, topView.height, topView.GetPixels32());
        c.Apply();
        return c;
    }

    // ---------------------------------------------------------------- asset I/O

    /// <summary>Overwrites in place the PNG asset bound to the material's _BaseMap, keeping its GUID and import settings.</summary>
    static bool OverwriteMaterialTexture(Material mat, Texture2D baked, out string assetPath)
    {
        assetPath = null;
        Texture current = mat.GetTexture(MainTexProperty);
        if (current == null)
        {
            Debug.LogError($"Material '{mat.name}' has no {MainTexProperty} texture to overwrite. Use 'New material + texture' instead.");
            return false;
        }
        string rel = AssetDatabase.GetAssetPath(current);
        if (string.IsNullOrEmpty(rel))
        {
            Debug.LogError("Current _BaseMap is not a saved asset; cannot overwrite. Use 'New material + texture' instead.");
            return false;
        }

        File.WriteAllBytes(ToAbsolute(rel), baked.EncodeToPNG());
        AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceUpdate); // re-ingest pixels, keep existing import settings
        assetPath = rel;
        return true;
    }

    static Texture2D SaveAsNewTexture(Texture2D baked, string projectRelativePath, bool allowAlpha)
    {
        string abs = ToAbsolute(projectRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllBytes(abs, baked.EncodeToPNG());
        AssetDatabase.ImportAsset(projectRelativePath, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(projectRelativePath) as TextureImporter;
        if (ti != null)
        {
            ti.alphaSource = allowAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = allowAlpha;
            ti.wrapMode = TextureWrapMode.Clamp; // avoid bleeding off the atlas edges
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(projectRelativePath);
    }

    /// <summary>New material asset: copies the template if it already uses a LOD shader, otherwise uses FarLodShader.</summary>
    static Material CreateNewLodMaterial(Material template, Texture2D newTex, string materialAssetPath)
    {
        Material mat = (template != null && UsesLodShader(template))
            ? new Material(template)
            : new Material(Shader.Find(FarLodShaderName));

        if (mat.shader == null) { Debug.LogError($"Shader '{FarLodShaderName}' not found."); return null; }

        mat.SetTexture(MainTexProperty, newTex);
        AssetDatabase.CreateAsset(mat, materialAssetPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void ReplaceMaterialOnRenderers(IEnumerable<Renderer> renderers, Material oldMat, Material newMat)
    {
        if (newMat == null) return;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] == oldMat) { mats[i] = newMat; changed = true; }
            if (changed)
            {
                Undo.RecordObject(r, "Assign LOD Material");
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
            }
        }
    }

    static string ToAbsolute(string projectRelativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, projectRelativePath);
    }
}
#endif