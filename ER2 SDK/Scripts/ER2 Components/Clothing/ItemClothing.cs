using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

public partial class ItemClothing : ItemObject, ISerializationCallbackReceiver
{
    [Header("⏺ Uniform Stats & type")]
    public WearableType type;
    [Tooltip("[Gears only] Allow to perform radio calls")]
    public bool isRadio = false;
    [Tooltip("[Gears only] Allow to fire a flamethrower")]
    public bool isFuelTank = false;

    [Tooltip("Damage reduction (penetration / fire / sliver). Damage is reduced by a random amount between 0 and the value.")]
    public ProtectionData protectionData;
    [Tooltip("Camouflage: 0 = none, 35 = max. Reduces detection range.")]
    [Range(0, 35)] public int camoIndex = 15;
    [Tooltip("Extra inventory slots granted to the wearer.")]
    [Range(0, 30)] public int extraInventorySpace = 0;

    [Header("⏺ Uniform Cover Modes  (uniforms only)")]
    [Tooltip("How sleeves/hands render.\nIgnored if a Short-Sleeve mesh is assigned below — that takes over.")]
    public UniformSleeveCoverType forceHideHands = UniformSleeveCoverType.longSleeve;
    [Tooltip("How head and headgear render.")]
    public UniformHeadCoverType forceHideHead = UniformHeadCoverType.dontCoverHead;
    [Tooltip("Rank decal offset on the uniform. (0,0,0) = hidden.")]
    public Vector3 ranks_coords = new Vector3(.5f, 1.21f, .75f);
    [Tooltip("Battalion patch offset on the uniform. (0,0,0) = hidden.")]
    public Vector3 patch_coords = new Vector3(.5f, 0.2f, .8f);

    [Header("⏺ TPS Mesh — REQUIRED")]
    [Tooltip("Third-person high-poly mesh. Required.")]
    public Mesh mesh;
    [Tooltip("Materials for the HP mesh. Required.")]
    public Material[] materials = new Material[0];

    [Space(4)]
    [Tooltip("Optional low-poly mesh used at distance.")]
    public Mesh mesh_lod;
    [Tooltip("Optional. If empty, falls back to the HP 'materials' above.")]
    public Material[] materials_lod = new Material[0];

    [Header("⏺ [Uniforms only] Short-Sleeve Variant (OPTIONAL)")]
    [Tooltip("Alternate HP mesh auto-selected when mission temperature exceeds the threshold below " +
             "(or when mission is forced to Summer).\nLeave EMPTY to disable the variant.")]
    public Mesh mesh_short_sleeve;
    [Tooltip("Optional. If empty, falls back to the main HP 'materials'.")]
    public Material[] materials_short = new Material[0];

    [Space(4)]
    [Tooltip("Optional LOD for the short-sleeve mesh.")]
    public Mesh mesh_short_sleeve_lod;
    [Tooltip("Optional. Fallback chain if empty:  materials_lod → materials_short → materials.")]
    public Material[] materials_short_lod = new Material[0];

    [Space(4)]
    [Tooltip("How hands/sleeves render when the short-sleeve mesh is active.")]
    public UniformSleeveCoverType handsModeWithShortSleeveUniform = UniformSleeveCoverType.shortSleeve;
    [Tooltip("Above this mission temperature (°C) the short-sleeve mesh is auto-selected in 'Auto' uniform mode.")]
    [Range(0, 50)] public float shortSleeveThreshold = 23;

    [Header("⏺ [Uniforms only] FPS Mesh")]
    [Tooltip("First-person mesh. Only needed when 'type' is Gear (uniforms don't use an FPS mesh).")]
    public Mesh fps_mesh;
    [Tooltip("Optional. If empty, falls back to TPS 'materials'.")]
    public Material[] materials_fps = new Material[0];

    [Space(4)]
    [Tooltip("FPS mesh for the short-sleeve variant. Rarely used — only relevant for gear that swaps with sleeve mode.")]
    public Mesh fps_mesh_short_sleeve;
    [Tooltip("Optional. Fallback chain if empty:  materials_fps → materials_short → materials.")]
    public Material[] materials_short_fps = new Material[0];


    partial void CheckShouldUseShortSleeve(ref bool result);

    public bool ShouldUseShortSleeve(SleeveMode sleeveMode = SleeveMode.auto)
    {
        if (sleeveMode == SleeveMode.rolled) return true;
        if (sleeveMode == SleeveMode.unrolled) return false;
        bool result = false;
        CheckShouldUseShortSleeve(ref result);
        return result;
    }

    public Material[] GetTpsMaterials(SleeveMode sleeveMode = SleeveMode.auto)
    {
        if (mesh_short_sleeve && ShouldUseShortSleeve(sleeveMode) && materials_short.Length > 0)
            return materials_short;
        else if (materials.Length > 0)
            return materials;
        else
            return new Material[] {  };
    }
    public Material[] GetTpsMaterialsLOD(SleeveMode sleeveMode = SleeveMode.auto)
    {
        if (mesh_short_sleeve && ShouldUseShortSleeve(sleeveMode) && materials_short_lod.Length > 0)
            return materials_short_lod;
        else if (materials_lod.Length > 0)
            return materials_lod;
        return GetTpsMaterials(sleeveMode);
    }
    public Material[] GetFpsMaterials(SleeveMode sleeveMode = SleeveMode.auto)
    {
        if (mesh_short_sleeve && ShouldUseShortSleeve(sleeveMode) && materials_short_fps.Length > 0)
            return materials_short_fps;
        else if (materials_fps.Length > 0)
            return materials_fps;
        return GetTpsMaterials(sleeveMode);
    }


#if UNITY_EDITOR
    public enum ValidateMode { None, LogsOnly, Low, Full }
    public static bool ValidateSetup(ItemClothing clothing, ValidateMode validationMode, string itemIdForLog = null)
    {
        if (clothing == null) return true;

        bool ok = true;
        string tag = string.IsNullOrEmpty(itemIdForLog) ? clothing.name : itemIdForLog;

        ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh, validationMode, clothing.GetTpsMaterials(SleeveMode.unrolled), "mesh/HP TPS");
        ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh_lod, validationMode, clothing.GetTpsMaterialsLOD(SleeveMode.unrolled), "mesh_lod TPS");

        if (clothing.mesh_short_sleeve != null)
            ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh_short_sleeve, validationMode, clothing.GetTpsMaterials(SleeveMode.rolled), "mesh_short_sleeve TPS");

        if (clothing.mesh_short_sleeve_lod != null)
            ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh_short_sleeve_lod, validationMode, clothing.GetTpsMaterialsLOD(SleeveMode.rolled), "mesh_short_sleeve_lod TPS");

        if (clothing.fps_mesh != null)
            ok &= ValidateMeshAndMaterials(tag, clothing, clothing.fps_mesh, validationMode, clothing.GetFpsMaterials(SleeveMode.unrolled), "fps_mesh");

        if (clothing.fps_mesh_short_sleeve != null)
            ok &= ValidateMeshAndMaterials(tag, clothing, clothing.fps_mesh_short_sleeve, validationMode, clothing.GetFpsMaterials(SleeveMode.rolled), "fps_mesh_short_sleeve");

        return ok;
    }

    private static bool ValidateMeshAndMaterials(string tag, ItemClothing clothing, Mesh mesh, ValidateMode validationMode, Material[] chosenMaterials, string label)
    {
        if (mesh == null)
        {
            if (label.StartsWith("mesh/HP"))
            {
                Debug.LogError($"[ItemClothing Validate] '{tag}' missing {label} mesh on prefab '{clothing.gameObject.name}'.", clothing);
                return false;
            }
            return true;
        }

        int expectedSlots = GetImporterMaterialSlotCount(mesh);
        if (expectedSlots <= 0)
        {
            Debug.LogWarning($"[ItemClothing Validate] '{tag}' cannot determine importer material slots for {label} (mesh='{mesh.name}').", clothing);
            return true;
        }

        int actual = chosenMaterials != null ? chosenMaterials.Length : 0;

        if (actual != expectedSlots)
        {
            bool lowValidationMode = validationMode == ValidateMode.Low && actual > expectedSlots;
            if (!lowValidationMode)
            {
                Debug.LogError(
                    $"<b><color=#ff5555>MATERIAL COUNT MISMATCH</color></b> : {tag} (<b>{mesh.name}</b>)\n" +
                    $"<b><color=#ffaa00>Expected</color></b> : {expectedSlots} slot(s) - " +
                    $"<b><color=#ffaa00>Resolved</color></b> : {actual} material(s) after fallback\n\n" +

                    $"<b>Prefab</b>    : {clothing.gameObject.name}\n" +
                    $"<b>Section</b>   : {label}\n" +

                    "<b>Material sources</b>\n" +
                    $"• materials           : {Len(clothing.materials)}\n" +
                    $"• materials_lod       : {Len(clothing.materials_lod)}\n" +
                    $"• materials_short     : {Len(clothing.materials_short)}\n" +
                    $"• materials_short_lod : {Len(clothing.materials_short_lod)}",
                    clothing
                );
            }

            if (validationMode == ValidateMode.Full) return false;
            if (lowValidationMode) return false;
        }

        for (int i = 0; i < actual; i++)
        {
            if (chosenMaterials[i] == null)
            {
                Debug.LogError(
                    $"[ItemClothing Validate] '{tag}' {label} has NULL material at index {i}/{actual - 1} (mesh='{mesh.name}').",
                    clothing
                );
                return false;
            }
        }

        return true;
    }

    private static int Len(Material[] a) => a == null ? 0 : a.Length;

    private static int GetImporterMaterialSlotCount(Mesh mesh)
    {
        var renderer = FindSourceRendererForMesh(mesh);
        if (renderer == null || renderer.sharedMaterials == null)
            return -1;
        return renderer.sharedMaterials.Length;
    }

    private static Renderer FindSourceRendererForMesh(Mesh mesh)
    {
        if (mesh == null) return null;
        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path)) return null;

        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in all)
                if (obj is GameObject go) { root = go; break; }
        }

        if (root == null) return null;

        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr != null && smr.sharedMesh == mesh) return smr;

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf != null && mf.sharedMesh == mesh)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null) return mr;
            }

        return null;
    }
#endif

    // deserialization fix
    [SerializeField, HideInInspector] private Material material;
    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {
        MigrateLegacyMaterial();
    }
    private void MigrateLegacyMaterial()
    {
        // Use ReferenceEquals to avoid Unity's overloaded == operator,
        // which is not thread-safe and can't be called during deserialization.
        if (ReferenceEquals(material, null)) return;

        if (materials == null || materials.Length == 0)
            materials = new Material[] { material };
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        int before = materials == null ? 0 : materials.Length;

        MigrateLegacyMaterial();

        if ((materials == null ? 0 : materials.Length) != before)
            UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public partial struct ProtectionData
{
    [Tooltip("Defines the maximum value of protection from penetration damage")]
    [Range(0, 90)]
    public byte penetrationProtection;
    [Tooltip("Defines the maximum value of protection from fire damage")]
    [Range(0, 90)]
    public byte fireProtection;
    [Tooltip("Defines the maximum value of protection from sliver damage")]
    [Range(0, 90)]
    public byte sliverProtection;
}

public enum SleeveMode { auto, rolled, unrolled }
public enum WearableType { uniform, gear }
public enum UniformSleeveCoverType
{
    shortSleeve = 0,
    longSleeve = 1,
    coversSleeveAndHand = 2,
    shortSleeveAndPants = 3,
    longSleeveShortPants = 4
}
public enum UniformHeadCoverType
{
    dontCoverHead = 0,
    coversHead = 1,
    coversHeadAndHeadgear = 2
}


#if UNITY_EDITOR

[CustomEditor(typeof(ItemClothing))]
[CanEditMultipleObjects]
public class ItemClothingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ItemClothing clothing = (ItemClothing)target;

        // === Linked banner ===
        if (TPSRigTesterManager.IsUniformLinked(clothing))
            TPSRigTesterManager.DrawLinkedBanner($"● LIVE-LINKED TO TPS TESTER ({TPSRigTesterManager.GetUniformSleeveMode().ToString().ToUpper()} SLEEVE)");
        else if (TPSRigTesterManager.IsGearLinked(clothing))
            TPSRigTesterManager.DrawLinkedBanner($"● LIVE-LINKED TO TPS TESTER (GEAR / {TPSRigTesterManager.GetGearSlotName(clothing)})");

        if (GUILayout.Button("VALIDATE"))
        {
            foreach (var obj in Selection.gameObjects)
            {
                ItemClothing clt = obj.GetComponent<ItemClothing>();
                if (clt == null) continue;

                if (ItemClothing.ValidateSetup(clothing, ItemClothing.ValidateMode.Full, clt.item_id))
                    Debug.Log($"<b>{clt.item_id}</b> seems setted up correctly.");
            }
        }

        if (Selection.objects.Length == 1)
        {
            string status = GetSyncStatus(clothing);
            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.HelpBox(status, MessageType.Warning);
        }

        // === TPS Rig Tester buttons ===
        GUILayout.Space(8);
        GUILayout.Label(" --- TPS Rig Tester ---");

        if (clothing.type == WearableType.uniform)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test (Long Sleeve)"))
                TPSRigTesterManager.LinkUniform(clothing, SleeveMode.unrolled);
            GUI.enabled = clothing.mesh_short_sleeve != null;
            if (GUILayout.Button("Test (Short Sleeve)"))
                TPSRigTesterManager.LinkUniform(clothing, SleeveMode.rolled);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Test in Rig"))
                TPSRigTesterManager.LinkGear(clothing);
        }

        bool isLinked = TPSRigTesterManager.IsUniformLinked(clothing) || TPSRigTesterManager.IsGearLinked(clothing);
        EditorGUILayout.BeginHorizontal();
        if (isLinked)
        {
            /*if (GUILayout.Button("Detach from Tester"))
            {
                if (TPSRigTesterManager.IsUniformLinked(clothing))
                    TPSRigTesterManager.UnlinkUniform();
                else
                    TPSRigTesterManager.UnlinkGear(clothing);
            }*/
        }
        if (TPSRigTesterManager.FindTester() != null)
        {
            if (GUILayout.Button("Disable Tester"))
                TPSRigTesterManager.DisableTester();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        DrawDefaultInspector();
    }

    private bool CanSyncMaterials()
    {
        foreach (Object obj in targets)
        {
            ItemClothing item = obj as ItemClothing;
            if (item != null && item.mesh != null && item.mesh_lod != null &&
                item.materials != null && item.materials.Length > 0)
                return true;
        }
        return false;
    }

    private string GetSyncStatus(ItemClothing clothing)
    {
        if (clothing.mesh == null) return "High Poly mesh is missing";
        if (clothing.mesh_lod == null) return "LOD mesh is missing";
        if (clothing.materials == null || clothing.materials.Length == 0) return "High Poly materials are missing";
        return null;
    }


    private static void SyncMaterialsToLOD(ItemClothing clothing, ItemClothingEditor ice)
    {
        if (clothing.mesh == null) { Debug.LogWarning($"[{clothing.name}] No HP mesh assigned, can't sync."); return; }

        string path = AssetDatabase.GetAssetPath(clothing.mesh);
        if (string.IsNullOrEmpty(path)) { Debug.LogWarning($"[{clothing.name}] Could not find FBX path for HP mesh."); return; }

        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { Debug.LogWarning($"[{clothing.name}] Asset at {path} is not a ModelImporter."); return; }

        WithTemporaryEmbeddedMaterials(importer, path, () =>
        {
            int successCount = 0;
            int failCount = 0;
            List<string> errors = new List<string>();

            foreach (Object obj in ice.targets)
            {
                ItemClothing item = obj as ItemClothing;
                if (item == null) continue;

                if (item.mesh == null || item.mesh_lod == null ||
                    item.materials == null || item.materials.Length == 0)
                {
                    failCount++;
                    errors.Add($"{item.name}: Missing required meshes or materials");
                    continue;
                }

                if (ice.SyncMaterialsForItem(item)) successCount++;
                else { failCount++; errors.Add($"{item.name}: Failed to sync materials"); }
            }

            string message = $"Material Sync Complete\nSuccess: {successCount}, Failed: {failCount}";
            if (errors.Count > 0)
            {
                message += "\n\nErrors:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5) message += $"\n... and {errors.Count - 5} more";
            }
            Debug.Log(message, ice);

            FixLodMaterials(clothing);
        });
    }

    private static void WithTemporaryEmbeddedMaterials(ModelImporter importer, string assetPath, System.Action body)
    {
        var originalMode = importer.materialImportMode;
        var originalLocation = importer.materialLocation;
        bool changed = false;

        if (originalMode == ModelImporterMaterialImportMode.None)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            changed = true;
        }

        try { body?.Invoke(); }
        finally
        {
            if (changed)
            {
                importer.materialImportMode = originalMode;
                importer.materialLocation = originalLocation;
                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    private static void FixLodMaterials(ItemClothing clothing) { }

    private bool SyncMaterialsForItem(ItemClothing item)
    {
        try
        {
            if (item.mesh == null || item.mesh_lod == null ||
                item.materials == null || item.materials.Length == 0)
            { Debug.LogWarning($"[{item.name}] Missing mesh or HP materials, skipping."); return false; }

            Material[] hpImporterMats = GetImporterMaterialsForMesh(item.mesh);
            Material[] lodImporterMats = GetImporterMaterialsForMesh(item.mesh_lod);

            if (hpImporterMats == null || hpImporterMats.Length == 0 ||
                lodImporterMats == null || lodImporterMats.Length == 0)
            { Debug.LogWarning($"[{item.name}] Could not find importer materials for HP or LOD mesh."); return false; }

            var hpSlotMap = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
            int count = Mathf.Min(hpImporterMats.Length, item.materials.Length);
            for (int i = 0; i < count; i++)
            {
                var src = hpImporterMats[i];
                var dst = item.materials[i];
                if (src == null || dst == null) continue;
                string key = CleanMaterialName(src.name);
                if (string.IsNullOrEmpty(key)) continue;
                if (!hpSlotMap.ContainsKey(key)) hpSlotMap[key] = dst;
            }

            var hpByName = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var mat in item.materials)
            {
                if (mat == null) continue;
                string key = CleanMaterialName(mat.name);
                if (!string.IsNullOrEmpty(key) && !hpByName.ContainsKey(key))
                    hpByName[key] = mat;
            }

            if (hpSlotMap.Count == 0 && hpByName.Count == 0)
            { Debug.LogWarning($"[{item.name}] No mapping could be built from HP materials."); return false; }

            Material[] newLodMaterials = new Material[lodImporterMats.Length];
            for (int i = 0; i < lodImporterMats.Length; i++)
            {
                Material chosen = null;
                var lodImporterMat = lodImporterMats[i];
                if (lodImporterMat != null)
                {
                    string lodKey = CleanMaterialName(lodImporterMat.name);
                    if (!string.IsNullOrEmpty(lodKey) && hpSlotMap.TryGetValue(lodKey, out var mapped)) chosen = mapped;
                    else if (!string.IsNullOrEmpty(lodKey) && hpByName.TryGetValue(lodKey, out var byName)) chosen = byName;
                }
                if (chosen == null)
                {
                    if (i < item.materials.Length) chosen = item.materials[i];
                    else chosen = item.materials[item.materials.Length - 1];
                }
                newLodMaterials[i] = chosen;
            }

            Undo.RecordObject(item, "Sync LOD Materials");
            item.materials_lod = newLodMaterials;
            EditorUtility.SetDirty(item);

#if OVERRIDE_PREFAB
            if (PrefabUtility.IsPartOfPrefabInstance(item.gameObject))
                PrefabUtility.ApplyPrefabInstance(item.gameObject, InteractionMode.UserAction);
#endif
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error syncing materials for {item.name}: {e.Message}");
            return false;
        }
    }

    private string CleanMaterialName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        string cleaned = name;
        string[] suffixes = { "_mat", "_Mat", "_MAT", "_Material", "_material", " Material", " material" };
        foreach (string suffix in suffixes)
            if (cleaned.EndsWith(suffix)) { cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length); break; }
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\(.*?\)\s*$", "");
        return cleaned.Trim();
    }

    private Renderer FindSourceRendererForMesh(Mesh mesh)
    {
        if (mesh == null) return null;
        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path)) return null;
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in all) { var go = obj as GameObject; if (go != null) { root = go; break; } }
        }
        if (root == null) return null;
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr != null && smr.sharedMesh == mesh) return smr;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf != null && mf.sharedMesh == mesh)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null) return mr;
            }
        return null;
    }

    private Material[] GetImporterMaterialsForMesh(Mesh mesh)
    {
        var renderer = FindSourceRendererForMesh(mesh);
        return renderer != null ? renderer.sharedMaterials : null;
    }
}


//id validation
public class ItemIdAutoAssignPostprocessor : AssetPostprocessor
{
    private static HashSet<string> s_knownPrefabPaths;

    [InitializeOnLoadMethod]
    private static void InitKnownPaths()
    {
        s_knownPrefabPaths = new HashSet<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            s_knownPrefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (s_knownPrefabPaths == null) InitKnownPaths();

        foreach (var p in deletedAssets)
            s_knownPrefabPaths.Remove(p);

        // Renames
        for (int i = 0; i < movedAssets.Length; i++)
        {
            string newPath = movedAssets[i];
            string oldPath = movedFromAssetPaths[i];
            s_knownPrefabPaths.Remove(oldPath);
            s_knownPrefabPaths.Add(newPath);

            if (!newPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

            string oldName = Path.GetFileNameWithoutExtension(oldPath);
            string newName = Path.GetFileNameWithoutExtension(newPath);
            if (oldName == newName) continue;

            string capPath = newPath, capOld = oldName, capNew = newName;
            EditorApplication.delayCall += () => HandleRename(capPath, capOld, capNew);
        }

        // Imports (creations + saves)
        foreach (string path in importedAssets)
        {
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;
            bool isNew = !s_knownPrefabPaths.Contains(path);
            s_knownPrefabPaths.Add(path);

            string fileName = Path.GetFileNameWithoutExtension(path);
            string capPath = path, capName = fileName;
            bool capIsNew = isNew;
            EditorApplication.delayCall += () => HandleImport(capPath, capName, capIsNew);
        }
    }

    private static bool IsVariant(GameObject go) =>
        PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Variant;

    private static bool IsItemIdOverridden(ItemObject item)
    {
        if (item == null) return false;
        var so = new SerializedObject(item);
        var prop = so.FindProperty("item_id");
        return prop != null && prop.prefabOverride;
    }

    private static void HandleImport(string path, string fileName, bool isNew)
    {
        var probe = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (probe == null) return;
        var item = probe.GetComponent<ItemObject>();
        if (item == null) return;
        if (item.item_id == fileName) return;

        bool variant = IsVariant(probe);
        bool shouldUpdate;
        if (variant)
            // New variant with inherited id → give it its own id matching the filename
            shouldUpdate = isNew && !IsItemIdOverridden(item);
        else
            // Regular new prefab with empty id
            shouldUpdate = string.IsNullOrEmpty(item.item_id);

        if (shouldUpdate) ApplyIdChange(path, fileName);
    }

    private static void HandleRename(string path, string oldName, string newName)
    {
        var probe = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (probe == null) return;
        var item = probe.GetComponent<ItemObject>();
        if (item == null) return;
        if (item.item_id == newName) return;

        bool variant = IsVariant(probe);
        bool shouldUpdate;
        if (variant && !IsItemIdOverridden(item))
            // Inherited id on variant: a rename should give it its own filename-based id
            shouldUpdate = true;
        else
            // Regular prefab or overridden variant: only update if id was synced with old name
            shouldUpdate = (item.item_id == oldName);

        if (shouldUpdate) ApplyIdChange(path, newName);
    }

    private static void ApplyIdChange(string path, string newId)
    {
        var instance = PrefabUtility.LoadPrefabContents(path);
        if (instance == null) return;
        try
        {
            var item = instance.GetComponent<ItemObject>();
            if (item == null || item.item_id == newId) return;
            item.item_id = newId;
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }
}
#endif