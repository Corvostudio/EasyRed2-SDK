using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class MacroColorTextureAssigner : MonoBehaviour
{

    [Serializable]
    public class Group
    {
        [Range(0f, 5f)] public float macroColorIntensity = 1.0f;

        [Tooltip("Texture to apply to _MaskLayerMask and _MaskLayerTexture (via MaterialPropertyBlock).")]
        public Texture texture;

        [Tooltip("MeshRenderers that should receive the texture.")]
        public MeshRenderer[] meshRenderers;
    }

    [SerializeField] private Group[] groups = new Group[0];
    public int CountGroups { get { return groups!=null? groups.Length : 0; } }

    // Shader property names
    private static readonly int MaskLayerMaskId = Shader.PropertyToID("_MaskLayerMask");
    private static readonly int MaskLayerTextureId = Shader.PropertyToID("_MaskLayerTexture");
    private static readonly int MaskLayerIntensityId = Shader.PropertyToID("_MaskLayerBrightness");

    // Keywords used to decide whether a material supports mask layer
    private static readonly string KW_MASK_LAYER_ON = "_MASK_LAYER_ON";
    private static readonly string KW_MASK_LAYER_G_CHANNEL = "_MASK_LAYER_G_CHANNEL";
    private static readonly string KW_MASK_LAYER_UV2 = "_MASK_LAYER_UV2";

    private void Awake()
    {
        ApplyAll();
    }

    /// <summary>
    /// Applies all non-null group textures to all listed renderers using MaterialPropertyBlock.
    /// Each material index is handled independently (per-submesh).
    /// </summary>
    public void ApplyAll()
    {
        if (groups == null || groups.Length == 0)
            return;

        MaterialPropertyBlock SharedBlock = new MaterialPropertyBlock();

        // If the same renderer appears in multiple groups, the last applied group wins for a given material index.
        for (int g = 0; g < groups.Length; g++)
        {
            var group = groups[g];
            if (group == null)
                continue;

            var intensity = group.macroColorIntensity;
            var tex = group.texture;
            var rens = group.meshRenderers;
            if (tex == null || rens == null || rens.Length == 0)
                continue;

            for (int r = 0; r < rens.Length; r++)
            {
                var mr = rens[r];
                if (!mr)
                    continue;

                // Using sharedMaterials for scanning; applying via SetPropertyBlock(materialIndex)
                var mats = mr.sharedMaterials;
                if (mats == null || mats.Length == 0)
                    continue;

                for (int mi = 0; mi < mats.Length; mi++)
                {
                    var mat = mats[mi];
                    if (!mat)
                        continue;

                    if (!MaterialUsesMaskLayer(mat))
                        continue;

                    mr.GetPropertyBlock(SharedBlock, mi);
                    SharedBlock.SetTexture(MaskLayerMaskId, tex);
                    SharedBlock.SetTexture(MaskLayerTextureId, tex);
                    SharedBlock.SetFloat(MaskLayerIntensityId, intensity);
                    mr.SetPropertyBlock(SharedBlock, mi);
                }
            }
        }
    }

    public void ClearAll()
    {
        if (groups == null || groups.Length == 0)
            return;

        for (int g = 0; g < groups.Length; g++)
        {
            var group = groups[g];
            if (group == null || group.meshRenderers == null)
                continue;

            for (int r = 0; r < group.meshRenderers.Length; r++)
            {
                var mr = group.meshRenderers[r];
                if (!mr)
                    continue;

                var mats = mr.sharedMaterials;
                if (mats == null || mats.Length == 0)
                    continue;

                for (int mi = 0; mi < mats.Length; mi++)
                {
                    var mat = mats[mi];
                    if (!mat)
                        continue;

                    if (!MaterialUsesMaskLayer(mat))
                        continue;

                    // Clearing the block removes all overrides for that submesh
                    mr.SetPropertyBlock(null, mi);
                }
            }
        }
    }


    private static bool MaterialUsesMaskLayer(Material mat)
    {
        return mat.IsKeywordEnabled(KW_MASK_LAYER_ON)
               || mat.IsKeywordEnabled(KW_MASK_LAYER_G_CHANNEL)
               || mat.IsKeywordEnabled(KW_MASK_LAYER_UV2);
    }

    // ---- Editor support (called by custom inspector) ----
#if UNITY_EDITOR
    /// <summary>
    /// Adds a new Group (texture null) and auto-fills its MeshRenderers with any child MeshRenderer
    /// that has at least one material using mask layer keywords.
    /// </summary>
    public void EditorQuickSetup_AddGroupFromChildren()
    {
        var found = new List<MeshRenderer>();
        GetComponentsInChildren(true, found);

        var eligible = new List<MeshRenderer>(found.Count);
        for (int i = 0; i < found.Count; i++)
        {
            var mr = found[i];
            if (!mr)
                continue;

            var mats = mr.sharedMaterials;
            if (mats == null || mats.Length == 0)
                continue;

            bool ok = false;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (!mat) continue;
                if (MaterialUsesMaskLayer(mat))
                {
                    ok = true;
                    break;
                }
            }

            if (ok)
                eligible.Add(mr);
        }

        Array.Resize(ref groups, (groups?.Length ?? 0) + 1);
        groups[groups.Length - 1] = new Group
        {
            texture = null,
            meshRenderers = eligible.ToArray()
        };
    }

    public bool EditorHasAnyValidSubsetForTest()
    {
        if (groups == null) return false;
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g == null) continue;
            if (g.texture == null) continue;
            if (g.meshRenderers == null || g.meshRenderers.Length == 0) continue;
            return true;
        }
        return false;
    }
#endif
}

#if UNITY_EDITOR

[CustomEditor(typeof(MacroColorTextureAssigner))]
[CanEditMultipleObjects]
public class MaskLayerTextureAssignerEditor : Editor
{
    private SerializedProperty _groupsProp;

    private void OnEnable()
    {
        _groupsProp = serializedObject.FindProperty("groups");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Default array UI
        EditorGUILayout.PropertyField(_groupsProp, includeChildren: true);

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Quickly Setup"))
            {
                // Multi-edit support
                foreach (var obj in targets)
                {
                    var comp = (MacroColorTextureAssigner)obj;

                    if (comp!=null && comp.CountGroups == 0)
                    {
                        Undo.RecordObject(comp, "Quickly Setup Mask Layer Groups");
                        comp.EditorQuickSetup_AddGroupFromChildren();
                        EditorUtility.SetDirty(comp);
                    }
                }

                // Ensure the serialized view refreshes
                serializedObject.Update();
            }

            bool showTest = false;
            foreach (var obj in targets)
            {
                var comp = (MacroColorTextureAssigner)obj;
                if (comp && comp.EditorHasAnyValidSubsetForTest())
                {
                    showTest = true;
                    break;
                }
            }

            using (new EditorGUI.DisabledScope(!showTest))
            {
                if (GUILayout.Button("Test Applied Texture"))
                {
                    foreach (var obj in targets)
                    {
                        var comp = (MacroColorTextureAssigner)obj;
                        if (!comp) continue;

                        // Apply in editor immediately
                        comp.ApplyAll();
                    }

                    // Repaint to reflect any inspector changes
                    Repaint();
                }

                if (GUILayout.Button("Unassign Applied Texture"))
                {
                    foreach (var obj in targets)
                    {
                        var comp = (MacroColorTextureAssigner)obj;
                        if (!comp) continue;

                        // Apply in editor immediately
                        comp.ClearAll();
                    }

                    // Repaint to reflect any inspector changes
                    Repaint();
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
