using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class ItemHelmet : ItemObject, ImpactSpecifier
{
    [Tooltip("Protection given by this headgear. Only Penetration Protection changes the damage taken.")]
    public ProtectionData protectionData;
    [Tooltip("Camouflage: 0 = none, 35 = max. Reduces detection range.")]
    [Range(0, 35)]
    public int camoIndex = 15;
    [Tooltip("Material of the headgear: sets the effect and sound of a hit on it.")]
    public ImpactType headgearImpactFX = ImpactType.metal;
}