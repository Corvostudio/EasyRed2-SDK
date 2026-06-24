using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RoadSignUvBaker : MonoBehaviour
{
    [Serializable]
    public sealed class SignTarget
    {
        [Tooltip("Renderer della faccia del cartello (quello col materiale dell'atlante).")]
        public Renderer targetRenderer;

        [Tooltip("Cella su cui la UV è autorata in Blender. È il default del prefab, mai mutato a runtime.")]
        public int atlasSlotIndex = 0;
    }

    public RoadSignAtlasDefinition atlasDefinition;

    [Tooltip("Applica all'OnEnable: gestisce il default e il reset delle istanze riusate dal pool.")]
    public bool applyOnEnable = true;

    [Tooltip("L'inspector applica subito quando cambi un valore.")]
    public bool autoApplyInEditor = true;

    public List<SignTarget> targets = new List<SignTarget>();



    // Convenzione standard scale/offset UV. xy = scale, zw = offset. La uso col materiale di default dello shader.
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private static MaterialPropertyBlock s_mpb;

    private void OnEnable()
    {
        if (applyOnEnable)
            ApplyAll();
    }

    // Applica a ogni target il suo slot di default.
    public void ApplyAll()
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Count; i++)
            ApplyTarget(i);
    }

    public bool ApplyTarget(int targetIndex)
    {
        if (targets == null || targetIndex < 0 || targetIndex >= targets.Count)
            return false;
        return ApplySlot(targetIndex, targets[targetIndex].atlasSlotIndex);
    }

    // Sposta la UV dalla cella di default (atlasSlotIndex) alla cella 'slot'.
    // La UV copre già una cella intera -> scale = 1, si trasla solo l'offset.
    public bool ApplySlot(int targetIndex, int slot)
    {
        if (atlasDefinition == null || targets == null || targetIndex < 0 || targetIndex >= targets.Count)
            return false;

        SignTarget target = targets[targetIndex];
        if (target == null || target.targetRenderer == null)
            return false;

        // base = cella su cui la UV è autorata in Blender (fissa). slot = destinazione.
        Rect baseRect = atlasDefinition.GetUvRect(0);
        Rect dstRect = atlasDefinition.GetUvRect(slot);
        Vector4 st = new Vector4(1f, 1f, dstRect.xMin - baseRect.xMin, dstRect.yMin - baseRect.yMin);

        if (s_mpb == null) s_mpb = new MaterialPropertyBlock();
        target.targetRenderer.GetPropertyBlock(s_mpb);
        s_mpb.SetVector(BaseMapST, st);
        target.targetRenderer.SetPropertyBlock(s_mpb);
        return true;
    }

    // Reset alla UV originale (cella di default).
    public void ClearAll()
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Count; i++)
        {
            SignTarget target = targets[i];
            if (target == null || target.targetRenderer == null) continue;
            if (s_mpb == null) s_mpb = new MaterialPropertyBlock();
            target.targetRenderer.GetPropertyBlock(s_mpb);
            s_mpb.SetVector(BaseMapST, new Vector4(1f, 1f, 0f, 0f));
            target.targetRenderer.SetPropertyBlock(s_mpb);
        }
    }
}