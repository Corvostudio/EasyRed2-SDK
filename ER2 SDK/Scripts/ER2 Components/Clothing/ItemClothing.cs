using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#endif

public partial class ItemClothing : ItemObject
{
    public WearableType type;
    [Tooltip("(ONLY IF TYPE IS UNIFORM) longSleeve: Use short hands model. shortSLeeve: Use long hands model. coversSleeveAndHand: Hide completly hands model")]
    public UniformSleeveCoverType forceHideHands = UniformSleeveCoverType.longSleeve;
    [Tooltip("(ONLY IF TYPE IS UNIFORM) dontCoverHead: shows head and headgear when wearing. coversHead: Hide head but shows headgear. coversHeadAndHeadgear: hide head and headgear.")]
    public UniformHeadCoverType forceHideHead = UniformHeadCoverType.dontCoverHead;

    [Tooltip("(ONLY IF TYPE IS UNIFORM) coordinates of the ranks on the uniform. (0,0,0) if shouldn't appear.")]
    public Vector3 ranks_coords = new Vector3(.5f, 1.21f, .75f);
    [Tooltip("(ONLY IF TYPE IS UNIFORM) coordinates of the batalion patch on the uniform. (0,0,0) if shouldn't appear.")]
    public Vector3 patch_coords = new Vector3(.5f, 0.2f, .8f);

    // TPS long sleeve
    public Material material;
    public Material[] materials = new Material[0];
    public Material[] materials_lod = new Material[0];
    public Mesh mesh;
    public Mesh mesh_lod;

    // TPS short sleeve
    public Material[] materials_short = new Material[0];
    public Material[] materials_short_lod = new Material[0];
    public Mesh mesh_short_sleeve;
    public Mesh mesh_short_sleeve_lod;
    public UniformSleeveCoverType handsModeWithShortSleeveUniform = UniformSleeveCoverType.shortSleeve;

    //FPS
    public Material[] materials_fps = new Material[0];
    public Material[] materials_short_fps = new Material[0];
    public Mesh fps_mesh;
    public Mesh fps_mesh_short_sleeve;

    public ProtectionData protectionData;
    [Range(0, 35)] public int camoIndex = 15;
    public float CamoPercReduction { get { return camoIndex / 35f; } }

    [Range(0, 30)] public int extraInventorySpace = 0;

    public bool isRadio = false;
    public bool isFuelTank = false;

    [Range(0, 50)]
    public float shortSleeveThreshold = 23;


    partial void CheckShouldUseShortSleeve(ref bool result);

    public bool ShouldUseShortSleeve(SleeveMode sleeveMode = SleeveMode.auto)
    {
        if (sleeveMode == SleeveMode.rolled)
            return true;
        if (sleeveMode == SleeveMode.unrolled)
            return false;

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
            return new Material[] { material };
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
        if (clothing == null)
            return true;

        bool ok = true;
        string tag = string.IsNullOrEmpty(itemIdForLog) ? clothing.name : itemIdForLog;

        // Validate TPS meshes (requested: mesh, mesh_lod, mesh_short_sleeve, mesh_short_sleeve_lod)
        ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh, validationMode, clothing.GetTpsMaterials(SleeveMode.unrolled), "mesh/HP TPS");
        ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh_lod, validationMode, clothing.GetTpsMaterialsLOD(SleeveMode.unrolled), "mesh_lod TPS");

        if (clothing.mesh_short_sleeve != null)
            ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh_short_sleeve, validationMode, clothing.GetTpsMaterials(SleeveMode.rolled), "mesh_short_sleeve TPS");

        if (clothing.mesh_short_sleeve_lod != null)
            ok &= ValidateMeshAndMaterials(tag, clothing, clothing.mesh_short_sleeve_lod, validationMode, clothing.GetTpsMaterialsLOD(SleeveMode.rolled), "mesh_short_sleeve_lod TPS");

        // Optional: FPS validation (safe to keep; remove if not needed)
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
            // Mesh can be legitimately null depending on content; treat as error only for the “main” mesh.
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
            return true; // don’t hard-fail if Unity can’t resolve importer info
        }

        int actual = chosenMaterials != null ? chosenMaterials.Length : 0;

        // Handle the single-material fallback field "material"
        // If no arrays are set, chosenMaterials might be 1 via GetTpsMaterials returning new[] { material }.
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
                    $"• materials_short_lod : {Len(clothing.materials_short_lod)}\n" +
                    $"• single material     : {(clothing.material ? clothing.material.name : "<color=#888888>null</color>")}",
                    clothing
                );
            }

            if (validationMode == ValidateMode.Full)
                return false;
            if (lowValidationMode)
                return false;
        }

        // Also catch null entries
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
        if (mesh == null)
            return null;

        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path))
            return null;

        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in all)
            {
                if (obj is GameObject go)
                {
                    root = go;
                    break;
                }
            }
        }

        if (root == null)
            return null;

        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr != null && smr.sharedMesh == mesh)
                return smr;
        }

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf != null && mf.sharedMesh == mesh)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null)
                    return mr;
            }
        }

        return null;
    }
#endif
}

//ProtectionData: I dani sono ridotti di un valore randomico che sta tra 0 e protectionValue
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
    public byte sliverProtection;//scheggie
}

public enum SleeveMode
{
    auto,
    rolled,
    unrolled
}

public enum WearableType
{
    uniform,
    gear
}
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

        // Fast Import header
        /*if (Selection.objects.Length == 1)
        {
            GUILayout.Label(" --- Fast Import ---");
            // Record undo before making any changes
            Undo.RecordObject(clothing, "Fast Import HP Mesh");

            // High poly import
            SkinnedMeshRenderer hp_import = EditorGUILayout.ObjectField(
                "Import HP Mesh & Materials",
                null,
                typeof(SkinnedMeshRenderer),
                true
            ) as SkinnedMeshRenderer;

            if (hp_import)
            {
                clothing.mesh = hp_import.sharedMesh;
                clothing.materials = hp_import.sharedMaterials;
                clothing.material = null;
                // Mark dirty so change sticks
                EditorUtility.SetDirty(clothing);

#if OVERRIDE_PREFAB
                // If this is a prefab instance, apply the override
                if (PrefabUtility.IsPartOfPrefabInstance(clothing.gameObject))
                {
                    PrefabUtility.ApplyPrefabInstance(
                        clothing.gameObject,
                        InteractionMode.UserAction
                    );
                }
#endif
            }

            // Record undo for LOD
            Undo.RecordObject(clothing, "Fast Import LOD Mesh");
            // Low poly import
            SkinnedMeshRenderer lod_import = EditorGUILayout.ObjectField(
                "Import LOD Mesh & Materials",
                null,
                typeof(SkinnedMeshRenderer),
                true
            ) as SkinnedMeshRenderer;

            if (lod_import)
            {
                clothing.mesh_lod = lod_import.sharedMesh;
                clothing.materials_lod = lod_import.sharedMaterials;
                // Mark dirty so change sticks
                EditorUtility.SetDirty(clothing);

#if OVERRIDE_PREFAB
                if (PrefabUtility.IsPartOfPrefabInstance(clothing.gameObject))
                {
                    PrefabUtility.ApplyPrefabInstance(
                        clothing.gameObject,
                        InteractionMode.UserAction
                    );
                }
#endif
            }

            GUILayout.Space(10);
        }

        // Material Sync Section - works with multi-selection
        GUILayout.Label(" --- Material Sync ---");

        EditorGUILayout.HelpBox(
            "Sync materials from High Poly to LOD using the original model importer's material arrangement.",
            MessageType.Info
        );

        GUI.enabled = CanSyncMaterials();

        if (GUILayout.Button("Sync HP Materials to LOD"))
        {
            foreach (var o in targets)
            {
                var clt = o as ItemClothing;
                if (clt == null)
                    continue;

                Undo.RecordObject(clt, "Sync Materials To LOD");

                SyncMaterialsToLOD(clt, this);

                EditorUtility.SetDirty(clt);

#if OVERRIDE_PREFAB
                if (PrefabUtility.IsPartOfPrefabInstance(clt.gameObject))
                {
                    PrefabUtility.ApplyPrefabInstance(
                        clt.gameObject,
                        InteractionMode.UserAction
                    );
                }
#endif
            }
        }*/


        if (GUILayout.Button("VALIDATE"))
        {
            foreach (var obj in Selection.gameObjects)
            {
                ItemClothing clt = obj.GetComponent<ItemClothing>();
                if (clt == null) continue;

                if (ItemClothing.ValidateSetup(clothing, ItemClothing.ValidateMode.Full, clt.item_id))
                    Debug.Log($"<b>{clt.item_id}</b> seems setted up correctly.");

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(clt.gameObject);
                if (prefab && !EditorUtility.IsPersistent(clt.gameObject))
                {
                    if (prefab.name != clt.item_id)
                        Debug.LogError($"[ItemClothing Validate] Prefab name '{prefab.name}' does not match item_id '{clt.item_id}'.", clothing);
                }
                else
                {
                    if (clt.gameObject.name != clt.item_id)
                        Debug.LogError($"[ItemClothing Validate] Prefab name '{prefab.name}' does not match item_id '{clt.item_id}'.", clothing);
                }
            }
        }

        GUI.enabled = true;

        // Show status for single selection
        if (Selection.objects.Length == 1)
        {
            string status = GetSyncStatus(clothing);
            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.HelpBox(status, MessageType.Warning);
            }
        }

        GUILayout.Space(10);
        //GUILayout.Label(" --- Default ---");

        DrawDefaultInspector();
    }

    private bool CanSyncMaterials()
    {
        foreach (Object obj in targets)
        {
            ItemClothing item = obj as ItemClothing;
            if (item != null && item.mesh != null && item.mesh_lod != null &&
                item.materials != null && item.materials.Length > 0)
            {
                return true;
            }
        }
        return false;
    }

    private string GetSyncStatus(ItemClothing clothing)
    {
        if (clothing.mesh == null)
            return "High Poly mesh is missing";
        if (clothing.mesh_lod == null)
            return "LOD mesh is missing";
        if (clothing.materials == null || clothing.materials.Length == 0)
            return "High Poly materials are missing";
        return null;
    }


    private static void SyncMaterialsToLOD(ItemClothing clothing, ItemClothingEditor ice)
    {
        if (clothing.mesh == null)
        {
            Debug.LogWarning($"[{clothing.name}] No HP mesh assigned, can't sync.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(clothing.mesh);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[{clothing.name}] Could not find FBX path for HP mesh.");
            return;
        }

        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[{clothing.name}] Asset at {path} is not a ModelImporter.");
            return;
        }

        WithTemporaryEmbeddedMaterials(importer, path, () =>
        {
            int successCount = 0;
            int failCount = 0;
            List<string> errors = new List<string>();

            foreach (Object obj in ice.targets)
            {
                ItemClothing item = obj as ItemClothing;
                if (item == null) continue;

                // Skip if required data is missing
                if (item.mesh == null || item.mesh_lod == null ||
                    item.materials == null || item.materials.Length == 0)
                {
                    failCount++;
                    errors.Add($"{item.name}: Missing required meshes or materials");
                    continue;
                }

                // Try to sync materials for this item
                if (ice.SyncMaterialsForItem(item))
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    errors.Add($"{item.name}: Failed to sync materials");
                }
            }

            // Show results
            string message = $"Material Sync Complete\nSuccess: {successCount}, Failed: {failCount}";
            if (errors.Count > 0)
            {
                message += "\n\nErrors:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                    message += $"\n... and {errors.Count - 5} more";
            }

            Debug.Log( message, ice);


            FixLodMaterials(clothing);
        });
    }
    /// <summary>
    /// Temporarily sets the importer to create embedded materials
    /// if it was previously set to None. Restores original mode afterwards.
    /// </summary>
    private static void WithTemporaryEmbeddedMaterials(
        ModelImporter importer,
        string assetPath,
        System.Action body)
    {
        var originalMode = importer.materialImportMode;
        var originalLocation = importer.materialLocation;

        bool changed = false;

        if (originalMode == ModelImporterMaterialImportMode.None)
        {
            // Switch to "Standard" creation mode + embedded materials
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab; // "Use Embedded Materials"

            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            changed = true;
        }

        try
        {
            body?.Invoke();
        }
        finally
        {
            if (changed)
            {
                // Restore original settings (None + previous location)
                importer.materialImportMode = originalMode;
                importer.materialLocation = originalLocation;

                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    // Example FixLodMaterials from previous message, or your own implementation
    private static void FixLodMaterials(ItemClothing clothing)
    {
        // Your existing name-based matching / assignment here…
        // (or leave empty if you just need the temporary material import behaviour)
    }

    private bool SyncMaterialsForItem(ItemClothing item)
    {
        try
        {
            if (item.mesh == null || item.mesh_lod == null ||
                item.materials == null || item.materials.Length == 0)
            {
                Debug.LogWarning($"[{item.name}] Missing mesh or HP materials, skipping.");
                return false;
            }

            // DEBUG: print what Unity found
            //DebugPrintImporterMaterials(item);

            // 1) Get importer materials (expected slots) for HP and LOD
            Material[] hpImporterMats = GetImporterMaterialsForMesh(item.mesh);
            Material[] lodImporterMats = GetImporterMaterialsForMesh(item.mesh_lod);

            if (hpImporterMats == null || hpImporterMats.Length == 0 ||
                lodImporterMats == null || lodImporterMats.Length == 0)
            {
                Debug.LogWarning($"[{item.name}] Could not find importer materials for HP or LOD mesh.");
                return false;
            }

            // 2) Build mapping: importer HP slot name -> user HP material in ItemClothing.materials
            // We assume user kept order aligned to importer, at least roughly.
            var hpSlotMap = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);

            int count = Mathf.Min(hpImporterMats.Length, item.materials.Length);
            for (int i = 0; i < count; i++)
            {
                var src = hpImporterMats[i];
                var dst = item.materials[i];
                if (src == null || dst == null)
                    continue;

                string key = CleanMaterialName(src.name);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (!hpSlotMap.ContainsKey(key))
                    hpSlotMap[key] = dst;
            }

            // 3) Also build a plain name map from user's HP materials (in case importer names differ)
            var hpByName = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var mat in item.materials)
            {
                if (mat == null) continue;
                string key = CleanMaterialName(mat.name);
                if (!string.IsNullOrEmpty(key) && !hpByName.ContainsKey(key))
                    hpByName[key] = mat;
            }

            if (hpSlotMap.Count == 0 && hpByName.Count == 0)
            {
                Debug.LogWarning($"[{item.name}] No mapping could be built from HP materials.");
                return false;
            }

            // 4) For each LOD importer slot, pick the right HP material
            Material[] newLodMaterials = new Material[lodImporterMats.Length];

            for (int i = 0; i < lodImporterMats.Length; i++)
            {
                Material chosen = null;
                var lodImporterMat = lodImporterMats[i];

                if (lodImporterMat != null)
                {
                    string lodKey = CleanMaterialName(lodImporterMat.name);

                    // 4.a) First try exact slot mapping via importer name
                    if (!string.IsNullOrEmpty(lodKey) && hpSlotMap.TryGetValue(lodKey, out var mapped))
                    {
                        chosen = mapped;
                    }
                    // 4.b) Then try name matching directly against user's HP material names
                    else if (!string.IsNullOrEmpty(lodKey) && hpByName.TryGetValue(lodKey, out var byName))
                    {
                        chosen = byName;
                    }
                }

                // 4.c) Fallbacks: same index if possible, else clamp to last non-null
                if (chosen == null)
                {
                    if (i < item.materials.Length)
                        chosen = item.materials[i];
                    else
                        chosen = item.materials[item.materials.Length - 1];
                }

                newLodMaterials[i] = chosen;
            }

            // 5) Apply to ItemClothing
            Undo.RecordObject(item, "Sync LOD Materials");
            item.materials_lod = newLodMaterials;
            EditorUtility.SetDirty(item);

#if OVERRIDE_PREFAB
            if (PrefabUtility.IsPartOfPrefabInstance(item.gameObject))
            {
                PrefabUtility.ApplyPrefabInstance(item.gameObject, InteractionMode.UserAction);
            }
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
        if (string.IsNullOrEmpty(name))
            return name;

        // Remove common material suffixes
        string cleaned = name;
        string[] suffixes = { "_mat", "_Mat", "_MAT", "_Material", "_material", " Material", " material" };

        foreach (string suffix in suffixes)
        {
            if (cleaned.EndsWith(suffix))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length);
                break;
            }
        }

        // Remove instance suffixes like "(Instance)" or "(Clone)"
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\(.*?\)\s*$", "");

        return cleaned.Trim();
    }
    private Renderer FindSourceRendererForMesh(Mesh mesh)
    {
        if (mesh == null)
            return null;

        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path))
            return null;

        // Try load the root GameObject (FBX prefab)
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            // Fallback: some pipelines give you the mesh as a .asset with no root
            // Try to find any GameObject subasset at that path.
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in all)
            {
                var go = obj as GameObject;
                if (go != null)
                {
                    root = go;
                    break;
                }
            }
        }

        if (root == null)
            return null;

        // 1) SkinnedMeshRenderer
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr != null && smr.sharedMesh == mesh)
                return smr;
        }

        // 2) MeshFilter + MeshRenderer
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf != null && mf.sharedMesh == mesh)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null)
                    return mr;
            }
        }

        return null;
    }

    private Material[] GetImporterMaterialsForMesh(Mesh mesh)
    {
        var renderer = FindSourceRendererForMesh(mesh);
        return renderer != null ? renderer.sharedMaterials : null;
    }

    private void DebugPrintImporterMaterials(ItemClothing item)
    {
        if (item == null)
            return;

        var hpMats = GetImporterMaterialsForMesh(item.mesh);
        var lodMats = GetImporterMaterialsForMesh(item.mesh_lod);

        string hpPath = item.mesh ? AssetDatabase.GetAssetPath(item.mesh) : "<null mesh>";
        string lodPath = item.mesh_lod ? AssetDatabase.GetAssetPath(item.mesh_lod) : "<null mesh_lod>";

        Debug.Log($"[ItemClothing Debug] {item.name} HP mesh path: {hpPath}");
        if (hpMats == null)
        {
            Debug.Log($"[ItemClothing Debug] {item.name} HP importer materials: <null / not found>");
        }
        else
        {
            for (int i = 0; i < hpMats.Length; i++)
            {
                var m = hpMats[i];
                Debug.Log($"[ItemClothing Debug] {item.name} HP slot {i}: {(m ? m.name : "<null>")}");
            }
        }

        Debug.Log($"[ItemClothing Debug] {item.name} LOD mesh path: {lodPath}");
        if (lodMats == null)
        {
            Debug.Log($"[ItemClothing Debug] {item.name} LOD importer materials: <null / not found>");
        }
        else
        {
            for (int i = 0; i < lodMats.Length; i++)
            {
                var m = lodMats[i];
                Debug.Log($"[ItemClothing Debug] {item.name} LOD slot {i}: {(m ? m.name : "<null>")}");
            }
        }
    }


}
#endif