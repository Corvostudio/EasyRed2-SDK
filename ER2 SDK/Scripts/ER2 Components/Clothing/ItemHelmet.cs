using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class ItemHelmet : ItemObject, ImpactSpecifier
{
    [Tooltip("Component that defines protection data when wearing item")]
    public ProtectionData protectionData;
    [Tooltip("Camuflage value of the clothing. Ranges from 0 (minimum) to 35 (maximum). NOTE: the overall camouflage of a soldier is recieved by summing this value with each piece of clothing/gear in use")]
    [Range(0, 35)]
    public int camoIndex = 15;
    [Tooltip("Defines the impact fx type to play")]
    public ImpactType headgearImpactFX = ImpactType.metal;
}