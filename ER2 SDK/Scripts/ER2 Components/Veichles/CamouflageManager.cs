using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public partial class CamouflageManager : MonoBehaviour
{
    [Range(0f, 10f)]
    [Tooltip("Basic camo scale (measure on camo 1, then all camos will adapt to this scale.")]
    public float camoUvScale = 1;

    [Header("Meshes to apply camos")]
    [Tooltip("All meshes that can receive camo replacements")]
    public Renderer[] targetMeshes;

    [Header("Camo Sets")]
    [Tooltip("Each set represents a complete camo variant with all swappable materials")]
    public CamoSet[] camoSets;

    [HideInInspector]
    [SerializeField]
    public string[] lastAppliedMaterialIds; // Keep track of last applied material IDs to handle removed camo sets

    [System.Serializable]
    public partial class CamoSet
    {
        [Header("Camo Configuration")]
        public CamouflagePreset camoPreset;


        [Range(0f, 10f)]
        [Tooltip("Possibly use the global one.")]
        public float finalRescale = 1;

#if UNITY_EDITOR
        [Header("Camo Texture (applied via PropertyBlock to _MASK_LAYER materials)")]
        public Texture applyCamo;

        [Header("Materials for this set")]
        [Tooltip("All swappable materials for this camo variant. Order must match across all sets!")]
        public Material[] materials;
#endif

        [HideInInspector]
        public string applyCamoId;

        [HideInInspector]
        public string[] materialIds;

        public GameObject[] showMeshes = new GameObject[0];
    }
}